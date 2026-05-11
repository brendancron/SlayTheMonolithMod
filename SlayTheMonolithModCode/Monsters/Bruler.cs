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

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

// Functionally identical to vanilla VineShambler:
//   Swipe (6x2) -> GraspingVines (8 + apply Tangled) -> Chomp (16) -> Swipe ...
//   Initial state = Swipe. 61 HP.
public sealed class Bruler : CustomMonsterModel, ILocalizationProvider
{
    private const string GraspingMoveId = "GRASPING_MOVE";
    private const string SwipeMoveId = "SWIPE_MOVE";
    private const string ChompMoveId = "CHOMP_MOVE";

    public override int MinInitialHp => 61;
    public override int MaxInitialHp => 61;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/bruler.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Magic;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Bruler",
        MoveTitles: new[]
        {
            (GraspingMoveId, "Grasping"),
            (SwipeMoveId, "Swipe"),
            (ChompMoveId, "Chomp"),
        });

    private int GraspingDamage => 8;
    private int SwipeDamage => 6;
    private int SwipeRepeat => 2;
    private int ChompDamage => 16;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var grasping = new MoveState(GraspingMoveId, GraspingMove, new SingleAttackIntent(GraspingDamage), new CardDebuffIntent());
        var swipe = new MoveState(SwipeMoveId, SwipeMove, new MultiAttackIntent(SwipeDamage, SwipeRepeat));
        var chomp = new MoveState(ChompMoveId, ChompMove, new SingleAttackIntent(ChompDamage));

        swipe.FollowUpState = grasping;
        grasping.FollowUpState = chomp;
        chomp.FollowUpState = swipe;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { grasping, swipe, chomp },
            swipe);
    }

    private async Task GraspingMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(GraspingDamage)
            .FromMonster(this)
            .WithAttackerAnim("Cast", 0.5f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await PowerCmd.Apply<TangledPower>(new ThrowingPlayerChoiceContext(), targets, 1m, base.Creature, null);
    }

    private async Task SwipeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SwipeDamage)
            .WithHitCount(SwipeRepeat)
            .FromMonster(this)
            .OnlyPlayAnimOnce()
            .WithAttackerAnim("Attack", 0.4f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_scratch")
            .Execute(null);
    }

    private async Task ChompMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(ChompDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.4f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_bite")
            .Execute(null);
    }
}
