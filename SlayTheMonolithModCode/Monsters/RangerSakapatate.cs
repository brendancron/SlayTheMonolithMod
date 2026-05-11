using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

// Functionally identical to vanilla AssassinRubyRaider:
//   Single move: Killshot (10 dmg) on repeat. 18-23 HP.
public sealed class RangerSakapatate : CustomMonsterModel, ILocalizationProvider
{
    private const string KillshotMoveId = "KILLSHOT_MOVE";

    public override int MinInitialHp => 18;
    public override int MaxInitialHp => 23;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/ranger_sakapatate.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Ranger Sakapatate",
        MoveTitles: new[] { (KillshotMoveId, "Killshot") });

    private int KillshotDamage => 10;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var killshot = new MoveState(KillshotMoveId, KillshotMove, new SingleAttackIntent(KillshotDamage));
        killshot.FollowUpState = killshot;
        return new MonsterMoveStateMachine(new List<MonsterState> { killshot }, killshot);
    }

    private async Task KillshotMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(KillshotDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.5f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }
}
