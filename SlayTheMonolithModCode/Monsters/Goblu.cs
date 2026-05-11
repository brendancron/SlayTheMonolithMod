using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

// Elite for The Continent. Fixed 3-turn cycle: Summon → Pounce (4x3) → Fortify.
public sealed class Goblu : CustomMonsterModel, ILocalizationProvider
{
    private const string SummonMoveId = "SUMMON_MOVE";
    private const string PounceMoveId = "POUNCE_MOVE";

    public override int MinInitialHp => 120;
    public override int MaxInitialHp => 120;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/goblu.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Magic;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Goblu",
        MoveTitles: new[]
        {
            (SummonMoveId, "Summon"),
            (PounceMoveId, "Pounce"),
        });

    private int PounceDamage => 5;
    private int PounceHits => 3;
    private int SummonStrength => 1;
    private int MaxFlowers => 4;

    // True when the encounter already holds the max number of live Flower
    // minions, in which case the Summon slot in the cycle is skipped in favor
    // of Pounce so Goblu doesn't telegraph a no-op turn.
    private bool AtFlowerCap =>
        base.Creature.CombatState.Enemies.Count(c => c.IsAlive && c.Monster is Flower) >= MaxFlowers;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var summon = new MoveState(SummonMoveId, SummonMove, new SummonIntent(), new BuffIntent());
        var pounce = new MoveState(PounceMoveId, PounceMove, new MultiAttackIntent(PounceDamage, PounceHits));

        // Conditional slot that picks Summon when there's room and Pounce when
        // the board is full. The chosen state's own FollowUpState carries the
        // cycle forward (pounce → summonSlot → pounce, or pounce → pounce when
        // at cap so Goblu doesn't telegraph a no-op summon).
        var summonSlot = new ConditionalBranchState("SUMMON_OR_POUNCE");
        summonSlot.AddState(summon, () => !AtFlowerCap);
        summonSlot.AddState(pounce, () => true);

        pounce.FollowUpState = summonSlot;
        summon.FollowUpState = pounce;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { summon, pounce, summonSlot },
            pounce);
    }

    private async Task SummonMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.5f);
        if (!base.CombatState.IsLiveCombat()) return;
        string? slot = base.CombatState.Encounter?.GetNextSlot(base.CombatState);
        if (string.IsNullOrEmpty(slot)) return;
        Creature flower = await CreatureCmd.Add<Flower>(base.CombatState, slot);
        await PowerCmd.Apply<MinionPower>(new ThrowingPlayerChoiceContext(), flower, 1m, base.Creature, null);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, SummonStrength, base.Creature, null);
    }

    private async Task PounceMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(PounceDamage)
            .FromMonster(this)
            .WithHitCount(PounceHits)
            .WithAttackerAnim("Attack", 0.15f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }
}
