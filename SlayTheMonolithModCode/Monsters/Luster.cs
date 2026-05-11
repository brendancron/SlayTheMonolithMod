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

// Functionally identical to vanilla CubexConstruct:
//   AfterAddedToRoom: gain 13 block, apply Artifact 1.
//   Cycle: ChargeUp -> Blast (7 + Strength 2) -> Blast (7 + Strength 2) ->
//          Expel (5x2) -> Blast -> Blast -> Expel ... (initial = ChargeUp)
//   65 HP. Blast appears twice in the cycle, requiring two distinct moveIds
//   that share the same display name -- the state machine keys by moveId.
// Vanilla CubexConstruct also runs a charge-attack SFX loop and tweaks an
// FMOD param on hurt; we drop that here since we don't have the bank/event.
public sealed class Luster : CustomMonsterModel, ILocalizationProvider
{
    private const string ChargeUpMoveId = "CHARGE_UP_MOVE";
    private const string BlastAMoveId = "BLAST_A_MOVE";
    private const string BlastBMoveId = "BLAST_B_MOVE";
    private const string ExpelMoveId = "EXPEL_MOVE";

    public override int MinInitialHp => 65;
    public override int MaxInitialHp => 65;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/luster.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Stone;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Luster",
        MoveTitles: new[]
        {
            (ChargeUpMoveId, "Charge Up"),
            (BlastAMoveId, "Repeater Blast"),
            (BlastBMoveId, "Repeater Blast"),
            (ExpelMoveId, "Expel Blast"),
        });

    private int StartingBlock => 13;
    private int BlastDamage => 7;
    private int ExpelDamage => 5;
    private int ExpelRepeat => 2;
    private int StrengthGain => 2;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await CreatureCmd.GainBlock(base.Creature, StartingBlock, ValueProp.Move, null);
        await PowerCmd.Apply<ArtifactPower>(new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var chargeUp = new MoveState(ChargeUpMoveId, ChargeUpMove, new BuffIntent());
        var blastA = new MoveState(BlastAMoveId, RepeaterBlastMove, new SingleAttackIntent(BlastDamage), new BuffIntent());
        var blastB = new MoveState(BlastBMoveId, RepeaterBlastMove, new SingleAttackIntent(BlastDamage), new BuffIntent());
        var expel = new MoveState(ExpelMoveId, ExpelBlastMove, new MultiAttackIntent(ExpelDamage, ExpelRepeat));

        chargeUp.FollowUpState = blastA;
        blastA.FollowUpState = blastB;
        blastB.FollowUpState = expel;
        expel.FollowUpState = blastA;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { chargeUp, blastA, blastB, expel },
            chargeUp);
    }

    private async Task ChargeUpMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.75f);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, StrengthGain, base.Creature, null);
    }

    private async Task RepeaterBlastMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(BlastDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, StrengthGain, base.Creature, null);
    }

    private async Task ExpelBlastMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(ExpelDamage)
            .WithHitCount(ExpelRepeat)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .OnlyPlayAnimOnce()
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }
}
