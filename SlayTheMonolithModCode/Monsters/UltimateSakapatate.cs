using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using SlayTheMonolithMod.SlayTheMonolithModCode.Powers;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

// Elite for The Continent. Two-phase fight gated by Shriek (mirrors Terror
// Eel's shriek mechanic):
//   - Phase 1 (HP > 70): alternates Fire (apply Burn 3 to player) ↔ Smash
//     (gain 10 block, deal 12 damage).
//   - Shriek 70 is applied on combat start. Once unblocked damage drops the
//     boss to ≤70 HP, Shriek stuns Sakapatate and queues Swing.
//   - Phase 2 (post-shriek): Swing (24 damage) on repeat.
//
// Stun state is intentionally a no-op move (CreatureCmd.Stun handles the
// stun visual + skipped action). Bestiary hides it.
public sealed class UltimateSakapatate : CustomMonsterModel, ILocalizationProvider
{
    private const string FireMoveId = "FIRE_MOVE";
    private const string SmashMoveId = "SMASH_MOVE";
    private const string StunMoveId = "STUN_MOVE";
    private const string SwingMoveId = "SWING_MOVE";

    public override int MinInitialHp => 140;
    public override int MaxInitialHp => 140;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/ultimatesakapatate.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Stone;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Ultimate Sakapatate",
        MoveTitles: new[]
        {
            (FireMoveId, "Fire"),
            (SmashMoveId, "Smash"),
            (StunMoveId, "Stunned"),
            (SwingMoveId, "Swing"),
        });

    private int ShriekAmount => 70;
    private int FireBurnBase => 3;
    private int SmashBlock => 10;
    private int SmashDamage => 12;
    private int SwingDamage => 18;

    // Counts the number of completed Fire moves so each subsequent Fire applies
    // one more Burn stack than the last (2, 3, 4, …). Mutable through the
    // public setter so AssertMutable lets the increment through on the
    // encounter's working copy. Resets to 0 with each fresh combat because
    // GenerateMonsters hands out a ToMutable() clone of the canonical model.
    public int _fireCount;
    public int FireCount
    {
        get => _fireCount;
        set
        {
            AssertMutable();
            _fireCount = value;
        }
    }

    // The move that should be queued when Shriek triggers and stuns the boss.
    // Pointing at Swing makes phase 2 a single attack on repeat.
    public string PostShriekStateId => SwingMoveId;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<Shriek>(new ThrowingPlayerChoiceContext(), base.Creature, ShriekAmount, base.Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var fire = new MoveState(FireMoveId, FireMove, new DebuffIntent());
        var smash = new MoveState(SmashMoveId, SmashMove, new DefendIntent(), new SingleAttackIntent(SmashDamage));
        var stun = new MoveState(StunMoveId, StunMove, new StunIntent());
        var swing = new MoveState(SwingMoveId, SwingMove, new SingleAttackIntent(SwingDamage));

        fire.FollowUpState = smash;
        smash.FollowUpState = fire;
        stun.FollowUpState = swing;
        swing.FollowUpState = swing;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { fire, smash, stun, swing },
            fire);
    }

    private async Task FireMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.5f);
        int stacks = FireBurnBase + FireCount;
        FireCount++;
        await PowerCmd.Apply<Burn>(new ThrowingPlayerChoiceContext(), targets, stacks, base.Creature, null);
    }

    private async Task SmashMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(base.Creature, SmashBlock, ValueProp.Move, null);
        await DamageCmd.Attack(SmashDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private Task StunMove(IReadOnlyList<Creature> targets)
    {
        return Task.CompletedTask;
    }

    private async Task SwingMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SwingDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.25f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    protected override bool ShouldShowMoveInBestiary(string moveStateId) =>
        moveStateId != StunMoveId;
}
