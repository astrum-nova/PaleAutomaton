using BepInEx.Configuration;

namespace PaleAutomaton;

public static class Settings
{
    public static bool DISABLE_PARRY_FREEZE = true;
    public static bool DISABLE_CAMERA_ZOOMOUT;
    public static bool CUSTOM_POGO_HITBOX = true;
    public static bool INFINITE_WALKWAY = true;
    public static bool BELL_BIND_EFFECT_ON_THE_BOSS = true;
    public static bool DISABLE_BOSS_PARRYING_YOU = false;
    public static bool DEBUG_MODE;
    public static int INITIAL_HP = 1000;
    public static int PHASE_2_THRESHOLD = 990;
    public static int PHASE_3_THRESHOLD = 890;
    public static int PHASE_4_THRESHOLD = 500;
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
        BELL_BIND_EFFECT_ON_THE_BOSS = Config.Bind(
            "Visual Effects",
            "Bell Bind Effect On The Boss",
            false,
            "Enables a scrapped effect for phase 3 and 4 where the boss gains the bell bind visual effect, it looked weird so i scrapepd it."
        ).Value;
        /*
        DISABLE_BOSS_PARRYING_YOU = Config.Bind(
            "Gameplay",
            "Disable Boss Parrying You",
            false,
            "Disables the mechanic where the boss can parry you if you attack during a telegraph."
        ).Value;
        */
        DEBUG_MODE = Config.Bind(
            "Debug",
            "Debug Mode",
            false,
            "Enables debug mode, allowing you to tinker with the hp to test what you want faster, or practice specific phases."
        ).Value;
        INITIAL_HP = Config.Bind(
            "Debug",
            "Initial HP",
            1000,
            "Sets the initial HP if Debug Mode is enabled."
        ).Value;
        PHASE_2_THRESHOLD = Config.Bind(
            "Debug",
            "Phase 2 Threshold",
            990,
            "Sets the phase 2 threshold HP if Debug Mode is enabled."
        ).Value;
        PHASE_3_THRESHOLD = Config.Bind(
            "Debug",
            "Phase 3 Threshold",
            980,
            "Sets the phase 3 threshold HP if Debug Mode is enabled."
        ).Value;
        PHASE_4_THRESHOLD = Config.Bind(
            "Debug",
            "Phase 4 Threshold",
            500,
            "Sets the phase 4 threshold HP if Debug Mode is enabled."
        ).Value;
    }
}