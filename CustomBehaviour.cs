using System;
using System.Collections;
using System.Collections.Generic;
using GenericVariableExtension;
using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Silksong.AssetHelper.ManagedAssets;
using Silksong.FsmUtil;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace PaleAutomaton;

public static class CustomBehaviour
{
    public static ManagedAsset<GameObject> SK_PROJECTILE_ASSET = null!;
    public static GameObject skProjectileSetup = null!;
    public static GameObject tpEffectSetup = null!;
    public static GameObject crossSlashSetup = null!;
    public static GameObject crossSlashAnticSetup = null!;
    public static GameObject groundSpikesCollider = null!;
    public static GameObject bellBindEffect = null!;
    public static Rigidbody2D rb = null!;
    public static bool csSpam;
    public static bool inPhase4Transition;
    public static readonly List<int> attackMemory = [3, 4, 5];
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
        yield return new WaitForSeconds(inPhase4Transition ? 10 : 1);
        instance.SetActive(false);
        instance.GetComponent<PlayMakerFSM>().Reset();
    }
    public static IEnumerator SpawnCrossSlash(float x, float y, float startDelay, float activationDelay, bool randomizePosition = false, float scaleMultiplier = 1, bool csStarter = false, bool iframes = false)
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
        yield return new WaitForSeconds(0.3f);
        crossSlash.SetActive(false);
    }
    //! MISC
    public static IEnumerator GroundSpikeAntic(GameObject silkSwish, float delay, bool flip = false)
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
        yield return new WaitForSeconds(1);
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
        yield return new WaitForSeconds(0.7f);
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
        yield return new WaitForSeconds(0.4f);
        PaleAutomatonPlugin.songKnight.transform.position = PaleAutomatonPlugin.songKnight.transform.position with { x = 500 };
        var corpse = PaleAutomatonPlugin.songKnight.transform.Find("Corpse Song Knight(Clone)").gameObject;
        corpse.SetActive(true);
        var corpseFsm = corpse.GetComponent<PlayMakerFSM>();
        corpseFsm.SetState("Land");
        corpseFsm.GetState("Leave Antic")!.AddMethod(() =>
        {
            PaleAutomatonPlugin.Instance.StartCoroutine(Helpers.TpEffect(corpse:true));
            corpse.transform.position = corpse.transform.position with { x = 500 };
        });
        corpse.transform.position = new Vector3(50.0426f, 27.0411f, 0.0097f);
    }
    public static IEnumerator AnticParry()
    {
        var currentState = PaleAutomatonPlugin.controlFsm.ActiveStateName;
        if (parriedState.Equals(currentState)) yield break;
        PaleAutomatonPlugin.controlFsm.SetState("Parry Dir");
        yield return new WaitForSeconds(0.15f);
        parriedState = currentState;
        PaleAutomatonPlugin.controlFsm.SetState(currentState switch
        {
            "Dive Antic" => "Dive Dir",
            _ => currentState
        });
    }
    public static IEnumerator Teleport(float x, float y, string nextState, float delay = 0.2f, float finishNextStateIn = -1, bool lookAtHornet = false)
    {
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
            stunHero = true,
            roarBurst = false,
            isSmall = false,
            noVisualEffect = false,
            forceThroughBind = true,
            stopOnExit = true
        });
        yield return new WaitForSeconds(0.5f);
        PaleAutomatonPlugin.controlFsm.SendEvent("BLOCKED HIT");
        yield return new WaitForSeconds(0.3f);
        HeroController.instance.StartInvulnerable(0.2f);
        yield return new WaitForSeconds(1);
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
        PaleAutomatonPlugin.Instance.StartCoroutine(Teleport(100, HeroController.instance.transform.position.y + 100, "First Idle"));
        PaleAutomatonPlugin.controlFsm.Fsm.ManualUpdate = true;
        InfiniteTerrainMover.cameraLockArea.cameraYMax = 15;
        InfiniteTerrainMover.cameraLockArea.enabled = false;
        InfiniteTerrainMover.cameraLockArea.enabled = true;
        InfiniteTerrainMover.cameraLockArea.OnInsideStateChanged(true);
        yield return new WaitForSeconds(0.6f);
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
        yield return Teleport(100, HeroController.instance.transform.position.y + 100, "First Idle");
        yield return new WaitForSeconds(0.6f);
        silkSwishOriginal.transform.position = new Vector3(-2f, -4, 1);
        silkSwishOriginal.transform.localScale = new Vector3(5.1726f, 0.6114f, 0.8734f);
        silkSwishOriginal.SetActive(true);
        yield return new WaitForSeconds(0.4f);
        Object.Destroy(PaleAutomatonPlugin.songKnight.transform.Find("WindSlash Hit").gameObject);
        PaleAutomatonPlugin.groundSpikesParent.SetActive(true);
        Helpers.clonedGroundSpikes.SetActive(true);
        groundSpikesCollider.SetActive(true);
        Helpers.clonedGroundSpikesCollider.SetActive(true);
        PaleAutomatonPlugin.Instance.StartCoroutine(PaleAutomatonPlugin.FancyZoomOut(2, 0.575f));
        yield return new WaitForSeconds(0.5f);
        PaleAutomatonPlugin.terrainCollider.transform.position = PaleAutomatonPlugin.terrainCollider.transform.position with {y = -50};
        var otherTerrainCollider = Helpers.clonedTerrainArt.transform.Find("Terrain Collider");
        otherTerrainCollider.transform.position = otherTerrainCollider.transform.position with { y = -50 };
        yield return new WaitForSeconds(0.5f);
        PaleAutomatonPlugin.controlFsm.Fsm.ManualUpdate = true;
        InfiniteTerrainMover.cameraLockArea.cameraYMax = 100000;
        InfiniteTerrainMover.cameraLockArea.enabled = false;
        InfiniteTerrainMover.cameraLockArea.enabled = true;
        InfiniteTerrainMover.cameraLockArea.OnInsideStateChanged(true);
        Helpers.ToggleDownSlashHitbox(true);
        bellBindEffect.SetActive(true);
        PaleAutomatonPlugin.Instance.StartCoroutine(SelectPhase3Attack());
    }
    public static IEnumerator Phase4Transition()
    {
        Helpers.DisableChargingEffects();
        inPhase4Transition = true;
        var hPos = HeroController.instance.transform.position;
        var yPos = Math.Clamp(hPos.y - 8, 20, 9999);
        yield return Teleport(hPos.x, yPos, "Windslash A");
        PaleAutomatonPlugin.songKnight.transform.localScale = PaleAutomatonPlugin.songKnight.transform.localScale with {x = 1};
        PaleAutomatonPlugin.songKnight.transform.SetRotation2D(90);
        yield return new WaitForSeconds(0.5f);
        PaleAutomatonPlugin.groundSpikesParent.SetActive(false);
        GameObject.Find("Main Terrain Art").SetActive(false);
        GameObject.Find("Cloned Terrain Art").SetActive(false);
        yield return new WaitForSeconds(0.2f);
        PaleAutomatonPlugin.songKnight.transform.SetRotation2D(0);
        yield return Teleport(hPos.x, 100, "First Idle");
        inPhase4Transition = false;
        yield return new WaitForSeconds(0.5f);
    }
    //! PHASE 2
    public static IEnumerator RisingSlashStarter()
    {
        PaleAutomatonPlugin.customComboSequence = true;
        yield return new WaitForSeconds(0.55f);
        PaleAutomatonPlugin.controlFsm.SetState("CrossSlash 1");
        yield return new WaitForSeconds(0.1f);
        PaleAutomatonPlugin.controlFsm.SetState("Rising Slash Antic");
        PaleAutomatonPlugin.controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rising Slash")!.speed = -70f;
        yield return new WaitForSeconds(0.01f);
        PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
        yield return new WaitForSeconds(0.4f);
        PaleAutomatonPlugin.controlFsm.SetState("Windslash A");
        yield return new WaitForSeconds(0.3f);
        PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
        if (Random.value > 0.5f)
        {
            yield return new WaitForSeconds(0.2f);
            PaleAutomatonPlugin.controlFsm.SetState("WindSlash");
            yield return new WaitForSeconds(0.2f);
            PaleAutomatonPlugin.controlFsm.SetState("Dive Dir");
            yield return new WaitForSeconds(0.2f);
        }
        else
        {
            yield return new WaitForSeconds(0.175f);
            PaleAutomatonPlugin.controlFsm.SetState("Dive Dir");
            yield return new WaitForSeconds(0.05f);
            PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
            yield return new WaitForSeconds(0.3f);
            PaleAutomatonPlugin.controlFsm.SetState("Windslash G");
            yield return new WaitForSeconds(0.3f);
            PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
        }
        PaleAutomatonPlugin.controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rising Slash")!.speed = -80f;
        yield return new WaitForSeconds(0.3f);
        PaleAutomatonPlugin.customComboSequence = false;
    }
    public static IEnumerator DoubleWindslashStarter()
    {
        PaleAutomatonPlugin.customComboSequence = true;
        yield return new WaitForSeconds(0.2f);
        PaleAutomatonPlugin.controlFsm.SetState("WindSlash");
        yield return new WaitForSeconds(0.2f);
        PaleAutomatonPlugin.controlFsm.SetState("Dive Dir");
        yield return new WaitForSeconds(0.6f);
        PaleAutomatonPlugin.controlFsm.SetState("Windslash G");
        yield return new WaitForSeconds(0.3f);
        PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
        yield return new WaitForSeconds(0.3f);
        PaleAutomatonPlugin.customComboSequence = false;
    }
    public static IEnumerator RapidSlashFollowup()
    {
        PaleAutomatonPlugin.controlFsm.SetState("DashStab Antic");
        PaleAutomatonPlugin.Instance.StartCoroutine(Helpers.ScheduleNextState(0.4f, "Stab 3"));
        yield return new WaitForSeconds(0.3f);
        PaleAutomatonPlugin.customComboSequence = false;
    }
    public static IEnumerator DiveStarter()
    {
        PaleAutomatonPlugin.customComboSequence = true;
        var radpiSlash = Random.value > 0.5f;
        if (radpiSlash)
        {
            yield return new WaitForSeconds(0.3f);
            PaleAutomatonPlugin.rapidSlashFollowupAllowed = true;
            PaleAutomatonPlugin.controlFsm.SetState("Rapid Slash Dash");
        }
        else
        {
            yield return new WaitForSeconds(0.2f);
            PaleAutomatonPlugin.controlFsm.SetState("Rising Slash Antic");   
            yield return new WaitForSeconds(0.15f);
            PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");   
            yield return new WaitForSeconds(0.4f);
            PaleAutomatonPlugin.controlFsm.SetState("CS Antic");
            yield return new WaitForSeconds(0.1f);
            PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
            yield return new WaitForSeconds(0.3f);
            PaleAutomatonPlugin.controlFsm.SetState("Windslash A");
            yield return new WaitForSeconds(0.3f);
            PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
        }
        yield return new WaitForSeconds(0.3f);
        if (!radpiSlash) PaleAutomatonPlugin.customComboSequence = false;
    }
    public static IEnumerator CrossSlashStarter()
    {
        PaleAutomatonPlugin.customComboSequence = true;
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(PaleAutomatonPlugin.songKnight.transform.localScale.x * -7 + PaleAutomatonPlugin.songKnight.transform.position.x, PaleAutomatonPlugin.songKnight.transform.position.y - 2, 0.05f, 0.15f, scaleMultiplier: 0.7f, csStarter:true));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(PaleAutomatonPlugin.songKnight.transform.localScale.x * -13 + PaleAutomatonPlugin.songKnight.transform.position.x, PaleAutomatonPlugin.songKnight.transform.position.y - 3, 0.1f, 0.3f, scaleMultiplier: 0.4f, csStarter:true));
        yield return new WaitForSeconds(0.1f);
        PaleAutomatonPlugin.controlFsm.SetState("Windslash A");
        yield return new WaitForSeconds(0.3f);
        PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
        yield return new WaitForSeconds(0.175f);
        PaleAutomatonPlugin.controlFsm.SetState("Dive Dir");
        yield return new WaitForSeconds(0.05f);
        PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
        yield return new WaitForSeconds(0.4f);
        PaleAutomatonPlugin.customComboSequence = false;
    }
    //! PHASE 3+4
    public static IEnumerator SelectPhase3Attack()
    {
        if (!PaleAutomatonPlugin.bossScene) yield break;
        while (PaleAutomatonPlugin.songKnight)
        {
            PaleAutomatonPlugin.controlFsm.FsmVariables.GetFsmFloat("Gravity").Value = 0;
            PaleAutomatonPlugin.controlFsm.SetState("First Idle");
            yield return new WaitForSeconds(0.5f);
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
                yield return new WaitForSeconds(1f);
                Pools.DisableAll();
                yield break;
            }
            int attack;
            do attack = Random.Range(1, 6); while (attackMemory.Contains(attack));
            attackMemory.RemoveAt(0);
            attackMemory.Add(attack);
            yield return attack switch
            {
                1 => WindSlashSpam(),
                2 => LiterallyBoundlessInfinity(),
                3 => CrossSlashSpam(),
                4 => DiveIntoCrossSlash(),
                5 => TripleRisingSlash(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        yield return new WaitForSeconds(1f);
        Pools.DisableAll();
    }
    public static IEnumerator LiterallyBoundlessInfinity()
    {
        var direction = Random.value > 0.5f ? -1 : 1;
        var hcPos = HeroController.instance.transform.position;
        var yOffset = Random.Range(-3, 3);
        var xPos = hcPos.x + 13 * direction - yOffset * direction;
        yield return Teleport(xPos, hcPos.y + yOffset, "DashStab Antic", lookAtHornet:true);
        yield return new WaitForSeconds(1.7f);
        PaleAutomatonPlugin.controlFsm.SetState("DashStab Antic");
        yield return new WaitForSeconds(0.05f);
        PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
        yield return new WaitForSeconds(1.1f);
    }
    public static IEnumerator DiveIntoCrossSlash()
    {
        //todo: make this more consistent
        var direction = Random.value > 0.5f ? -1 : 1;
        var hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + 8.5f * direction, hcPos.y + 5, "Dive Dir");
        yield return new WaitForSeconds(0.7f);
        hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + 8.5f * -direction, hcPos.y + 5, "Dive Dir", delay:0, finishNextStateIn: 0.2f);
        yield return new WaitForSeconds(0.2f);
        hcPos = HeroController.instance.transform.position;
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x, hcPos.y, 0f, 0.4f));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x - 7, hcPos.y + 7, 0.05f, 0.45f, true));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x + 7, hcPos.y - 7, 0.1f, 0.5f, true));
        yield return Teleport(hcPos.x, hcPos.y + 100, "First Idle", delay:0f, finishNextStateIn: 0f);
        yield return new WaitForSeconds(0.8f);
        hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + 8.5f * -direction, hcPos.y + 5, "Dive Dir", finishNextStateIn: 0.2f);
        yield return new WaitForSeconds(0.2f);
        hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + 8.5f * direction, hcPos.y + 5, "Dive Dir", delay:0, finishNextStateIn: 0.2f);
        yield return new WaitForSeconds(0.2f);
        hcPos = HeroController.instance.transform.position;
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x, hcPos.y, 0f, 0.4f));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x + 7, hcPos.y + 7, 0.05f, 0.45f, true));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x - 7, hcPos.y - 7, 0.1f, 0.5f, true));
        yield return Teleport(hcPos.x, hcPos.y + 100, "First Idle", delay:0f, finishNextStateIn: 0f);
        yield return new WaitForSeconds(0.8f);
        hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + 8.5f * -direction, hcPos.y + 5, "Dive Dir", finishNextStateIn: 0.2f);
        yield return new WaitForSeconds(0.2f);
    }
    public static IEnumerator WindSlashSpam()
    {
        var direction = Random.value > 0.5f ? -1 : 1;
        var xOffset = Random.Range(10f, 13f) * direction;
        var yOffset = Random.Range(-4f, 1f);
        var hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + xOffset, hcPos.y + yOffset, "Windslash A", lookAtHornet:true);
        yield return new WaitForSeconds(0.6f);
        PaleAutomatonPlugin.controlFsm.SetState("WindSlash");
        yield return new WaitForSeconds(0.2f);
        xOffset = Random.Range(10f, 13f) * -direction;
        yOffset = Random.Range(-4f, 1f);
        hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + xOffset, hcPos.y + yOffset, "Windslash A", lookAtHornet:true);
        yield return new WaitForSeconds(0.6f);
        xOffset = Random.Range(10f, 13f) * direction;
        yOffset = Random.Range(-4f, 1f);
        hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + xOffset, hcPos.y + yOffset, "Windslash A", lookAtHornet:true);
        yield return new WaitForSeconds(0.6f);
        xOffset = Random.Range(10f, 13f) * -direction;
        yOffset = Random.Range(-4f, 1f);
        hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + xOffset, hcPos.y + yOffset, "Windslash A", lookAtHornet:true);
        yield return new WaitForSeconds(0.6f);
        PaleAutomatonPlugin.controlFsm.SetState("WindSlash");
        yield return new WaitForSeconds(0.2f);
        PaleAutomatonPlugin.controlFsm.SetState("WindSlash");
        yield return new WaitForSeconds(0.2f);
    }
    public static IEnumerator CrossSlashSpam()
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
        yield return new WaitForSeconds(0.6f);
        hcPos = HeroController.instance.transform.position;
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x, hcPos.y, 0, 0.3f, csStarter:true, randomizePosition:true, iframes:true));
        yield return Teleport(hcPos.x + 3 * -direction, hcPos.y - 2, "CS Antic", lookAtHornet:true);
        yield return new WaitForSeconds(anticTime + 0.2f);
        hcPos = HeroController.instance.transform.position;
        PaleAutomatonPlugin.Instance.StartCoroutine(Teleport(hcPos.x, hcPos.y + 500, "First Idle"));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x, hcPos.y, 0, 0.3f, csStarter:true, randomizePosition:true, iframes:true));
        yield return new WaitForSeconds(0.6f);
        hcPos = HeroController.instance.transform.position;
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x, hcPos.y, 0, 0.3f, csStarter:true, randomizePosition:true, iframes:true));
        yield return Teleport(hcPos.x + 3 * direction, hcPos.y - 2, "CS Antic", lookAtHornet:true);
        yield return new WaitForSeconds(anticTime + 0.2f);
        hcPos = HeroController.instance.transform.position;
        PaleAutomatonPlugin.Instance.StartCoroutine(Teleport(hcPos.x, hcPos.y + 500, "First Idle"));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x, hcPos.y, 0, 0.3f, csStarter:true, randomizePosition:true, iframes:true));
        yield return new WaitForSeconds(0.6f);
        hcPos = HeroController.instance.transform.position;
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x, hcPos.y, 0, 0.3f, csStarter:true, randomizePosition:true, iframes:true));
        yield return new WaitForSeconds(0.6f);
    }
    public static IEnumerator TripleRisingSlash()
    {
        PaleAutomatonPlugin.controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rising Slash")!.ySpeed = 40;
        var direction = Random.value > 0.5f ? -1 : 1;
        var hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + 4 * direction, hcPos.y - 6, "Rising Slash Antic", finishNextStateIn: 0.4f);
        yield return new WaitForSeconds(0.5f);
        PaleAutomatonPlugin.controlFsm.SetState("CrossSlash 1");
        yield return new WaitForSeconds(0.1f);
        hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + 4 * -direction, hcPos.y - 6, "Rising Slash Antic", finishNextStateIn: 0.4f);
        yield return new WaitForSeconds(0.5f);
        PaleAutomatonPlugin.controlFsm.SetState("CrossSlash 1");
        yield return new WaitForSeconds(0.1f);
        PaleAutomatonPlugin.controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rising Slash")!.speed = 0;
        PaleAutomatonPlugin.controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rising Slash")!.ySpeed = 90;
        hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x, hcPos.y - 6, "Rising Slash Antic", finishNextStateIn: 0.4f);
        yield return new WaitForSeconds(0.5f);
        PaleAutomatonPlugin.controlFsm.SetState("Windslash A");
        yield return new WaitForSeconds(0.2f);
        PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
        PaleAutomatonPlugin.controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rising Slash")!.speed = -80;
        PaleAutomatonPlugin.controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rising Slash")!.ySpeed = 15;
        yield return new WaitForSeconds(0.2f);
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