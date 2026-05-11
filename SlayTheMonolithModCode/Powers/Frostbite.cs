using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using SlayTheMonolithMod.SlayTheMonolithModCode.Cards;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Powers;

// At the end of each player turn, transforms N random non-Frozen cards in the
// player's hand into Frozen status cards, where N = current stack count. The
// stack itself does NOT tick down on its own — Frostbite persists until
// something removes it. Stacks add when re-applied.
public sealed class Frostbite : CustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization => new PowerLoc(
        Title: "Frostbite",
        Description: "At the end of your turn, transform a random card in your hand into [card]Frozen[/card] for each stack of Frostbite.",
        SmartDescription: "At the end of your turn, transform a random card in your hand into [card]Frozen[/card] for each stack of Frostbite.");

    // BeforeTurnEnd fires before vanilla discards the player's hand; AfterTurnEnd
    // is too late (the hand is already empty). Burn / Beckon use OnTurnEndInHand
    // on the card itself for the same reason.
    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        MainFile.Logger.Info($"[Frostbite] BeforeTurnEnd side={side} ownerSide={Owner.Side} isPlayer={Owner.IsPlayer} stacks={(int)Amount}");
        if (side != CombatSide.Player) return;
        if (!Owner.IsPlayer) return;

        var hand = Owner.Player.PlayerCombatState?.Hand;
        if (hand == null)
        {
            MainFile.Logger.Warn("[Frostbite] hand is null");
            return;
        }
        MainFile.Logger.Info($"[Frostbite] hand has {hand.Cards.Count} cards");

        var stacks = (int)Amount;
        for (var i = 0; i < stacks; i++)
        {
            var candidates = hand.Cards.Where(c => c is not Frozen).ToList();
            if (candidates.Count == 0)
            {
                MainFile.Logger.Info($"[Frostbite] no non-Frozen cards left after {i} transforms");
                break;
            }
            var target = candidates[Random.Shared.Next(candidates.Count)];
            MainFile.Logger.Info($"[Frostbite] transforming {target.GetType().Name} to Frozen");
            await CardCmd.TransformTo<Frozen>(target);
        }
    }
}
