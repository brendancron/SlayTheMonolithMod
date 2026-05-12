using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using SlayTheMonolithMod.SlayTheMonolithModCode.Acts;
using SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;
using OnoPunchoMonster = SlayTheMonolithMod.SlayTheMonolithModCode.Monsters.OnoPuncho;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Events;

// Ono Puncho dares you to one-shot him. If you hit him and he survives, his
// OneShotOrEscape power Escapes him -- combat ends with no real victory.
// Only an actual kill triggers the relic reward.
//
//   - Take the challenge -> enter OnoPunchoFight. After combat, check
//                            CombatState.EscapedCreatures: if Ono Puncho is
//                            there he escaped (player failed); otherwise he
//                            died (player one-shot him -> obtain Ono's Glove).
//   - He's too strong    -> RewardsCmd.OfferCustom a random potion, finish.
public sealed class OnoPunchoEvent : CustomEventModel
{
    public override string? LocTable => "events";

    public override string? CustomInitialPortraitPath =>
        "res://SlayTheMonolithMod/images/ono_puncho.png";

    public override bool IsShared => true;

    public override bool IsAllowed(IRunState runState) =>
        runState.Act is TheContinent;

    public override List<(string, string)>? Localization => new EventLoc(
        Title: "Ono Puncho",
        Pages: new[]
        {
            new EventPageLoc(
                PageKey: "INITIAL",
                Description: "A muscled Gestral named Ono Puncho stands cross-armed in your path. \"Hit me once and end it,\" he says, grinning. \"Kill me in one shot and the prize is yours. Otherwise I'm walking away.\"",
                Options: new[]
                {
                    new EventOptionLoc("FIGHT", "Take the challenge", "Fight Ono Puncho. If you don't kill him in a single hit, he flees and you get nothing."),
                    new EventOptionLoc("LEAVE", "He's too strong",    "Admit defeat. Ono Puncho hands you a small consolation."),
                }),
            new EventPageLoc("VICTORY", "Ono Puncho hits the ground in one piece and stays down. He'd respect that.", Array.Empty<EventOptionLoc>()),
            new EventPageLoc("ESCAPED", "Ono Puncho takes the hit, smirks, and walks off into the dust. You're empty-handed.", Array.Empty<EventOptionLoc>()),
            new EventPageLoc("LEAVE",   "Ono Puncho nods, then tosses you a flask. \"Smart.\"", Array.Empty<EventOptionLoc>()),
        });

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
        new[]
        {
            new EventOption(this, Fight, $"{Id.Entry}.pages.INITIAL.options.FIGHT"),
            new EventOption(this, Leave, $"{Id.Entry}.pages.INITIAL.options.LEAVE"),
        };

    private Task Fight()
    {
        // Queue Ono's Glove on the combat reward screen. If Ono Puncho
        // escapes, SuppressOnoPunchoEscapeRewardsPatch returns early from
        // CombatRoom.OfferRoomEndRewards so the screen never opens and the
        // pre-queued relic is silently discarded.
        var rewards = new List<Reward>
        {
            new RelicReward(ModelDb.Relic<Relics.OnosGlove>().ToMutable(), Owner),
        };
        EnterCombatWithoutExitingEvent<OnoPunchoFight>(rewards, shouldResumeAfterCombat: true);
        return Task.CompletedTask;
    }

    private async Task Leave()
    {
        await RewardsCmd.OfferCustom(Owner, new List<Reward>
        {
            new PotionReward(Owner),
        });
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.LEAVE.description"));
    }

    public override Task Resume(AbstractRoom exitedRoom)
    {
        // Reward delivery is handled by the combat reward screen (or its
        // suppression on escape). Resume just renders the correct outcome
        // text and closes the event.
        var combat = (CombatRoom)exitedRoom;
        bool escaped = combat.CombatState.EscapedCreatures
            .Any(c => c.Monster is OnoPunchoMonster);
        SetEventFinished(L10NLookup(
            escaped
                ? $"{Id.Entry}.pages.ESCAPED.description"
                : $"{Id.Entry}.pages.VICTORY.description"));
        return Task.CompletedTask;
    }
}
