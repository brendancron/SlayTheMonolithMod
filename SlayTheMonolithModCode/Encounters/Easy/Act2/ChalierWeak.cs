using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using SlayTheMonolithMod.SlayTheMonolithModCode.Acts;
using SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;

// Mirrors vanilla TunnelerWeak: single Chalier in an early-act slot.
public sealed class ChalierWeak : CustomEncounterModel, ILocalizationProvider
{
    public ChalierWeak() : base(RoomType.Monster) { }

    public override bool IsValidForAct(ActModel act) => act is TheAxons;

    public override bool IsWeak => true;

    public List<(string, string)>? Localization => new EncounterLoc(
        Title: "Chalier",
        LossText: "Buried alive beneath the dust.");

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Chalier>(),
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Chalier>().ToMutable(), null),
        };
}
