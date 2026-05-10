using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Acts;

// First biome of the alternate storyline. Visuals + audio currently piggyback on
// vanilla Overgrowth so the wiring can be validated before any asset work. All
// encounters (weak, normal, elite, boss) come from CustomEncounterModels that
// IsValidForAct(act is TheContinent), auto-injected by BaseLib's
// AddCustomEncounters postfix.
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

    // Empty event pool for now — vanilla events would feel out of place in the alt
    // arc, and we have no custom events yet. BaseLib's AddCustomEvents postfix
    // injects per-act CustomEventModels here when we add them later.
    public override IEnumerable<EventModel> AllEvents => Array.Empty<EventModel>();

    // First-run boss order: players see Lampmaster first since he's our only boss.
    // ApplyDiscoveryOrderModifications walks this list and assigns the first
    // unseen entry to _rooms.Boss for the run.
    public override IEnumerable<EncounterModel> BossDiscoveryOrder => new EncounterModel[]
    {
        ModelDb.Encounter<LampmasterBoss>(),
    };

    // No vanilla encounters needed: LampmasterBoss + the three *Elite encounters
    // + the four easy-pool encounters + the 21 hard-pool *Normal encounters all
    // auto-inject via BaseLib's postfix on this method (each gates with
    // IsValidForAct(act is TheContinent)).
    public override IEnumerable<EncounterModel> GenerateAllEncounters() =>
        Array.Empty<EncounterModel>();
}
