using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using SlayTheMonolithMod.SlayTheMonolithModCode.Acts;
using SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;

// First TheAxons elite -- alt-path InfestedPrism. Solo 200 HP with VitalSpark
// 1 on entry; cycles Jab -> Radiate -> Whirlwind -> Pulsate. Plays the
// authored monoco_elite track; ContinuousActMusicPatch pauses/resumes the act
// theme around it.
public sealed class MonocoElite : CustomEncounterModel, ILocalizationProvider
{
    public MonocoElite() : base(RoomType.Elite) { }

    public override bool IsValidForAct(ActModel act) => act is TheAxons;

    public List<(string, string)>? Localization => new EncounterLoc(
        Title: "Monoco",
        LossText: "Pinned beneath Monoco's chained maul.");

    public override string CustomBgm => "event:/mods/slaythemonolithmod/monoco_elite";

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Monoco>(),
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Monoco>().ToMutable(), null),
        };
}
