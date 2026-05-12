using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using SlayTheMonolithMod.SlayTheMonolithModCode.Acts;
using SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;

// Alt-path Chompers fight. Two Ramasseurs in TheAxons easy pool: one opens
// with Gather (attack), the other opens with Howl (Dazed status), so each
// turn the player sees one attack intent and one debuff intent.
public sealed class RamasseursPair : CustomEncounterModel, ILocalizationProvider
{
    public RamasseursPair() : base(RoomType.Monster) { }

    public override bool IsValidForAct(ActModel act) => act is TheAxons;

    public List<(string, string)>? Localization => new EncounterLoc(
        Title: "Ramasseurs",
        LossText: "Pinned between the Ramasseurs' jaws.");

    public override bool IsWeak => true;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Ramasseur>(),
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        var gatherer = (Ramasseur)ModelDb.Monster<Ramasseur>().ToMutable();
        var howler = (Ramasseur)ModelDb.Monster<Ramasseur>().ToMutable();
        howler.HowlsFirst = true;
        return new List<(MonsterModel, string?)>
        {
            (gatherer, null),
            (howler, null),
        };
    }
}
