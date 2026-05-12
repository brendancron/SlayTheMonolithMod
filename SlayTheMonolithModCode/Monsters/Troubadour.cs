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

// Alt-path Exoskeleton mirror. Three-monster fight where each opens with a
// different move based on slot (first/second/third). Skitter and Mandibles
// rotate via a random branch; Mandibles always sets up Enrage to give itself
// Strength +2. HardToKill 9 on entry matches vanilla Exoskeleton.
public sealed class Troubadour : CustomMonsterModel, ILocalizationProvider
{
    private const string SkitterMoveId = "SKITTER_MOVE";
    private const string MandiblesMoveId = "MANDIBLES_MOVE";
    private const string EnrageMoveId = "ENRAGE_MOVE";

    public override int MinInitialHp => 24;
    public override int MaxInitialHp => 28;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/troubadour.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/roaches/roaches_attack";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Magic;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Troubadour",
        MoveTitles: new[]
        {
            (SkitterMoveId, "Refrain"),
            (MandiblesMoveId, "Crescendo"),
            (EnrageMoveId, "Bravura"),
        });

    private int SkitterDamage => 1;
    private int SkitterRepeats => 3;
    private int MandiblesDamage => 8;
    private int EnrageStrength => 2;
    private int HardToKillStacks => 9;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<HardToKillPower>(
            new ThrowingPlayerChoiceContext(), base.Creature, HardToKillStacks, base.Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var skitter = new MoveState(SkitterMoveId, SkitterMove, new MultiAttackIntent(SkitterDamage, SkitterRepeats));
        var mandibles = new MoveState(MandiblesMoveId, MandiblesMove, new SingleAttackIntent(MandiblesDamage));
        var enrage = new MoveState(EnrageMoveId, EnrageMove, new BuffIntent());

        var rand = new RandomBranchState("RAND");
        rand.AddBranch(skitter, MoveRepeatType.CannotRepeat, 1f);
        rand.AddBranch(mandibles, MoveRepeatType.CannotRepeat, 1f);

        var init = new ConditionalBranchState("INIT_MOVE");
        init.AddState(skitter, () => base.Creature.SlotName == "first");
        init.AddState(mandibles, () => base.Creature.SlotName == "second");
        init.AddState(enrage, () => base.Creature.SlotName == "third");
        init.AddState(rand, () => true);

        skitter.FollowUpState = rand;
        mandibles.FollowUpState = enrage;
        enrage.FollowUpState = rand;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { init, rand, skitter, mandibles, enrage },
            init);
    }

    private async Task SkitterMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SkitterDamage)
            .WithHitCount(SkitterRepeats)
            .FromMonster(this)
            .OnlyPlayAnimOnce()
            .WithAttackerAnim("Attack", 0.15f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task MandiblesMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(MandiblesDamage)
            .FromMonster(this)
            .WithAttackerAnim("HeavyAttack", 0.3f)
            .WithAttackerFx(null, "event:/sfx/enemy/enemy_attacks/roaches/roaches_attack_heavy")
            .WithHitFx("vfx/vfx_bite")
            .Execute(null);
    }

    private async Task EnrageMove(IReadOnlyList<Creature> targets)
    {
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/roaches/roaches_buff");
        await CreatureCmd.TriggerAnim(base.Creature, "Buff", 0.3f);
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(), base.Creature, EnrageStrength, base.Creature, null);
    }
}