using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using SlayTheMonolithMod.SlayTheMonolithModCode.Acts;
using SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;

// Replaces both the old DemineurNormal (3 Demineurs) and LusterNormal
// (1 Luster) encounters. Starts with one of each; the Demineur's explode-on-
// death still triggers, hitting both the player and the Luster -- a Luster
// kill timing puzzle on its own.
public sealed class DemineurLuster : CustomEncounterModel, ILocalizationProvider
{
    public DemineurLuster() : base(RoomType.Monster) { }

    public override bool IsValidForAct(ActModel act) => act is TheContinent;

    public List<(string, string)>? Localization => new EncounterLoc(
        Title: "Demineur and Luster",
        LossText: "The pair overwhelmed you.");

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Demineur>(),
        ModelDb.Monster<Luster>(),
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Demineur>().ToMutable(), null),
            (ModelDb.Monster<Luster>().ToMutable(), null),
        };
}
