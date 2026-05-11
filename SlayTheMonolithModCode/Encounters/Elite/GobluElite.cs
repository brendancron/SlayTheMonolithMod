using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using SlayTheMonolithMod.SlayTheMonolithModCode.Acts;
using SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;

public sealed class GobluElite : CustomEncounterModel, ILocalizationProvider
{
    public GobluElite() : base(RoomType.Elite) { }

    public override bool IsValidForAct(ActModel act) => act is TheContinent;

    public List<(string, string)>? Localization => new EncounterLoc(
        Title: "Goblu",
        LossText: "Goblu swallowed you whole.");

    public override string CustomBgm => "event:/mods/slaythemonolithmod/goblu_elite";

    // BaseLib reads the slot names off this scene's Marker2D children. Five
    // markers: Goblu in the middle, flanked by 4 flower slots. GetNextSlot
    // returns the first unoccupied slot in marker order, so Goblu can re-summon
    // up to 4 Flowers without extra bookkeeping in Goblu.SummonMove.
    public override string? CustomScenePath =>
        "res://SlayTheMonolithMod/scenes/encounters/goblu_elite.tscn";

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Goblu>(),
        ModelDb.Monster<Flower>(),
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Goblu>().ToMutable(), "goblu"),
        };
}
