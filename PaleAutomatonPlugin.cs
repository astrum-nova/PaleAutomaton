using System.Collections;
using AssetsTools.NET.Extra;
using BepInEx;
using HarmonyLib;
using HutongGames.PlayMaker.Actions;
using Silksong.AssetHelper.ManagedAssets;
using Silksong.FsmUtil;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PaleAutomaton;

[BepInAutoPlugin(id: "io.github.astrum-nova.paleautomaton")]
public partial class PaleAutomatonPlugin : BaseUnityPlugin
{
    public static PaleAutomatonPlugin Instance { get; private set; } = null!;
    public static ManagedAsset<GameObject> SK_ASSET = null!;
    public static GameObject songKnightBossScene = null!;
    public static GameObject songKnight = null!;
    public static PlayMakerFSM controlFsm = null!;
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
    private static IEnumerator SpawnSongKnight()
    {
        yield return SK_ASSET.Load();
        songKnightBossScene = SK_ASSET.InstantiateAsset();
        songKnightBossScene.transform.position = new Vector3(125, 7.19f, 0.004f);
        Destroy(songKnightBossScene.transform.GetChild(0).gameObject);
        Destroy(songKnightBossScene.transform.GetChild(1).gameObject);
        songKnight = songKnightBossScene.transform.GetChild(2).gameObject;
        controlFsm = songKnight.LocateMyFSM("Control");
    }
    private static IEnumerator PlaceHornet()
    {
        yield return new WaitForSeconds(0.6f);
        GameCameras.instance.cameraFadeFSM.SetState("Scene Fade In");
        HeroController.instance.transform.position = new Vector3(46.8476f, 25.5938f, 0.004f);
    }
}