using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

// Functionally identical to vanilla HauntedShip (same as Hexga).
//   T1: Haunt (Weak 3 + 5 Dazed into discard).
//   T2+: random branch of {Ramming 10, Swipe 13, Stomp 4x3} weighted by
//        odd/even round (RoundNumber % 2 != 0) and MoveRepeatType.CannotRepeat.
//   63 HP. Ramming hidden from bestiary, matching vanilla.
public sealed class Jar : CustomMonsterModel, ILocalizationProvider
{
    private const string RammingMoveId = "RAMMING_MOVE";
    private const string SwipeMoveId = "SWIPE_MOVE";
    private const string StompMoveId = "STOMP_MOVE";
    private const string HauntMoveId = "HAUNT_MOVE";

    public override int MinInitialHp => 63;
    public override int MaxInitialHp => 63;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/jar.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.ArmorBig;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Jar",
        MoveTitles: new[]
        {
            (RammingMoveId, "Ramming"),
            (SwipeMoveId, "Swipe"),
            (StompMoveId, "Stomp"),
            (HauntMoveId, "Haunt"),
        });

    private int RammingDamage => 10;
    private int SwipeDamage => 13;
    private int StompDamage => 4;
    private int StompRepeat => 3;
    private int HauntWeak => 3;
    private int HauntDazed => 5;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var ramming = new MoveState(RammingMoveId, RammingMove, new SingleAttackIntent(RammingDamage));
        var swipe = new MoveState(SwipeMoveId, SwipeMove, new SingleAttackIntent(SwipeDamage));
        var stomp = new MoveState(StompMoveId, StompMove, new MultiAttackIntent(StompDamage, StompRepeat));
        var haunt = new MoveState(HauntMoveId, HauntMove, new DebuffIntent(), new StatusIntent(HauntDazed));

        var randomBranch = new RandomBranchState("RAND");
        ramming.FollowUpState = randomBranch;
        swipe.FollowUpState = randomBranch;
        stomp.FollowUpState = randomBranch;
        haunt.FollowUpState = randomBranch;

        randomBranch.AddBranch(ramming, MoveRepeatType.CannotRepeat, () => (base.CombatState.RoundNumber % 2 != 0) ? 1 : 0);
        randomBranch.AddBranch(swipe, MoveRepeatType.CannotRepeat, () => (base.CombatState.RoundNumber % 2 != 0) ? 1 : 0);
        randomBranch.AddBranch(stomp, MoveRepeatType.CannotRepeat, () => (base.CombatState.RoundNumber % 2 != 0) ? 1 : 0);

        return new MonsterMoveStateMachine(
            new List<MonsterState> { randomBranch, ramming, swipe, stomp, haunt },
            haunt);
    }

    private async Task RammingMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(RammingDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.15f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task SwipeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SwipeDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.15f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task StompMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(StompDamage)
            .WithHitCount(StompRepeat)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.15f)
            .OnlyPlayAnimOnce()
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task HauntMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.6f);
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets, HauntWeak, base.Creature, null);
        await CardPileCmd.AddToCombatAndPreview<Dazed>(targets, PileType.Discard, HauntDazed, null);
    }

    protected override bool ShouldShowMoveInBestiary(string moveStateId) =>
        moveStateId != RammingMoveId;
}
