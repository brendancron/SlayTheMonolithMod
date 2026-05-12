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

// Esquie's archnemesis. Modeled on Bygone Effigy: same HP, same Slow 1 self-
// debuff applied on combat start (player damage costs scale up per card
// played). Unlike the effigy's Sleep -> Wake -> Slash spam, Francois
// alternates indefinitely between Sleep (skip turn) and "The Greatest Ice
// Attack Ever" (25 dmg single hit).
public sealed class Francois : CustomMonsterModel, ILocalizationProvider
{
    private const string SleepMoveId = "SLEEP_MOVE";
    private const string IceAttackMoveId = "ICE_ATTACK_MOVE";

    public override int MinInitialHp => 127;
    public override int MaxInitialHp => 127;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/francois.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Stone;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Francois",
        MoveTitles: new[]
        {
            (SleepMoveId, "Sleeping"),
            (IceAttackMoveId, "The Greatest Ice Attack Ever"),
        });

    private int IceAttackDamage => 25;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<SlowPower>(new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var sleep = new MoveState(SleepMoveId, SleepMove, new SleepIntent());
        var iceAttack = new MoveState(IceAttackMoveId, IceAttackMove, new SingleAttackIntent(IceAttackDamage));
        sleep.FollowUpState = iceAttack;
        iceAttack.FollowUpState = sleep;
        return new MonsterMoveStateMachine(new List<MonsterState> { sleep, iceAttack }, sleep);
    }

    private Task SleepMove(IReadOnlyList<Creature> targets) => Task.CompletedTask;

    private async Task IceAttackMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(IceAttackDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }
}
