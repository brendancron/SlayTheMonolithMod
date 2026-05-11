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

// Shard Throw deals a fresh roll of 4-6 each turn. The rolled value has to be
// stable across the player turn so the telegraphed intent matches what the
// move actually deals -- so we cache the roll in a mutable field, expose it
// to the intent via () => CachedShardDamage, then re-roll at the end of the
// move so the next intent re-render picks up a new value. AfterAddedToRoom
// primes the first turn's roll.
public sealed class Volester : CustomMonsterModel, ILocalizationProvider
{
    private const string MoveIdConst = "SHARD_THROW";

    // Reduced ~33% from 14-18 to 9-12 to compensate for the 50%-damage
    // mitigation Flying provides on combat start.
    public override int MinInitialHp => 9;
    public override int MaxInitialHp => 12;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/volester.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Stone;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Volester",
        MoveTitles: new[] { (MoveIdConst, "Shard Throw") });

    private int MinDamage => 4;
    private int MaxDamage => 6;

    public int _cachedShardDamage;
    public int CachedShardDamage
    {
        get => _cachedShardDamage;
        set { AssertMutable(); _cachedShardDamage = value; }
    }

    private int FlyingStacks => 3;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        CachedShardDamage = RollFreshDamage();
        await PowerCmd.Apply<Flying>(new ThrowingPlayerChoiceContext(), base.Creature, FlyingStacks, base.Creature, null);
    }

    private int RollFreshDamage() => base.RunRng.MonsterAi.NextInt(MinDamage, MaxDamage + 1);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var move = new MoveState(MoveIdConst, DoMove, new SingleAttackIntent(() => CachedShardDamage));
        move.FollowUpState = move;
        return new MonsterMoveStateMachine(new List<MonsterState> { move }, move);
    }

    private async Task DoMove(IReadOnlyList<Creature> targets)
    {
        int damage = CachedShardDamage;
        await DamageCmd.Attack(damage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.15f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        CachedShardDamage = RollFreshDamage();
    }
}
