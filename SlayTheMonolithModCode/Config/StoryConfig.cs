using BaseLib.Config;

namespace SlayTheMonolithMod.SlayTheMonolithModCode.Config;

[ConfigHoverTipsByDefault]
public class StoryConfig : SimpleModConfig
{
    [ConfigSection("Storyline")]
    public static bool AlternateStorylineEnabled { get; set; } = false;
}
