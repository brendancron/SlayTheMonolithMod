using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using SlayTheMonolithMod.SlayTheMonolithModCode.Acts;
using SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;

// Alt-path STS1 Snecko mirror. Solo Glissando in TheAxons hard pool. Always
// opens with Perplexing Glare; subsequent turns roll 60% Bite, 40% Tail Whip
// with Bite capped at two in a row.
public sealed class GlissandoFight : CustomEncounterModel, ILocalizationProvider
{
    public GlissandoFight() : base(RoomType.Monster) { }

    public override bool IsValidForAct(ActModel act) => act is TheAxons;

    public List<(string, string)>? Localization => new EncounterLoc(
        Title: "Glissando",
        LossText: "Lost in the Glissando's perplexing gaze.");

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Glissando>(),
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Glissando>().ToMutable(), null),
        };
}
