using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using SlayTheMonolithMod.SlayTheMonolithModCode.Acts;
using SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;
using SlayTheMonolithMod.SlayTheMonolithModCode.Relics;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Events;

// "Esquie remembers that he left Florrie with his archnemesis Francois."
// Three options:
//   - Barter (150g, locked if poor)   -> spend gold, obtain Florrie, finish
//   - Take it (combat)                 -> enter FrancoisFight; on Resume after
//                                          a victory, obtain Florrie + finish
//                                          (defeat = game over, Resume never fires)
//   - Leave                            -> finish, no reward
//
// IsShared must be true to permit EnterCombatWithoutExitingEvent; LayoutType
// stays Default so the event UI is restored after combat returns.
public sealed class Archnemesis : CustomEventModel
{
    private const int BarterCost = 150;

    public override string? LocTable => "events";

    public override string? CustomInitialPortraitPath =>
        "res://SlayTheMonolithMod/images/francois.png";

    public override bool IsShared => true;

    public override bool IsAllowed(IRunState runState) =>
        runState.Act is TheContinent;

    public override List<(string, string)>? Localization => new EventLoc(
        Title: "Archnemesis",
        Pages: new[]
        {
            new EventPageLoc(
                PageKey: "INITIAL",
                Description: "Esquie remembers that he left Florrie with his archnemesis Francois. You can either barter for the rock or try to take it.",
                Options: new[]
                {
                    new EventOptionLoc("BARTER",        "Barter for the rock",          $"Pay {BarterCost} gold to receive [relic]Florrie[/relic]."),
                    new EventOptionLoc("BARTER_LOCKED", "Barter for the rock",          $"[You don't have {BarterCost} gold.]"),
                    new EventOptionLoc("FIGHT",         "Take it by force",             "Fight Francois for [relic]Florrie[/relic]."),
                    new EventOptionLoc("LEAVE",         "Leave without Florrie",        "Esquie will be disappointed."),
                }),
            new EventPageLoc("BARTER",  "Francois pockets your gold and rolls the rock to you. \"He's all yours.\"",  Array.Empty<EventOptionLoc>()),
            new EventPageLoc("VICTORY", "Francois yields. You scoop up Florrie and walk on.",                          Array.Empty<EventOptionLoc>()),
            new EventPageLoc("LEAVE",   "You leave Florrie with Francois. Esquie watches mournfully from your pack.",  Array.Empty<EventOptionLoc>()),
        });

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var canAfford = Owner.Gold >= BarterCost;
        return new[]
        {
            canAfford
                ? new EventOption(this, Barter, $"{Id.Entry}.pages.INITIAL.options.BARTER")
                : new EventOption(this, null,   $"{Id.Entry}.pages.INITIAL.options.BARTER_LOCKED"),
            new EventOption(this, Fight,  $"{Id.Entry}.pages.INITIAL.options.FIGHT"),
            new EventOption(this, Leave,  $"{Id.Entry}.pages.INITIAL.options.LEAVE"),
        };
    }

    private async Task Barter()
    {
        await PlayerCmd.LoseGold(BarterCost, Owner, GoldLossType.Spent);
        await RelicCmd.Obtain<Florrie>(Owner);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.BARTER.description"));
    }

    private Task Fight()
    {
        // Pass Florrie as an extraReward so the combat-end reward screen
        // offers it alongside any standard drops. (Doing this via
        // RelicCmd.Obtain in Resume bypasses the rewards UI entirely.)
        var rewards = new List<Reward>
        {
            new RelicReward(ModelDb.Relic<Florrie>().ToMutable(), Owner),
        };
        EnterCombatWithoutExitingEvent<FrancoisFight>(rewards, shouldResumeAfterCombat: true);
        return Task.CompletedTask;
    }

    private Task Leave()
    {
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.LEAVE.description"));
        return Task.CompletedTask;
    }

    // Only fires on combat victory -- defeat ends the run instead. Florrie
    // is delivered through the combat reward screen (see the extraRewards
    // list passed to EnterCombatWithoutExitingEvent above), so all that's
    // left here is closing the event.
    public override Task Resume(AbstractRoom exitedRoom)
    {
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.VICTORY.description"));
        return Task.CompletedTask;
    }
}
