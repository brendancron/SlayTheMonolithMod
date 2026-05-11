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

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

// Functionally identical to vanilla CalcifiedCultist except Incantation
// applies 3 Ritual instead of 2.
//   T1: Incantation (apply Ritual 3 to self)
//   T2+: Dark Strike (9 dmg) on repeat
//   38-41 HP.
public sealed class ReaperCultist : CustomMonsterModel, ILocalizationProvider
{
    private const string IncantationMoveId = "INCANTATION_MOVE";
    private const string DarkStrikeMoveId = "DARK_STRIKE_MOVE";

    public override int MinInitialHp => 38;
    public override int MaxInitialHp => 41;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/reaper_cultist.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Fur;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Reaper Cultist",
        MoveTitles: new[]
        {
            (IncantationMoveId, "Incantation"),
            (DarkStrikeMoveId, "Dark Strike"),
        });

    private int IncantationAmount => 3;
    private int DarkStrikeDamage => 9;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var incantation = new MoveState(IncantationMoveId, IncantationMove, new BuffIntent());
        var darkStrike = new MoveState(DarkStrikeMoveId, DarkStrikeMove, new SingleAttackIntent(DarkStrikeDamage));
        incantation.FollowUpState = darkStrike;
        darkStrike.FollowUpState = darkStrike;
        return new MonsterMoveStateMachine(
            new List<MonsterState> { incantation, darkStrike },
            incantation);
    }

    private async Task IncantationMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.5f);
        await PowerCmd.Apply<RitualPower>(new ThrowingPlayerChoiceContext(), base.Creature, IncantationAmount, base.Creature, null);
    }

    private async Task DarkStrikeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(DarkStrikeDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }
}
