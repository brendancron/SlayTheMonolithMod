using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using SlayTheMonolithMod.SlayTheMonolithModCode.Acts;
using SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;

// Second TheAxons elite -- alt-path Entomancer. Solo 145 HP with PersonalHive 1
// on entry; cycles Bees -> Spear -> Pheromone Spit. Plays authored
// dualliste_elite track; ContinuousActMusicPatch pauses/resumes the act theme.
public sealed class DuallisteElite : CustomEncounterModel, ILocalizationProvider
{
    public DuallisteElite() : base(RoomType.Elite) { }

    public override bool IsValidForAct(ActModel act) => act is TheAxons;

    public List<(string, string)>? Localization => new EncounterLoc(
        Title: "Dualliste",
        LossText: "Outpaced by the Dualliste's twin blades.");

    public override string CustomBgm => "event:/mods/slaythemonolithmod/dualliste_elite";

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Dualliste>(),
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Dualliste>().ToMutable(), null),
        };
}
