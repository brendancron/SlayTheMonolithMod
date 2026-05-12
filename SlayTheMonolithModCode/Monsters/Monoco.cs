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

// First Axons elite. Functionally identical to vanilla InfestedPrism: 200 HP,
// VitalSparkPower 1 on entry, 4-state cycle Jab -> Radiate -> Whirlwind ->
// Pulsate -> loop. Damage and block values mirror non-Ascension vanilla.
public sealed class Monoco : CustomMonsterModel, ILocalizationProvider
{
    private const string JabMoveId = "JAB_MOVE";
    private const string RadiateMoveId = "RADIATE_MOVE";
    private const string WhirlwindMoveId = "WHIRLWIND_MOVE";
    private const string PulsateMoveId = "PULSATE_MOVE";

    public override int MinInitialHp => 200;
    public override int MaxInitialHp => 200;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/monoco.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx =>
        "event:/sfx/enemy/enemy_attacks/infested_prisms/infested_prisms_attack";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Stone;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Monoco",
        MoveTitles: new[]
        {
            (JabMoveId, "Jab"),
            (RadiateMoveId, "Radiate"),
            (WhirlwindMoveId, "Whirlwind"),
            (PulsateMoveId, "Pulsate"),
        });

    private int JabDamage => 22;
    private int RadiateDamage => 16;
    private int RadiateBlock => 16;
    private int WhirlwindDamage => 9;
    private int WhirlwindRepeat => 3;
    private int PulsateBlock => 20;
    private int PulsateStrength => 4;
    private int VitalSparkStacks => 1;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<VitalSparkPower>(new ThrowingPlayerChoiceContext(), base.Creature, VitalSparkStacks, base.Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var jab = new MoveState(JabMoveId, JabMove, new SingleAttackIntent(JabDamage));
        var radiate = new MoveState(RadiateMoveId, RadiateMove, new SingleAttackIntent(RadiateDamage), new DefendIntent());
        var whirlwind = new MoveState(WhirlwindMoveId, WhirlwindMove, new MultiAttackIntent(WhirlwindDamage, WhirlwindRepeat));
        var pulsate = new MoveState(PulsateMoveId, PulsateMove, new BuffIntent(), new DefendIntent());

        jab.FollowUpState = radiate;
        radiate.FollowUpState = whirlwind;
        whirlwind.FollowUpState = pulsate;
        pulsate.FollowUpState = jab;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { jab, radiate, whirlwind, pulsate },
            jab);
    }

    private async Task JabMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(JabDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.1f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task RadiateMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(RadiateDamage)
            .FromMonster(this)
            .WithAttackerAnim("AttackBlock", 0.25f)
            .WithAttackerFx(null, "event:/sfx/enemy/enemy_attacks/infested_prisms/infested_prisms_attack_defend")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await CreatureCmd.GainBlock(base.Creature, RadiateBlock, ValueProp.Move, null);
    }

    private async Task WhirlwindMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(WhirlwindDamage)
            .WithHitCount(WhirlwindRepeat)
            .FromMonster(this)
            .WithAttackerAnim("AttackDouble", 0.2f)
            .OnlyPlayAnimOnce()
            .WithAttackerFx(null, "event:/sfx/enemy/enemy_attacks/infested_prisms/infested_prisms_attack_spin")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task PulsateMove(IReadOnlyList<Creature> targets)
    {
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/infested_prisms/infested_prisms_buff");
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.6f);
        await CreatureCmd.GainBlock(base.Creature, PulsateBlock, ValueProp.Move, null);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, PulsateStrength, base.Creature, null);
    }
}
