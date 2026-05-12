using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Powers;

// Reflective Burn analogue of vanilla ThornsPower. When the owner takes damage
// from a powered attack, apply Burn to the dealer equal to the stack count,
// instead of dealing reflected damage. Gate matches Thorns (powered attacks
// only), so status-card damage and non-attack effects don't trigger it.
public sealed class SearingSkin : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization => new PowerLoc(
        Title: "Searing Skin",
        Description: "When attacked, apply {0} [power]Burn[/power] to the attacker.",
        SmartDescription: "When attacked, apply {0} [power]Burn[/power] to the attacker.");

    public override async Task BeforeDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner) return;
        if (dealer == null) return;
        if (!props.IsPoweredAttack()) return;

        Flash();
        await PowerCmd.Apply<Burn>(choiceContext, dealer, Amount, Owner, null);
    }
}
