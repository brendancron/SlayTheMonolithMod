using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Random;
using SlayTheMonolithMod.SlayTheMonolithModCode.Powers;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

// Move cycle cribbed from vanilla CorpseSlug, but with two divergences:
//   - No RavenousPower (vanilla slugs stun + buff each other when an ally
//     dies; we don't want that).
//   - Explode-on-death: when a Demineur dies, it deals ExplodeDamage to
//     every other living creature in the encounter (other Demineurs AND
//     the player). Chain reactions are intentional -- a Demineur killed
//     by another's explosion will also explode in turn.
//   Cycle: WhipSlap (3x2) -> Glomp (8) -> Goop (Frail 2) -> WhipSlap ...
//   25-27 HP. StarterMoveIdx (0/1/2) sets which move each Demineur begins
//   with so the trio doesn't telegraph the same intent on T1;
//   EnsureDemineursStartWithDifferentMoves is called by the encounter.
public sealed class Demineur : CustomMonsterModel, ILocalizationProvider
{
    private const string WhipSlapMoveId = "WHIP_SLAP_MOVE";
    private const string GlompMoveId = "GLOMP_MOVE";
    private const string GoopMoveId = "GOOP_MOVE";

    public override int MinInitialHp => 25;
    public override int MaxInitialHp => 27;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/demineur.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Slime;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Demineur",
        MoveTitles: new[]
        {
            (WhipSlapMoveId, "Whip Slap"),
            (GlompMoveId, "Glomp"),
            (GoopMoveId, "Goop"),
        });

    private int WhipSlapDamage => 3;
    private int WhipSlapRepeat => 2;
    private int GlompDamage => 8;
    private int GoopFrailAmt => 2;
    private int ExplodeDamage => 8;

    public int _starterMoveIdx;
    public int StarterMoveIdx
    {
        get => _starterMoveIdx;
        set { AssertMutable(); _starterMoveIdx = value; }
    }

    // Apply the Explosive power so AfterDeath fires through the registered
    // power-hook system. (Overriding AbstractModel.AfterDeath on the monster
    // model itself doesn't reliably fire when the creature dies; powers do.)
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<Explosive>(new ThrowingPlayerChoiceContext(), base.Creature, ExplodeDamage, base.Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var whip = new MoveState(WhipSlapMoveId, WhipSlapMove, new MultiAttackIntent(WhipSlapDamage, WhipSlapRepeat));
        var glomp = new MoveState(GlompMoveId, GlompMove, new SingleAttackIntent(GlompDamage));
        var goop = new MoveState(GoopMoveId, GoopMove, new DebuffIntent());

        whip.FollowUpState = glomp;
        glomp.FollowUpState = goop;
        goop.FollowUpState = whip;

        MonsterState initial = (StarterMoveIdx % 3) switch
        {
            0 => whip,
            1 => glomp,
            _ => goop,
        };

        return new MonsterMoveStateMachine(
            new List<MonsterState> { whip, glomp, goop },
            initial);
    }

    private async Task WhipSlapMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(WhipSlapDamage)
            .WithHitCount(WhipSlapRepeat)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .OnlyPlayAnimOnce()
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task GlompMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(GlompDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task GoopMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.2f);
        await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(), targets, GoopFrailAmt, base.Creature, null);
    }

    // Mirrors CorpseSlug.EnsureCorpseSlugsStartWithDifferentMoves -- assigns
    // each Demineur in the encounter a different StarterMoveIdx so the trio
    // telegraphs three different intents on round 1. Called from the encounter
    // after GenerateMonsters builds the list.
    public static void EnsureDemineursStartWithDifferentMoves(IEnumerable<MonsterModel> monsters, Rng rng)
    {
        var demineurs = monsters.OfType<Demineur>().ToList();
        int start = rng.NextInt(3);
        for (int i = 0; i < demineurs.Count; i++)
        {
            demineurs[i].StarterMoveIdx = (start + i) % 3;
        }
    }
}
