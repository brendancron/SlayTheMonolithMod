using System.IO;
using BaseLib.Config;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using SlayTheMonolithMod.SlayTheMonolithModCode.Config;

namespace SlayTheMonolithMod.SlayTheMonolithModCode
{
    //You're recommended but not required to keep all your code in this package and all your assets in the SlayTheMonolithMod folder.
    [ModInitializer(nameof(Initialize))]
    public partial class MainFile : Node
    {
        public const string ModId = "SlayTheMonolithMod"; //At the moment, this is used only for the Logger and harmony names.

        public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

        public static void Initialize()
        {
            Harmony harmony = new(ModId);

            harmony.PatchAll();

            ModConfigRegistry.Register(ModId, new StoryConfig());

            LoadCustomAudioBanks();
        }

        // Load custom FMOD banks under <modDir>/audio/. Order matters per BaseLib's
        // FmodAudio.LoadBank doc-comment: strings bank first (so event paths can be
        // resolved), then content bank.
        //
        // The strings file here is built from a slaythemonolithmod bank that's
        // marked "Master Bank" in FMOD Studio — that gives it its own strings table
        // containing ONLY our event paths, distinct from the game's Master.strings
        // bank. Don't replace the file with Master.strings.bank from the template
        // build output: that overrides the game's master string table and silences
        // vanilla music.
        private static void LoadCustomAudioBanks()
        {
            var dllPath = typeof(MainFile).Assembly.Location;
            if (string.IsNullOrEmpty(dllPath)) return;
            var modDir = Path.GetDirectoryName(dllPath);
            if (modDir == null) return;

            var stringsBank = Path.Combine(modDir, "audio", "slaythemonolithmod.strings.bank");
            var contentBank = Path.Combine(modDir, "audio", "slaythemonolithmod.bank");

            if (File.Exists(stringsBank))
            {
                if (FmodAudio.LoadBank(stringsBank))
                    Logger.Info("Loaded audio strings bank.");
                else
                    Logger.Warn($"FmodAudio.LoadBank failed for strings bank: {stringsBank}");
            }
            if (File.Exists(contentBank))
            {
                if (FmodAudio.LoadBank(contentBank))
                    Logger.Info("Loaded audio content bank.");
                else
                    Logger.Warn($"FmodAudio.LoadBank failed for content bank: {contentBank}");
            }
        }
    }
}
