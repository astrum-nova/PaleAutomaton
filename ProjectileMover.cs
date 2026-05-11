using UnityEngine;

namespace PaleAutomaton;

[RequireComponent(typeof(PlayMakerFSM))]
public class ProjectileMover : MonoBehaviour
{
    private Vector3 moveDirection;
    private int speedSet;
    private void OnEnable()
    {
        speedSet = 100;
        GetComponent<PlayMakerFSM>().Reset();
        transform.localScale = new Vector3(2.7f, 2.7f, 1);
        var targetPos = HeroController.instance.transform.position;
        transform.SetRotation2D(0);
        transform.position = PaleAutomatonPlugin.songKnight.transform.position;
        if (PaleAutomatonPlugin.windslashGround)
        {
            moveDirection = transform.position.x < targetPos.x ? Vector3.right : Vector3.left;
            transform.localScale = transform.localScale with { x = transform.localScale.x * (moveDirection.x * -1) };
            transform.position = transform.position with { y = transform.position.y + 2.75f };
        }
        else
        {
            moveDirection = (targetPos - transform.position).normalized;
            var speed = moveDirection.magnitude;
            moveDirection.y = Mathf.Clamp(moveDirection.y, -0.75f * speed, 0.75f * speed);
            var newXSquared = speed * speed - moveDirection.y * moveDirection.y;
            moveDirection.x = Mathf.Sqrt(Mathf.Max(0, newXSquared)) * Mathf.Sign(moveDirection.x);
            var angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = moveDirection.x < 0 ? Quaternion.Euler(0, 0, angle - 180) : Quaternion.Euler(0, 0, angle + 180);
        }
    }
    private void Update()
    {
        transform.position += moveDirection * (Time.deltaTime * speedSet);
    }
}