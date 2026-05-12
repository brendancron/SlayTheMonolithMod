using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using SlayTheMonolithMod.SlayTheMonolithModCode.Acts;
using SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;

// Third TheAxons elite -- alt-path InfestedPrism, share-cast with MonocoElite.
// Named *Fight to avoid the ModelDb slug collision with the RenoirElite
// monster class (both would slugify to "renoir-elite" otherwise). Plays
// authored renoir_elite track; ContinuousActMusicPatch pauses/resumes the
// act theme.
public sealed class RenoirEliteFight : CustomEncounterModel, ILocalizationProvider
{
    public RenoirEliteFight() : base(RoomType.Elite) { }

    public override bool IsValidForAct(ActModel act) => act is TheAxons;

    public List<(string, string)>? Localization => new EncounterLoc(
        Title: "Renoir",
        LossText: "Cut down by the painter's old steel.");

    public override string CustomBgm => "event:/mods/slaythemonolithmod/renoir_elite";

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<RenoirElite>(),
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<RenoirElite>().ToMutable(), null),
        };
}
