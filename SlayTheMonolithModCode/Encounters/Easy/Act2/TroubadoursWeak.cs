using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using SlayTheMonolithMod.SlayTheMonolithModCode.Acts;
using SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;

// Alt-path 3-Exoskeletons mirror. Three Troubadours opening Refrain / Crescendo
// / Bravura by slot. Slots override is mandatory: without it Troubadour's
// slot-name conditional branch falls through to the random branch.
public sealed class TroubadoursWeak : CustomEncounterModel, ILocalizationProvider
{
    public TroubadoursWeak() : base(RoomType.Monster) { }

    public override bool IsValidForAct(ActModel act) => act is TheAxons;

    public List<(string, string)>? Localization => new EncounterLoc(
        Title: "Troubadours",
        LossText: "Drowned in the Troubadours' refrain.");

    public override bool IsWeak => true;

    public override IReadOnlyList<string> Slots =>
        new[] { "first", "second", "third" };

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Troubadour>(),
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Troubadour>().ToMutable(), "first"),
            (ModelDb.Monster<Troubadour>().ToMutable(), "second"),
            (ModelDb.Monster<Troubadour>().ToMutable(), "third"),
        };
}