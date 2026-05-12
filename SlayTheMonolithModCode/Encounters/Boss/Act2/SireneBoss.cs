using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using SlayTheMonolithMod.SlayTheMonolithModCode.Acts;
using SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;

// Act-2 boss encounter. Placeholder art piggybacks on vanilla KaiserCrabBoss
// run-history icons / map node spine until we author real Sirene assets.
public sealed class SireneBoss : CustomEncounterModel, ILocalizationProvider
{
    public SireneBoss() : base(RoomType.Boss) { }

    public override bool IsValidForAct(ActModel act) => act is TheAxons;

    public List<(string, string)>? Localization => new EncounterLoc(
        Title: "Sirene",
        LossText: "Her song was the last thing you heard.");

    public override string CustomRunHistoryIconPath =>
        "res://images/ui/run_history/kaiser_crab_boss.png";
    public override string CustomRunHistoryIconOutlinePath =>
        "res://images/ui/run_history/kaiser_crab_boss_outline.png";
    public override string BossNodePath =>
        "res://animations/map/kaiser_crab_boss/kaiser_crab_boss_node_skel_data.tres";

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Sirene>(),
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Sirene>().ToMutable(), null),
        };
}
