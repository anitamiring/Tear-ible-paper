using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Apkd;

public class Fly : Movement
{
    [S] float offsetY { get; } = 1;
    

    private float minY;
    private float maxY;

    private void Start()
    {
        minY = transform.position.y - offsetY;
        maxY = transform.position.y + offsetY;
    }

    public override void Move()
    {
        float y = transform.position.y;

        y += Time.deltaTime * speed;

        if (y > maxY || y < minY)
        {
            speed *= -1;
        }


        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }
}
