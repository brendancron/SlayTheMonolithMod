using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;

// Event-only encounter spawned by the OnoPuncho event. IsValidForAct returns
// false so it never appears in the normal hard/easy pools.
public sealed class OnoPunchoFight : CustomEncounterModel, ILocalizationProvider
{
    public OnoPunchoFight() : base(RoomType.Monster) { }

    public override bool IsValidForAct(ActModel act) => false;

    public List<(string, string)>? Localization => new EncounterLoc(
        Title: "Ono Puncho",
        LossText: "Ono Puncho flexes over your unconscious body.");

    public override string CustomBgm => "event:/mods/slaythemonolithmod/fight_for_the_win_event";

    // When Ono Puncho escapes (player didn't one-shot) we suppress the entire
    // reward screen. NCombatUi.OnCombatWon checks this before calling
    // ShowRewards -- returning false routes to ProceedWithoutRewards instead.
    public override bool ShouldGiveRewards
    {
        get
        {
            ICombatState? state = CombatManager.Instance?._state;
            if (state == null) return true;
            return !state.EscapedCreatures.Any(c => c.Monster is OnoPuncho);
        }
    }

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<OnoPuncho>(),
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<OnoPuncho>().ToMutable(), null),
        };
}
