using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using SlayTheMonolithMod.SlayTheMonolithModCode.Encounters;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Acts;

// Second biome of the alternate storyline. Visuals + ambient piggyback on
// vanilla Hive (act 2). Music is our authored robe_de_jour_theme, played
// continuously by ContinuousActMusicPatch the same way TheContinent's themes
// are. AltActListPatch substitutes this act into vanilla act 2 slot when
// StoryConfig.AlternateStorylineEnabled is true (matched on ActNumber).
public sealed class TheAxons : CustomActModel, ILocalizationProvider
{
    public TheAxons() : base(actNumber: 2) { }

    public List<(string, string)>? Localization => new ActLoc(Title: "The Axons");

    protected override string CustomMapTopBgPath =>
        "res://images/packed/map/map_bgs/hive/map_top_hive.png";
    protected override string CustomMapMidBgPath =>
        "res://images/packed/map/map_bgs/hive/map_middle_hive.png";
    protected override string CustomMapBotBgPath =>
        "res://images/packed/map/map_bgs/hive/map_bottom_hive.png";
    protected override string CustomRestSiteBackgroundPath =>
        "res://scenes/rest_site/hive_rest_site.tscn";

    public override string[] BgMusicOptions =>
        new[] { "event:/mods/slaythemonolithmod/robe_de_jour_theme" };

    public override string[] MusicBankPaths =>
        new[] { "res://banks/desktop/act2_a1.bank" };

    public override string AmbientSfx => "event:/sfx/ambience/act2_ambience";
    public override string ChestSpineResourcePath =>
        "res://animations/backgrounds/treasure_room/chest_room_act_2_skel_data.tres";
    public override string ChestSpineSkinNameNormal => "act2";
    public override string ChestSpineSkinNameStroke => "act2_stroke";
    public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act2";

    protected override BackgroundAssets CustomGenerateBackgroundAssets(Rng rng) =>
        new BackgroundAssets("hive", rng);

    // Empty event pool until act-2-specific events exist. CustomEventModels
    // with IsAllowed(runState => runState.Act is TheAxons) auto-inject later
    // via BaseLib's AddCustomEvents postfix.
    public override IEnumerable<EventModel> AllEvents => Array.Empty<EventModel>();

    public override IEnumerable<EncounterModel> BossDiscoveryOrder => new EncounterModel[]
    {
        ModelDb.Encounter<SireneBoss>(),
    };

    public override IEnumerable<EncounterModel> GenerateAllEncounters() => new EncounterModel[]
    {
        ModelDb.Encounter<RamasseursPair>(),
        ModelDb.Encounter<TroubadoursWeak>(),
        ModelDb.Encounter<StalactFight>(),
        ModelDb.Encounter<ChevaliereFight>(),
        ModelDb.Encounter<BouchelierFight>(),
        ModelDb.Encounter<GlissandoFight>(),
        ModelDb.Encounter<MonocoElite>(),
        ModelDb.Encounter<DuallisteElite>(),
        ModelDb.Encounter<RenoirEliteFight>(),
    };
}
