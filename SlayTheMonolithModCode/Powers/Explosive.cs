using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Powers;

// "When this creature dies, deal Amount damage to every other living
// creature." Used by Demineur for its explode-on-death mechanic. Chain
// reactions are intentional: if the explosion kills another Demineur, that
// one's Explosive power fires next, etc.
//
// Damage is Move | Unpowered: blockable on the player side, no Strength /
// Vulnerable scaling so it's always exactly the value the power was applied
// with.
public sealed class Explosive : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization => new PowerLoc(
        Title: "Explosive",
        Description: "When this creature dies, deals {0} damage to every other creature.",
        SmartDescription: "When this creature dies, deals {0} damage to every other creature.");

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature target,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (target != Owner) return;
        if (wasRemovalPrevented) return;

        var combatState = Owner.CombatState;
        if (combatState == null) return;

        var others = combatState.Creatures
            .Where(c => c != Owner && c.IsAlive)
            .ToList();
        if (others.Count == 0) return;

        Flash();
        // Pass dealer=null because Owner is already dead at this point and
        // CreatureCmd.Damage short-circuits to zero-damage results when the
        // dealer is dead (CreatureCmd.cs:126). The explosion isn't really
        // attributable to anyone specific anyway.
        await CreatureCmd.Damage(
            choiceContext,
            others,
            Amount,
            ValueProp.Move | ValueProp.Unpowered,
            null,
            null);
    }
}
