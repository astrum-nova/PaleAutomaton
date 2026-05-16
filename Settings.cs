using BepInEx.Configuration;

namespace PaleAutomaton;

public static class Settings
{
    public static bool DISABLE_PARRY_FREEZE = true;
    public static bool DISABLE_CAMERA_ZOOMOUT;
    public static bool CUSTOM_POGO_HITBOX = true;
    public static bool INFINITE_WALKWAY = true;
    public static bool DISABLE_BOSS_PARRYING_YOU = false;
    public static void InitializeSettings(ConfigFile Config)
    {
        DISABLE_PARRY_FREEZE = Config.Bind(
            "Accessibility",
            "Disable Parry Freeze",
            true,
            "Disables parry freeze, set this to false to use vanilla parry freeze behaviour."
        ).Value;
        DISABLE_CAMERA_ZOOMOUT = Config.Bind(
            "Accessibility",
            "Disable Camera Zoomout",
            false,
            "Disables the camera zoomout at the start of the fight and phase 3."
        ).Value;
        CUSTOM_POGO_HITBOX = Config.Bind(
            "Accessibility",
            "Custom Pogo Hitbox",
            true,
            "I made a custom pogo hitbox for phase 3 and 4 to make aerial parries more consistent (its a circle around the player)."
        ).Value;
        INFINITE_WALKWAY = Config.Bind(
            "Gameplay",
            "Infinite Walkway",
            true,
            "Turns the arena into an infinite walkway, will revert once the boss is defeated or the player dies."
        ).Value;
        /*
        DISABLE_BOSS_PARRYING_YOU = Config.Bind(
            "Gameplay",
            "Disable Boss Parrying You",
            false,
            "Disables the mechanic where the boss can parry you if you attack during a telegraph."
        ).Value;
        */
    }
}