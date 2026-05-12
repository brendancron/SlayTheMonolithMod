using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using SlayTheMonolithMod.SlayTheMonolithModCode.Acts;
using SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;
using SlayTheMonolithMod.SlayTheMonolithModCode.Relics;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Events;

// A strange mime stands ahead, gold scattered at its feet.
//   - Attack -> enter MimeFight combat (Mime with Slippery 5 + Artifact 1
//     and a PunchConstruct cycle). Standard combat rewards on victory.
//   - Leave  -> grab 50 gold from the ground, finish.
//
// IsShared = true required by EnterCombatWithoutExitingEvent. LayoutType
// stays Default so the event UI is restored after combat.
public sealed class MysteriousMime : CustomEventModel
{
    private const int LeaveGold = 50;

    public override string? LocTable => "events";

    public override string? CustomInitialPortraitPath =>
        "res://SlayTheMonolithMod/images/mime.png";

    public override bool IsShared => true;

    public override bool IsAllowed(IRunState runState) =>
        runState.Act is TheContinent;

    public override List<(string, string)>? Localization => new EventLoc(
        Title: "Mysterious Mime",
        Pages: new[]
        {
            new EventPageLoc(
                PageKey: "INITIAL",
                Description: "A strange mime stands in your path, miming a silent argument with the air. Coins glint on the ground at its feet.",
                Options: new[]
                {
                    new EventOptionLoc("ATTACK", "Attack the mime",  "Fight it. Standard combat rewards on victory."),
                    new EventOptionLoc("LEAVE",  "Leave it be",      $"Pocket {LeaveGold} gold from the ground and walk on."),
                }),
            new EventPageLoc("VICTORY", "The mime sinks into an invisible chair, defeated.",        Array.Empty<EventOptionLoc>()),
            new EventPageLoc("LEAVE",   "You scoop the coins up. The mime mimes a slow clap.",      Array.Empty<EventOptionLoc>()),
        });

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
        new[]
        {
            new EventOption(this, Attack, $"{Id.Entry}.pages.INITIAL.options.ATTACK"),
            new EventOption(this, Leave,  $"{Id.Entry}.pages.INITIAL.options.LEAVE"),
        };

    private Task Attack()
    {
        // Baguette drops on the combat-end reward screen alongside standard
        // gold/card rewards. Defeat ends the run, so no need to guard.
        var rewards = new List<Reward>
        {
            new RelicReward(ModelDb.Relic<Baguette>().ToMutable(), Owner),
        };
        EnterCombatWithoutExitingEvent<MimeFight>(rewards, shouldResumeAfterCombat: true);
        return Task.CompletedTask;
    }

    private async Task Leave()
    {
        await PlayerCmd.GainGold(LeaveGold, Owner);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.LEAVE.description"));
    }

    public override Task Resume(AbstractRoom exitedRoom)
    {
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.VICTORY.description"));
        return Task.CompletedTask;
    }
}
