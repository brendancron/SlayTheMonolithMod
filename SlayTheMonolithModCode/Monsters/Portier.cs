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

    // Strict Charge -> Jump -> Charge alternation. Charge is the wind-up turn
    // (no damage, BuffIntent so the player reads the telegraph). Jump deals 16
    // and, if any unblocked damage lands, applies vanilla ShrinkPower with
    // Amount=-1 (infinite). ShrinkPower already handles the de-shrink on
    // applier death (AfterDeath -> Remove), so killing Portier reverts the
    // player's scale automatically -- no extra bookkeeping needed.
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var charge = new MoveState(ChargeMoveId, ChargeMove, new BuffIntent());
        var jump = new MoveState(JumpMoveId, JumpMove, new SingleAttackIntent(JumpDamage), new DebuffIntent(strong: true));
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
        var attack = await DamageCmd.Attack(JumpDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.25f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        bool dealtUnblocked = attack.Results
            .SelectMany(r => r)
            .Any(r => r.UnblockedDamage > 0);
        if (!dealtUnblocked) return;

        await PowerCmd.Apply<ShrinkPower>(new ThrowingPlayerChoiceContext(), targets, -1m, base.Creature, null);
    }
}
