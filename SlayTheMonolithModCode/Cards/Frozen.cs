using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Cards;

// Status card spawned in the player's hand by the Frostbite power. Unplayable
// means you can't cast it; Retain means it doesn't get discarded at end of
// turn so it just clogs the hand until something exhausts it.
//
// Registers in vanilla's StatusCardPool via the [Pool] attribute. Without a
// pool, CardModel.Pool falls back to MockCardPool which calls the test-only
// NeverEverCallThisOutsideOfTests_ClearOwner — that throws "You monster!" on
// transformations into a Frozen card. The pool registration only adds Frozen
// to the status pool's id list (so the Pool getter resolves) — it doesn't
// make Frozen appear as a reward, since status cards aren't drafted anyway.
[Pool(typeof(StatusCardPool))]
public sealed class Frozen : CustomCardModel
{
    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[]
    {
        CardKeyword.Unplayable,
        CardKeyword.Retain,
    };

    public Frozen()
        : base(-1, CardType.Status, CardRarity.Status, TargetType.None,
               showInCardLibrary: false)
    { }

    public override List<(string, string)>? Localization => new CardLoc(
        Title: "Frozen",
        Description: "Unplayable. Retain.");
}
