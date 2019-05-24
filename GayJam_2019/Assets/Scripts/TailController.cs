using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Apkd;
using UnityEngine.Experimental.VFX;

public class TailController : MonoBehaviour
{

    [S, Inject.FromParents] BallController ball { get; }
    [S, Inject] VisualEffect visualEffect { get; }
    [S, Inject] Transform transform { get;  }
    Vector3 v1, v2, v3;

    private void Update()
    {
        var back = -ball.Velocity.normalized;
        v1 = v2 = v3 = new Vector3(back.x, back.y, 0f) + transform.position;
        var backDirection = new Vector3(back.x, back.y, 0f);
        v1 = Vector3.Lerp(v1, backDirection, 0.2f);
        v2 = Vector3.Lerp(v2, backDirection * 1.5f, 0.15f);
        v3 = Vector3.Lerp(v3, backDirection * 4f, 0.1f);
        //v2 = new Vector3(0.5f, 0.5f, 0.5f) + transform.position;
        //v3 = new Vector3(0.5f, 0.5f, 0.5f) + transform.position;

        visualEffect.SetVector3("Pos 1", v1);
        visualEffect.SetVector3("Pos 3", v3);
        visualEffect.SetVector3("Pos 2", v2);

    }
}
