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
    public static Rigidbody2D rb = null!;
    public static bool csSpam;
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
    private static string parriedState = "";
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
    public static IEnumerator Phase2Transition()
    {
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
    public static IEnumerator Teleport(float x, float y, string nextState, float delay = 0.2f, float finishNextStateIn = -1, bool lookAtHornet = false)
    {
        rb.linearVelocityY = 0;
        rb.linearVelocityX = 0;
        PaleAutomatonPlugin.controlFsm.Fsm.manualUpdate = true;
        var transform = PaleAutomatonPlugin.songKnight.transform;
        PaleAutomatonPlugin.Instance.StartCoroutine(Helpers.TpEffect());
        transform.position = transform.position with { y = 100 };
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
    }
    public static IEnumerator SpawnCrossSlash(float x, float y, float startDelay, float activationDelay, bool randomizePosition = false)
    {
        var xOffset = randomizePosition ? Random.Range(-2, 2) : 0;
        var yOffset = randomizePosition ? Random.Range(-2, 2) : 0;
        var rotationOffset = 90 + (randomizePosition ? Random.Range(-10, 10) : 0);
        var scaleModifier = Random.Range(2f, 2.3f);
        yield return new WaitForSeconds(startDelay);
        var antic = Pools.GetCrossSlashAntic();
        antic.transform.localScale = new Vector3(1, 1, 1);
        antic.SetActive(true);
        antic.transform.position = new Vector3(x + xOffset, y + yOffset, antic.transform.position.z);
        antic.transform.localScale *= scaleModifier;
        antic.transform.localScale = antic.transform.localScale with { x = PaleAutomatonPlugin.songKnight.transform.localScale.x };
        antic.transform.SetRotation2D(rotationOffset + 180);
        antic.transform.FlipLocalScale(y:true);
        //todo: remember to prewarm the antic in the pool maybe, same with the crosslashes themselves
        yield return new WaitForSeconds(activationDelay - startDelay);
        antic.SetActive(false);
        var crossSlash = Pools.GetCrossSlash();
        crossSlash.transform.localScale = new Vector3(1, 1, 1);
        try { crossSlash.GetComponent<PlayMakerFSM>().GetState("Recycle")!.RemoveActionsOfType<RecycleSelf>(); }
        catch { /*ignored*/ }
        crossSlash.SetActive(true);
        crossSlash.transform.position = new Vector3(x + xOffset, y + yOffset, antic.transform.position.z);
        crossSlash.transform.localScale *= scaleModifier;
        crossSlash.transform.localScale = crossSlash.transform.localScale with { x = PaleAutomatonPlugin.songKnight.transform.localScale.x };
        crossSlash.transform.SetRotation2D(rotationOffset);
        yield return new WaitForSeconds(0.3f);
        crossSlash.SetActive(false);
    }
    public static bool inPhase4Transition = false;
    public static IEnumerator Phase4Transition()
    {
        inPhase4Transition = true;
        var hPos = HeroController.instance.transform.position;
        var yPos = Math.Clamp(hPos.y - 5, 20, 9999);
        yield return Teleport(hPos.x, yPos, "Windslash A");
        PaleAutomatonPlugin.songKnight.transform.localScale = PaleAutomatonPlugin.songKnight.transform.localScale with {x = 1};
        PaleAutomatonPlugin.songKnight.transform.SetRotation2D(90);
        yield return new WaitForSeconds(0.5f);
        PaleAutomatonPlugin.groundSpikesParent.SetActive(false);
        groundSpikesCollider.SetActive(false);
        GameObject.Find("strut_bg_song_bridge_example").SetActive(false);
        yield return new WaitForSeconds(0.2f);
        PaleAutomatonPlugin.songKnight.transform.SetRotation2D(0);
        yield return Teleport(hPos.x, 100, "First Idle");
        inPhase4Transition = false;
        yield return new WaitForSeconds(0.5f);
    }
    public static IEnumerator Phase3Transition()
    {
        yield return Teleport(100, 100, "First Idle");
        PaleAutomatonPlugin.controlFsm.Fsm.ManualUpdate = true;
        yield return new WaitForSeconds(1);
        Object.Destroy(PaleAutomatonPlugin.songKnight.transform.Find("WindSlash Hit").gameObject);
        GameObject.Find("CameraLockArea (1)").transform.localScale = GameObject.Find("CameraLockArea (1)").transform.localScale with { y = GameObject.Find("CameraLockArea (1)").transform.localScale.y + 100 };
        groundSpikesCollider = Object.Instantiate(GameObject.Find("Spike Collider"))!;
        groundSpikesCollider.name = "GroundSpikesCollider";
        var fallKiller = Object.Instantiate(GameObject.Find("Spike Collider"))!;
        Helpers.SetupGroundSpikeHitbox(groundSpikesCollider);
        Helpers.SetupGroundSpikeHitbox(fallKiller);
        fallKiller.name = "FallKiller";
        fallKiller.transform.position = fallKiller.transform.position with {y = -6};
        fallKiller.GetComponent<DamageHero>().SetDamageAmount(999);
        fallKiller.transform.localScale = fallKiller.transform.localScale with {y = 180};
        fallKiller.transform.localScale = fallKiller.transform.localScale with {x = 100};
        yield return new WaitForSeconds(0.5f);
        PaleAutomatonPlugin.terrainCollider.transform.position = PaleAutomatonPlugin.terrainCollider.transform.position with {y = -50};
        yield return new WaitForSeconds(0.5f);
        Helpers.ToggleDownSlashHitbox(true);
        PaleAutomatonPlugin.controlFsm.Fsm.ManualUpdate = true;
        PaleAutomatonPlugin.Instance.StartCoroutine(SelectPhase3Attack());
    }
    public static List<int> attackMemory = [3, 4, 5];
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
        yield return Teleport(hcPos.x, hcPos.y + 100, "Dive Dir", delay:0f, finishNextStateIn: 0f);
        yield return new WaitForSeconds(0.8f);
        hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + 8.5f * -direction, hcPos.y + 5, "Dive Dir", finishNextStateIn: 0.2f);
        yield return new WaitForSeconds(0.2f);
        hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + 8.5f * direction, hcPos.y + 5, "Dive Dir", delay:0, finishNextStateIn: 0.1f);
        yield return new WaitForSeconds(0.2f);
        hcPos = HeroController.instance.transform.position;
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x, hcPos.y, 0f, 0.4f));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x + 7, hcPos.y + 7, 0.05f, 0.45f, true));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x - 7, hcPos.y - 7, 0.1f, 0.5f, true));
        yield return Teleport(hcPos.x, hcPos.y + 100, "Dive Dir", delay:0f, finishNextStateIn: 0f);
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
        yield return Teleport(hcPos.x + 3 * direction, hcPos.y + 2, "CS Antic");
        csSpam = true;
        //todo: randomize the order in which these happen
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x + 7, hcPos.y - 5, 0.1f, anticTime + 0.014f, true));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x - 7, hcPos.y - 5, 0.2f, anticTime + 0.015f, true));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x + 10, hcPos.y + 0, 0.3f, anticTime + 0.012f, true));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x - 10, hcPos.y + 0, 0.4f, anticTime + 0.013f, true));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x + 0, hcPos.y + 8, 0.5f, anticTime + 0.011f, true));
        yield return new WaitForSeconds(anticTime + 0.2f);
        csSpam = false;
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
        if (Random.value > 0.5f || true)
        {
            PaleAutomatonPlugin.controlFsm.SetState("Windslash A");
        }
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
        //todo: instead of stall tps try tping into dashstab antic and letting it go normally without finishing the state early, and try using the tp delay
        //todo: also update the hcpos for the crossslashes too cause its using an old pos
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
    */
}