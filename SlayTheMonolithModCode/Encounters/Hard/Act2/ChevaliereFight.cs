using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using SlayTheMonolithMod.SlayTheMonolithModCode.Acts;
using SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;

// Alt-path HunterKiller fight: a single Chevaliere in TheAxons normal pool.
// Solo big HP pool (121) opening with Tender 1, then alternating between
// Bite (17) and Puncture (7x3) -- Puncture weighted 2x, Bite cannot repeat
// back-to-back.
public sealed class ChevaliereFight : CustomEncounterModel, ILocalizationProvider
{
    public ChevaliereFight() : base(RoomType.Monster) { }

    public override bool IsValidForAct(ActModel act) => act is TheAxons;

    public List<(string, string)>? Localization => new EncounterLoc(
        Title: "Chevaliere",
        LossText: "Run through by the Chevaliere's lance.");

    public override bool IsWeak => false;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Chevaliere>(),
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Chevaliere>().ToMutable(), null),
        };
}
