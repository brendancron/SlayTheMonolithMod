using System;
using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

// Functionally identical to vanilla PunchConstruct, with Slippery 5 stacked
// on top: incoming damage to the mime is capped at 1 per hit per attack until
// the stacks are gone. Ready -> StrongPunch (14) -> FastPunch (5x2 + Weak 1)
// -> Ready cycle, Artifact 1 absorbs the first debuff. Used by the
// MysteriousMime event's combat branch.
public sealed class Mime : CustomMonsterModel, ILocalizationProvider
{
    private const string ReadyMoveId = "READY_MOVE";
    private const string StrongPunchMoveId = "STRONG_PUNCH_MOVE";
    private const string FastPunchMoveId = "FAST_PUNCH_MOVE";

    public override int MinInitialHp => 55;
    public override int MaxInitialHp => 55;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/mime.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Mime",
        MoveTitles: new[]
        {
            (ReadyMoveId, "Ready"),
            (StrongPunchMoveId, "Strong Punch"),
            (FastPunchMoveId, "Fast Punch"),
        });

    private int ReadyBlock => 10;
    private int StrongPunchDamage => 14;
    private int FastPunchDamage => 5;
    private int FastPunchRepeat => 2;
    private int FastPunchWeak => 1;
    private int InitialSlippery => 5;

    public bool _startsWithStrongPunch;
    public int _startingHpReduction;

    public bool StartsWithStrongPunch
    {
        get => _startsWithStrongPunch;
        set { AssertMutable(); _startsWithStrongPunch = value; }
    }

    public int StartingHpReduction
    {
        get => _startingHpReduction;
        set { AssertMutable(); _startingHpReduction = value; }
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<ArtifactPower>(new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, null);
        await PowerCmd.Apply<SlipperyPower>(new ThrowingPlayerChoiceContext(), base.Creature, InitialSlippery, base.Creature, null);
        if (StartingHpReduction > 0)
        {
            base.Creature.SetCurrentHpInternal(Math.Max(1, base.Creature.CurrentHp - StartingHpReduction));
        }
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var ready = new MoveState(ReadyMoveId, ReadyMove, new DefendIntent());
        var strong = new MoveState(StrongPunchMoveId, StrongPunchMove, new SingleAttackIntent(StrongPunchDamage));
        var fast = new MoveState(FastPunchMoveId, FastPunchMove, new MultiAttackIntent(FastPunchDamage, FastPunchRepeat), new DebuffIntent());

        ready.FollowUpState = strong;
        strong.FollowUpState = fast;
        fast.FollowUpState = ready;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { ready, strong, fast },
            StartsWithStrongPunch ? (MonsterState)strong : ready);
    }

    private async Task ReadyMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.8f);
        await CreatureCmd.GainBlock(base.Creature, ReadyBlock, ValueProp.Move, null);
    }

    private async Task StrongPunchMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(StrongPunchDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.25f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task FastPunchMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(FastPunchDamage)
            .WithHitCount(FastPunchRepeat)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .OnlyPlayAnimOnce()
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets, FastPunchWeak, base.Creature, null);
    }
}
