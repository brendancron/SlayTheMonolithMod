using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using SlayTheMonolithMod.SlayTheMonolithModCode.Acts;
using SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;

// Alt-path SpinyToad fight: a single Stalact in TheAxons normal pool. Solo big
// HP pool (~116-119) cycling Protrude -> Explosion -> Lash. Protrude raises
// SearingSkin 5; while up, every powered attack against Stalact applies 5 Burn
// to the attacker. Explosion consumes the coating.
public sealed class StalactFight : CustomEncounterModel, ILocalizationProvider
{
    public StalactFight() : base(RoomType.Monster) { }

    public override bool IsValidForAct(ActModel act) => act is TheAxons;

    public List<(string, string)>? Localization => new EncounterLoc(
        Title: "Stalact",
        LossText: "Burned away against the Stalact's searing hide.");

    public override bool IsWeak => false;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Stalact>(),
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Stalact>().ToMutable(), null),
        };
}
