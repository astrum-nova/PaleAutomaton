using System;
using System.Collections;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Silksong.AssetHelper.ManagedAssets;
using Silksong.FsmUtil;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace PaleAutomaton;

public static class CustomBehaviour
{
    private static readonly WaitForSeconds _waitForSeconds1_1 = new(1.1f);
    private static readonly WaitForSeconds _waitForSeconds1_7 = new(1.7f);
    private static readonly WaitForSeconds _waitForSeconds0_05 = new(0.05f);
    private static readonly WaitForSeconds _waitForSeconds0_175 = new(0.175f);
    private static readonly WaitForSeconds _waitForSeconds0_01 = new(0.01f);
    private static readonly WaitForSeconds _waitForSeconds0_55 = new(0.55f);
    private static readonly WaitForSeconds _waitForSeconds0_2 = new(0.2f);
    private static readonly WaitForSeconds _waitForSeconds0_1 = new(0.1f);
    private static readonly WaitForSeconds _waitForSeconds0_8 = new(0.8f);
    private static readonly WaitForSeconds _waitForSeconds0_6 = new(0.6f);
    private static readonly WaitForSeconds _waitForSeconds0_5 = new(0.5f);
    private static readonly WaitForSeconds _waitForSeconds0_15 = new(0.15f);
    private static readonly WaitForSeconds _waitForSeconds0_4 = new(0.4f);
    private static readonly WaitForSeconds _waitForSeconds0_7 = new(0.7f);
    private static readonly WaitForSeconds _waitForSeconds1 = new(1);
    private static readonly WaitForSeconds _waitForSeconds0_3 = new(0.3f);
    private static readonly WaitForSeconds _waitForSeconds10 = new(10);
    public static ManagedAsset<GameObject> SK_PROJECTILE_ASSET = null!;
    public static GameObject skProjectileSetup = null!;
    public static GameObject tpEffectSetup = null!;
    public static GameObject crossSlashSetup = null!;
    public static GameObject crossSlashAnticSetup = null!;
    public static GameObject groundSpikesCollider = null!;
    public static GameObject bellBindEffect = null!;
    public static Rigidbody2D rb = null!;
    public static bool teleporting;
    public static bool csSpam;
    public static bool thirdRisingSlash;
    public static bool inPhase4Transition;
    private static readonly List<int> attackMemory = [3, 4, 5];
    private static string parriedState = "";
    //! SPAWN CUSTOM OBJECTS
    public static IEnumerator SpawnWindSlash()
    {
        if (!skProjectileSetup)
        {
            yield return SK_PROJECTILE_ASSET.Load();
            skProjectileSetup = SK_PROJECTILE_ASSET.InstantiateAsset();
            skProjectileSetup.GetComponent<Collider2D>().isTrigger = true;
            Helpers.MakeProjectileIgnoreEnvironment(skProjectileSetup);
            Helpers.RemoveProjectileWallEvents(skProjectileSetup);
            Helpers.MakeProjectileRenderAboveWalls(skProjectileSetup);
            skProjectileSetup.AddComponent<ProjectileMover>();
            skProjectileSetup.GetComponent<DamageHero>().SetDamageAmount(2);
            skProjectileSetup.SetActive(false);
            skProjectileSetup.transform.position = new Vector3(0, -1000, 0);
            skProjectileSetup.name = "WindSlash";
        }
        var instance = Pools.GetWindSlash();
        instance.SetActive(true);
        instance.GetComponent<DamageHero>().enabled = !inPhase4Transition;
        yield return inPhase4Transition ? _waitForSeconds10 : _waitForSeconds1;
        instance.SetActive(false);
        instance.GetComponent<PlayMakerFSM>().Reset();
    }
    private static IEnumerator SpawnCrossSlash(float x, float y, float startDelay, float activationDelay, bool randomizePosition = false, float scaleMultiplier = 1, bool csStarter = false, bool iframes = false)
    {
        var xOffset = randomizePosition ? Random.Range(-2, 2) + 0.00001f : 0;
        var yOffset = randomizePosition ? Random.Range(-2, 2) + 0.00001f : 0;
        if (csStarter && randomizePosition)
        {
            xOffset /= 2;
            yOffset /= 2;
        }
        var rotation = 90 + (randomizePosition ? Random.Range(-10, 10) : 0) + (csStarter && PaleAutomatonPlugin.songKnight.transform.localScale.x == 1 ? 270 : 90);
        var scaleModifier = Random.Range(2f, 2.3f) * scaleMultiplier;
        yield return new WaitForSeconds(startDelay);
        var antic = Pools.GetCrossSlashAntic();
        antic.transform.localScale = new Vector3(1, 1, 1);
        antic.SetActive(true);
        antic.transform.position = new Vector3(x + xOffset, y + yOffset, antic.transform.position.z);
        antic.transform.localScale *= scaleModifier;
        antic.transform.SetRotation2D(rotation);
        antic.transform.FlipLocalScale(y:true);
        yield return new WaitForSeconds(activationDelay - startDelay);
        antic.SetActive(false);
        var crossSlash = Pools.GetCrossSlash();
        crossSlash.transform.localScale = new Vector3(1, 1, 1);
        try { crossSlash.GetComponent<PlayMakerFSM>().GetState("Recycle")!.RemoveActionsOfType<RecycleSelf>(); }
        catch { /*ignored*/ }
        if (iframes) HeroController.instance.StartInvulnerable(0.1f);
        crossSlash.SetActive(true);
        crossSlash.transform.position = antic.transform.position;
        crossSlash.transform.localScale *= scaleModifier;
        crossSlash.transform.SetRotation2D(rotation - 20);
        yield return _waitForSeconds0_3;
        crossSlash.SetActive(false);
    }
    //! MISC
    private static IEnumerator GroundSpikeAntic(GameObject silkSwish, float delay, bool flip = false)
    {
        silkSwish.SetActive(false);
        yield return new WaitForSeconds(delay);
        Object.Destroy(silkSwish.transform.GetChild(2).gameObject);
        silkSwish.transform.position = silkSwish.transform.position with { x = silkSwish.transform.position.x + Random.Range(-6, 6) };
        if (flip) silkSwish.transform.FlipLocalScale(x:true);
        silkSwish.SetActive(true);
    }
    public static IEnumerator LieDown()
    {
        var keepHornetInPlace = HeroController.instance.gameObject.AddComponent<KeepHornetInPlace>();
        var heroController = HeroController.instance;
        var heroAnim = heroController.GetComponent<HeroAnimationController>();
        heroAnim.PlayClipForced("Prostrate");
        yield return _waitForSeconds1;
        var clipLength1 = heroAnim.GetClipDuration("Wake Up Ground");
        heroAnim.PlayClipForced("Wake Up Ground");
        var clip1 = heroAnim.animator.CurrentClip;
        while (heroAnim.animator.IsPlaying(clip1) && clipLength1 > 0f)
        {
            yield return null;
            clipLength1 -= Time.deltaTime;
        }
        keepHornetInPlace.enabled = false;
    }
    public static IEnumerator DeathSequence()
    {
        yield return _waitForSeconds0_7;
        PaleAutomatonPlugin.songKnight = null!;
        GameManager.instance.BeginSceneTransition(new GameManager.SceneLoadInfo
        {
            IsFirstLevelForPlayer = false,
            SceneName = "Hang_17b",
            SceneResourceLocation = null,
            AsyncPriority = 0,
            HeroLeaveDirection = null,
            EntryGateName = null,
            EntryDelay = 0,
            EntrySkip = false,
            PreventCameraFadeOut = false,
            WaitForSceneTransitionCameraFade = false,
            Visualization = GameManager.SceneLoadVisualizations.Default,
            AlwaysUnloadUnusedAssets = false,
            ForceWaitFetch = false,
            TransitionID = 0
        });
        yield return _waitForSeconds0_4;
        PaleAutomatonPlugin.songKnight.transform.position = PaleAutomatonPlugin.songKnight.transform.position with { x = 500000 };
        var corpse = PaleAutomatonPlugin.songKnight.transform.Find("Corpse Song Knight(Clone)").gameObject;
        corpse.SetActive(true);
        var corpseFsm = corpse.GetComponent<PlayMakerFSM>();
        corpseFsm.SetState("Land");
        corpseFsm.GetFirstActionOfType<Tk2dPlayAnimationWithEvents>("Leave Antic")!.clipName = "Bow";
        corpseFsm.GetState("Leave Antic")!.RemoveActionsOfType<AudioPlayRandomVoiceFromTableV2>();
        corpseFsm.GetState("Leave Antic")!.AddMethod(() => corpse.transform.FlipLocalScale(x:true));
        corpseFsm.GetState("Leave Jump")!.AddMethod(() =>
        {
            PaleAutomatonPlugin.Instance.StartCoroutine(Helpers.TpEffect(corpse:true));
            corpse.transform.position = corpse.transform.position with { x = 500000 };
        });
        corpse.transform.position = new Vector3(50.0426f, 27.0411f, 0.0097f);
    }
    public static IEnumerator AnticParry()
    {
        var currentState = PaleAutomatonPlugin.controlFsm.ActiveStateName;
        if (parriedState.Equals(currentState)) yield break;
        PaleAutomatonPlugin.controlFsm.SetState("Parry Dir");
        yield return _waitForSeconds0_15;
        parriedState = currentState;
        PaleAutomatonPlugin.controlFsm.SetState(currentState switch
        {
            "Dive Antic" => "Dive Dir",
            _ => currentState
        });
    }
    public static IEnumerator Teleport(float x, float y, string nextState, float delay = 0.2f, float finishNextStateIn = -1, bool lookAtHornet = false)
    {
        teleporting = true;
        rb.linearVelocityY = 0;
        rb.linearVelocityX = 0;
        PaleAutomatonPlugin.controlFsm.Fsm.manualUpdate = true;
        var transform = PaleAutomatonPlugin.songKnight.transform;
        PaleAutomatonPlugin.Instance.StartCoroutine(Helpers.TpEffect());
        transform.position = transform.position with { y = HeroController.instance.transform.position.y + 100 };
        PaleAutomatonPlugin.controlFsm.SetState("First Idle");
        yield return new WaitForSeconds(delay);
        transform.position = transform.position with { x = x };
        transform.position = transform.position with { y = y };
        PaleAutomatonPlugin.Instance.StartCoroutine(Helpers.TpEffect());
        PaleAutomatonPlugin.controlFsm.Fsm.manualUpdate = false;
        PaleAutomatonPlugin.controlFsm.SetState(nextState);
        if (lookAtHornet) Helpers.LookAtHornet();
        else PaleAutomatonPlugin.songKnight.transform.rotation = Quaternion.Euler(0, 0, 0);
        if (finishNextStateIn != -1)
        {
            yield return new WaitForSeconds(finishNextStateIn);
            PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
        }
        Helpers.DisableChargingEffects();
        teleporting = false;
    }
    //! PHASE TRANSITIONS
    public static IEnumerator Phase2Transition()
    {
        Helpers.DisableChargingEffects();
        PaleAutomatonPlugin.controlFsm.SetState("Parry Antic");
        PaleAutomatonPlugin.controlFsm.GetState("Parry Stance")!.AddAction(new StartRoarEmitter
        {
            spawnPoint = new FsmOwnerDefault { gameObject = PaleAutomatonPlugin.songKnight, GameObject = PaleAutomatonPlugin.songKnight },
            delay = 0f,
            stunHero = false,
            roarBurst = false,
            isSmall = false,
            noVisualEffect = false,
            forceThroughBind = true,
            stopOnExit = true
        });
        yield return _waitForSeconds0_5;
        PaleAutomatonPlugin.controlFsm.SendEvent("BLOCKED HIT");
        yield return _waitForSeconds0_3;
        HeroController.instance.StartInvulnerable(0.2f);
        yield return _waitForSeconds1;
        PaleAutomatonPlugin.controlFsm.GetState("Parry Stance")!.RemoveActionsOfType<StartRoarEmitter>();
        PaleAutomatonPlugin.controlFsm.GetState("Rapid Slash End")!.AddAction(new SetVelocityByScale
        {
            gameObject = PaleAutomatonPlugin.controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rapid Slash Dash")!.gameObject,
            speed = 90,
            ySpeed = 0,
            everyFrame = false
        });
        PaleAutomatonPlugin.controlFsm.GetState("Dash Slash End 2")!.AddAction(new SetVelocityByScale
        {
            gameObject = PaleAutomatonPlugin.controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rapid Slash Dash")!.gameObject,
            speed = 90,
            ySpeed = 0,
            everyFrame = false
        });
    }
    public static IEnumerator Phase3Transition()
    {
        Helpers.DisableChargingEffects();
        PaleAutomatonPlugin.Instance.StartCoroutine(Teleport(100, HeroController.instance.transform.position.y + 500, "First Idle"));
        PaleAutomatonPlugin.controlFsm.Fsm.ManualUpdate = true;
        InfiniteTerrainMover.cameraLockArea.cameraYMax = 15;
        InfiniteTerrainMover.cameraLockArea.enabled = false;
        InfiniteTerrainMover.cameraLockArea.enabled = true;
        InfiniteTerrainMover.cameraLockArea.OnInsideStateChanged(true);
        yield return _waitForSeconds0_6;
        var silkSwishOriginal = GameObject.Find("Boss Title(Clone)").transform.GetChild(0).gameObject;
        silkSwishOriginal.SetActive(false);
        foreach (var spriteRenderer in silkSwishOriginal.GetComponentsInChildren<SpriteRenderer>()) spriteRenderer.sortingOrder = 500;
        silkSwishOriginal.transform.position = new Vector3(1.2f, -5, 1);
        silkSwishOriginal.transform.localScale = new Vector3(1.2f, 0.6114f, 0.8734f);
        PaleAutomatonPlugin.Instance.StartCoroutine(GroundSpikeAntic(Object.Instantiate(silkSwishOriginal), 0));yield return null;
        PaleAutomatonPlugin.Instance.StartCoroutine(GroundSpikeAntic(Object.Instantiate(silkSwishOriginal), 0.5f, true));yield return null;
        PaleAutomatonPlugin.Instance.StartCoroutine(GroundSpikeAntic(Object.Instantiate(silkSwishOriginal), 0.2f));yield return null;
        PaleAutomatonPlugin.Instance.StartCoroutine(GroundSpikeAntic(Object.Instantiate(silkSwishOriginal), 0.25f, true));yield return null;
        PaleAutomatonPlugin.Instance.StartCoroutine(GroundSpikeAntic(Object.Instantiate(silkSwishOriginal), 0.4f));yield return null;
        PaleAutomatonPlugin.Instance.StartCoroutine(GroundSpikeAntic(Object.Instantiate(silkSwishOriginal), 0.45f, true));yield return null;
        PaleAutomatonPlugin.Instance.StartCoroutine(GroundSpikeAntic(Object.Instantiate(silkSwishOriginal), 0.6f));yield return null;
        PaleAutomatonPlugin.Instance.StartCoroutine(GroundSpikeAntic(Object.Instantiate(silkSwishOriginal), 0.65f, true));yield return null;
        PaleAutomatonPlugin.Instance.StartCoroutine(GroundSpikeAntic(Object.Instantiate(silkSwishOriginal), 0.8f));yield return null;
        PaleAutomatonPlugin.Instance.StartCoroutine(GroundSpikeAntic(Object.Instantiate(silkSwishOriginal), 0.85f, true));yield return null;
        yield return Teleport(500, HeroController.instance.transform.position.y + 100, "First Idle");
        yield return _waitForSeconds0_6;
        silkSwishOriginal.transform.position = new Vector3(-2f, -4, 1);
        silkSwishOriginal.transform.localScale = new Vector3(5.1726f, 0.6114f, 0.8734f);
        silkSwishOriginal.SetActive(true);
        yield return _waitForSeconds0_4;
        Object.Destroy(PaleAutomatonPlugin.songKnight.transform.Find("WindSlash Hit").gameObject);
        PaleAutomatonPlugin.groundSpikesParent.SetActive(true);
        Helpers.clonedGroundSpikes.SetActive(true);
        groundSpikesCollider.SetActive(true);
        Helpers.clonedGroundSpikesCollider.SetActive(true);
        if (!Settings.DISABLE_CAMERA_ZOOMOUT) PaleAutomatonPlugin.Instance.StartCoroutine(PaleAutomatonPlugin.FancyZoomOut(2, 0.575f));
        yield return _waitForSeconds0_5;
        PaleAutomatonPlugin.terrainCollider.transform.position = PaleAutomatonPlugin.terrainCollider.transform.position with {y = -50};
        var otherTerrainCollider = Helpers.clonedTerrainArt.transform.Find("Terrain Collider");
        otherTerrainCollider.transform.position = otherTerrainCollider.transform.position with { y = -50 };
        yield return _waitForSeconds0_5;
        PaleAutomatonPlugin.controlFsm.Fsm.ManualUpdate = true;
        InfiniteTerrainMover.cameraLockArea.cameraYMax = 100000;
        InfiniteTerrainMover.cameraLockArea.enabled = false;
        InfiniteTerrainMover.cameraLockArea.enabled = true;
        InfiniteTerrainMover.cameraLockArea.OnInsideStateChanged(true);
        if (Settings.CUSTOM_POGO_HITBOX) Helpers.ToggleDownSlashHitbox(true);
        if (Settings.BELL_BIND_EFFECT_ON_THE_BOSS)
        {
            yield return PaleAutomatonPlugin.BELL_BIND_EFFECT.Load();
            bellBindEffect = PaleAutomatonPlugin.BELL_BIND_EFFECT.InstantiateAsset();
            bellBindEffect.transform.SetParent(PaleAutomatonPlugin.songKnight.transform);
            bellBindEffect.GetComponent<FollowTransform>().enabled = false;
            bellBindEffect.transform.localScale = new Vector3(1.7f, 1.7f, 1);
            bellBindEffect.transform.localPosition = Vector3.zero;
            bellBindEffect.SetActive(true);
        }
        PaleAutomatonPlugin.songKnight.transform.Find("Rising Slash").transform.localScale = new Vector3(1, 1f, 1);
        PaleAutomatonPlugin.Instance.StartCoroutine(SelectPhase3Attack());
    }
    private static IEnumerator Phase4Transition()
    {
        Helpers.DisableChargingEffects();
        inPhase4Transition = true;
        var hPos = HeroController.instance.transform.position;
        var yPos = Math.Clamp(hPos.y - 8, 20, hPos.y + 9999);
        yield return Teleport(hPos.x, yPos, "Windslash A");
        PaleAutomatonPlugin.songKnight.transform.Find("Charge Effect").gameObject.SetActive(true);
        PaleAutomatonPlugin.songKnight.transform.localScale = PaleAutomatonPlugin.songKnight.transform.localScale with {x = 1};
        PaleAutomatonPlugin.songKnight.transform.SetRotation2D(90);
        PaleAutomatonPlugin.controlFsm.Fsm.Stop();
        yield return _waitForSeconds0_8;
        PaleAutomatonPlugin.controlFsm.Fsm.Start();
        PaleAutomatonPlugin.controlFsm.SetState("WindSlash");
        PaleAutomatonPlugin.songKnight.transform.Find("Charge Effect").gameObject.SetActive(false);
        yield return _waitForSeconds0_1;
        PaleAutomatonPlugin.groundSpikesParent.SetActive(false);
        GameObject.Find("Main Terrain Art").SetActive(false);
        GameObject.Find("Cloned Terrain Art").SetActive(false);
        yield return _waitForSeconds0_2;
        PaleAutomatonPlugin.songKnight.transform.SetRotation2D(0);
        yield return Teleport(hPos.x, HeroController.instance.transform.position.y + 100, "First Idle");
        inPhase4Transition = false;
        yield return _waitForSeconds0_5;
    }
    //! PHASE 2
    public static IEnumerator RisingSlashStarter()
    {
        PaleAutomatonPlugin.customComboSequence = true;
        yield return _waitForSeconds0_55;
        PaleAutomatonPlugin.controlFsm.SetState("CrossSlash 1");
        yield return _waitForSeconds0_1;
        PaleAutomatonPlugin.controlFsm.SetState("Rising Slash Antic");
        PaleAutomatonPlugin.controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rising Slash")!.speed = -70f;
        yield return _waitForSeconds0_01;
        PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
        yield return _waitForSeconds0_4;
        PaleAutomatonPlugin.controlFsm.SetState("Windslash A");
        yield return _waitForSeconds0_3;
        PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
        if (Random.value > 0.5f)
        {
            yield return _waitForSeconds0_2;
            PaleAutomatonPlugin.controlFsm.SetState("WindSlash");
            yield return _waitForSeconds0_2;
            PaleAutomatonPlugin.controlFsm.SetState("Dive Dir");
            yield return _waitForSeconds0_2;
        }
        else
        {
            yield return _waitForSeconds0_175;
            PaleAutomatonPlugin.controlFsm.SetState("Dive Dir");
            yield return _waitForSeconds0_05;
            PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
            yield return _waitForSeconds0_3;
            PaleAutomatonPlugin.controlFsm.SetState("Windslash G");
            yield return _waitForSeconds0_3;
            PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
        }
        PaleAutomatonPlugin.controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rising Slash")!.speed = -80f;
        yield return _waitForSeconds0_3;
        PaleAutomatonPlugin.customComboSequence = false;
    }
    public static IEnumerator DoubleWindslashStarter()
    {
        PaleAutomatonPlugin.customComboSequence = true;
        yield return _waitForSeconds0_2;
        PaleAutomatonPlugin.controlFsm.SetState("WindSlash");
        yield return _waitForSeconds0_2;
        PaleAutomatonPlugin.controlFsm.SetState("Dive Dir");
        yield return _waitForSeconds0_6;
        PaleAutomatonPlugin.controlFsm.SetState("Windslash G");
        yield return _waitForSeconds0_3;
        PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
        yield return _waitForSeconds0_3;
        PaleAutomatonPlugin.customComboSequence = false;
    }
    public static IEnumerator RapidSlashFollowup()
    {
        PaleAutomatonPlugin.controlFsm.SetState("DashStab Antic");
        PaleAutomatonPlugin.Instance.StartCoroutine(Helpers.ScheduleNextState(0.4f, "Stab 3"));
        yield return _waitForSeconds0_3;
        PaleAutomatonPlugin.customComboSequence = false;
    }
    public static IEnumerator DiveStarter()
    {
        PaleAutomatonPlugin.customComboSequence = true;
        var radpiSlash = Random.value > 0.5f;
        if (radpiSlash)
        {
            yield return _waitForSeconds0_3;
            PaleAutomatonPlugin.rapidSlashFollowupAllowed = true;
            PaleAutomatonPlugin.controlFsm.SetState("Rapid Slash Dash");
        }
        else
        {
            yield return _waitForSeconds0_2;
            PaleAutomatonPlugin.controlFsm.SetState("Rising Slash Antic");   
            yield return _waitForSeconds0_15;
            PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");   
            yield return _waitForSeconds0_4;
            PaleAutomatonPlugin.controlFsm.SetState("CS Antic");
            yield return _waitForSeconds0_1;
            PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
            yield return _waitForSeconds0_3;
            PaleAutomatonPlugin.controlFsm.SetState("Windslash A");
            yield return _waitForSeconds0_3;
            PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
        }
        yield return _waitForSeconds0_3;
        if (!radpiSlash) PaleAutomatonPlugin.customComboSequence = false;
    }
    public static IEnumerator CrossSlashStarter()
    {
        PaleAutomatonPlugin.customComboSequence = true;
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(PaleAutomatonPlugin.songKnight.transform.localScale.x * -9 + PaleAutomatonPlugin.songKnight.transform.position.x, PaleAutomatonPlugin.songKnight.transform.position.y - 2, 0.05f, 0.15f, scaleMultiplier: 0.7f, csStarter:true));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(PaleAutomatonPlugin.songKnight.transform.localScale.x * -15 + PaleAutomatonPlugin.songKnight.transform.position.x, PaleAutomatonPlugin.songKnight.transform.position.y - 3, 0.1f, 0.3f, scaleMultiplier: 0.4f, csStarter:true));
        yield return _waitForSeconds0_1;
        PaleAutomatonPlugin.controlFsm.SetState("Windslash A");
        yield return _waitForSeconds0_3;
        PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
        yield return _waitForSeconds0_175;
        PaleAutomatonPlugin.controlFsm.SetState("Dive Dir");
        yield return _waitForSeconds0_05;
        PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
        yield return _waitForSeconds0_4;
        PaleAutomatonPlugin.customComboSequence = false;
    }
    //! PHASE 3+4
    private static IEnumerator SelectPhase3Attack()
    {
        if (!PaleAutomatonPlugin.bossScene) yield break;
        while (PaleAutomatonPlugin.songKnight)
        {
            PaleAutomatonPlugin.controlFsm.FsmVariables.GetFsmFloat("Gravity").Value = 0;
            PaleAutomatonPlugin.controlFsm.SetState("First Idle");
            yield return _waitForSeconds0_5;
            if (PaleAutomatonPlugin.healthManager.hp <= PaleAutomatonPlugin.PHASE_4_THRESHOLD && !PaleAutomatonPlugin.PHASE_4)
            {
                PaleAutomatonPlugin.PHASE_4 = true;
                yield return Phase4Transition();
            }
            if (PaleAutomatonPlugin.PHASE_4)
            {
                Helpers.fallKiller.transform.position = Helpers.fallKiller.transform.position with { x = HeroController.instance.transform.position.x - 250 };
                var claBounds = InfiniteTerrainMover.cameraLockArea.box2d.bounds;
                claBounds.min = claBounds.min with { x = HeroController.instance.transform.position.x - 500 };
                claBounds.max = claBounds.max with { x = HeroController.instance.transform.position.x + 500 };
                InfiniteTerrainMover.cameraLockArea.cameraXMin = HeroController.instance.transform.position.x - 500;
                InfiniteTerrainMover.cameraLockArea.cameraXMax = HeroController.instance.transform.position.x + 500;
                InfiniteTerrainMover.cameraLockArea.cameraYMax = HeroController.instance.transform.position.y + 100000;
                Helpers.UpdateSaveHeroClamps();
                InfiniteTerrainMover.cameraLockArea.enabled = false;
                Helpers.cameraLockArea.transform.position = Helpers.cameraLockArea.transform.position with { x = HeroController.instance.transform.position.x };
                InfiniteTerrainMover.cameraLockArea.enabled = true;
                InfiniteTerrainMover.cameraLockArea.OnInsideStateChanged(true);
            }
            if (PaleAutomatonPlugin.controlFsm.GetFsmBoolIfExists("Hornet Dead"))
            {
                yield return Teleport(HeroController.instance.transform.position.x, HeroController.instance.transform.position.y + 100, "First Idle");
                yield return _waitForSeconds1;
                Pools.DisableAll();
                yield break;
            }
            int attack;
            do attack = Random.Range(1, 6); while (attackMemory.Contains(attack));
            attackMemory.RemoveAt(0);
            attackMemory.Add(attack);
            yield return 5 switch
            {
                1 => WindSlashSpam(),
                2 => LiterallyBoundlessInfinity(),
                3 => CrossSlashSpam(),
                4 => DiveIntoCrossSlash(),
                5 => TripleRisingSlash(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        yield return _waitForSeconds1;
        Pools.DisableAll();
    }
    private static IEnumerator LiterallyBoundlessInfinity()
    {
        var direction = Random.value > 0.5f ? -1 : 1;
        var hcPos = HeroController.instance.transform.position;
        var yOffset = Random.Range(-3, 3);
        var xPos = hcPos.x + 13 * direction - yOffset * direction;
        yield return Teleport(xPos, hcPos.y + yOffset, "DashStab Antic", lookAtHornet:true);
        yield return _waitForSeconds1_7;
        PaleAutomatonPlugin.controlFsm.SetState("DashStab Antic");
        yield return _waitForSeconds0_05;
        PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
        yield return _waitForSeconds1_1;
    }
    private static IEnumerator DiveIntoCrossSlash()
    {
        //todo: make this more consistent
        var direction = Random.value > 0.5f ? -1 : 1;
        var hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + 8.5f * direction, hcPos.y + 5, "Dive Dir");
        yield return _waitForSeconds0_7;
        hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + 8.5f * -direction, hcPos.y + 5, "Dive Dir", delay:0, finishNextStateIn: 0.2f);
        yield return _waitForSeconds0_2;
        hcPos = HeroController.instance.transform.position;
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x, hcPos.y, 0f, 0.4f));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x - 7, hcPos.y + 7, 0.05f, 0.45f, true));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x + 7, hcPos.y - 7, 0.1f, 0.5f, true));
        yield return Teleport(hcPos.x, hcPos.y + 500, "First Idle", delay:0f, finishNextStateIn: 0f);
        yield return _waitForSeconds0_8;
        hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + 8.5f * -direction, hcPos.y + 5, "Dive Dir", finishNextStateIn: 0.2f);
        yield return _waitForSeconds0_2;
        hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + 8.5f * direction, hcPos.y + 5, "Dive Dir", delay:0, finishNextStateIn: 0.2f);
        yield return _waitForSeconds0_2;
        hcPos = HeroController.instance.transform.position;
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x, hcPos.y, 0f, 0.4f));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x + 7, hcPos.y + 7, 0.05f, 0.45f, true));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x - 7, hcPos.y - 7, 0.1f, 0.5f, true));
        yield return Teleport(hcPos.x, hcPos.y + 500, "First Idle", delay:0f, finishNextStateIn: 0f);
        yield return _waitForSeconds0_8;
        hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + 8.5f * -direction, hcPos.y + 5, "Dive Dir", finishNextStateIn: 0.2f);
        yield return _waitForSeconds0_2;
    }
    private static IEnumerator WindSlashSpam()
    {
        var direction = Random.value > 0.5f ? -1 : 1;
        var xOffset = Random.Range(10f, 13f) * direction;
        var yOffset = Random.Range(-4f, 1f);
        var hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + xOffset, hcPos.y + yOffset, "Windslash A", lookAtHornet:true);
        yield return _waitForSeconds0_6;
        PaleAutomatonPlugin.controlFsm.SetState("WindSlash");
        yield return _waitForSeconds0_2;
        xOffset = Random.Range(10f, 13f) * -direction;
        yOffset = Random.Range(-4f, 1f);
        hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + xOffset, hcPos.y + yOffset, "Windslash A", lookAtHornet:true);
        yield return _waitForSeconds0_6;
        xOffset = Random.Range(10f, 13f) * direction;
        yOffset = Random.Range(-4f, 1f);
        hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + xOffset, hcPos.y + yOffset, "Windslash A", lookAtHornet:true);
        yield return _waitForSeconds0_6;
        xOffset = Random.Range(10f, 13f) * -direction;
        yOffset = Random.Range(-4f, 1f);
        hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + xOffset, hcPos.y + yOffset, "Windslash A", lookAtHornet:true);
        yield return _waitForSeconds0_6;
        PaleAutomatonPlugin.controlFsm.SetState("WindSlash");
        yield return _waitForSeconds0_2;
        PaleAutomatonPlugin.controlFsm.SetState("WindSlash");
        yield return _waitForSeconds0_2;
    }
    private static IEnumerator CrossSlashSpam()
    {
        var anticTime = 0.8f;
        PaleAutomatonPlugin.controlFsm.GetFirstActionOfType<Wait>("CS Antic")!.time = anticTime;
        var direction = Random.value > 0.5f ? -1 : 1;
        var hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + 3 * direction, hcPos.y - 2, "CS Antic", lookAtHornet:true);
        yield return new WaitForSeconds(anticTime + 0.2f);
        hcPos = HeroController.instance.transform.position;
        PaleAutomatonPlugin.Instance.StartCoroutine(Teleport(hcPos.x, hcPos.y + 500, "First Idle"));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x, hcPos.y, 0, 0.3f, csStarter:true, randomizePosition:true, iframes:true));
        yield return _waitForSeconds0_6;
        hcPos = HeroController.instance.transform.position;
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x, hcPos.y, 0, 0.3f, csStarter:true, randomizePosition:true, iframes:true));
        yield return Teleport(hcPos.x + 3 * -direction, hcPos.y - 2, "CS Antic", lookAtHornet:true);
        yield return new WaitForSeconds(anticTime + 0.2f);
        hcPos = HeroController.instance.transform.position;
        PaleAutomatonPlugin.Instance.StartCoroutine(Teleport(hcPos.x, hcPos.y + 500, "First Idle"));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x, hcPos.y, 0, 0.3f, csStarter:true, randomizePosition:true, iframes:true));
        yield return _waitForSeconds0_6;
        hcPos = HeroController.instance.transform.position;
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x, hcPos.y, 0, 0.3f, csStarter:true, randomizePosition:true, iframes:true));
        yield return Teleport(hcPos.x + 3 * direction, hcPos.y - 2, "CS Antic", lookAtHornet:true);
        yield return new WaitForSeconds(anticTime + 0.2f);
        hcPos = HeroController.instance.transform.position;
        PaleAutomatonPlugin.Instance.StartCoroutine(Teleport(hcPos.x, hcPos.y + 500, "First Idle"));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x, hcPos.y, 0, 0.3f, csStarter:true, randomizePosition:true, iframes:true));
        yield return _waitForSeconds0_6;
        hcPos = HeroController.instance.transform.position;
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x, hcPos.y, 0, 0.3f, csStarter:true, randomizePosition:true, iframes:true));
        yield return _waitForSeconds0_6;
    }
    private static IEnumerator TripleRisingSlash()
    {
        thirdRisingSlash = false;
        PaleAutomatonPlugin.controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rising Slash")!.ySpeed = 40;
        var direction = Random.value > 0.5f ? -1 : 1;
        var hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + 4 * direction, hcPos.y - 6, "Rising Slash Antic", finishNextStateIn: 0.5f);
        yield return _waitForSeconds0_5;
        PaleAutomatonPlugin.controlFsm.SetState("CrossSlash 1");
        yield return _waitForSeconds0_1;
        hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + 4 * -direction, hcPos.y - 6, "Rising Slash Antic", finishNextStateIn: 0.1f);
        yield return _waitForSeconds0_5;
        PaleAutomatonPlugin.controlFsm.SetState("CrossSlash 1");
        yield return _waitForSeconds0_1;
        PaleAutomatonPlugin.controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rising Slash")!.speed = 0;
        PaleAutomatonPlugin.controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rising Slash")!.ySpeed = 90;
        hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x, hcPos.y - 6, "Rising Slash Antic", finishNextStateIn: 0.1f);
        yield return _waitForSeconds0_2;
        thirdRisingSlash = true;
        yield return _waitForSeconds0_3;
        PaleAutomatonPlugin.controlFsm.SetState("Windslash A");
        yield return _waitForSeconds0_2;
        PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
        PaleAutomatonPlugin.controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rising Slash")!.speed = -80;
        PaleAutomatonPlugin.controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rising Slash")!.ySpeed = 15;
        yield return _waitForSeconds0_2;
    }
    //! CUT ATTACKS
    /*
    public static IEnumerator FiveDive()
    {
        var hcPos = HeroController.instance.transform.position;
        float direction = Random.value > 0.5f ? -1 : 1;
        float yOffset = Random.Range(-3, 3);
        yield return Teleport(hcPos.x + 10 * direction - Math.Abs(yOffset) * direction, hcPos.y + 6, "Dive Antic");
        yield return new WaitForSeconds(0.6f);
        PaleAutomatonPlugin.controlFsm.SetState("Dive Antic");
        yield return new WaitForSeconds(0.2f);
        PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
        yield return new WaitForSeconds(0.2f);
        PaleAutomatonPlugin.controlFsm.SetState("Dive Antic");
        yield return new WaitForSeconds(0.2f);
        PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
        yield return new WaitForSeconds(0.2f);
        PaleAutomatonPlugin.controlFsm.SetState("Dive Antic");
        yield return new WaitForSeconds(0.2f);
        PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
        yield return new WaitForSeconds(0.2f);
        hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x, hcPos.y + 6, "Dive Antic", delay:0);
        PaleAutomatonPlugin.songKnight.transform.SetRotation2D(180);
        yield return new WaitForSeconds(0.2f);
        PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
        yield return new WaitForSeconds(0.2f);
    }
    public static IEnumerator DashSlashIntoCrossSlash()
    {
        //instead of stall tps try tping into dashstab antic and letting it go normally without finishing the state early, and try using the tp delay
        //also update the hcpos for the crossslashes too cause its using an old pos
        var direction = Random.value > 0.5f ? -1 : 1;
        var hcPos = HeroController.instance.transform.position;
        var xPos = hcPos.x + 13 * direction;
        yield return Teleport(xPos, hcPos.y + Random.Range(-3, 3), "DashStab Antic");
        yield return new WaitForSeconds(1.1f);
        PaleAutomatonPlugin.Instance.StartCoroutine(Teleport(xPos, 100, "First Idle"));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x, hcPos.y, 0f, 0.4f));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x + 7, hcPos.y + 7, 0.05f, 0.45f, true));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x - 7, hcPos.y - 7, 0.1f, 0.5f, true));
        yield return new WaitForSeconds(0.6f);
        hcPos = HeroController.instance.transform.position;
        xPos = hcPos.x + 13 * direction;
        yield return Teleport(xPos, hcPos.y + Random.Range(-3, 3), "DashStab Antic", finishNextStateIn: 0.4f);
        yield return new WaitForSeconds(0.7f);
        PaleAutomatonPlugin.Instance.StartCoroutine(Teleport(xPos, 100, "First Idle"));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x, hcPos.y, 0f, 0.4f));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x - 7, hcPos.y + 7, 0.05f, 0.45f, true));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x + 7, hcPos.y - 7, 0.1f, 0.5f, true));
        yield return new WaitForSeconds(0.6f);
        hcPos = HeroController.instance.transform.position;
        xPos = hcPos.x + 13 * direction;
        yield return Teleport(xPos, hcPos.y + Random.Range(-3, 3), "DashStab Antic", finishNextStateIn: 0.4f);
        yield return new WaitForSeconds(0.3f);
    }
    public static IEnumerator CrossSlashSpam()
    {
        var anticTime = 0.8f;
        PaleAutomatonPlugin.controlFsm.GetFirstActionOfType<Wait>("CS Antic")!.time = anticTime;
        var direction = Random.value > 0.5f ? -1 : 1;
        var hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + 3 * direction, hcPos.y + 2, "CS Antic");
        csSpam = true;
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x + 7, hcPos.y - 5, 0.1f, anticTime + 0.014f, true));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x - 7, hcPos.y - 5, 0.2f, anticTime + 0.015f, true));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x + 10, hcPos.y + 0, 0.3f, anticTime + 0.012f, true));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x - 10, hcPos.y + 0, 0.4f, anticTime + 0.013f, true));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x + 0, hcPos.y + 8, 0.5f, anticTime + 0.011f, true));
        yield return new WaitForSeconds(anticTime + 0.2f);
        csSpam = false;
    }
    */
}