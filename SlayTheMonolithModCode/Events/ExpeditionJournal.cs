using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;
using SlayTheMonolithMod.SlayTheMonolithModCode.Acts;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Events;

// Themed reskin of vanilla SelfHelpBook for The Continent:
//   "In front of you lies an old expedition journal."
//   - Study the maps          -> enchant a chosen Attack with Sharp 2
//   - Read the field notes    -> enchant a chosen Skill  with Nimble 2
//   - Pore over the journal   -> enchant a chosen Power  with Swift 2
// Each option is locked if the player has no enchantable cards of that type;
// if all three are locked, a single fall-through "leave it" option appears.
public sealed class ExpeditionJournal : CustomEventModel
{
    private const int EnchantAmount = 2;

    // BaseLib's ModelLocPatch needs this hint to inject our Localization
    // entries into the right table -- the default category dictionary doesn't
    // include events.
    public override string? LocTable => "events";

    // Portrait override. Without this, NEventRoom.SetupLayout looks for
    // res://images/events/<id>.png and throws AssetLoadException when it
    // doesn't exist -- the event screen renders blank as a result.
    public override string? CustomInitialPortraitPath =>
        "res://SlayTheMonolithMod/images/expedition_journal.jpg";

    // Gate to TheContinent at RUN TIME via IsAllowed instead of populating
    // the Acts array. Overriding Acts with `ModelDb.Act<TheContinent>()`
    // would throw KeyNotFoundException during the ctor: CustomContentDictionary
    // .AddEvent reads Acts.Length from inside the base constructor, but at
    // that point ModelDb.Init may not have constructed TheContinent yet
    // (model construction order is reflection-enumeration-dependent).
    // Leaving Acts empty buckets us as a "shared" event; IsAllowed below is
    // the real gate, and it's only evaluated at event-selection time when
    // every model is fully registered.
    public override bool IsAllowed(IRunState runState) =>
        runState.Act is TheContinent;

    public override List<(string, string)>? Localization => new EventLoc(
        Title: "Expedition Journal",
        Pages: new[]
        {
            new EventPageLoc(
                PageKey: "INITIAL",
                Description: "In front of you lies an old expedition journal.",
                Options: new[]
                {
                    new EventOptionLoc("STUDY_MAPS",          "Study the maps",          "Enchant a chosen Attack with 2 Sharp."),
                    new EventOptionLoc("STUDY_MAPS_LOCKED",   "Study the maps",          "[You have no enchantable Attack cards.]"),
                    new EventOptionLoc("READ_FIELD_NOTES",    "Read the field notes",    "Enchant a chosen Skill with 2 Nimble."),
                    new EventOptionLoc("READ_FIELD_NOTES_LOCKED", "Read the field notes","[You have no enchantable Skill cards.]"),
                    new EventOptionLoc("PORE_OVER_JOURNAL",   "Pore over the journal",   "Enchant a chosen Power with 2 Swift."),
                    new EventOptionLoc("PORE_OVER_JOURNAL_LOCKED","Pore over the journal","[You have no enchantable Power cards.]"),
                    new EventOptionLoc("LEAVE_IT",            "Leave it be",             "Nothing here to learn from."),
                }),
            new EventPageLoc("STUDY_MAPS",       "You commit the maps to memory.",          Array.Empty<EventOptionLoc>()),
            new EventPageLoc("READ_FIELD_NOTES", "The marginalia teach you a trick or two.", Array.Empty<EventOptionLoc>()),
            new EventPageLoc("PORE_OVER_JOURNAL","Hours pass. The patterns begin to make sense.", Array.Empty<EventOptionLoc>()),
            new EventPageLoc("LEAVE_IT",         "You close the journal and walk on.",       Array.Empty<EventOptionLoc>()),
        });

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>();
        bool hasAttack = PlayerHasCardsAvailable<Sharp>(Owner, CardType.Attack);
        bool hasSkill  = PlayerHasCardsAvailable<Nimble>(Owner, CardType.Skill);
        bool hasPower  = PlayerHasCardsAvailable<Swift>(Owner, CardType.Power);

        if (hasAttack || hasSkill || hasPower)
        {
            options.Add(hasAttack
                ? new EventOption(this, StudyMaps,        $"{Id.Entry}.pages.INITIAL.options.STUDY_MAPS",        HoverTipFactory.FromEnchantment<Sharp>(EnchantAmount))
                : new EventOption(this, null,             $"{Id.Entry}.pages.INITIAL.options.STUDY_MAPS_LOCKED"));
            options.Add(hasSkill
                ? new EventOption(this, ReadFieldNotes,   $"{Id.Entry}.pages.INITIAL.options.READ_FIELD_NOTES",  HoverTipFactory.FromEnchantment<Nimble>(EnchantAmount))
                : new EventOption(this, null,             $"{Id.Entry}.pages.INITIAL.options.READ_FIELD_NOTES_LOCKED"));
            options.Add(hasPower
                ? new EventOption(this, PoreOverJournal,  $"{Id.Entry}.pages.INITIAL.options.PORE_OVER_JOURNAL", HoverTipFactory.FromEnchantment<Swift>(EnchantAmount))
                : new EventOption(this, null,             $"{Id.Entry}.pages.INITIAL.options.PORE_OVER_JOURNAL_LOCKED"));
        }
        else
        {
            options.Add(new EventOption(this, LeaveIt, $"{Id.Entry}.pages.INITIAL.options.LEAVE_IT"));
        }

        return options;
    }

    private Task StudyMaps()        => SelectAndEnchant<Sharp>(CardType.Attack, L10NLookup($"{Id.Entry}.pages.STUDY_MAPS.description"));
    private Task ReadFieldNotes()   => SelectAndEnchant<Nimble>(CardType.Skill, L10NLookup($"{Id.Entry}.pages.READ_FIELD_NOTES.description"));
    private Task PoreOverJournal()  => SelectAndEnchant<Swift>(CardType.Power, L10NLookup($"{Id.Entry}.pages.PORE_OVER_JOURNAL.description"));

    private Task LeaveIt()
    {
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.LEAVE_IT.description"));
        return Task.CompletedTask;
    }

    private bool PlayerHasCardsAvailable<T>(Player player, CardType typeRestriction) where T : EnchantmentModel
    {
        var enchantment = ModelDb.Enchantment<T>();
        return PileType.Deck.GetPile(player).Cards.FirstOrDefault(c => DeckFilter(c, enchantment, typeRestriction)) != null;
    }

    private async Task SelectAndEnchant<T>(CardType typeRestriction, LocString finalDescription) where T : EnchantmentModel
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1);
        var enchantment = ModelDb.Enchantment<T>();
        var picked = (await CardSelectCmd.FromDeckForEnchantment(
            Owner, enchantment, EnchantAmount,
            c => c.Type == typeRestriction, prefs)).FirstOrDefault();
        if (picked != null)
        {
            CardCmd.Enchant<T>(picked, EnchantAmount);
            var vfx = NCardEnchantVfx.Create(picked);
            if (vfx != null)
            {
                ((Node?)NRun.Instance?.GlobalUi.CardPreviewContainer)?.AddChildSafely((Node?)(object)vfx);
            }
        }
        SetEventFinished(finalDescription);
    }

    private bool DeckFilter(CardModel card, EnchantmentModel enchantment, CardType type)
    {
        if (card.Pile.Type != PileType.Deck) return false;
        if (card.Type != type) return false;
        return enchantment.CanEnchant(card);
    }
}
