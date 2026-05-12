using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;

// Event-only encounter spawned by the MysteriousMime event. IsValidForAct
// returns false so it never appears in the normal hard/easy pools.
public sealed class MimeFight : CustomEncounterModel, ILocalizationProvider
{
    public MimeFight() : base(RoomType.Monster) { }

    public override bool IsValidForAct(ActModel act) => false;

    public List<(string, string)>? Localization => new EncounterLoc(
        Title: "Mime",
        LossText: "The mime mimes a victory dance.");

    public override string CustomBgm => "event:/mods/slaythemonolithmod/in_lumieres_name_event";

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Mime>(),
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Mime>().ToMutable(), null),
        };
}
