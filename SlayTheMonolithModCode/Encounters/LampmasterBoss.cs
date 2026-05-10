using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using SlayTheMonolithMod.SlayTheMonolithModCode.Acts;
using SlayTheMonolithMod.SlayTheMonolithModCode.Monsters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;

public sealed class LampmasterBoss : CustomEncounterModel, ILocalizationProvider
{
    public LampmasterBoss() : base(RoomType.Boss) { }

    public override bool IsValidForAct(ActModel act) => act is TheContinent;

    public List<(string, string)>? Localization => new EncounterLoc(
        Title: "The Lampmaster",
        LossText: "Their light extinguished yours.");

    // Custom boss music. Vanilla NRunMusicController.PlayCustomMusic stops the act
    // bank and plays this event when combat starts; restores the act's BGM on
    // combat end. Event lives in audio/slaythemonolithmod.bank, with paths resolved
    // via audio/slaythemonolithmod.strings.bank (both loaded by MainFile.Initialize).
    public override string CustomBgm => "event:/mods/slaythemonolithmod/lampmaster_boss";

    // Boss encounters need three extra assets that the engine resolves by ID:
    //   1. images/ui/run_history/<id>.png         (top-bar icon)
    //   2. images/ui/run_history/<id>_outline.png (outlined variant)
    //   3. animations/map/<id>/<id>_node_skel_data.tres (map node spine)
    // Until we author Lampmaster art, point at vanilla VantomBoss's so the
    // icon shows up. Replace these overrides when we have real assets.
    public override string CustomRunHistoryIconPath =>
        "res://images/ui/run_history/vantom_boss.png";
    public override string CustomRunHistoryIconOutlinePath =>
        "res://images/ui/run_history/vantom_boss_outline.png";
    public override string BossNodePath =>
        "res://animations/map/vantom_boss/vantom_boss_node_skel_data.tres";

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Lampmaster>(),
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new List<(MonsterModel, string?)>
        {
            (ModelDb.Monster<Lampmaster>().ToMutable(), null),
        };
}
