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
    public static Rigidbody2D rb = null!;
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
        yield return new WaitForSeconds(1);
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
    public static IEnumerator Teleport(float x, float y, string nextState, float delay = 0.2f, float finishNextStateIn = -1)
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
        antic.transform.SetRotation2D(rotationOffset);
        //todo: remember to prewarm the antic in the pool maybe, same with the crosslashes themselves
        yield return new WaitForSeconds(activationDelay);
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
    public static IEnumerator Phase3Transition()
    {
        yield return Teleport(100, 100, "First Idle");
        PaleAutomatonPlugin.controlFsm.Fsm.ManualUpdate = true;
        yield return new WaitForSeconds(1);
        var groundSpikesCollider = Object.Instantiate(GameObject.Find("Spike Collider"))!;
        groundSpikesCollider.name = "GroundSpikesCollider";
        groundSpikesCollider.layer = LayerMask.NameToLayer("Enemies");
        var damageHero = groundSpikesCollider.GetComponent<DamageHero>();
        damageHero.SetDamageAmount(2);
        damageHero.hazardType = HazardType.NON_HAZARD;
        var collider = groundSpikesCollider.GetComponent<PolygonCollider2D>();
        collider.name = "GroundSpikeColliderComponent";
        collider.SetPath(0, new List<Vector2>()
        {
            new(0, 0),
            new(0, 1),
            new(1, 1),
            new(1, 0),
        });
        groundSpikesCollider.transform.position = groundSpikesCollider.transform.position with { y = 12.5f };
        groundSpikesCollider.transform.localScale = groundSpikesCollider.transform.localScale with { y = 180 };
        groundSpikesCollider.transform.localScale = groundSpikesCollider.transform.localScale with { x = 16.4f };
        PaleAutomatonPlugin.groundSpikesParent.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        PaleAutomatonPlugin.terrainCollider.SetActive(false);
        yield return new WaitForSeconds(0.5f);
        PaleAutomatonPlugin.controlFsm.Fsm.ManualUpdate = true;
        PaleAutomatonPlugin.Instance.StartCoroutine(SelectPhase3Attack());
    }
    public static IEnumerator SelectPhase3Attack()
    {
        if (!PaleAutomatonPlugin.bossScene) yield break;
        while (PaleAutomatonPlugin.songKnight)
        {
            PaleAutomatonPlugin.controlFsm.FsmVariables.GetFsmFloat("Gravity").Value = 0;
            PaleAutomatonPlugin.controlFsm.SetState("First Idle");
            yield return new WaitForSeconds(0.5f);
            yield return /*Random.Range(1, 6)*/ 3 switch
            {
                1 => WindSlashSpam(),
                2 => DashSlashIntoCrossSlash(),
                3 => CrossSlashSpam(),
                4 => TripleDive(),
                5 => TripleRisingSlash(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
    //? 1: double windslash > tp
    //? 2: single windslash > tp
    //? 3: single windslash > tp
    //? 4: triple windslash > new attack
    public static IEnumerator WindSlashSpam()
    {
        var direction = Random.value > 0.5f ? -1 : 1;
        var xOffset = Random.Range(10f, 13f) * direction;
        var yOffset = Random.Range(-4f, 1f);
        var hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + xOffset, hcPos.y + yOffset, "Windslash A");
        yield return new WaitForSeconds(0.6f);
        PaleAutomatonPlugin.controlFsm.SetState("WindSlash");
        yield return new WaitForSeconds(0.2f);
        xOffset = Random.Range(10f, 13f) * -direction;
        yOffset = Random.Range(-4f, 1f);
        hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + xOffset, hcPos.y + yOffset, "Windslash A");
        yield return new WaitForSeconds(0.6f);
        xOffset = Random.Range(10f, 13f) * direction;
        yOffset = Random.Range(-4f, 1f);
        hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + xOffset, hcPos.y + yOffset, "Windslash A");
        yield return new WaitForSeconds(0.6f);
        xOffset = Random.Range(10f, 13f) * -direction;
        yOffset = Random.Range(-4f, 1f);
        hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + xOffset, hcPos.y + yOffset, "Windslash A");
        yield return new WaitForSeconds(0.6f);
        PaleAutomatonPlugin.controlFsm.SetState("WindSlash");
        yield return new WaitForSeconds(0.2f);
        PaleAutomatonPlugin.controlFsm.SetState("WindSlash");
        yield return new WaitForSeconds(0.2f);
    }
    //? 1: dash slash > cross slash on hornet > tp
    //? 2: dash slash > cross slash on hornet > tp
    //? 3: stab flurry or windslash > new attack
    public static IEnumerator DashSlashIntoCrossSlash()
    {
        var hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x, hcPos.y, "DashStab Antic");
        yield return new WaitForSeconds(0.2f);
    }
    //? 1: charge a long cross slash
    //? 2: a bunch of cross slash telegraphs spawn randomly
    //? 3: trigger all the cross slashes > new attack
    public static IEnumerator CrossSlashSpam()
    {
        PaleAutomatonPlugin.controlFsm.GetFirstActionOfType<Wait>("CS Antic")!.time = 1;
        var direction = Random.value > 0.5f ? -1 : 1;
        var hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x + 3 * direction, hcPos.y + 2, "CS Antic");
        //todo: randomize the order in which these happen
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x + 7, hcPos.y - 5, 0.1f, 1f, true));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x - 7, hcPos.y - 5, 0.2f, 0.95f, true));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x + 10, hcPos.y + 0, 0.3f, 0.9f, true));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x - 10, hcPos.y + 0, 0.4f, 0.85f, true));
        PaleAutomatonPlugin.Instance.StartCoroutine(SpawnCrossSlash(hcPos.x + 0, hcPos.y + 8, 0.5f, 0.8f, true));
        yield return new WaitForSeconds(1.2f);
    }
    //? 1: dive > tp on opposite direction
    //? 2: dive > tp above hornet
    //? 3: dive straight down > new attack
    public static IEnumerator TripleDive()
    {
        var hcPos = HeroController.instance.transform.position;
        yield return Teleport(hcPos.x, hcPos.y, "Dive Dir");
        yield return new WaitForSeconds(0.2f);
    }
    //? 1: diagonal rising slash from bottom > tp opposite direction
    //? 2: diagonal rising slash from bottom > tp below hornet
    //? 3: rising slash up
    //? 4: stab flurry or windslash or dive straight down > new attack
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
        else
        {
            //todo: dive straight down
        }
        yield return new WaitForSeconds(0.2f);
        PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
        PaleAutomatonPlugin.controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rising Slash")!.speed = -80;
        PaleAutomatonPlugin.controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rising Slash")!.ySpeed = 15;
        yield return new WaitForSeconds(0.2f);
    }
}