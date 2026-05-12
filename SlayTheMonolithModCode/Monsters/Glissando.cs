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

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

// Alt-path STS1 Snecko mirror. Always opens with Perplexing Glare (Confused),
// then alternates Bite (15) and Tail Whip (8 + 2 Vulnerable). Bite weights 60%
// vs Tail Whip's 40%, and Bite cannot repeat three turns in a row
// (CanRepeatXTimes with maxRepeats=2 zeroes its weight when the last two
// moves were both Bite).
public sealed class Glissando : CustomMonsterModel, ILocalizationProvider
{
    private const string GlareMoveId = "PERPLEXING_GLARE_MOVE";
    private const string BiteMoveId = "BITE_MOVE";
    private const string TailWhipMoveId = "TAIL_WHIP_MOVE";

    public override int MinInitialHp => 114;
    public override int MaxInitialHp => 120;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/glissando.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Fur;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Glissando",
        MoveTitles: new[]
        {
            (GlareMoveId, "Perplexing Glare"),
            (BiteMoveId, "Bite"),
            (TailWhipMoveId, "Tail Whip"),
        });

    private int BiteDamage => 15;
    private int TailWhipDamage => 8;
    private int TailWhipVulnerable => 2;
    private int ConfusedStacks => 1;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var glare = new MoveState(GlareMoveId, GlareMove, new DebuffIntent());
        var bite = new MoveState(BiteMoveId, BiteMove, new SingleAttackIntent(BiteDamage));
        var tailWhip = new MoveState(TailWhipMoveId, TailWhipMove,
            new SingleAttackIntent(TailWhipDamage), new DebuffIntent());

        var rand = new RandomBranchState("RAND");
        rand.AddBranch(bite, maxRepeats: 2, weight: 0.6f);
        rand.AddBranch(tailWhip, MoveRepeatType.CanRepeatForever, 0.4f);

        glare.FollowUpState = rand;
        bite.FollowUpState = rand;
        tailWhip.FollowUpState = rand;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { glare, rand, bite, tailWhip },
            glare);
    }

    private async Task GlareMove(IReadOnlyList<Creature> targets)
    {
        SfxCmd.Play(CastSfx);
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.5f);
        await PowerCmd.Apply<ConfusedPower>(
            new ThrowingPlayerChoiceContext(), targets, ConfusedStacks, base.Creature, null);
    }

    private async Task BiteMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(BiteDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_bite")
            .Execute(null);
    }

    private async Task TailWhipMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(TailWhipDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await PowerCmd.Apply<VulnerablePower>(
            new ThrowingPlayerChoiceContext(), targets, TailWhipVulnerable, base.Creature, null);
    }
}
