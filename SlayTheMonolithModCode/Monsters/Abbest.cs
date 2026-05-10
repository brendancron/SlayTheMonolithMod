using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

public sealed class Abbest : CustomMonsterModel, ILocalizationProvider
{
    private const string TomeStrikeMoveId = "TOME_STRIKE_MOVE";

    public override int MinInitialHp => 28;
    public override int MaxInitialHp => 34;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/abbest.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Magic;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Abbest",
        MoveTitles: new[] { (TomeStrikeMoveId, "Tome Strike") });

    private int TomeStrikeDamage => 7;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var strike = new MoveState(TomeStrikeMoveId, TomeStrikeMove, new SingleAttackIntent(TomeStrikeDamage));
        strike.FollowUpState = strike;
        return new MonsterMoveStateMachine(new List<MonsterState> { strike }, strike);
    }

    private async Task TomeStrikeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(TomeStrikeDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.15f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }
}
