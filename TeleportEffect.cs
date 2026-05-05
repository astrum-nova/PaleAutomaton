using System;
using UnityEngine;

namespace PaleAutomaton;

[RequireComponent(typeof(PlayMakerFSM))]
public class TeleportEffect : MonoBehaviour
{
    private static bool upOrDown = true;
    private Vector3 moveDirection;
    private float speed = 5;
    private void OnEnable()
    {
        speed = 5;
        transform.position = PaleAutomatonPlugin.songKnight.transform.position;
        GetComponent<PlayMakerFSM>().Reset();
        if (upOrDown)
        {
            upOrDown = false;
            transform.SetRotation2D(270);
            moveDirection = Vector3.up;
        }
        else
        {
            upOrDown = true;
            transform.SetRotation2D(90);
            moveDirection = Vector3.down;
        }
    }
    private void Update()
    {
        speed -= 5 * Time.deltaTime;
        transform.position += moveDirection * (Time.deltaTime * speed);
    }
}