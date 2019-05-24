using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Apkd;

public class ForwardMovement : Movement
{
    [Inject.Singleton]
    private Castle castle { get; }

    bool stopMove;

    public override void Move()
    {
        if (stopMove) return;

        float steep = Time.deltaTime * speed;
        float x = Mathf.MoveTowards(transform.position.x, castle.transform.position.x, steep);

        transform.position = new Vector3(x, transform.position.y, transform.position.z);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == castle.gameObject)
            stopMove = true;
    }
}
