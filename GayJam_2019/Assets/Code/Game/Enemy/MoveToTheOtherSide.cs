using Apkd;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveToTheOtherSide : Movement
{
    [Inject.Singleton]
    private Castle castle { get; }


    private void Start()
    {
        base.Start();
        ChooseDirection();
    }

    void ChooseDirection()
    {
        if (castle.transform.position.x < transform.position.x)
            speed *= -1;
    }

    public override void Move()
    {
        float step = Time.deltaTime * speed;
        transform.position += new Vector3(step, 0, 0);
    }
}
