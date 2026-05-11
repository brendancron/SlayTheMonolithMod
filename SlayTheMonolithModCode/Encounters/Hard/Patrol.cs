using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using SlayTheMonolithMod.SlayTheMonolithModCode.Acts;
using SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;

// Hard-pool variant featuring 2 distinct monsters drawn from
// {Lancelier, Abbest, Portier}. Same monster never appears twice in the
// same fight -- we shuffle the pool and take the first two.
public sealed class Patrol : CustomEncounterModel, ILocalizationProvider
{
    public Patrol() : base(RoomType.Monster) { }

    public override bool IsValidForAct(ActModel act) => act is TheContinent;

    public List<(string, string)>? Localization => new EncounterLoc(
        Title: "Patrol",
        LossText: "The patrol cut you down.");

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Lancelier>(),
        ModelDb.Monster<Abbest>(),
        ModelDb.Monster<Portier>(),
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        var pool = new List<MonsterModel>
        {
            ModelDb.Monster<Lancelier>(),
            ModelDb.Monster<Abbest>(),
            ModelDb.Monster<Portier>(),
        };
        var first = base.Rng.NextItem(pool);
        var remaining = pool.Where(m => m != first).ToList();
        var second = base.Rng.NextItem(remaining);
        return new List<(MonsterModel, string?)>
        {
            (first.ToMutable(), null),
            (second.ToMutable(), null),
        };
    }
}
