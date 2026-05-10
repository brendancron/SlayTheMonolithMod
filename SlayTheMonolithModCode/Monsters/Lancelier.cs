using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

public sealed class Lancelier : CustomMonsterModel, ILocalizationProvider
{
    private const string LungeMoveId = "LUNGE_MOVE";

    public override int MinInitialHp => 32;
    public override int MaxInitialHp => 38;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/lancelier.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Lancelier",
        MoveTitles: new[] { (LungeMoveId, "Lunge") });

    private int LungeDamage => 9;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var lunge = new MoveState(LungeMoveId, LungeMove, new SingleAttackIntent(LungeDamage));
        lunge.FollowUpState = lunge;
        return new MonsterMoveStateMachine(new List<MonsterState> { lunge }, lunge);
    }

    private async Task LungeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(LungeDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.15f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }
}
