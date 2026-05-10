using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

public sealed class Portier : CustomMonsterModel, ILocalizationProvider
{
    private const string ChargeMoveId = "CHARGE_MOVE";
    private const string JumpMoveId = "JUMP_MOVE";

    public override int MinInitialHp => 26;
    public override int MaxInitialHp => 32;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/portier.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Portier",
        MoveTitles: new[]
        {
            (ChargeMoveId, "Charge"),
            (JumpMoveId, "Jump"),
        });

    private int JumpDamage => 16;

    // Strict A → B → A → B alternation. Charge does nothing (wind-up turn);
    // Jump deals heavy damage. BuffIntent on Charge signals "I'm doing something
    // to myself" so the player can read the wind-up.
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var charge = new MoveState(ChargeMoveId, ChargeMove, new BuffIntent());
        var jump = new MoveState(JumpMoveId, JumpMove, new SingleAttackIntent(JumpDamage));
        charge.FollowUpState = jump;
        jump.FollowUpState = charge;
        return new MonsterMoveStateMachine(new List<MonsterState> { charge, jump }, charge);
    }

    private async Task ChargeMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.5f);
    }

    private async Task JumpMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(JumpDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.25f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }
}
