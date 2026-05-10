using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

public sealed class Volester : CustomMonsterModel, ILocalizationProvider
{
    private const string MoveIdConst = "SHARD_THROW";

    public override int MinInitialHp => 14;
    public override int MaxInitialHp => 18;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/volester.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Stone;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Volester",
        MoveTitles: new[] { (MoveIdConst, "Shard Throw") });

    private int MoveDamage => 5;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var move = new MoveState(MoveIdConst, DoMove, new SingleAttackIntent(MoveDamage));
        move.FollowUpState = move;
        return new MonsterMoveStateMachine(new List<MonsterState> { move }, move);
    }

    private async Task DoMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(MoveDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.15f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }
}
