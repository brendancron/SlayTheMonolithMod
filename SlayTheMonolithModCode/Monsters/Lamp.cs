using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

// Passive minion summoned by Lampmaster's ritual. Carries an IgnitionOrder
// power (1-4) telling the player which order to kill it in. Has no offensive
// moves — its only purpose is to be killed.
public sealed class Lamp : CustomMonsterModel, ILocalizationProvider
{
    private const string IdleMoveId = "LAMP_IDLE_MOVE";

    public override int MinInitialHp => 6;
    public override int MaxInitialHp => 6;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/lamp.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Magic;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Lamp",
        MoveTitles: new[] { (IdleMoveId, "Idle") });

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var idle = new MoveState(IdleMoveId, IdleMove, new HiddenIntent());
        idle.FollowUpState = idle;
        return new MonsterMoveStateMachine(new List<MonsterState> { idle }, idle);
    }

    private Task IdleMove(IReadOnlyList<Creature> targets) => Task.CompletedTask;

    protected override bool ShouldShowMoveInBestiary(string moveStateId) => false;
}
