using UnityEngine;

namespace PaleAutomaton;

//? Holy bandaid :wilted rose:
public class KeepHornetInPlace : MonoBehaviour
{
    private void OnEnable()
    {
        HeroController.instance.transform.GetChild(8).gameObject.SetActive(false);
        HeroController.instance.RelinquishControl();
        HeroController.instance.StopAnimationControl();
    }

    private void OnDisable()
    {
        HeroController.instance.RegainControl();
        HeroController.instance.StartAnimationControl();
        HeroController.instance.transform.GetChild(8).gameObject.SetActive(false);
    }

    private void Update()
    {
        HeroController.instance.transform.position = new Vector3(45.5f, 25.5938f, 0.004f);
        HeroController.instance.transform.localScale = HeroController.instance.transform.localScale with { x = -1 };
    }
}