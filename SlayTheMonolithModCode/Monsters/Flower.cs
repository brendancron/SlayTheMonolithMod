using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using SlayTheMonolithMod.SlayTheMonolithModCode.Cards;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

// Minion summoned by Goblu. Single move every turn: shuffle 1 Dazed into the
// player's draw pile.
public sealed class Flower : CustomMonsterModel, ILocalizationProvider
{
    private const string PollenMoveId = "POLLEN_MOVE";

    public override int MinInitialHp => 8;
    public override int MaxInitialHp => 11;

    public override string CustomVisualPath =>
        "res://SlayTheMonolithMod/scenes/creature_visuals/flower.tscn";

    public override NCreatureVisuals? CreateCustomVisuals() =>
        CustomVisualPath is { } path
            ? NodeFactory<NCreatureVisuals>.CreateFromScene(path)
            : null;

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Magic;

    public List<(string, string)>? Localization => new MonsterLoc(
        Name: "Flower",
        MoveTitles: new[] { (PollenMoveId, "Pollen") });

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var pollen = new MoveState(PollenMoveId, PollenMove, new StatusIntent(1));
        pollen.FollowUpState = pollen;
        return new MonsterMoveStateMachine(new List<MonsterState> { pollen }, pollen);
    }

    private async Task PollenMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.5f);
        foreach (Creature target in targets)
        {
            Player player = target.Player ?? target.PetOwner;
            CardModel card = base.CombatState.CreateCard<Petal>(player);
            var result = await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Discard, null, CardPilePosition.Random);
            if (LocalContext.IsMe(player))
            {
                CardCmd.PreviewCardPileAdd(result);
                await Cmd.Wait(1f);
            }
        }
    }
}
