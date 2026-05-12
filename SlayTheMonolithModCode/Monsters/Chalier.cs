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
using MegaCrit.Sts2.Core.ValueProps;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

// Functionally identical to vanilla Tunneler. Move cycle:
//   Bite (13) -> Burrow (apply BurrowedPower + 32 block) -> Below (23, loops).
// 87 HP. DIZZY_MOVE state included for parity but only enters via external
// stun. Animation triggers ("Burrow", "BurrowAttack", "WakeUp") match
// Tunneler's spine; with no custom scene yet they no-op harmlessly.
public sealed class Chalier : CustomMonsterModel, ILocalizationProvider
{
    private const string BiteMoveId = "BITE_MOVE";
    private const string BurrowMoveId = "BURROW_MOVE";
    private const string BelowMoveId = "BELOW_MOVE";
    private const string DizzyMoveId = "DIZZY_MOVE";

    public override int MinInitialHp => 87;
    public override int MaxInitialHp => 87;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/chalier.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx =>
        "event:/sfx/enemy/enemy_attacks/burrowing_bug/burrowing_bug_attack";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Stone;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Chalier",
        MoveTitles: new[]
        {
            (BiteMoveId, "Bite"),
            (BurrowMoveId, "Burrow"),
            (BelowMoveId, "From Below"),
        });

    private int BiteDamage => 13;
    private int BlockGain => 32;
    private int BelowDamage => 23;

    private bool _isStunned;
    public bool IsStunned
    {
        get => _isStunned;
        set { AssertMutable(); _isStunned = value; }
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var bite = new MoveState(BiteMoveId, BiteMove, new SingleAttackIntent(BiteDamage));
        var burrow = new MoveState(BurrowMoveId, BurrowMove, new BuffIntent(), new DefendIntent());
        var below = new MoveState(BelowMoveId, BelowMove, new SingleAttackIntent(BelowDamage));
        var dizzy = new MoveState(DizzyMoveId, StillDizzyMove, new StunIntent());

        bite.FollowUpState = burrow;
        burrow.FollowUpState = below;
        below.FollowUpState = below;
        dizzy.FollowUpState = bite;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { bite, burrow, below, dizzy },
            bite);
    }

    private async Task BiteMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(BiteDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.25f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task BurrowMove(IReadOnlyList<Creature> targets)
    {
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/burrowing_bug/burrowing_bug_burrow");
        await CreatureCmd.TriggerAnim(base.Creature, "Burrow", 0.25f);
        await PowerCmd.Apply<BurrowedPower>(new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, null);
        await CreatureCmd.GainBlock(base.Creature, BlockGain, ValueProp.Move, null);
    }

    private async Task BelowMove(IReadOnlyList<Creature> targets)
    {
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/burrowing_bug/burrowing_bug_hidden_attack");
        await CreatureCmd.TriggerAnim(base.Creature, "BurrowAttack", 0.25f);
        await DamageCmd.Attack(BelowDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    public async Task GetStunned()
    {
        IsStunned = true;
        await CreatureCmd.TriggerAnim(base.Creature, "Stun", 0.25f);
    }

    // Public so ChalierBurrowedStunPatch can pass it as the synthetic STUNNED
    // move body, matching vanilla Tunneler.StillDizzyMove.
    public async Task StillDizzyMove(IReadOnlyList<Creature> targets)
    {
        IsStunned = false;
        await CreatureCmd.TriggerAnim(base.Creature, "WakeUp", 0.25f);
    }

    protected override bool ShouldShowMoveInBestiary(string moveStateId) =>
        moveStateId != DizzyMoveId;
}
