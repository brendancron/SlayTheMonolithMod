using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

// Functionally identical to vanilla AxeRubyRaider.
//   Cycle: Swing (5 + 5 block) -> Swing -> BigSwing (12) -> Swing ...
//   Swing 1 & 2 share the same display name "Swing"; they are two distinct
//   moveIds in the state machine but the MoveTitles entries collapse them to
//   one label, matching vanilla. Bestiary hides Swing 1 & 2 (only BigSwing
//   is logged). 20-22 HP.
public sealed class RobustSakapatate : CustomMonsterModel, ILocalizationProvider
{
    private const string Swing1MoveId = "SWING_1";
    private const string Swing2MoveId = "SWING_2";
    private const string BigSwingMoveId = "BIG_SWING";

    public override int MinInitialHp => 20;
    public override int MaxInitialHp => 22;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/robust_sakapatate.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Robust Sakapatate",
        MoveTitles: new[]
        {
            (Swing1MoveId, "Swing"),
            (Swing2MoveId, "Swing"),
            (BigSwingMoveId, "Big Swing"),
        });

    private int SwingDamage => 5;
    private int SwingBlock => 5;
    private int BigSwingDamage => 12;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var swing1 = new MoveState(Swing1MoveId, SwingMove, new SingleAttackIntent(SwingDamage), new DefendIntent());
        var swing2 = new MoveState(Swing2MoveId, SwingMove, new SingleAttackIntent(SwingDamage), new DefendIntent());
        var bigSwing = new MoveState(BigSwingMoveId, BigSwingMove, new SingleAttackIntent(BigSwingDamage));
        swing1.FollowUpState = swing2;
        swing2.FollowUpState = bigSwing;
        bigSwing.FollowUpState = swing1;
        return new MonsterMoveStateMachine(
            new List<MonsterState> { swing1, swing2, bigSwing },
            swing1);
    }

    private async Task SwingMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SwingDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.5f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await CreatureCmd.GainBlock(base.Creature, SwingBlock, ValueProp.Move, null);
    }

    private async Task BigSwingMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(BigSwingDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.5f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    protected override bool ShouldShowMoveInBestiary(string moveStateId) =>
        moveStateId != Swing1MoveId && moveStateId != Swing2MoveId;
}
