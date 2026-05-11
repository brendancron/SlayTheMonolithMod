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

// Snake-Plant-style behaviour (STS1):
//   - 65% chance Chomp (6 x 3 dmg). Max 2 in a row.
//   - 35% chance Spores (apply Weak 2). No two in a row.
//   - No passive (vanilla Snake Plant's Malleable is intentionally dropped).
//   63-65 HP.
public sealed class Rocher : CustomMonsterModel, ILocalizationProvider
{
    private const string ChompMoveId = "CHOMP_MOVE";
    private const string SporesMoveId = "SPORES_MOVE";

    public override int MinInitialHp => 63;
    public override int MaxInitialHp => 65;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/rocher.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Stone;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Rocher",
        MoveTitles: new[]
        {
            (ChompMoveId, "Chomp"),
            (SporesMoveId, "Enfeebling Spores"),
        });

    private int ChompDamage => 6;
    private int ChompHits => 3;
    private int SporesWeak => 2;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<ArtifactPower>(new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var chomp = new MoveState(ChompMoveId, ChompMove, new MultiAttackIntent(ChompDamage, ChompHits));
        var spores = new MoveState(SporesMoveId, SporesMove, new DebuffIntent());

        var rand = new RandomBranchState("RAND");
        chomp.FollowUpState = rand;
        spores.FollowUpState = rand;

        // Chomp: weight 0.65, max 2 consecutive (i.e. cannot fire on the
        // third consecutive turn). Spores: weight 0.35, CannotRepeat (no two
        // in a row). RandomBranchState handles the repeat-tracking via the
        // StateLog so we don't need bookkeeping.
        rand.AddBranch(chomp, 2, 0.65f);
        rand.AddBranch(spores, MoveRepeatType.CannotRepeat, 0.35f);

        return new MonsterMoveStateMachine(
            new List<MonsterState> { rand, chomp, spores },
            rand);
    }

    private async Task ChompMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(ChompDamage)
            .WithHitCount(ChompHits)
            .FromMonster(this)
            .OnlyPlayAnimOnce()
            .WithAttackerAnim("Attack", 0.2f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task SporesMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.5f);
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets, SporesWeak, base.Creature, null);
    }
}
