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
    }

    public static IEnumerator DoubleWindslashStarter()
    {
        PaleAutomatonPlugin.customComboSequence = true;
        PaleAutomatonPlugin.controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rising Slash")!.ySpeed = 15;
        yield return new WaitForSeconds(0.25f);
        PaleAutomatonPlugin.controlFsm.SetState("WindSlash");
        yield return new WaitForSeconds(0.2f);
        PaleAutomatonPlugin.controlFsm.SetState("Dive Dir");
        yield return new WaitForSeconds(0.6f);
        //todo: add a DashStab Antic >> Stab 3 mixup for the ground windslash
        PaleAutomatonPlugin.controlFsm.SetState("Windslash G");
        yield return new WaitForSeconds(0.3f);
        PaleAutomatonPlugin.controlFsm.SendEvent("FINISHED");
        yield return new WaitForSeconds(0.3f);
        PaleAutomatonPlugin.customComboSequence = false;
        PaleAutomatonPlugin.controlFsm.GetFirstActionOfType<SetVelocityByScale>("Rising Slash")!.ySpeed = 15;
    }
}