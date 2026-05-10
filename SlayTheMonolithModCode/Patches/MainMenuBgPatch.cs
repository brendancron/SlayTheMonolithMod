using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Patches;

// Replaces the main menu's spine-animated background with a static texture.
// The vanilla BgContainer holds Bg/Fg spine meshes + cloud/star particles
// + a Rainbow node, plus a "Control" node that wraps the Logo. We hide every
// child of BgContainer except "Control" (so Logo stays visible) and inject a
// fullscreen TextureRect into NMainMenuBg itself (not BgContainer — that's a
// scaled Control that would distort the image and may not fill the viewport).
// MoveChild to index 0 so it renders behind BgContainer (which contains Logo).
[HarmonyPatch(typeof(NMainMenuBg), "_Ready")]
internal static class MainMenuBgPatch
{
    private const string CustomBgPath = "res://SlayTheMonolithMod/images/main_menu_bg.png";

    [HarmonyPostfix]
    private static void Postfix(NMainMenuBg __instance)
    {
        if (!ResourceLoader.Exists(CustomBgPath, ""))
        {
            MainFile.Logger.Warn($"Custom main menu bg not found at {CustomBgPath}");
            return;
        }
        var texture = ResourceLoader.Load<Texture2D>(CustomBgPath);
        if (texture == null)
        {
            MainFile.Logger.Warn($"ResourceLoader returned null for {CustomBgPath}");
            return;
        }
        if (__instance._bg == null)
        {
            MainFile.Logger.Warn("NMainMenuBg._bg is null; skipping background swap.");
            return;
        }

        // Hide vanilla animated layers. The "Control" child wraps the Logo, so
        // we leave it alone.
        foreach (var child in __instance._bg.GetChildren())
        {
            if (child.Name.ToString() == "Control") continue;
            if (child is CanvasItem canvasItem)
            {
                canvasItem.Visible = false;
            }
        }

        // Inject our texture as a child of NMainMenuBg (the outer Control),
        // moved to index 0 so it renders behind BgContainer + the Logo.
        var rect = new TextureRect
        {
            Texture = texture,
            Name = "ModBackground",
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
        };
        __instance.AddChild(rect);
        rect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        __instance.MoveChild(rect, 0);

        MainFile.Logger.Info("Main menu background replaced.");
    }
}
