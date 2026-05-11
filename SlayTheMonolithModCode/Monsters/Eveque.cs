using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using SlayTheMonolithMod.SlayTheMonolithModCode.Powers;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

public sealed class Eveque : CustomMonsterModel, ILocalizationProvider
{
    private const string HoarfrostMoveId = "HOARFROST_MOVE";
    private const string NumbingStrikeMoveId = "NUMBING_STRIKE_MOVE";
    // Two Decree state instances are needed for the N→D→D pattern, and the state
    // machine keys by moveId so they must be unique. Both map to "Decree" in the
    // MoveTitles list below so the UI shows the same name.
    private const string DecreeAMoveId = "DECREE_A_MOVE";
    private const string DecreeBMoveId = "DECREE_B_MOVE";

    public override int MinInitialHp => 85;
    public override int MaxInitialHp => 90;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/eveque.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Magic;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Eveque",
        MoveTitles: new[]
        {
            (HoarfrostMoveId, "Hoarfrost"),
            (NumbingStrikeMoveId, "Numbing Strike"),
            (DecreeAMoveId, "Decree"),
            (DecreeBMoveId, "Decree"),
        });

    private int HoarfrostStacks => 1;
    private int NumbingStrikeDamage => 6;
    private int NumbingStrikeFrail => 2;
    private int DecreeDamage => 15;

    // Fixed pattern: Hoarfrost (once on turn 1) → Numbing Strike → Decree → Decree
    // → loop back to Numbing Strike. Two separate Decree state instances share the
    // same moveId (so the UI shows "Decree" both times) and the same move function;
    // they only differ in their FollowUpState wiring.
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var hoarfrost = new MoveState(HoarfrostMoveId, HoarfrostMove, new DebuffIntent());
        var numbing = new MoveState(NumbingStrikeMoveId, NumbingStrikeMove, new SingleAttackIntent(NumbingStrikeDamage), new DebuffIntent());
        var decreeA = new MoveState(DecreeAMoveId, DecreeMove, new SingleAttackIntent(DecreeDamage));
        var decreeB = new MoveState(DecreeBMoveId, DecreeMove, new SingleAttackIntent(DecreeDamage));

        hoarfrost.FollowUpState = numbing;
        numbing.FollowUpState = decreeA;
        decreeA.FollowUpState = decreeB;
        decreeB.FollowUpState = numbing;  // loop

        return new MonsterMoveStateMachine(
            new List<MonsterState> { hoarfrost, numbing, decreeA, decreeB },
            hoarfrost);
    }

    private async Task HoarfrostMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.5f);
        await PowerCmd.Apply<Frostbite>(new ThrowingPlayerChoiceContext(), targets, HoarfrostStacks, base.Creature, null);
    }

    private async Task NumbingStrikeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(NumbingStrikeDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(), targets, NumbingStrikeFrail, base.Creature, null);
    }

    private async Task DecreeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(DecreeDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }
}
