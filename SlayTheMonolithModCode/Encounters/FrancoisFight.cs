using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;

// Event-only encounter spawned by the Archnemesis event when the player picks
// the "take the rock by force" branch. IsValidForAct always returns false so
// it never appears in the normal hard/easy pools; ModelDb still registers it
// so the event can look it up by type.
public sealed class FrancoisFight : CustomEncounterModel, ILocalizationProvider
{
    public FrancoisFight() : base(RoomType.Monster) { }

    public override bool IsValidForAct(ActModel act) => false;

    public List<(string, string)>? Localization => new EncounterLoc(
        Title: "Francois",
        LossText: "Francois will keep the rock.");

    // Combat-only BGM. Vanilla NRunMusicController.PlayCustomMusic is invoked
    // from CombatManager.StartCombatInternal when the encounter has a non-null
    // CustomBgm; ContinuousActMusicPatch pauses our act track for the duration
    // and resumes it (at the saved timeline position) when the combat ends.
    public override string CustomBgm => "event:/mods/slaythemonolithmod/francois_event";

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Francois>(),
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Francois>().ToMutable(), null),
        };
}
