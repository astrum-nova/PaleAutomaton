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
public partial class PaleAutomatonPlugin : BaseUnityPlugin
{
    private static WaitForSeconds _waitForSeconds0_01 = new WaitForSeconds(0.01f);

    public static PaleAutomatonPlugin Instance { get; private set; } = null!;
    public static ManagedAsset<GameObject> SK_ASSET = null!;
    public static GameObject songKnightBossScene = null!;
    public static GameObject songKnight = null!;
    public static GameObject projectile = null!;
    public static PlayMakerFSM controlFsm = null!;
    public static HealthManager healthManager;
    private void Awake()
    {
        Instance = this;
        Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
        Harmony.CreateAndPatchAll(typeof(Patches));
        SceneManager.sceneLoaded += (scene, _) =>
        {
            if (!GameManager.instance.IsGameplayScene()) return;
            if (scene.name != "Arborium_11") return;
            StartCoroutine(PlaceHornet());
            var quest = GameObject.Find("Merchant Quest Parent")!;
            if (quest.transform.GetChild(0).gameObject.activeSelf) return; //! REMEMBER TO PLAYTEST THIS
            quest.SetActive(false);
            foreach (var rootGameObject in scene.GetRootGameObjects()) if (rootGameObject.name.StartsWith("Alert Range")) Destroy(rootGameObject);
            Destroy(GameObject.Find("citadel_bat_swarms"));
            Destroy(GameObject.Find("bat swarm_bg_left")!);
            StartCoroutine(SpawnSongKnight());
            PlayerData.instance.encounteredSongChevalierBoss = true;
        };
        SK_ASSET = ManagedAsset<GameObject>.FromSceneAsset("hang_17b", "Boss Scene - To Additive Load");
    }
    private static IEnumerator PlaceHornet()
    {
        yield return new WaitForSeconds(0.6f);
        GameCameras.instance.cameraFadeFSM.SetState("Scene Fade In");
        HeroController.instance.transform.position = new Vector3(46.8476f, 25.5938f, 0.004f);
        yield return FancyZoomOut();
    }
    private static IEnumerator FancyZoomOut()
    {
        HeroController.instance.vignette.enabled = false;
        for (var i = 0; i < 400; i++)
        {
            yield return _waitForSeconds0_01;
            GameCameras.instance.tk2dCam.ZoomFactor *= 0.999f;
        }
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
        Destroy(songKnight.LocateMyFSM("Stun Control"));
        controlFsm = songKnight.LocateMyFSM("Control");
        controlFsm.GetFirstActionOfType<FloatClamp>("Dash Slash 3")!.minValue = -1000;
        controlFsm.GetFirstActionOfType<FloatClamp>("Dash Slash 3")!.maxValue = 1000;
        var saveHeroFsm = songKnight.LocateMyFSM("Save Hero");
        saveHeroFsm.GetFirstActionOfType<FloatClamp>("State 2")!.minValue = -1000;
        saveHeroFsm.GetFirstActionOfType<FloatClamp>("State 2")!.maxValue = 1000;
        var mainFsm = songKnight.LocateMyFSM("FSM");
        mainFsm.GetState("Catch")!.InsertLambdaMethod(_ => { if (HeroController.instance.transform.position.x < songKnight.transform.position.x) mainFsm.GetFirstActionOfType<AnimatePositionTo>("Catch")!.toValue.value.x *= -1; }, 3);
        Destroy(songKnight.GetComponent<DamageHero>());
        SetupPaleAutomaton();
    }
    private static void SetupPaleAutomaton()
    {
        Helpers.RemoveEventFromState("Target Check", "NEEDOLIN");
        Helpers.RemoveEventFromState("Rising Slash Followup", "FALL");
        Helpers.RemoveEventFromState("Rising Slash Followup", "STEP");
        Helpers.RemoveEventFromState("WJ Cross Slash", "CANCEL");
        Helpers.RemoveEventFromState("Jump Rise", "LAND");
        controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rising Slash")!.speed = -60f;
        controlFsm.GetFirstActionOfType<SetVelocityAsAngle>("Dive")!.speed = 120f;
        controlFsm.GetFirstActionOfType<SetVelocityByScale>("DashStab Dash")!.speed = -100f;
        controlFsm.GetFirstActionOfType<SetFloatValue>("Windslash G")!.floatValue = -70f;
        controlFsm.GetFirstActionOfType<SetVelocity2d>("Windslash G")!.y = 40f;
        controlFsm.GetFirstActionOfType<SetFloatValue>("Windslash A")!.floatValue = -70f;
        controlFsm.GetLastActionOfType<SetFloatValue>("Windslash A")!.floatValue = -40f;
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
}