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

// Functionally identical to vanilla LouseProgenitor. 134-136 HP solo.
// Opens with Web Cannon (9 atk + Frail 2), then Curl and Grow (14 block +
// Strength 5), then Pounce (14), looping back to Web. CurlUpPower with 14
// stacks on entry. Curled flag drives the Uncurl animation before any
// post-curl attack, same as vanilla.
public sealed class Bouchelier : CustomMonsterModel, ILocalizationProvider
{
    private const string WebMoveId = "WEB_CANNON_MOVE";
    private const string CurlMoveId = "CURL_AND_GROW_MOVE";
    private const string PounceMoveId = "POUNCE_MOVE";

    public override int MinInitialHp => 134;
    public override int MaxInitialHp => 136;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/bouchelier.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override string CustomAttackSfx => "event:/sfx/enemy/enemy_attacks/giant_louse/giant_louse_attack";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Bouchelier",
        MoveTitles: new[]
        {
            (WebMoveId, "Web Cannon"),
            (CurlMoveId, "Curl and Grow"),
            (PounceMoveId, "Pounce"),
        });

    private int WebDamage => 9;
    private int WebFrail => 2;
    private int PounceDamage => 14;
    private int CurlBlock => 14;
    private int GrowStrength => 5;

    private bool _curled;
    public bool Curled
    {
        get => _curled;
        set { AssertMutable(); _curled = value; }
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<CurlUpPower>(new ThrowingPlayerChoiceContext(), base.Creature, CurlBlock, base.Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var web = new MoveState(WebMoveId, WebMove, new SingleAttackIntent(WebDamage), new DebuffIntent());
        var curl = new MoveState(CurlMoveId, CurlAndGrowMove, new DefendIntent(), new BuffIntent());
        var pounce = new MoveState(PounceMoveId, PounceMove, new SingleAttackIntent(PounceDamage));

        web.FollowUpState = curl;
        curl.FollowUpState = pounce;
        pounce.FollowUpState = web;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { curl, web, pounce },
            web);
    }

    private async Task WebMove(IReadOnlyList<Creature> targets)
    {
        if (Curled)
        {
            SfxCmd.Play("event:/sfx/enemy/enemy_attacks/giant_louse/giant_louse_uncurl");
            await CreatureCmd.TriggerAnim(base.Creature, "Uncurl", 0.9f);
            Curled = false;
        }
        await DamageCmd.Attack(WebDamage)
            .FromMonster(this)
            .WithAttackerAnim("Web", 0.2f)
            .WithAttackerFx(null, "event:/sfx/enemy/enemy_attacks/giant_louse/giant_louse_attack_web")
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(), targets, WebFrail, base.Creature, null);
    }

    private async Task CurlAndGrowMove(IReadOnlyList<Creature> targets)
    {
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/giant_louse/giant_louse_curl");
        await CreatureCmd.TriggerAnim(base.Creature, "Curl", 0.25f);
        await CreatureCmd.GainBlock(base.Creature, CurlBlock, ValueProp.Move, null);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, GrowStrength, base.Creature, null);
        Curled = true;
    }

    private async Task PounceMove(IReadOnlyList<Creature> targets)
    {
        if (Curled)
        {
            SfxCmd.Play("event:/sfx/enemy/enemy_attacks/giant_louse/giant_louse_uncurl");
            await CreatureCmd.TriggerAnim(base.Creature, "Uncurl", 0.9f);
            Curled = false;
        }
        await DamageCmd.Attack(PounceDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithAttackerFx(null, AttackSfx)
            .Execute(null);
    }
}
