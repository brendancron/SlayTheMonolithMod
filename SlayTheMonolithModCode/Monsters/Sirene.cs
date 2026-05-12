using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

// Act-2 boss for The Axons. Placeholder moveset and stats -- to be designed.
// Currently: simple two-move cycle (Slash -> Wail) so it's playable end-to-end
// without crashing. Real mechanics come later.
public sealed class Sirene : CustomMonsterModel, ILocalizationProvider
{
    private const string SlashMoveId = "SLASH_MOVE";
    private const string WailMoveId = "WAIL_MOVE";

    public override int MinInitialHp => 250;
    public override int MaxInitialHp => 250;

    public override string CustomAttackSfx =>
        "event:/sfx/enemy/enemy_attacks/axebot/axebot_attack_spin";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Magic;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Sirene",
        MoveTitles: new[]
        {
            (SlashMoveId, "Slash"),
            (WailMoveId, "Wail"),
        });

    private int SlashDamage => 18;
    private int WailDamage => 8;
    private int WailHits => 3;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var slash = new MoveState(SlashMoveId, SlashMove, new SingleAttackIntent(SlashDamage));
        var wail = new MoveState(WailMoveId, WailMove, new MultiAttackIntent(WailDamage, WailHits));

        slash.FollowUpState = wail;
        wail.FollowUpState = slash;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { slash, wail },
            slash);
    }

    private async Task SlashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SlashDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task WailMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(WailDamage)
            .WithHitCount(WailHits)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }
}
