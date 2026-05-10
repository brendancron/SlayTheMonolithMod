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
using MegaCrit.Sts2.Core.ValueProps;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

public sealed class Lancelier : CustomMonsterModel, ILocalizationProvider
{
    private const string DoubleStrikeMoveId = "DOUBLE_STRIKE_MOVE";
    private const string CleaveMoveId = "CLEAVE_MOVE";
    private const string PhalanxMoveId = "PHALANX_MOVE";

    public override int MinInitialHp => 32;
    public override int MaxInitialHp => 38;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/lancelier.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Lancelier",
        MoveTitles: new[]
        {
            (DoubleStrikeMoveId, "Double Strike"),
            (CleaveMoveId, "Cleave"),
            (PhalanxMoveId, "Phalanx"),
        });

    private int DoubleStrikeDamage => 5;
    private int CleaveDamage => 11;
    private int PhalanxStrength => 2;
    private int PhalanxBlock => 12;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var doubleStrike = new MoveState(DoubleStrikeMoveId, DoubleStrikeMove, new MultiAttackIntent(DoubleStrikeDamage, 2));
        var cleave = new MoveState(CleaveMoveId, CleaveMove, new SingleAttackIntent(CleaveDamage));
        var phalanx = new MoveState(PhalanxMoveId, PhalanxMove, new BuffIntent());
        var rand = new RandomBranchState("LANCELIER_RAND");
        doubleStrike.FollowUpState = rand;
        cleave.FollowUpState = rand;
        phalanx.FollowUpState = rand;
        rand.AddBranch(doubleStrike, MoveRepeatType.CannotRepeat, 1f);
        rand.AddBranch(cleave, MoveRepeatType.CannotRepeat, 1f);
        rand.AddBranch(phalanx, MoveRepeatType.CannotRepeat, 1f);
        return new MonsterMoveStateMachine(
            new List<MonsterState> { doubleStrike, cleave, phalanx, rand },
            cleave);
    }

    private async Task DoubleStrikeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(DoubleStrikeDamage)
            .WithHitCount(2)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.15f)
            .OnlyPlayAnimOnce()
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task CleaveMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(CleaveDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.15f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task PhalanxMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(base.Creature, PhalanxBlock, ValueProp.Move, null);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, PhalanxStrength, base.Creature, null);
    }
}
