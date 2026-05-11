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

// Functionally identical to vanilla SludgeSpinner.
//   OilSpray (8 + Weak 1) / Slam (11) / Rage (6 + self Strength 3) random
//   branch with CannotRepeat. Initial state = OilSpray. 37 HP. Rage hidden
//   from bestiary, matching vanilla.
public sealed class Abbest : CustomMonsterModel, ILocalizationProvider
{
    private const string OilSprayMoveId = "OIL_SPRAY_MOVE";
    private const string SlamMoveId = "SLAM_MOVE";
    private const string RageMoveId = "RAGE_MOVE";

    public override int MinInitialHp => 37;
    public override int MaxInitialHp => 37;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/abbest.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Stone;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Abbest",
        MoveTitles: new[]
        {
            (OilSprayMoveId, "Oil Spray"),
            (SlamMoveId, "Slam"),
            (RageMoveId, "Rage"),
        });

    private int OilSprayDamage => 8;
    private int OilSprayWeak => 1;
    private int SlamDamage => 11;
    private int RageDamage => 6;
    private int RageStrength => 3;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var oilSpray = new MoveState(OilSprayMoveId, OilSprayMove, new SingleAttackIntent(OilSprayDamage), new DebuffIntent());
        var slam = new MoveState(SlamMoveId, SlamMove, new SingleAttackIntent(SlamDamage));
        var rage = new MoveState(RageMoveId, RageMove, new SingleAttackIntent(RageDamage), new BuffIntent());

        var rand = new RandomBranchState("RAND");
        oilSpray.FollowUpState = rand;
        slam.FollowUpState = rand;
        rage.FollowUpState = rand;
        rand.AddBranch(oilSpray, MoveRepeatType.CannotRepeat);
        rand.AddBranch(slam, MoveRepeatType.CannotRepeat);
        rand.AddBranch(rage, MoveRepeatType.CannotRepeat);

        return new MonsterMoveStateMachine(
            new List<MonsterState> { rand, oilSpray, slam, rage },
            oilSpray);
    }

    private async Task OilSprayMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(OilSprayDamage)
            .FromMonster(this)
            .WithAttackerAnim("Cast", 0.5f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets, OilSprayWeak, base.Creature, null);
    }

    private async Task SlamMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SlamDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.15f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task RageMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(RageDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.5f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, RageStrength, base.Creature, null);
    }

    protected override bool ShouldShowMoveInBestiary(string moveStateId) =>
        moveStateId != RageMoveId;
}
