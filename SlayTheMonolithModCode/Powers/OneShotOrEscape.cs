using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Powers;

// "Hit me once and end it." When the owner takes unblocked damage but
// survives, the owner Escape()s -- combat ends as a victory (last enemy
// gone), and the event Resume hook distinguishes outcome by checking
// CombatState.EscapedCreatures.
public sealed class OneShotOrEscape : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization => new PowerLoc(
        Title: "One Shot or Nothing",
        Description: "If this creature survives an attack, it flees.",
        SmartDescription: "If this creature survives an attack, it flees.");

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner) return;
        if (!Owner.IsAlive) return;          // already dead -- player won
        if (result.UnblockedDamage <= 0) return;

        Flash();
        await CreatureCmd.Escape(Owner);
    }
}
