using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Relics;

// Reward from the OnoPuncho event when the player one-shots him.
//   "The first Attack costing 2+ energy you play each combat is played twice."
// Mirrors vanilla ThrowingAxe's once-per-combat play-twice scaffold, plus an
// Attack-type + cost>=2 filter.
[Pool(typeof(EventRelicPool))]
public sealed class OnosGlove : CustomRelicModel
{
    private const int MinCost = 2;

    public bool _usedThisCombat;

    public override RelicRarity Rarity => RelicRarity.Event;

    public override List<(string, string)>? Localization => new RelicLoc(
        Title: "Ono's Glove",
        Description: $"The first Attack costing {MinCost} or more energy that you play each combat is played twice.",
        Flavor: "Still warm from his fist.");

    public bool UsedThisCombat
    {
        get => _usedThisCombat;
        set { AssertMutable(); _usedThisCombat = value; }
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is CombatRoom)
        {
            UsedThisCombat = false;
            Status = RelicStatus.Active;
        }
        return Task.CompletedTask;
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (UsedThisCombat) return playCount;
        if (card.Owner != Owner) return playCount;
        if (card.Type != CardType.Attack) return playCount;
        if (card.EnergyCost.GetAmountToSpend() < MinCost) return playCount;
        return playCount + 1;
    }

    public override Task AfterModifyingCardPlayCount(CardModel card)
    {
        UsedThisCombat = true;
        Flash();
        Status = RelicStatus.Normal;
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        UsedThisCombat = false;
        Status = RelicStatus.Normal;
        return Task.CompletedTask;
    }
}
