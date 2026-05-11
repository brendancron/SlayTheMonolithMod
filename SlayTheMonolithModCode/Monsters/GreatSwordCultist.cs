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

// Heavy cultist that opens by buffing itself and then trades attacks for
// armor:
//   T1: Incant (apply Ritual 1 to self)
//   T2: Slash (1x3)
//   T3: Forge (deal 10 self-damage, gain 20 block)
//   T4: Slash, T5: Forge, ...
//   51-53 HP. Forge's self-damage uses ValueProp.Unblockable | Unpowered so
//   it ignores its own block and Strength.
public sealed class GreatSwordCultist : CustomMonsterModel, ILocalizationProvider
{
    private const string IncantMoveId = "INCANT_MOVE";
    private const string SlashMoveId = "SLASH_MOVE";
    private const string ForgeMoveId = "FORGE_MOVE";

    public override int MinInitialHp => 51;
    public override int MaxInitialHp => 53;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/great_sword_cultist.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Great Sword Cultist",
        MoveTitles: new[]
        {
            (IncantMoveId, "Incant"),
            (SlashMoveId, "Slash"),
            (ForgeMoveId, "Forge"),
        });

    private int IncantRitual => 1;
    private int SlashDamage => 1;
    private int SlashHits => 3;
    private int ForgeSelfDamage => 10;
    private int ForgeBlock => 20;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var incant = new MoveState(IncantMoveId, IncantMove, new BuffIntent());
        var slash = new MoveState(SlashMoveId, SlashMove, new MultiAttackIntent(SlashDamage, SlashHits));
        var forge = new MoveState(ForgeMoveId, ForgeMove, new DefendIntent());

        incant.FollowUpState = slash;
        slash.FollowUpState = forge;
        forge.FollowUpState = slash;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { incant, slash, forge },
            incant);
    }

    private async Task IncantMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.5f);
        await PowerCmd.Apply<RitualPower>(new ThrowingPlayerChoiceContext(), base.Creature, IncantRitual, base.Creature, null);
    }

    private async Task SlashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SlashDamage)
            .WithHitCount(SlashHits)
            .FromMonster(this)
            .OnlyPlayAnimOnce()
            .WithAttackerAnim("Attack", 0.2f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task ForgeMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.3f);
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            base.Creature,
            ForgeSelfDamage,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);
        if (!base.Creature.IsAlive) return;
        await CreatureCmd.GainBlock(base.Creature, ForgeBlock, ValueProp.Move, null);
    }
}
