using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using SlayTheMonolithMod.SlayTheMonolithModCode.Acts;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Events;

// Themed reskin of vanilla DoorsOfLightAndDark for The Continent:
//   "Two staircases rise from the manor's grand hall."
//   - Lit staircase     -> upgrade 2 random upgradable cards from the deck
//   - Shadowed staircase -> remove a card of the player's choice from the deck
// Same gating pattern as ExpeditionJournal: empty Acts (shared bucket) +
// IsAllowed gate by act, to avoid the ctor-time ModelDb lookup crash.
public sealed class TheManor : CustomEventModel
{
    private const int UpgradeCount = 2;

    public override string? LocTable => "events";

    public override string? CustomInitialPortraitPath =>
        "res://SlayTheMonolithMod/images/the_manor.png";

    public override bool IsAllowed(IRunState runState) =>
        runState.Act is TheContinent;

    public override List<(string, string)>? Localization => new EventLoc(
        Title: "The Manor",
        Pages: new[]
        {
            new EventPageLoc(
                PageKey: "INITIAL",
                Description: "You step into the manor's grand hall. Two staircases rise from the entryway -- one lit by a warm chandelier, the other receding into shadow.",
                Options: new[]
                {
                    new EventOptionLoc("LIT_STAIRCASE",      "Climb the lit staircase",      "Upgrade 2 random cards in your deck."),
                    new EventOptionLoc("SHADOWED_STAIRCASE", "Take the shadowed staircase",  "Remove a card from your deck."),
                }),
            new EventPageLoc("LIT_STAIRCASE",      "Light pools at each step. Two of your cards feel sharper for it.", Array.Empty<EventOptionLoc>()),
            new EventPageLoc("SHADOWED_STAIRCASE", "The dark takes something from you. The weight in your hands is lighter.", Array.Empty<EventOptionLoc>()),
        });

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
        new[]
        {
            new EventOption(this, LitStaircase,      $"{Id.Entry}.pages.INITIAL.options.LIT_STAIRCASE"),
            new EventOption(this, ShadowedStaircase, $"{Id.Entry}.pages.INITIAL.options.SHADOWED_STAIRCASE"),
        };

    private Task LitStaircase()
    {
        var toUpgrade = PileType.Deck.GetPile(Owner).Cards
            .Where(c => c?.IsUpgradable ?? false)
            .ToList()
            .StableShuffle(Owner.RunState.Rng.Niche)
            .Take(UpgradeCount);
        foreach (var card in toUpgrade)
        {
            CardCmd.Upgrade(card);
        }
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.LIT_STAIRCASE.description"));
        return Task.CompletedTask;
    }

    private async Task ShadowedStaircase()
    {
        var picked = (await CardSelectCmd.FromDeckForRemoval(
            player: Owner,
            prefs: new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1))).ToList();
        await CardPileCmd.RemoveFromDeck(picked);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.SHADOWED_STAIRCASE.description"));
    }
}
