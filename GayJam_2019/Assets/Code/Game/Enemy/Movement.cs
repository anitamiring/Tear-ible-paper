using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Apkd;

public abstract class Movement : MonoBehaviour
{
    [S] protected float speed { get; set; } = 1;
    public abstract void Move();

    [Inject.Singleton]
    private Castle castle { get; }

    private void Update()
    {
        Move();
    }

    protected void Start()
    {
        FlipDependsOnDirection();
    }

    protected void FlipDependsOnDirection()
    {
        if (transform.position.x > castle.transform.position.x)
            transform.localScale = new Vector2(transform.localScale.x * -1, transform.localScale.y);
    }
}
