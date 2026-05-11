using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Powers;

// Functionally an alias of PoisonPower: at the start of the owner's turn,
// deals Amount unblockable damage, then decrements by 1. No Accelerant
// interaction (we're applied by enemies, not the player).
public sealed class Burn : CustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization => new PowerLoc(
        Title: "Burn",
        Description: "At the start of your turn, take {0} damage, then reduce Burn by 1.",
        SmartDescription: "At the start of your turn, take {0} damage, then reduce Burn by 1.");

    public override async Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        if (side != Owner.Side) return;

        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner,
            Amount,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);
        if (Owner.IsAlive)
        {
            await PowerCmd.Decrement(this);
        }
    }
}
