using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Intents;

// Visual marker for moves whose debuff only triggers on unblocked attack
// damage. Reuses the vanilla debuff sprite -- the distinction lives in the
// hover-tip title/description, looked up from
// localization/<lang>/intents.json. The conditional logic itself stays in
// the monster's move method (see Portier.JumpMove / Lampmaster.DarkExplosionMove);
// vanilla intents have no hook into damage results, so the intent is
// purely display.
//
// GetAnimation is overridden to "debuff" because NIntent.UpdateVisuals
// resolves the floating icon's frame sequence through IntentAnimData using
// that name; falling back to the default (IntentPrefix.ToLowerInvariant())
// would look up "mod_conditional_debuff", find no frames, and render the
// creature with no intent icon at all -- only the side-panel tooltip would
// show. Sharing the vanilla "debuff" animation keeps the icon visible.
public sealed class ConditionalDebuffIntent : DebuffIntent
{
    private const string Prefix = "MOD_CONDITIONAL_DEBUFF";

    protected override string IntentPrefix => Prefix;

    public ConditionalDebuffIntent(bool strong = false) : base(strong) { }

    public override string GetAnimation(IEnumerable<Creature> targets, Creature owner) => "debuff";
}
