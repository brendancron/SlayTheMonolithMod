using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using SlayTheMonolithMod.SlayTheMonolithModCode.Acts;
using SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;

// Alt-path LouseProgenitor fight: a single Bouchelier in TheAxons normal pool.
// Solo big HP pool (134-136), CurlUp 14 on entry. Web -> Curl -> Pounce loop.
public sealed class BouchelierFight : CustomEncounterModel, ILocalizationProvider
{
    public BouchelierFight() : base(RoomType.Monster) { }

    public override bool IsValidForAct(ActModel act) => act is TheAxons;

    public List<(string, string)>? Localization => new EncounterLoc(
        Title: "Bouchelier",
        LossText: "Crushed beneath the Bouchelier's bulk.");

    public override bool IsWeak => false;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Bouchelier>(),
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Bouchelier>().ToMutable(), null),
        };
}
