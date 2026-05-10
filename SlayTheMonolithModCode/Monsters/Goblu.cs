using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

// Elite for The Continent. Stub moveset — replace later.
public sealed class Goblu : CustomMonsterModel, ILocalizationProvider
{
    private const string GulpMoveId = "GULP_MOVE";

    public override int MinInitialHp => 55;
    public override int MaxInitialHp => 65;

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
        MoveTitles: new[] { (GulpMoveId, "Gulp") });

    private int GulpDamage => 16;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var gulp = new MoveState(GulpMoveId, GulpMove, new SingleAttackIntent(GulpDamage));
        gulp.FollowUpState = gulp;
        return new MonsterMoveStateMachine(new List<MonsterState> { gulp }, gulp);
    }

    private async Task GulpMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(GulpDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }
}
