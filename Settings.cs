using BepInEx.Configuration;

namespace PaleAutomaton;

public static class Settings
{
    public static bool DISABLE_PARRY_FREEZE = true;
    public static bool DISABLE_BOSS_PARRYING_YOU = false;
    public static void InitializeSettings(ConfigFile Config)
    {
        DISABLE_PARRY_FREEZE = Config.Bind(
            "Accessibility",
            "Disable Parry Freeze",
            true,
            "Disables parry freeze, set this to false to use vanilla parry freeze behaviour."
        ).Value;
        DISABLE_BOSS_PARRYING_YOU = Config.Bind(
            "Gameplay",
            "Disable Boss Parrying You",
            false,
            "Disables the mechanic where the boss can parry you if you attack during a telegraph."
        ).Value;
    }
}