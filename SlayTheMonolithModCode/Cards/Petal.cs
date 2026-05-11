using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Cards;

// Status card shuffled into the discard pile by Flower's Pollen move.
// Unplayable + Ethereal so it auto-exhausts at end of turn. Just before
// exhaust, each Petal in hand heals every living Goblu in the encounter for 5.
[Pool(typeof(StatusCardPool))]
public sealed class Petal : CustomCardModel
{
    private const int HealPerPetal = 5;

    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[]
    {
        CardKeyword.Unplayable,
        CardKeyword.Ethereal,
    };

    public override bool HasTurnEndInHandEffect => true;

    public Petal()
        : base(-1, CardType.Status, CardRarity.Status, TargetType.None,
               showInCardLibrary: false)
    { }

    public override List<(string, string)>? Localization => new CardLoc(
        Title: "Petal",
        Description: "Unplayable. Ethereal. At the end of your turn, while in hand, heals Goblu for 5.");

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        foreach (var enemy in base.Owner.Creature.CombatState.Enemies)
        {
            if (enemy.IsAlive && enemy.Monster is Goblu)
            {
                await CreatureCmd.Heal(enemy, HealPerPetal);
            }
        }
    }
}
