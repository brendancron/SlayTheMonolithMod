using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

// Boss for The Continent. Stub moveset — single heavy attack, replace later.
public sealed class Lampmaster : CustomMonsterModel, ILocalizationProvider
{
    private const string SmiteMoveId = "SMITE_MOVE";

    public override int MinInitialHp => 110;
    public override int MaxInitialHp => 130;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/lampmaster.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Magic;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Lampmaster",
        MoveTitles: new[] { (SmiteMoveId, "Smite") });

    private int SmiteDamage => 22;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var smite = new MoveState(SmiteMoveId, SmiteMove, new SingleAttackIntent(SmiteDamage));
        smite.FollowUpState = smite;
        return new MonsterMoveStateMachine(new List<MonsterState> { smite }, smite);
    }

    private async Task SmiteMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SmiteDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.25f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }
}
