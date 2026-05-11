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

// Generic flight buff modeled on vanilla FlutterPower, minus the ThievingHopper-
// specific bookkeeping. Owner takes 50% damage from powered attacks; each
// unblocked attack hit decrements one stack. When stacks reach 0, the owner
// is stunned for one turn and Flying is removed -- the post-stun move comes
// from the state machine's next-state lookup, so we don't need to know the
// monster's move IDs.
public sealed class Flying : CustomPowerModel
{
    private const decimal DamageDecreasePercent = 50m;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization => new PowerLoc(
        Title: "Flying",
        Description: "Takes 50% damage from attacks. Loses 1 stack each time it takes unblocked attack damage. Stunned for 1 turn when Flying ends.",
        SmartDescription: "Takes 50% damage from attacks. Loses 1 stack each time it takes unblocked attack damage. Stunned for 1 turn when Flying ends.");

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner) return 1m;
        if (!props.IsPoweredAttack()) return 1m;
        return (100m - DamageDecreasePercent) / 100m;
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner) return;
        if (result.UnblockedDamage == 0) return;
        if (!props.IsPoweredAttack()) return;

        await PowerCmd.Decrement(this);
        if (Amount > 0) return;

        // Stacks hit 0: stun for one turn, then resume whatever move the
        // state machine would have transitioned to. StateLog.Last() is the
        // most recent move; its GetNextState gives the natural follow-up.
        var monster = Owner.Monster;
        string nextState = monster.MoveStateMachine.StateLog.Last()
            .GetNextState(Owner, monster.RunRng.MonsterAi);
        Flash();
        await CreatureCmd.Stun(Owner, nextState);
    }
}
