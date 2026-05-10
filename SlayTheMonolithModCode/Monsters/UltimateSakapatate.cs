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
public sealed class UltimateSakapatate : CustomMonsterModel, ILocalizationProvider
{
    private const string SquishMoveId = "SQUISH_MOVE";

    public override int MinInitialHp => 70;
    public override int MaxInitialHp => 80;

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
        MoveTitles: new[] { (SquishMoveId, "Squish") });

    private int SquishDamage => 13;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var squish = new MoveState(SquishMoveId, SquishMove, new SingleAttackIntent(SquishDamage));
        squish.FollowUpState = squish;
        return new MonsterMoveStateMachine(new List<MonsterState> { squish }, squish);
    }

    private async Task SquishMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SquishDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }
}
