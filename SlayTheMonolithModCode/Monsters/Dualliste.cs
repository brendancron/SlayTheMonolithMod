using System.Linq;
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

// Second TheAxons elite. Functionally identical to vanilla Entomancer: 145 HP,
// PersonalHive 1 on entry, opens with Bees (3x7) then Spear (18) then
// Pheromone Spit (HivePower+1 + Strength+1, capped to Strength+2 once
// HivePower >= 3), loops back to Bees.
public sealed class Dualliste : CustomMonsterModel, ILocalizationProvider
{
    private const string SpitMoveId = "PHEROMONE_SPIT_MOVE";
    private const string BeesMoveId = "BEES_MOVE";
    private const string SpearMoveId = "SPEAR_MOVE";

    public override int MinInitialHp => 145;
    public override int MaxInitialHp => 145;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/dualliste.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx =>
        "event:/sfx/enemy/enemy_attacks/entomancer/entomancer_attack_ranged";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Dualliste",
        MoveTitles: new[]
        {
            (SpitMoveId, "Pheromone Spit"),
            (BeesMoveId, "Swarm"),
            (SpearMoveId, "Spear"),
        });

    private int SpearDamage => 18;
    private int BeesDamage => 3;
    private int BeesRepeat => 7;
    private int HiveCap => 3;
    private int LowHiveStrengthGain => 1;
    private int HighHiveStrengthGain => 2;
    private int InitialHive => 1;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<PersonalHivePower>(new ThrowingPlayerChoiceContext(), base.Creature, InitialHive, base.Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var spit = new MoveState(SpitMoveId, SpitMove, new BuffIntent());
        var bees = new MoveState(BeesMoveId, BeesMove, new MultiAttackIntent(BeesDamage, BeesRepeat));
        var spear = new MoveState(SpearMoveId, SpearMove, new SingleAttackIntent(SpearDamage));

        bees.FollowUpState = spear;
        spear.FollowUpState = spit;
        spit.FollowUpState = bees;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { spit, bees, spear },
            bees);
    }

    private async Task SpitMove(IReadOnlyList<Creature> targets)
    {
        SfxCmd.Play(CastSfx);
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.5f);
        var hive = base.Creature.Powers.OfType<PersonalHivePower>().First();
        if (hive.Amount < HiveCap)
        {
            await PowerCmd.Apply<PersonalHivePower>(new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, null);
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, LowHiveStrengthGain, base.Creature, null);
        }
        else
        {
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, HighHiveStrengthGain, base.Creature, null);
        }
    }

    private async Task BeesMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(BeesDamage)
            .WithHitCount(BeesRepeat)
            .FromMonster(this)
            .WithAttackerAnim("attack_ranged", 0.3f)
            .OnlyPlayAnimOnce()
            .WithAttackerFx(null, "event:/sfx/enemy/enemy_attacks/entomancer/entomancer_attack_ranged")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task SpearMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SpearDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.25f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }
}
