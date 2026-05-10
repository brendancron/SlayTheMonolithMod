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
public sealed class Eveque : CustomMonsterModel, ILocalizationProvider
{
    private const string DecreeMoveId = "DECREE_MOVE";

    public override int MinInitialHp => 60;
    public override int MaxInitialHp => 70;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/eveque.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Magic;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Eveque",
        MoveTitles: new[] { (DecreeMoveId, "Decree") });

    private int DecreeDamage => 15;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var decree = new MoveState(DecreeMoveId, DecreeMove, new SingleAttackIntent(DecreeDamage));
        decree.FollowUpState = decree;
        return new MonsterMoveStateMachine(new List<MonsterState> { decree }, decree);
    }

    private async Task DecreeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(DecreeDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }
}
