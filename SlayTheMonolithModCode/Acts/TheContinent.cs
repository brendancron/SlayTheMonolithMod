using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Acts;

// First biome of the alternate storyline. Visuals + audio currently piggyback on
// vanilla Overgrowth so the wiring can be validated before any asset work. The
// boss/elite/weak encounter pool also borrows from Overgrowth as placeholders;
// the 25 mod normal encounters auto-inject via BaseLib's AddCustomEncounters.
public sealed class TheContinent : CustomActModel, ILocalizationProvider
{
    public TheContinent() : base(actNumber: 1) { }

    public List<(string, string)>? Localization => new ActLoc(Title: "The Continent");

    protected override string CustomMapTopBgPath =>
        "res://images/packed/map/map_bgs/overgrowth/map_top_overgrowth.png";
    protected override string CustomMapMidBgPath =>
        "res://images/packed/map/map_bgs/overgrowth/map_middle_overgrowth.png";
    protected override string CustomMapBotBgPath =>
        "res://images/packed/map/map_bgs/overgrowth/map_bottom_overgrowth.png";
    protected override string CustomRestSiteBackgroundPath =>
        "res://scenes/rest_site/overgrowth_rest_site.tscn";

    // CustomActModel's base defaults to act-3/Glory audio. Override to act-1/Overgrowth.
    public override string[] BgMusicOptions =>
        new[] { "event:/music/act1_a1_v1", "event:/music/act1_a2_v2" };
    public override string[] MusicBankPaths =>
        new[] { "res://banks/desktop/act1_a1.bank", "res://banks/desktop/act1_a2.bank" };
    public override string AmbientSfx => "event:/sfx/ambience/act1_ambience";
    public override string ChestSpineResourcePath =>
        "res://animations/backgrounds/treasure_room/chest_room_act_1_skel_data.tres";
    public override string ChestSpineSkinNameNormal => "act1";
    public override string ChestSpineSkinNameStroke => "act1_stroke";
    public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act1";

    // Procedural BG layers — feed them Overgrowth's assets so the dynamic background
    // doesn't render as Glory (CustomActModel's default).
    protected override BackgroundAssets CustomGenerateBackgroundAssets(Rng rng) =>
        new BackgroundAssets("overgrowth", rng);

    // Empty event pool for the spike — vanilla events would feel out of place in the
    // alt arc, and we have no custom events yet. BaseLib's AddCustomEvents postfix
    // injects per-act CustomEventModels here when we add them later.
    public override IEnumerable<EventModel> AllEvents => Array.Empty<EventModel>();

    // GenerateRooms expects at least one boss/elite/weak encounter per act, otherwise
    // _rooms.Boss ends up null and AssetPaths crashes during act-asset preload. Bosses
    // and elites still borrow from vanilla Overgrowth as placeholders — replace them
    // when authored. The four easy-pool encounters (LancelierFight, PortierFight,
    // Volesters, Abbests) and the 21 hard-pool *Normal encounters are auto-injected
    // by BaseLib's AddCustomEncounters postfix on top of this list.
    public override IEnumerable<EncounterModel> GenerateAllEncounters() => new EncounterModel[]
    {
        ModelDb.Encounter<VantomBoss>(),
        ModelDb.Encounter<CeremonialBeastBoss>(),
        ModelDb.Encounter<TheKinBoss>(),
        ModelDb.Encounter<BygoneEffigyElite>(),
        ModelDb.Encounter<ByrdonisElite>(),
        ModelDb.Encounter<PhrogParasiteElite>(),
    };
}
