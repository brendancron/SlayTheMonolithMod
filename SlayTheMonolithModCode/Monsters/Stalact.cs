using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using SlayTheMonolithMod.SlayTheMonolithModCode.Powers;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

// Alt-path stand-in for vanilla SpinyToad. Same 3-move cycle (Protrude ->
// Explosion -> Lash), but its protective coating is SearingSkin instead of
// Thorns: attackers take Burn stacks rather than reflected damage. Protrude
// applies 5 SearingSkin; Explosion attacks and consumes 5 SearingSkin; Lash
// is a plain attack with the coating still up.
public sealed class Stalact : CustomMonsterModel, ILocalizationProvider
{
    private const string ProtrudeMoveId = "PROTRUDE_MOVE";
    private const string ExplosionMoveId = "EXPLOSION_MOVE";
    private const string LashMoveId = "LASH_MOVE";

    public override int MinInitialHp => 116;
    public override int MaxInitialHp => 119;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/stalact.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/spiny_toad/spiny_toad_lick";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Stalact",
        MoveTitles: new[]
        {
            (ProtrudeMoveId, "Searing Skin"),
            (ExplosionMoveId, "Eruption"),
            (LashMoveId, "Tongue Lash"),
        });

    private int LashDamage => 15;
    private int ExplosionDamage => 19;
    private int SearingStacks => 2;

    private bool _isSearing;
    public bool IsSearing
    {
        get => _isSearing;
        set { AssertMutable(); _isSearing = value; }
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var protrude = new MoveState(ProtrudeMoveId, ProtrudeMove, new BuffIntent());
        var explosion = new MoveState(ExplosionMoveId, ExplosionMove, new SingleAttackIntent(ExplosionDamage));
        var lash = new MoveState(LashMoveId, LashMove, new SingleAttackIntent(LashDamage));

        protrude.FollowUpState = explosion;
        explosion.FollowUpState = lash;
        lash.FollowUpState = protrude;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { protrude, explosion, lash },
            protrude);
    }

    private async Task ProtrudeMove(IReadOnlyList<Creature> targets)
    {
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/spiny_toad/spiny_toad_protrude");
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.5f);
        IsSearing = true;
        await PowerCmd.Apply<SearingSkin>(new ThrowingPlayerChoiceContext(), base.Creature, SearingStacks, base.Creature, null);
    }

    private async Task ExplosionMove(IReadOnlyList<Creature> targets)
    {
        IsSearing = false;
        await DamageCmd.Attack(ExplosionDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.7f)
            .WithAttackerFx(null, "event:/sfx/enemy/enemy_attacks/spiny_toad/spiny_toad_explode")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await PowerCmd.Apply<SearingSkin>(new ThrowingPlayerChoiceContext(), base.Creature, -SearingStacks, base.Creature, null);
        await Cmd.Wait(1f);
    }

    private async Task LashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(LashDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }
}
