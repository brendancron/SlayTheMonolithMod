using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using SlayTheMonolithMod.SlayTheMonolithModCode.Powers;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

// Gestral challenge monster: 50 HP, skips every turn, applies
// OneShotOrEscape on combat start. Any non-fatal attack triggers his Escape
// and the event Resume hook reads CombatState.EscapedCreatures to know which
// branch the player took.
public sealed class OnoPuncho : CustomMonsterModel, ILocalizationProvider
{
    private const string SleepMoveId = "ONO_SLEEP";

    public override int MinInitialHp => 50;
    public override int MaxInitialHp => 50;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/ono_puncho.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Stone;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Ono Puncho",
        MoveTitles: new[] { (SleepMoveId, "Waiting") });

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<OneShotOrEscape>(new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var sleep = new MoveState(SleepMoveId, SleepMove, new SleepIntent());
        sleep.FollowUpState = sleep;
        return new MonsterMoveStateMachine(new List<MonsterState> { sleep }, sleep);
    }

    private Task SleepMove(IReadOnlyList<Creature> targets) => Task.CompletedTask;
}
