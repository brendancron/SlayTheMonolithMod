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

// Functionally identical to vanilla SewerClam.
//   AfterAddedToRoom: apply Plating 8.
//   Cycle: Jet (10) -> Pressurize (+Strength 4) -> Jet ... initial = Jet.
//   56 HP.
public sealed class Hexga : CustomMonsterModel, ILocalizationProvider
{
    private const string PressurizeMoveId = "PRESSURIZE_MOVE";
    private const string JetMoveId = "JET_MOVE";

    public override int MinInitialHp => 56;
    public override int MaxInitialHp => 56;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/hexga.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Stone;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Hexga",
        MoveTitles: new[]
        {
            (PressurizeMoveId, "Pressurize"),
            (JetMoveId, "Jet"),
        });

    private int PlatingAmount => 8;
    private int PressurizeStrength => 4;
    private int JetDamage => 10;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<PlatingPower>(new ThrowingPlayerChoiceContext(), base.Creature, PlatingAmount, base.Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var pressurize = new MoveState(PressurizeMoveId, PressurizeMove, new BuffIntent());
        var jet = new MoveState(JetMoveId, JetMove, new SingleAttackIntent(JetDamage));
        pressurize.FollowUpState = jet;
        jet.FollowUpState = pressurize;
        return new MonsterMoveStateMachine(
            new List<MonsterState> { pressurize, jet },
            jet);
    }

    private async Task PressurizeMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 1f);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, PressurizeStrength, base.Creature, null);
    }

    private async Task JetMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(JetDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.45f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }
}
