using System;
using System.Collections;
using BepInEx;
using HarmonyLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Silksong.AssetHelper.ManagedAssets;
using Silksong.FsmUtil;
using TMProOld;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PaleAutomaton;

[BepInAutoPlugin(id: "io.github.astrum-nova.paleautomaton")]
public partial class PaleAutomatonPlugin : BaseUnityPlugin
{
    //* Cached WaitForSeconds
    private static WaitForSeconds _waitForSeconds0_005 = new(0.005f);

    //* Boss References
    public static PaleAutomatonPlugin Instance { get; private set; } = null!;
    public static ManagedAsset<GameObject> SK_ASSET = null!;
    public static ManagedAsset<GameObject> BIG_TITLE = null!;
    public static GameObject songKnightBossScene = null!;
    public static GameObject songKnight = null!;
    public static GameObject projectile = null!;
    public static PlayMakerFSM controlFsm = null!;
    public static HealthManager healthManager;
    public static DamageHero damageHero;
    public static GameObject projectilePoint;
    
    //* Flags
    public static bool bossScene = false;
    public static bool windslashGround = false;
    private void Awake()
    {
        Instance = this;
        Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
        Harmony.CreateAndPatchAll(typeof(Patches));
        SceneManager.sceneLoaded += (scene, _) =>
        {
            bossScene = false;
            GameCameras.instance.tk2dCam.ZoomFactor = 1;
            if (!GameManager.instance.IsGameplayScene()) return;
            if (scene.name != "Arborium_11") return;
            bossScene = true;
            StartCoroutine(PlaceHornet());
            var quest = GameObject.Find("Merchant Quest Parent")!;
            if (quest.transform.GetChild(0).gameObject.activeSelf) return; //! REMEMBER TO PLAYTEST THIS
            quest.SetActive(false);
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
        SK_ASSET = ManagedAsset<GameObject>.FromSceneAsset("hang_17b", "Boss Scene - To Additive Load");
        BIG_TITLE = ManagedAsset<GameObject>.FromSceneAsset("cradle_03", "Boss Scene/Boss Title");
        CustomBehaviour.SK_PROJECTILE_ASSET = ManagedAsset<GameObject>.FromNonSceneAsset("Assets/Prefabs/Hornet Enemies/Song Knight Projectile.prefab", "localpoolprefabs_assets_areahangareasong");
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
            var easeInOut = t < 0.5 ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
            GameCameras.instance.tk2dCam.ZoomFactor = Mathf.Lerp(startZoom, targetZoom, easeInOut);
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
        Destroy(bigTitle.transform.GetChild(1).GetChild(2).gameObject);
        yield return new WaitForSeconds(0.1f);
        bigTitle.transform.GetChild(1).GetChild(1).gameObject.SetActive(true);
        var super = bigTitle.transform.GetChild(1).GetChild(1).GetChild(1).gameObject;
        var main = bigTitle.transform.GetChild(1).GetChild(1).GetChild(0).gameObject;
        super.transform.localScale *= 1.4f;
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
        projectilePoint = songKnight.transform.Find("Projectile Point").gameObject;
        healthManager = songKnight.GetComponent<HealthManager>();
        healthManager.recoil = null;
        Destroy(songKnight.LocateMyFSM("Stun Control"));
        controlFsm = songKnight.LocateMyFSM("Control");
        controlFsm.GetFirstActionOfType<FloatClamp>("Dash Slash 3")!.minValue = -1000;
        controlFsm.GetFirstActionOfType<FloatClamp>("Dash Slash 3")!.maxValue = 1000;
        var saveHeroFsm = songKnight.LocateMyFSM("Save Hero");
        saveHeroFsm.GetFirstActionOfType<FloatClamp>("State 2")!.minValue = -1000;
        saveHeroFsm.GetFirstActionOfType<FloatClamp>("State 2")!.maxValue = 1000;
        var mainFsm = songKnight.LocateMyFSM("FSM");
        mainFsm.GetState("Catch")!.InsertLambdaMethod(_ => { if (HeroController.instance.transform.position.x < songKnight.transform.position.x) mainFsm.GetFirstActionOfType<AnimatePositionTo>("Catch")!.toValue.value.x *= -1; }, 3);
        damageHero = songKnight.GetComponent<DamageHero>();
        damageHero.enabled = false;
        SetupPaleAutomaton();
    }
    private static void SetupPaleAutomaton()
    {
        Helpers.RemoveEventFromState("Target Check", "NEEDOLIN");
        Helpers.RemoveEventFromState("Rising Slash Followup", "FALL");
        Helpers.RemoveEventFromState("Rising Slash Followup", "STEP");
        Helpers.RemoveEventFromState("WJ Cross Slash", "CANCEL");
        Helpers.RemoveEventFromState("Jump Rise", "LAND");
        controlFsm.GetState("Enc Wake")!.AddLambdaMethod(_ =>
        {
            Instance.StartCoroutine(DisplayBigTitle());
            Instance.StartCoroutine(FancyZoomOut(2, 0.675f));
        });
        controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rising Slash")!.speed = -60f;
        controlFsm.GetFirstActionOfType<SetVelocityAsAngle>("Dive")!.speed = 120f;
        controlFsm.GetFirstActionOfType<DecelerateXY>("Dive Land")!.decelerationX = 0.9f;
        controlFsm.GetFirstActionOfType<SetVelocityByScale>("DashStab Dash")!.speed = -100f;
        controlFsm.GetState("WindSlash Antic")!.AddLambdaMethod(_ => { windslashGround = controlFsm.Fsm.previousActiveState.name.EndsWith('G'); });
        controlFsm.GetLastActionOfType<SetVelocityByScale>("CrossSlash Recoil")!.speed = 15f;
        controlFsm.GetFirstActionOfType<Wait>("Idle")!.time = -1f;
        controlFsm.GetFirstActionOfType<ConvertBoolToFloat>("Idle")!.floatVariable = 0f;
        controlFsm.GetFirstActionOfType<ConvertBoolToFloat>("Idle")!.falseValue = 0f;
        controlFsm.GetLastActionOfType<FaceObjectV2>("Dive Dir")!.pauseBetweenTurns = 0f;
        controlFsm.GetLastActionOfType<Wait>("CS Antic")!.time = 0.25f;
        controlFsm.GetState("Rising Slash Antic")!.AddAction(new Wait { time = 0.6f, finishEvent = FsmEvent.Finished, realTime = false });
        controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rising Slash")!.speed = -60f;
        controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rising Slash")!.ySpeed = 15;
        controlFsm.GetState("Dive Antic")!.AddAction(new Wait { time = 0.3f, finishEvent = FsmEvent.Finished, realTime = false });
        controlFsm.GetState("WindSlash Antic")!.AddAction(new Wait { time = 0.4f, finishEvent = FsmEvent.Finished, realTime = false });
        controlFsm.GetState("DashStab Antic")!.AddAction(new Wait { time = 0.5f, finishEvent = FsmEvent.Finished, realTime = false });
    }
    public IEnumerator Start()
    {
        yield return new WaitForSeconds(2f);
        Harmony.CreateAndPatchAll(typeof(BossTitlePatch));
    }
}