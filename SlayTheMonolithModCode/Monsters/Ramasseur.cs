using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

// Alt-path stand-in for vanilla Chomper. Pair fight: one Ramasseur opens with
// Gather (8x2 attack), the other opens with Howl (adds 3 Dazed to discard).
// Each cycles between the two moves. 2 Artifact stacks on entry, mirroring
// Chomper's debuff resistance.
public sealed class Ramasseur : CustomMonsterModel, ILocalizationProvider
{
    private const string GatherMoveId = "GATHER_MOVE";
    private const string HowlMoveId = "HOWL_MOVE";

    public override int MinInitialHp => 60;
    public override int MaxInitialHp => 64;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/ramasseur.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Ramasseur",
        MoveTitles: new[]
        {
            (GatherMoveId, "Gather"),
            (HowlMoveId, "Howl"),
        });

    private int GatherDamage => 8;
    private int GatherHitCount => 2;
    private int HowlDazedCount => 3;
    private int InitialArtifact => 2;

    public bool _howlsFirst;

    public bool HowlsFirst
    {
        get => _howlsFirst;
        set { AssertMutable(); _howlsFirst = value; }
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<ArtifactPower>(new ThrowingPlayerChoiceContext(), base.Creature, InitialArtifact, base.Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var gather = new MoveState(GatherMoveId, GatherMove, new MultiAttackIntent(GatherDamage, GatherHitCount));
        var howl = new MoveState(HowlMoveId, HowlMove, new StatusIntent(HowlDazedCount));

        gather.FollowUpState = howl;
        howl.FollowUpState = gather;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { gather, howl },
            HowlsFirst ? (MonsterState)howl : gather);
    }

    private async Task GatherMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(GatherDamage)
            .WithHitCount(GatherHitCount)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .OnlyPlayAnimOnce()
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task HowlMove(IReadOnlyList<Creature> targets)
    {
        SfxCmd.Play(CastSfx);
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 1f);
        await CardPileCmd.AddToCombatAndPreview<Dazed>(targets, PileType.Discard, HowlDazedCount, null);
    }
}
