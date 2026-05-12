using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using SlayTheMonolithMod.SlayTheMonolithModCode.Intents;
using SlayTheMonolithMod.SlayTheMonolithModCode.Powers;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

// Boss for The Continent. Two-move cycle gated by a kill-order puzzle:
//   - Ritual (T1): deal 12 damage to the player, despawn any leftover Lamps
//     from a prior ritual, then summon 4 fresh Lamps with shuffled
//     IgnitionOrder powers (values 1-4).
//   - Sword of Light (T2): 35 damage.
//   - Player has T1's player-turn to kill all 4 Lamps in 1->4 order. Each
//     lamp grants 1 energy on kill. If the player nails the order, the
//     IgnitionOrder.AfterDeath callback fires OnLampKilled, which calls
//     CreatureCmd.Stun on Lampmaster -- overriding the rolled Sword of Light
//     intent live and showing the player the cancellation immediately.
//   - Wrong order or incomplete: Sword of Light hits as rolled.
//
// Bestiary hides STUN_MOVE since it's only reachable via the success branch.
public sealed class Lampmaster : CustomMonsterModel, ILocalizationProvider
{
    private const string RitualMoveId = "RITUAL_MOVE";
    private const string SwordMoveId = "SWORD_MOVE";
    private const string DarkExplosionMoveId = "DARK_EXPLOSION_MOVE";

    public override int MinInitialHp => 200;
    public override int MaxInitialHp => 200;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/lampmaster.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Magic;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Lampmaster",
        MoveTitles: new[]
        {
            (RitualMoveId, "A Strange Ritual"),
            (SwordMoveId, "Sword of Light"),
            (DarkExplosionMoveId, "Dark Explosion"),
        });

    private int RitualDamage => 3;
    private int RitualHits => 4;
    private int SwordDamage => 35;
    private int DarkExplosionDamage => 16;
    private int DarkExplosionDebuffStacks => 2;
    private int LampCount => 4;

    // Puzzle progress trackers. Reset each time RitualMove fires. Mutable via
    // public setters so AssertMutable allows updates on the encounter's
    // working clone (same pattern as Sakapatate.FireCount).
    public int _expectedNext = 1;
    public bool _failed;

    public int ExpectedNext
    {
        get => _expectedNext;
        set { AssertMutable(); _expectedNext = value; }
    }

    public bool Failed
    {
        get => _failed;
        set { AssertMutable(); _failed = value; }
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var ritual = new MoveState(RitualMoveId, RitualMove, new MultiAttackIntent(RitualDamage, RitualHits), new SummonIntent());
        var sword = new MoveState(SwordMoveId, SwordMove, new SingleAttackIntent(SwordDamage));
        var darkExplosion = new MoveState(DarkExplosionMoveId, DarkExplosionMove, new SingleAttackIntent(DarkExplosionDamage), new ConditionalDebuffIntent());

        ritual.FollowUpState = sword;
        sword.FollowUpState = darkExplosion;
        darkExplosion.FollowUpState = ritual;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { ritual, sword, darkExplosion },
            ritual);
    }

    private async Task RitualMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.5f);

        // Damage component of the ritual.
        await DamageCmd.Attack(RitualDamage)
            .WithHitCount(RitualHits)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        if (!base.CombatState.IsLiveCombat()) return;

        // Reset puzzle trackers. Surviving lamps from a prior cycle are
        // already cleared by SwordMove's prologue (the only path that leads
        // back to Ritual without all four lamps already dead).
        ExpectedNext = 1;
        Failed = false;

        // Summon LampCount fresh lamps, each with a shuffled IgnitionOrder.
        var orders = new List<int>();
        for (int i = 1; i <= LampCount; i++) orders.Add(i);
        // Fisher-Yates shuffle using the monster AI Rng so seed-based runs are deterministic.
        var rng = base.RunRng.MonsterAi;
        for (int i = orders.Count - 1; i > 0; i--)
        {
            int j = rng.NextInt(0, i + 1);
            (orders[i], orders[j]) = (orders[j], orders[i]);
        }

        for (int i = 0; i < LampCount; i++)
        {
            string? slot = base.CombatState.Encounter?.GetNextSlot(base.CombatState);
            if (string.IsNullOrEmpty(slot)) break;
            Creature lamp = await CreatureCmd.Add<Lamp>(base.CombatState, slot);
            await PowerCmd.Apply<MinionPower>(new ThrowingPlayerChoiceContext(), lamp, 1m, base.Creature, null);
            await PowerCmd.Apply<IgnitionOrder>(new ThrowingPlayerChoiceContext(), lamp, orders[i], base.Creature, null);
        }

    }

    // Conditional attack pattern (cribbed from PaperCutsPower.AfterDamageGiven):
    // hit for 16, then if any unblocked damage landed, stack 2 of each of the
    // three "fragility" debuffs on the player. AttackCommand.Execute returns
    // the awaited builder; its Results collection is one List<DamageResult>
    // per intended target, so we flatten and check UnblockedDamage > 0.
    private async Task DarkExplosionMove(IReadOnlyList<Creature> targets)
    {
        var attack = await DamageCmd.Attack(DarkExplosionDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        bool dealtUnblocked = attack.Results
            .SelectMany(r => r)
            .Any(r => r.UnblockedDamage > 0);
        if (!dealtUnblocked) return;

        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets, DarkExplosionDebuffStacks, base.Creature, null);
        await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), targets, DarkExplosionDebuffStacks, base.Creature, null);
        await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(), targets, DarkExplosionDebuffStacks, base.Creature, null);
    }

    private async Task SwordMove(IReadOnlyList<Creature> targets)
    {
        // Despawn any lamps still standing right before the swing. Escape
        // removes without triggering AfterDeath, so no stray energy grants
        // and no OnLampKilled callbacks fire.
        var survivors = base.CombatState.Enemies
            .Where(c => c.IsAlive && c.Monster is Lamp)
            .ToList();
        foreach (var lamp in survivors)
        {
            await CreatureCmd.Escape(lamp);
        }

        await DamageCmd.Attack(SwordDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.4f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    // Called by IgnitionOrder.AfterDeath whenever a lamp owned by this
    // Lampmaster dies from damage. If the kill matches the expected next
    // order, advance the counter. When all 4 are killed in order, force the
    // Lampmaster to Stun for its next turn -- this overrides the already-
    // rolled Sword of Light intent and the player sees the cancellation live.
    // CreatureCmd.Stun queues a synthetic "STUNNED" move whose follow-up is
    // the moveId we pass. Pointing at Dark Explosion means solving the puzzle
    // skips only the 35-dmg Sword (the real reward) and the cycle continues
    // through Dark Explosion -> Ritual. Pointing at Ritual instead would
    // skip both Sword and Dark Explosion, making the puzzle dominant.
    public async Task OnLampKilled(int order)
    {
        if (Failed) return;
        if (order != ExpectedNext)
        {
            Failed = true;
            return;
        }
        ExpectedNext++;
        if (ExpectedNext > LampCount)
        {
            await CreatureCmd.Stun(base.Creature, DarkExplosionMoveId);
        }
    }
}
