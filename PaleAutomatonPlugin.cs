using System;
using System.Collections;
using BepInEx;
using HarmonyLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Silksong.AssetHelper.ManagedAssets;
using Silksong.FsmUtil;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PaleAutomaton;

[BepInAutoPlugin(id: "io.github.astrum-nova.paleautomaton")]
[BepInDependency("org.silksong-modding.fsmutil")]
[BepInDependency("org.silksong-modding.assethelper")]
public partial class PaleAutomatonPlugin : BaseUnityPlugin
{
    //* Boss References
    public static PaleAutomatonPlugin Instance { get; private set; } = null!;
    public static ManagedAsset<GameObject> SK_ASSET = null!;
    public static ManagedAsset<GameObject> BIG_TITLE = null!;
    public static GameObject songKnightBossScene = null!;
    public static GameObject songKnight = null!;
    public static PlayMakerFSM controlFsm = null!;
    public static HealthManager healthManager = null!;
    public static DamageHero damageHero = null!;
    
    //* Flags
    public static int INITIAL_HP = 1800;
    public static int PHASE_2_THRESHOLD = 1775;
    public static bool PHASE_2 = false;
    public static int PHASE_3_THRESHOLD = 1000;
    public static bool PHASE_3 = false;
    public static int PHASE_4_THRESHOLD = 1000;
    public static bool PHASE_4 = false;
    public static bool bossScene;
    public static bool windslashGround;
    public static bool dashStabbedOnce;
    
    private void Awake()
    {
        Instance = this;
        Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
        Harmony.CreateAndPatchAll(typeof(Patches));
        SK_ASSET = ManagedAsset<GameObject>.FromSceneAsset("hang_17b", "Boss Scene - To Additive Load");
        BIG_TITLE = ManagedAsset<GameObject>.FromSceneAsset("cradle_03", "Boss Scene/Boss Title");
        CustomBehaviour.SK_PROJECTILE_ASSET = ManagedAsset<GameObject>.FromNonSceneAsset("Assets/Prefabs/Hornet Enemies/Song Knight Projectile.prefab", "localpoolprefabs_assets_areahangareasong");
        SceneManager.sceneLoaded += (scene, _) =>
        {
            bossScene = false;
            GameCameras.instance.tk2dCam.ZoomFactor = 1;
            if (!GameManager.instance.IsGameplayScene()) return;
            if (scene.name != "Arborium_11") return;
            PHASE_2 = false;
            PHASE_3 = false;
            PHASE_4 = false;
            windslashGround = false;
            dashStabbedOnce = false;
            var quest = GameObject.Find("Merchant Quest Parent")!;
            if (quest.transform.GetChild(0).gameObject.activeSelf) return; //! REMEMBER TO PLAYTEST THIS
            quest.SetActive(false);
            bossScene = true;
            StartCoroutine(PlaceHornet());
            foreach (var rootGameObject in scene.GetRootGameObjects())
            {
                if (rootGameObject.name.StartsWith("Alert Range")) Destroy(rootGameObject);
                if (rootGameObject.name.StartsWith("Hero Corpse Marker"))
                {
                    if (rootGameObject.name.EndsWith("(10)")) rootGameObject.transform.position = rootGameObject.transform.position with { x = 129 };
                    else Destroy(rootGameObject);
                }
            }
            Destroy(GameObject.Find("citadel_bat_swarms"));
            Destroy(GameObject.Find("bat swarm_bg_left")!);
            StartCoroutine(SpawnSongKnight());
            PlayerData.instance.encounteredSongChevalierBoss = true;
            Pools.Clear();
        };
    }
    private static IEnumerator PlaceHornet()
    {
        yield return new WaitForSeconds(0.3475f);
        HeroController.instance.transform.position = new Vector3(46.8476f, 25.5938f, 0.004f);
        HeroController.instance.vignette.enabled = false;
    }
    private static IEnumerator FancyZoomOut(float duration, float targetZoom)
    {
        var elapsed = 0f;
        var startZoom = GameCameras.instance.tk2dCam.ZoomFactor;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            GameCameras.instance.tk2dCam.ZoomFactor = Mathf.Lerp(startZoom, targetZoom, t < 0.5 ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2);
            yield return null;
        }
        GameCameras.instance.tk2dCam.ZoomFactor = targetZoom;
    }
    private static IEnumerator DisplayBigTitle()
    {
        yield return BIG_TITLE.Load();
        var bigTitle = BIG_TITLE.InstantiateAsset();
        var bigTitleFsm = bigTitle.GetComponent<PlayMakerFSM>();
        bigTitleFsm.SendEvent("TITLE UP");
        //? The text object of the title is disabled cause GMS uses a custom image, we destroy the image to use custom text
        Destroy(bigTitle.transform.GetChild(1).GetChild(2).gameObject);
        yield return new WaitForSeconds(0.1f);
        //? Enable the text object
        bigTitle.transform.GetChild(1).GetChild(1).gameObject.SetActive(true);
        var super = bigTitle.transform.GetChild(1).GetChild(1).GetChild(1).gameObject;
        var main = bigTitle.transform.GetChild(1).GetChild(1).GetChild(0).gameObject;
        super.transform.localScale *= 1.6f;
        main.transform.localScale *= 0.8f;
        super.GetComponent<SetTextMeshProGameText>()!.enabled = false;
        main.GetComponent<SetTextMeshProGameText>()!.enabled = false;
        super.GetComponent<ChangeFontByLanguage>()!.enabled = false;
        main.GetComponent<ChangeFontByLanguage>()!.enabled = false;
        super.SendMessage("SetText", "Pale");
        main.SendMessage("SetText", "Automaton");
    }
    private static IEnumerator SpawnSongKnight()
    {
        yield return SK_ASSET.Load();
        songKnightBossScene = SK_ASSET.InstantiateAsset();
        songKnightBossScene.transform.position = new Vector3(125, 7.19f, 0.004f);
        Destroy(songKnightBossScene.transform.GetChild(0).gameObject);
        Destroy(songKnightBossScene.transform.GetChild(1).gameObject);
        songKnight = songKnightBossScene.transform.GetChild(2).gameObject;
        healthManager = songKnight.GetComponent<HealthManager>();
        healthManager.recoil = null;
        healthManager.hp = INITIAL_HP;
        Destroy(songKnight.LocateMyFSM("Stun Control"));
        controlFsm = songKnight.LocateMyFSM("Control");
        //? These clamp hornets position on connect, these are intended for the original arena so we need to expand them
        //TODO: Fine tune them to the arena borders if you end up making arena borders
        controlFsm.GetFirstActionOfType<FloatClamp>("Dash Slash 3")!.minValue = -1000;
        controlFsm.GetFirstActionOfType<FloatClamp>("Dash Slash 3")!.maxValue = 1000;
        var saveHeroFsm = songKnight.LocateMyFSM("Save Hero");
        saveHeroFsm.GetFirstActionOfType<FloatClamp>("State 2")!.minValue = -1000;
        saveHeroFsm.GetFirstActionOfType<FloatClamp>("State 2")!.maxValue = 1000;
        //? The rising slash animates hornets position to a point, but for some reason the point is always on the right, this fixes it
        var mainFsm = songKnight.LocateMyFSM("FSM");
        mainFsm.GetState("Catch")!.InsertLambdaMethod(_ => { if (HeroController.instance.transform.position.x < songKnight.transform.position.x) mainFsm.GetFirstActionOfType<AnimatePositionTo>("Catch")!.toValue.value.x *= -1; }, 3);
        damageHero = songKnight.GetComponent<DamageHero>();
        damageHero.enabled = false;
        var mainHitbox = songKnight.transform.Find("ComboSlash 1").gameObject.GetComponent<PolygonCollider2D>().points!;
        foreach (var damageHeroComponent in songKnight.GetComponentsInChildren<DamageHero>(true)) damageHeroComponent.SetDamageAmount(2);
        foreach (var hitbox in new[] {"DashStab Hit 1", "DashStab Hit 2", "ComboSlash 2"}) songKnight.transform.Find(hitbox).gameObject.GetComponent<PolygonCollider2D>().SetPath(0, mainHitbox);
        SetupPaleAutomaton();
    }
    public static void PhaseCheck()
    {
        if (healthManager.hp <= PHASE_2_THRESHOLD && !PHASE_2)
        {
            PHASE_2 = true;
            Instance.StartCoroutine(CustomBehaviour.Phase2Transition());
        }
    }
    private static void SetupPaleAutomaton()
    {
        Helpers.RemoveEventFromState("Parry Antic", "TOOK DAMAGE");
        Helpers.RemoveEventFromState("Target Check", "NEEDOLIN");
        Helpers.RemoveEventFromState("Rising Slash Followup", "FALL");
        Helpers.RemoveEventFromState("Rising Slash Followup", "STEP");
        Helpers.RemoveEventFromState("WJ Cross Slash", "CANCEL");
        Helpers.RemoveEventFromState("Jump Rise", "LAND");
        Helpers.RemoveEventFromState("Become Active", "BLOCKED HIT");
        controlFsm.GetState("Enc Wake")!.AddMethod(() =>
        {
            Instance.StartCoroutine(DisplayBigTitle());
            Instance.StartCoroutine(FancyZoomOut(2, 0.675f));
            controlFsm.GetState("Battle Start")!.RemoveActionsOfType<DisplayBossTitle>();
        });
        foreach (var stateName in new[] {"Set DiveSlash", "Set Dash Attack", "Set Wind Slash", "Set CrossSlash", "Set Rising Slash"}) controlFsm.GetState(stateName)!.AddMethod(PhaseCheck);
        controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rising Slash")!.speed = -60f;
        controlFsm.GetFirstActionOfType<SetVelocityAsAngle>("Dive")!.speed = 120f;
        controlFsm.GetFirstActionOfType<DecelerateXY>("Dive Land")!.decelerationX = 0.9f;
        controlFsm.GetFirstActionOfType<SetVelocityByScale>("DashStab Dash")!.speed = -250;
        controlFsm.GetState("WindSlash Antic")!.AddMethod(() => { windslashGround = controlFsm.Fsm.previousActiveState.name.EndsWith('G'); });
        controlFsm.GetLastActionOfType<SetVelocityByScale>("CrossSlash Recoil")!.speed = 15f;
        controlFsm.GetFirstActionOfType<Wait>("Idle")!.time = -1f;
        controlFsm.GetFirstActionOfType<ConvertBoolToFloat>("Idle")!.floatVariable = 0f;
        controlFsm.GetFirstActionOfType<ConvertBoolToFloat>("Idle")!.falseValue = 0f;
        controlFsm.GetFirstActionOfType<ConvertBoolToFloat>("Idle")!.trueValue = 0f;
        controlFsm.GetFirstActionOfType<FloatClamp>("Dive L")!.maxValue = 255;
        controlFsm.GetFirstActionOfType<FloatClamp>("Dive R")!.minValue = 285f;
        controlFsm.GetLastActionOfType<FaceObjectV2>("Dive Dir")!.pauseBetweenTurns = 0f;
        controlFsm.GetLastActionOfType<Wait>("CS Antic")!.time = 0.25f;
        controlFsm.GetState("Rising Slash Antic")!.AddAction(new Wait { time = 0.6f, finishEvent = FsmEvent.Finished, realTime = false });
        controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rising Slash")!.speed = -60f;
        controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rising Slash")!.ySpeed = 15;
        controlFsm.GetState("Dive Antic")!.AddAction(new Wait { time = 0.3f, finishEvent = FsmEvent.Finished, realTime = false });
        controlFsm.GetState("Dive Land")!.AddMethod(() => Instance.StartCoroutine(Helpers.DiveTurnaround()));
        controlFsm.GetState("WindSlash Antic")!.AddAction(new Wait { time = 0.4f, finishEvent = FsmEvent.Finished, realTime = false });
        controlFsm.GetState("DashStab Antic")!.AddAction(new Wait { time = 0.5f, finishEvent = FsmEvent.Finished, realTime = false });
        controlFsm.GetState("DashStab Dash")!.AddAction(new Wait { time = 0.02f, finishEvent = FsmEvent.Finished, realTime = false });
        controlFsm.GetState("CrossSlash 1")!.AddMethod(() => HeroController.instance.StartInvulnerable(0.1f));
        controlFsm.GetState("Rising Slash")!.AddMethod(() => HeroController.instance.StartInvulnerable(0.1f));
        controlFsm.GetState("Stab 1")!.AddMethod(() =>
        {
            controlFsm.StartCoroutine(Helpers.DelayedTurnAround(0.15f));
            if (PHASE_2)
            {
                if (dashStabbedOnce)
                {
                    controlFsm.StartCoroutine(Helpers.ScheduleNextState(0.2f, "DashStab Antic"));
                    dashStabbedOnce = false;
                }
                else dashStabbedOnce = true;
            }
        });
    }
}