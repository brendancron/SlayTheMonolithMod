using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;
using SlayTheMonolithMod.SlayTheMonolithModCode.Acts;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Events;

// Themed reskin of vanilla WoodCarvings for The Continent. The mechanics are
// preserved exactly:
//   - "The bird dish"   -> transform a chosen Basic card into Peck
//   - "The hidden herb" -> enchant a chosen card with Slither (1)
//   - "Doughy bread"    -> transform a chosen Basic card into Toric Toughness
// Same allowed-only-if-deck-has-basic-removable check; the Snake option is
// locked when no enchantable cards remain.
public sealed class VillageChef : CustomEventModel
{
    public override string? LocTable => "events";

    public override string? CustomInitialPortraitPath =>
        "res://SlayTheMonolithMod/images/village_chef.jpg";

    public override bool IsAllowed(IRunState runState)
    {
        if (runState.Act is not TheContinent) return false;
        return runState.Players.All(p =>
            CardPile.Get(PileType.Deck, p).Cards.Any(c =>
                c != null && c.Rarity == CardRarity.Basic && c.IsRemovable));
    }

    public override List<(string, string)>? Localization => new EventLoc(
        Title: "Village Chef",
        Pages: new[]
        {
            new EventPageLoc(
                PageKey: "INITIAL",
                Description: "The village \"chef\" stands hunched over a stew pot. She gestures at three recipes scrawled on the walls and offers to teach you one.",
                Options: new[]
                {
                    new EventOptionLoc("BIRD",        "The bird dish",   "Transform a Basic card into [card]Peck[/card]."),
                    new EventOptionLoc("HERB",        "The hidden herb", "Enchant a card with [enchantment]Slither[/enchantment]."),
                    new EventOptionLoc("HERB_LOCKED", "The hidden herb", "[No card here can be seasoned.]"),
                    new EventOptionLoc("BREAD",       "Doughy bread",    "Transform a Basic card into [card]Toric Toughness[/card]."),
                }),
            new EventPageLoc("BIRD",  "She plucks a card from your hand, roasts it on the spit, and hands it back.", Array.Empty<EventOptionLoc>()),
            new EventPageLoc("HERB",  "The seasoning sinks in. The card feels just a little sneakier in your hand.",  Array.Empty<EventOptionLoc>()),
            new EventPageLoc("BREAD", "She kneads a ring of dough around the card. It's somehow tougher now.",       Array.Empty<EventOptionLoc>()),
        });

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var cards = PileType.Deck.GetPile(Owner).Cards;
        bool canEnchant = cards.Any(c => ModelDb.Enchantment<Slither>().CanEnchant(c));

        var herbOption = canEnchant
            ? new EventOption(this, HiddenHerb, $"{Id.Entry}.pages.INITIAL.options.HERB",
                HoverTipFactory.FromEnchantment<Slither>())
            : new EventOption(this, null, $"{Id.Entry}.pages.INITIAL.options.HERB_LOCKED");

        return new[]
        {
            new EventOption(this, BirdDish,     $"{Id.Entry}.pages.INITIAL.options.BIRD",
                HoverTipFactory.FromCardWithCardHoverTips<Peck>()),
            herbOption,
            new EventOption(this, DoughyBread,  $"{Id.Entry}.pages.INITIAL.options.BREAD",
                HoverTipFactory.FromCardWithCardHoverTips<ToricToughness>()),
        };
    }

    private async Task BirdDish()
    {
        var picked = (await CardSelectCmd.FromDeckGeneric(
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1),
            c => c.IsTransformable && c.Rarity == CardRarity.Basic)).FirstOrDefault();
        if (picked != null)
        {
            await CardCmd.TransformTo<Peck>(picked, CardPreviewStyle.EventLayout);
        }
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.BIRD.description"));
    }

    private async Task HiddenHerb()
    {
        var picked = (await CardSelectCmd.FromDeckForEnchantment(
            Owner,
            ModelDb.Enchantment<Slither>(),
            1,
            new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1))).FirstOrDefault();
        if (picked != null)
        {
            CardCmd.Enchant<Slither>(picked, 1m);
            var vfx = NCardEnchantVfx.Create(picked);
            if (vfx != null)
            {
                ((Node?)NRun.Instance?.GlobalUi.CardPreviewContainer)?.AddChildSafely((Node?)(object)vfx);
            }
        }
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.HERB.description"));
    }

    private async Task DoughyBread()
    {
        var picked = (await CardSelectCmd.FromDeckGeneric(
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1),
            c => c != null && c.IsTransformable && c.Rarity == CardRarity.Basic)).FirstOrDefault();
        if (picked != null)
        {
            await CardCmd.TransformTo<ToricToughness>(picked, CardPreviewStyle.EventLayout);
        }
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.BREAD.description"));
    }
}
