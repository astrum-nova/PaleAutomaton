using System;
using System.Collections;
using System.Linq;
using BepInEx;
using GlobalEnums;
using HarmonyLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using InControl;
using Silksong.AssetHelper.ManagedAssets;
using Silksong.FsmUtil;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

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
    public static ManagedAsset<GameObject> GROUND_SPIKES = null!;
    public static GameObject songKnightBossScene = null!;
    public static GameObject songKnight = null!;
    public static GameObject groundSpikesSetup = null!;
    public static GameObject groundSpikesParent = null!;
    public static GameObject terrainCollider = null!;
    public static PlayMakerFSM controlFsm = null!;
    public static HealthManager healthManager = null!;
    public static DamageHero damageHero = null!;
    
    //* Flags
    public const int INITIAL_HP = 40;
    public const int PHASE_2_THRESHOLD = 30;
    public static bool PHASE_2;
    public const int PHASE_3_THRESHOLD = 20;
    public static bool PHASE_3;
    public const int PHASE_4_THRESHOLD = 10;
    public static bool PHASE_4;
    public static bool bossScene;
    public static bool windslashGround;
    public static bool customComboSequence;
    public static bool dashToWindslashFollowup;
    public static bool rapidSlashFollowupAllowed;
    public static bool dead;
    
    private void Awake()
    {
        Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
        Instance = this;
        Settings.InitializeSettings(Config);
        Harmony.CreateAndPatchAll(typeof(Patches));
        SK_ASSET = ManagedAsset<GameObject>.FromSceneAsset("hang_17b", "Boss Scene - To Additive Load");
        BIG_TITLE = ManagedAsset<GameObject>.FromSceneAsset("cradle_03", "Boss Scene/Boss Title");
        GROUND_SPIKES = ManagedAsset<GameObject>.FromSceneAsset("song_24", "sc_wall_spiked");
        CustomBehaviour.SK_PROJECTILE_ASSET = ManagedAsset<GameObject>.FromNonSceneAsset("Assets/Prefabs/Hornet Enemies/Song Knight Projectile.prefab", "localpoolprefabs_assets_areahangareasong");
        SceneManager.sceneLoaded += (scene, _) =>
        {
            bossScene = false;
            songKnight = null!;
            controlFsm = null!;
            healthManager = null!;
            windslashGround = false;
            customComboSequence = false;
            dashToWindslashFollowup = false;
            rapidSlashFollowupAllowed = false;
            Patches.tookBellBind = false;
            GameCameras.instance.tk2dCam.ZoomFactor = 1;
            Helpers.ToggleDownSlashHitbox(false);
            if (!GameManager.instance.IsGameplayScene()) return;
            if (scene.name != "Arborium_11") return;
            PHASE_2 = false;
            PHASE_3 = false;
            PHASE_4 = false;
            windslashGround = false;
            CustomBehaviour.csSpam = false;
            var blackThreadStates = GameObject.Find("Black Thread States").transform;
            blackThreadStates.GetChild(0).gameObject.SetActive(true);
            blackThreadStates.GetChild(1).gameObject.SetActive(false);
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
                if (rootGameObject.name == "terrain collider") terrainCollider = rootGameObject;
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
        HeroController.instance.transform.position = new Vector3(dead ? 44 : 46.8476f, 25.5938f, 0.004f);
        if (dead)
        {
            Instance.StartCoroutine(CustomBehaviour.LieDown());
            foreach (var tk2dsprite in songKnight.GetComponentsInChildren<tk2dSprite>(true)) tk2dsprite.renderLayer = 0;
        }
        dead = false;
        HeroController.instance.vignette.enabled = false;
        HeroController.instance.transform.Find("Tool Effects").Find("Bell Bind").gameObject.SetActive(true);
        yield return GROUND_SPIKES.Load();
        groundSpikesSetup = GROUND_SPIKES.InstantiateAsset();
        groundSpikesSetup.transform.position = new Vector3(1000, 1000, 0);
        groundSpikesParent = new GameObject("GroundSpikesParent");
        for (var i = 0; i < groundSpikesSetup.transform.childCount; i++) if (i > 7) groundSpikesSetup.transform.GetChild(i).gameObject.SetActive(false);
        for (var i = 0; i < 18; i++)
        {
            yield return null;
            var shifted = Instantiate(groundSpikesSetup, groundSpikesParent.transform, true);
            shifted.transform.GetChild(0).transform.position = shifted.transform.GetChild(0).transform.position with { x = shifted.transform.GetChild(0).transform.position.x - 1.1f };
            shifted.transform.GetChild(1).transform.position = shifted.transform.GetChild(1).transform.position with { x = shifted.transform.GetChild(1).transform.position.x - 1.1f };
            shifted.transform.position = shifted.transform.position with { x = i * 10.15f + 43 };
            shifted.transform.position = shifted.transform.position with { y = 7 };
            shifted.transform.GetChild(0).transform.position = shifted.transform.GetChild(0).transform.position with { z = -1.4537f };
            shifted.transform.GetChild(1).transform.position = shifted.transform.GetChild(1).transform.position with { z = -1.4537f };
            shifted.transform.GetChild(6).transform.position = shifted.transform.GetChild(6).transform.position with { z = -1.4537f };
            shifted.transform.GetChild(7).transform.position = shifted.transform.GetChild(7).transform.position with { z = -1.4537f };
        }
        groundSpikesParent.SetActive(false);
        Helpers.originalDownSlash = HeroController.instance.transform.Find("Attacks").Find("Wanderer").Find("DownSlash").gameObject.GetComponent<PolygonCollider2D>().points;
        Helpers.originalDownSlashAlt = HeroController.instance.transform.Find("Attacks").Find("Wanderer").Find("DownSlashAlt").gameObject.GetComponent<PolygonCollider2D>().points;
    }
    public static IEnumerator FancyZoomOut(float duration, float targetZoom)
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
        bigTitleFsm.GetState("Flash Effect")!.RemoveActionsOfType<ApplyMusicCue>();
        bigTitleFsm.GetState("Flash Effect")!.RemoveActionsOfType<TransitionToAudioSnapshot>();
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
        healthManager.SetImmuneToSpikes(true);
        healthManager.SetImmuneToTraps(true);
        Destroy(songKnight.LocateMyFSM("Stun Control"));
        controlFsm = songKnight.LocateMyFSM("Control");
        Helpers.UpdateSaveHeroClamps();
        var mainFsm = songKnight.LocateMyFSM("FSM");
        mainFsm.GetState("Catch")!.InsertLambdaMethod(_ => { if (HeroController.instance.transform.position.x < songKnight.transform.position.x) mainFsm.GetFirstActionOfType<AnimatePositionTo>("Catch")!.toValue.value.x *= -1; }, 3);
        damageHero = songKnight.GetComponent<DamageHero>();
        damageHero.enabled = false;
        var comboSlash1 = songKnight.transform.Find("ComboSlash 1").gameObject;
        var mainHitbox = comboSlash1.GetComponent<PolygonCollider2D>().points!;
        comboSlash1.transform.localScale = new Vector3(1, 2, 1);
        foreach (var damageHeroComponent in songKnight.GetComponentsInChildren<DamageHero>(true)) damageHeroComponent.SetDamageAmount(2);
        foreach (var hitboxName in new[] {"DashStab Hit 1", "DashStab Hit 2", "ComboSlash 2"})
        {
            var hitbox = songKnight.transform.Find(hitboxName).gameObject;
            hitbox.GetComponent<PolygonCollider2D>().SetPath(0, mainHitbox);
            hitbox.transform.localScale = new Vector3(1, 2, 1);
        }
        CustomBehaviour.rb = songKnight.GetComponent<Rigidbody2D>();
        songKnight.transform.Find("Rising Slash").transform.localScale = new Vector3(1, 1.8f, 1);
        songKnight.transform.Find("RapidSlash Collider").transform.localScale = new Vector3(1.2f, 1f, 1);
        foreach (var tk2dsprite in songKnight.GetComponentsInChildren<tk2dSprite>(true)) tk2dsprite.renderLayer = 500;
        CustomBehaviour.tpEffectSetup = Instantiate(Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g => g.name == "Boss Death FinalHit")!);
        CustomBehaviour.tpEffectSetup.SetActive(false);
        songKnight.transform.GetChild(27).localScale = new Vector3(2.5f, 2.5f, 1);
        CustomBehaviour.crossSlashAnticSetup = Instantiate(songKnight.transform.GetChild(27).gameObject);
        CustomBehaviour.crossSlashAnticSetup.SetActive(false);
        Destroy(CustomBehaviour.tpEffectSetup.transform.Find("white_solid").gameObject);
        Destroy(CustomBehaviour.tpEffectSetup.transform.Find("Slash").gameObject);
        Destroy(CustomBehaviour.tpEffectSetup.transform.Find("Strike L").gameObject);
        Destroy(CustomBehaviour.tpEffectSetup.transform.Find("Strike R").gameObject);
        healthManager.OnDeath += () =>
        {
            dead = true;
            Instance.StopAllCoroutines();
            GameManager.instance.FreezeMoment(FreezeMomentTypes.RaceWinSlow);
            Instance.StartCoroutine(CustomBehaviour.DeathSequence());
        };
        SetupPaleAutomaton();
    }
    private static bool PhaseCheck()
    {
        if (healthManager.hp <= PHASE_2_THRESHOLD && !PHASE_2)
        {
            PHASE_2 = true;
            Instance.StartCoroutine(CustomBehaviour.Phase2Transition());
            SetupPhase2();
            return true;
        }
        if (healthManager.hp <= PHASE_3_THRESHOLD && !PHASE_3 && CustomBehaviour.crossSlashSetup)
        {
            PHASE_3 = true;
            Helpers.constrainPosition.enabled = false;
            Instance.StartCoroutine(CustomBehaviour.Phase3Transition());
            SetupPhase3();
            var hitbox = songKnight.transform.Find("Dive Hit").gameObject;
            hitbox.GetComponent<PolygonCollider2D>().SetPath(0, songKnight.transform.Find("ComboSlash 1").gameObject.GetComponent<PolygonCollider2D>().points);
            hitbox.transform.localScale = new Vector3(1, 2, 1);
            return true;
        }
        return false;
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
        Helpers.RemoveEventFromState("Far Air Attack", "DIVE SLASH");
        controlFsm.GetState("Enc Wake")!.AddMethod(() =>
        {
            Instance.StartCoroutine(DisplayBigTitle());
            Instance.StartCoroutine(FancyZoomOut(2, 0.675f));
            controlFsm.GetState("Battle Start")!.RemoveActionsOfType<DisplayBossTitle>();
            Helpers.SetupArena();
        });
        foreach (var stateName in new[] {"Set DiveSlash", "Set Dash Attack", "Set Wind Slash", "Set CrossSlash", "Set Rising Slash"}) controlFsm.GetState(stateName)!.AddMethod(() =>
        {
            if (!PHASE_3 && Math.Abs(songKnight.transform.position.x - HeroController.instance.transform.position.x) > 60)
            {
                Helpers.constrainPosition.SetYMax(HeroController.instance.transform.position.y + 100);
                Instance.StartCoroutine(CustomBehaviour.Teleport(HeroController.instance.transform.position.x + (Random.value > 0.5f ? 10 : -10), 12.9413f, "First Idle"));
            };
            if (!PhaseCheck()) return;
        });
        controlFsm.GetState("DashStab Dash")!.InsertMethod(() =>
        {
            var speed = Helpers.GetAdaptedSpeed(25, 230, 330);
            if (!PHASE_3) controlFsm.GetFirstActionOfType<SetVelocityByScale>("DashStab Dash")!.speed = -speed;
            else
            {
                var direction = Helpers.GetNormalizedDirection();
                speed *= 1.25f;
                controlFsm.GetFirstActionOfType<SetVelocityByScale>("DashStab Dash")!.speed = direction.x * -speed;
                controlFsm.GetFirstActionOfType<SetVelocityByScale>("DashStab Dash")!.ySpeed = direction.y * speed;
            }
        }, 0);
        controlFsm.GetState("DashStab Dash")!.AddAction(new ActivateGameObject
        {
            gameObject = controlFsm.GetFirstActionOfType<ActivateGameObject>("Stab 1")!.gameObject,
            activate = true,
            recursive = false,
            resetOnExit = false,
            everyFrame = false
        });
        controlFsm.GetFirstActionOfType<SetVelocityAsAngle>("Dive")!.speed = 120f;
        controlFsm.GetFirstActionOfType<DecelerateXY>("Dive Land")!.decelerationX = 0.85f;
        controlFsm.GetState("WindSlash Antic")!.AddMethod(() => { windslashGround = controlFsm.Fsm.previousActiveState.name.EndsWith('G'); });
        controlFsm.GetLastActionOfType<SetVelocityByScale>("CrossSlash Recoil")!.speed = 15f;
        controlFsm.GetFirstActionOfType<Wait>("Idle")!.time = -1f;
        controlFsm.GetFirstActionOfType<ConvertBoolToFloat>("Idle")!.floatVariable = 0f;
        controlFsm.GetFirstActionOfType<ConvertBoolToFloat>("Idle")!.falseValue = 0f;
        controlFsm.GetFirstActionOfType<ConvertBoolToFloat>("Idle")!.trueValue = 0f;
        controlFsm.GetFirstActionOfType<FloatClamp>("Dive L")!.minValue = 180;
        controlFsm.GetFirstActionOfType<FloatClamp>("Dive R")!.maxValue = 360;
        controlFsm.GetLastActionOfType<FaceObjectV2>("Dive Dir")!.pauseBetweenTurns = 0f;
        controlFsm.GetLastActionOfType<Wait>("CS Antic")!.time = 0.25f;
        controlFsm.GetState("Rising Slash Antic")!.AddAction(new Wait { time = 0.6f, finishEvent = FsmEvent.Finished, realTime = false });
        controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rising Slash")!.speed = -80f;
        controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rising Slash")!.ySpeed = 15;
        controlFsm.GetFirstActionOfType<FloatCompare>("Rising Slash Followup")!.float2 = 1000;
        controlFsm.GetState("Dive Antic")!.AddAction(new Wait { time = 0.3f, finishEvent = FsmEvent.Finished, realTime = false });
        controlFsm.GetState("Dive Land")!.AddMethod(() => Instance.StartCoroutine(Helpers.DiveTurnaround()));
        controlFsm.GetState("WindSlash Antic")!.AddAction(new Wait { time = 0.4f, finishEvent = FsmEvent.Finished, realTime = false });
        controlFsm.GetState("DashStab Antic")!.AddAction(new Wait { time = 0.5f, finishEvent = FsmEvent.Finished, realTime = false });
        controlFsm.GetState("DashStab Dash")!.AddAction(new Wait { time = 0.02f, finishEvent = FsmEvent.Finished, realTime = false });
        controlFsm.GetState("CrossSlash 1")!.AddMethod(() => HeroController.instance.StartInvulnerable(0.1f));
        controlFsm.GetState("Rising Slash")!.AddMethod(() => HeroController.instance.StartInvulnerable(0.1f));
        controlFsm.GetState("RapidSlash")!.AddMethod(() => HeroController.instance.StartInvulnerable(0.1f));
        controlFsm.GetState("Stab 1")!.AddMethod(() => controlFsm.StartCoroutine(Helpers.DelayedTurnAround(0.15f)));
    }
    private static void SetupPhase2()
    {
        controlFsm.GetState("Dash to CS?")!.InsertMethod(() => controlFsm.SendEvent("FINISHED"), 0);
        controlFsm.GetState("DashStab Dash")!.InsertMethod(() =>
        {
            var speed = Helpers.GetAdaptedSpeed(35, 260, 870);
            if (!PHASE_3) controlFsm.GetFirstActionOfType<SetVelocityByScale>("DashStab Dash")!.speed = -speed;
            else
            {
                var direction = Helpers.GetNormalizedDirection();
                speed *= 1.25f;
                controlFsm.GetFirstActionOfType<SetVelocityByScale>("DashStab Dash")!.speed = direction.x * -speed;
                controlFsm.GetFirstActionOfType<SetVelocityByScale>("DashStab Dash")!.ySpeed = direction.y * speed;
            }
        }, 0);
        controlFsm.GetState("Dash Slash Antic")!.InsertMethod(() =>
        {
            var speed = Helpers.GetAdaptedSpeed(12.5f, 110, 210);
            if (!PHASE_3) controlFsm.GetFirstActionOfType<SetVelocityByScale>("Dash Slash Antic")!.speed = -speed;
            else
            {
                var direction = Helpers.GetNormalizedDirection();
                speed *= 1.15f;
                controlFsm.GetFirstActionOfType<SetVelocityByScale>("Dash Slash Antic")!.speed = direction.x * -speed;
                controlFsm.GetFirstActionOfType<SetVelocityByScale>("Dash Slash Antic")!.ySpeed = direction.y * speed;
            }
        }, 0);
        controlFsm.GetState("Stab 3")!.InsertMethod(() =>
        {
            var speed = Helpers.GetAdaptedSpeed(25f, 250, 300);
            if (!PHASE_3) controlFsm.GetFirstActionOfType<SetVelocityByScale>("Stab 3")!.speed = -speed;
            else
            {
                var direction = Helpers.GetNormalizedDirection();
                speed *= 1.25f;
                controlFsm.GetFirstActionOfType<SetVelocityByScale>("Stab 3")!.speed = direction.x * -speed;
                controlFsm.GetFirstActionOfType<SetVelocityByScale>("Stab 3")!.ySpeed = direction.y * speed;
            }
        }, 0);
        controlFsm.GetState("Rapid Slash Dash")!.InsertMethod(() => controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rapid Slash Dash")!.speed = -Helpers.GetAdaptedSpeed(4, 30, 300), 0);
        controlFsm.GetState("Dash Slash Antic")!.AddAction(new ActivateGameObject
        {
            gameObject = controlFsm.GetFirstActionOfType<ActivateGameObject>("Dash Slash 1")!.gameObject,
            activate = true,
            recursive = false,
            resetOnExit = false,
            everyFrame = false
        });
        controlFsm.GetState("Dash Slash End")!.AddMethod(() => Instance.StartCoroutine(Helpers.DelayedTurnAround(0.1f)));
        controlFsm.GetState("Dash Slash End")!.AddMethod(() => Instance.StartCoroutine(Helpers.ScheduleNextState(0.25f, "Stab 3")));
        controlFsm.GetState("WindSlash")!.AddMethod(() =>
        {
            if (PHASE_3) return;
            if (customComboSequence) return;
            if (!windslashGround) Instance.StartCoroutine(CustomBehaviour.DoubleWindslashStarter());
            else if (!dashToWindslashFollowup)
            {
                Instance.StartCoroutine(Helpers.ScheduleNextState(0.3f, "DashStab Antic"));
                Instance.StartCoroutine(Helpers.ScheduleNextState(0.47f, "DashStab Dash"));
                dashToWindslashFollowup = true;
            }
            else dashToWindslashFollowup = false;
        });
        controlFsm.GetState("Stab End 2")!.AddMethod(() =>
        {
            if (PHASE_3) return;
            if (customComboSequence) return;
            if (!dashToWindslashFollowup)
            {
                controlFsm.SetState("Windslash G");
                Instance.StartCoroutine(Helpers.ScheduleNextState(0.3f, "WindSlash"));
                dashToWindslashFollowup = true;
            }
            else dashToWindslashFollowup = false;
        });
        controlFsm.GetState("Rising Slash")!.AddMethod(() =>
        {
            if (PHASE_3) return;
            if (customComboSequence) return;
            Instance.StartCoroutine(CustomBehaviour.RisingSlashStarter());
        });
        controlFsm.GetState("Dive Land")!.AddMethod(() =>
        {
            if (PHASE_3) return;
            if (customComboSequence) return;
            Instance.StartCoroutine(CustomBehaviour.DiveStarter());
        });
        controlFsm.GetState("Dash Slash End 2")!.AddMethod(() =>
        {
            if (PHASE_3) return;
            if (!rapidSlashFollowupAllowed || !customComboSequence) return;
            Instance.StartCoroutine(CustomBehaviour.RapidSlashFollowup());
        });
        controlFsm.GetState("Rapid Slash End")!.AddMethod(() =>
        {
            if (PHASE_3) return;
            if (!rapidSlashFollowupAllowed || !customComboSequence) return;
            Instance.StartCoroutine(CustomBehaviour.RapidSlashFollowup());
        });
        controlFsm.GetState("CrossSlash Recoil")!.AddMethod(() =>
        {
            if (PHASE_3) return;
            if (customComboSequence) return;
            Instance.StartCoroutine(CustomBehaviour.CrossSlashStarter());
        });
        controlFsm.GetState("Jump Antic")!.AddAction(new Wait { time = 0.01f, finishEvent = FsmEvent.Finished, realTime = false });
        controlFsm.GetFirstActionOfType<SetFloatValue>("Set Wind Slash")!.floatValue = 9;
    }
    private static void SetupPhase3()
    {
        controlFsm.GetFirstActionOfType<FloatClamp>("Dive L")!.minValue = 135;
        controlFsm.GetFirstActionOfType<FloatClamp>("Dive L")!.maxValue = 270;
        controlFsm.GetFirstActionOfType<FloatClamp>("Dive R")!.minValue = 270;
        controlFsm.GetFirstActionOfType<FloatClamp>("Dive R")!.maxValue = 360;
        controlFsm.GetFirstActionOfType<Wait>("Dive Antic")!.time = 0.6f;
        string[] states = ["DashStab Dash", "Dash Slash Antic", "Stab 3"];
        foreach (var stateName in states) controlFsm.GetState(stateName)!.AddMethod(Helpers.LookAtHornet);
        controlFsm.GetState("CS Antic")!.AddAction(new FaceObjectV2
        {
            objectA = controlFsm.GetFirstActionOfType<FaceObjectV2>("CS Jump Antic")!.objectA,
            objectB = controlFsm.GetFirstActionOfType<FaceObjectV2>("CS Jump Antic")!.objectB,
            spriteFacesRight = false,
            playNewAnimation = false,
            newAnimationClip = "",
            resetFrame = false,
            everyFrame = false,
            pauseBetweenTurns = 0
        });
        controlFsm.GetState("Dive Antic")!.AddAction(new DecelerateXY
        {
            gameObject = controlFsm.GetFirstActionOfType<DecelerateXY>("Dive Land")!.gameObject,
            decelerationX = 0.6f,
            decelerationY = 0.6f,
            brakeOnExit = true
        });
        controlFsm.GetState("First Idle")!.AddAction(new DecelerateXY
        {
            gameObject = controlFsm.GetFirstActionOfType<DecelerateXY>("Dive Land")!.gameObject,
            decelerationX = 0.7f,
            decelerationY = 0.7f,
            brakeOnExit = true
        });
        controlFsm.GetFirstActionOfType<Wait>("WindSlash Antic")!.time = 0.6f;
        MirrorYDecel("Stab 1");
        MirrorYDecel("Stab 2");
        MirrorYDecel("Stab End");
        MirrorYDecel("Dash Slash 1");
        MirrorYDecel("Dash Slash 2");
        MirrorYDecel("Dash Slash End");
        MirrorYDecel("Stab 3");
        MirrorYDecel("Stab 4");
        MirrorYDecel("Stab End 2");
        MirrorYDecel("Dive Land");
        return;
        void MirrorYDecel(string stateName) => controlFsm.GetFirstActionOfType<DecelerateXY>(stateName)!.decelerationY = controlFsm.GetFirstActionOfType<DecelerateXY>(stateName)!.decelerationX;
    }
}