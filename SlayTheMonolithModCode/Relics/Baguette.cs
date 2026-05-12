using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Relics;

// Functionally identical to vanilla ChosenCheese: at the end of every combat,
// gain 1 max HP. Awarded as the combat-reward drop of the MysteriousMime
// event.
[Pool(typeof(EventRelicPool))]
public sealed class Baguette : CustomRelicModel
{
    private const int MaxHpPerCombat = 1;

    public override RelicRarity Rarity => RelicRarity.Event;

    public override List<(string, string)>? Localization => new RelicLoc(
        Title: "Baguette",
        Description: $"At the end of every combat, gain {MaxHpPerCombat} Max HP.",
        Flavor: "Stale on the outside, surprisingly nourishing within.");

    public override async Task AfterCombatEnd(CombatRoom _)
    {
        Flash();
        await CreatureCmd.GainMaxHp(Owner.Creature, MaxHpPerCombat);
    }
}
