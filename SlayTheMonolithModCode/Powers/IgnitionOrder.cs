using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Powers;

// Marks a Lamp minion with its position (1-4) in the Lampmaster's ritual kill
// sequence. When the lamp owner dies from damage (i.e. not via Escape-style
// despawn), grants every player 1 energy and notifies the Lampmaster so it can
// check whether the player has cleared all four lamps in the expected order.
public sealed class IgnitionOrder : CustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization => new PowerLoc(
        Title: "Ignition Order",
        Description: "Kill the lamps in ascending order to interrupt the Lampmaster's next attack. Killing this lamp grants 1 [E].",
        SmartDescription: "Kill the lamps in ascending order to interrupt the Lampmaster's next attack. Killing this lamp grants 1 [E].");

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (creature != Owner) return;
        if (wasRemovalPrevented) return;

        foreach (var player in creature.CombatState.Players)
        {
            await PlayerCmd.GainEnergy(1m, player);
        }

        var lampmaster = creature.CombatState.Enemies
            .Where(c => c.IsAlive)
            .Select(c => c.Monster as Lampmaster)
            .FirstOrDefault(m => m != null);
        if (lampmaster != null)
        {
            await lampmaster.OnLampKilled((int)Amount);
        }
    }
}
