using BepInEx.Configuration;

namespace PaleAutomaton;

public static class Settings
{
    public static bool DISABLE_PARRY_FREEZE = true;
    public static void InitializeSettings(ConfigFile Config)
    {
        DISABLE_PARRY_FREEZE = Config.Bind(
            "Accessibility",
            "Disable Parry Freeze",
            true,
            "Disables parry freeze, set this to false to use vanilla parry freeze behaviour."
        ).Value;
    }
}