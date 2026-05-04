using System.Collections;
using GenericVariableExtension;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Silksong.AssetHelper.ManagedAssets;
using Silksong.FsmUtil;
using UnityEngine;

namespace PaleAutomaton;

public static class CustomBehaviour
{
    public static ManagedAsset<GameObject> SK_PROJECTILE_ASSET = null!;
    public static GameObject skProjectileSetup = null!;
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
        PaleAutomatonPlugin.controlFsm.GetState("Rapid Slash End")!.AddAction(new SetVelocityByScale
        {
            gameObject = PaleAutomatonPlugin.controlFsm.GetFirstActionOfType<SetVelocityByScale>("Dash Slash End 2")!.gameObject,
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
    public static IEnumerator Teleport(float x, float y, string nextState, float delay = 0.3f, float finishNextStateIn = -1)
    {
        var transform = PaleAutomatonPlugin.songKnight.transform;
        PaleAutomatonPlugin.Instance.StartCoroutine(Helpers.TpEffect());
        yield return new WaitForSeconds(0.05f);
        transform.position = transform.position with { y = 1000 };
        yield return new WaitForSeconds(delay);
        PaleAutomatonPlugin.Instance.StartCoroutine(Helpers.TpEffect());
        yield return new WaitForSeconds(0.05f);
        transform.position = transform.position with { x = x };
        transform.position = transform.position with { y = y };
        PaleAutomatonPlugin.controlFsm.SetState(nextState);
        if (finishNextStateIn != -1)
        {
            yield return new WaitForSeconds(finishNextStateIn);
            PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
        }
    }
}