using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

// Functionally identical to vanilla CrossbowRubyRaider:
//   Cycle: Reload (3 block) -> Fire (14 dmg) -> Reload -> Fire ...
//   Initial state = Reload. 18-21 HP. We drop the IsCrossbowReloaded flag
//   (vanilla uses it to switch animator skins; we don't have a reloaded /
//   unreloaded skin pair).
public sealed class CatapultSakapatate : CustomMonsterModel, ILocalizationProvider
{
    private const string ReloadMoveId = "RELOAD_MOVE";
    private const string FireMoveId = "FIRE_MOVE";

    public override int MinInitialHp => 18;
    public override int MaxInitialHp => 21;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/catapult_sakapatate.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Catapult Sakapatate",
        MoveTitles: new[]
        {
            (ReloadMoveId, "Reload"),
            (FireMoveId, "Fire"),
        });

    private int ReloadBlock => 3;
    private int FireDamage => 14;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var reload = new MoveState(ReloadMoveId, ReloadMove, new DefendIntent());
        var fire = new MoveState(FireMoveId, FireMove, new SingleAttackIntent(FireDamage));
        reload.FollowUpState = fire;
        fire.FollowUpState = reload;
        return new MonsterMoveStateMachine(
            new List<MonsterState> { reload, fire },
            reload);
    }

    private async Task ReloadMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.25f);
        await CreatureCmd.GainBlock(base.Creature, ReloadBlock, ValueProp.Move, null);
    }

    private async Task FireMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(FireDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.25f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }
}
