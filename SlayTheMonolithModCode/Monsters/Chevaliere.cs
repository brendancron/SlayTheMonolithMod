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

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

// Functionally identical to vanilla HunterKiller. Opens with Goop (Tender 1),
// then every subsequent turn picks randomly between Bite (17) and Puncture
// (7x3). Bite cannot repeat back-to-back; Puncture has 2x weight so the
// 3-hit option dominates. 121 HP, no Ascension scaling (mod doesn't gate on
// AscensionHelper anywhere).
public sealed class Chevaliere : CustomMonsterModel, ILocalizationProvider
{
    private const string GoopMoveId = "GOOP_MOVE";
    private const string BiteMoveId = "BITE_MOVE";
    private const string PunctureMoveId = "PUNCTURE_MOVE";
    private const string RandBranchId = "RAND";

    public override int MinInitialHp => 121;
    public override int MaxInitialHp => 121;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/chevaliere.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx =>
        "event:/sfx/enemy/enemy_attacks/hunter_killer/hunter_killer_attack";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Chevaliere",
        MoveTitles: new[]
        {
            (GoopMoveId, "Tendering Strike"),
            (BiteMoveId, "Bite"),
            (PunctureMoveId, "Puncture"),
        });

    private int BiteDamage => 17;
    private int PunctureDamage => 7;
    private int PunctureRepeat => 3;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var goop = new MoveState(GoopMoveId, GoopMove, new DebuffIntent());
        var bite = new MoveState(BiteMoveId, BiteMove, new SingleAttackIntent(BiteDamage));
        var puncture = new MoveState(PunctureMoveId, PunctureMove, new MultiAttackIntent(PunctureDamage, PunctureRepeat));

        var rand = new RandomBranchState(RandBranchId);
        rand.AddBranch(bite, MoveRepeatType.CannotRepeat);
        rand.AddBranch(puncture, 2);

        goop.FollowUpState = rand;
        bite.FollowUpState = rand;
        puncture.FollowUpState = rand;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { goop, bite, puncture, rand },
            goop);
    }

    private async Task GoopMove(IReadOnlyList<Creature> targets)
    {
        SfxCmd.Play(CastSfx);
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.4f);
        await PowerCmd.Apply<TenderPower>(new ThrowingPlayerChoiceContext(), targets, 1m, base.Creature, null);
    }

    private async Task BiteMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(BiteDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_bite")
            .Execute(null);
    }

    private async Task PunctureMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(PunctureDamage)
            .WithHitCount(PunctureRepeat)
            .OnlyPlayAnimOnce()
            .FromMonster(this)
            .WithAttackerAnim("TripleAttack", 0.3f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }
}
