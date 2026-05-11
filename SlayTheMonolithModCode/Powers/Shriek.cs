using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Powers;

// Mirrors vanilla ShriekPower's mechanic but reads Sakapatate's TerrorStateId
// instead of casting to TerrorEel. When the owner takes unblocked damage that
// drops CurrentHp at or below the remaining Shriek amount, the owner is
// stunned and forced to queue its terror state on the next turn, then the
// power consumes itself.
public sealed class Shriek : CustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => true;
    public override bool ShouldScaleInMultiplayer => true;

    public override List<(string, string)>? Localization => new PowerLoc(
        Title: "Shriek",
        Description: "When dropped to {0} HP or below, this enemy is Stunned and shrieks in terror.",
        SmartDescription: "When dropped to {0} HP or below, this enemy is Stunned and shrieks in terror.");

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner) return;
        if (result.UnblockedDamage <= 0) return;
        if (target.CurrentHp > Amount) return;
        if (Owner.Monster is not UltimateSakapatate saka) return;

        Flash();
        await CreatureCmd.Stun(Owner, saka.PostShriekStateId);
        await PowerCmd.Remove(this);
    }
}
