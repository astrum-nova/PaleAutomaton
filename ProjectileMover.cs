using UnityEngine;

namespace PaleAutomaton;

public class ProjectileMover : MonoBehaviour
{
    private Vector3 moveDirection;

    private void OnEnable()
    {
        transform.localScale = new Vector3(2.7f, 2.7f, 1);
        if (PaleAutomatonPlugin.windslashGround)
        {
            moveDirection = transform.position.x < HeroController.instance.transform.position.x ? Vector3.right : Vector3.left;
            transform.localScale = transform.localScale with {x = transform.localScale.x * (moveDirection.x * -1)};
            transform.position = transform.position with {y = transform.position.y + 2};
        }
        else
        {
            
        }
    }
    private void Update()
    {
        transform.position += moveDirection * (Time.deltaTime * 100);
    }
}