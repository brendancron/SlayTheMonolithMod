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
using MegaCrit.Sts2.Core.ValueProps;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

public sealed class Abbest : CustomMonsterModel, ILocalizationProvider
{
    private const string TomeStrikeMoveId = "TOME_STRIKE_MOVE";
    private const string HexMoveId = "HEX_MOVE";
    private const string LitanyMoveId = "LITANY_MOVE";

    public override int MinInitialHp => 22;
    public override int MaxInitialHp => 28;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/abbest.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Magic;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Abbest",
        MoveTitles: new[]
        {
            (TomeStrikeMoveId, "Tome Strike"),
            (HexMoveId, "Hex"),
            (LitanyMoveId, "Litany"),
        });

    private int TomeStrikeDamage => 7;
    private int HexVulnerable => 2;
    private int LitanyBlock => 4;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var tome = new MoveState(TomeStrikeMoveId, TomeStrikeMove, new SingleAttackIntent(TomeStrikeDamage));
        var hex = new MoveState(HexMoveId, HexMove, new DebuffIntent());
        var litany = new MoveState(LitanyMoveId, LitanyMove, new DefendIntent());
        var rand = new RandomBranchState("ABBEST_RAND");
        tome.FollowUpState = rand;
        hex.FollowUpState = rand;
        litany.FollowUpState = rand;
        rand.AddBranch(tome, MoveRepeatType.CannotRepeat, 1f);
        rand.AddBranch(hex, MoveRepeatType.CannotRepeat, 1f);
        rand.AddBranch(litany, MoveRepeatType.CannotRepeat, 1f);
        return new MonsterMoveStateMachine(
            new List<MonsterState> { tome, hex, litany, rand },
            tome);
    }

    private async Task TomeStrikeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(TomeStrikeDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.15f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task HexMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.5f);
        await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), targets, HexVulnerable, base.Creature, null);
    }

    private async Task LitanyMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.5f);
        await CreatureCmd.GainBlock(base.Creature, LitanyBlock, ValueProp.Move, null);
    }
}
