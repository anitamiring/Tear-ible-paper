using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Apkd;

public class CastleBlock : MonoBehaviour
{
    [S, Inject(Optional = true)]
    new Collider2D collider { get; }

    [S, Inject]
    new Renderer renderer { get; }



    public bool HasCollider => collider != null ? true : false;

    bool isDroping;
    public async void DropThis()
    {
        if (isDroping)
            return;

        isDroping = true;
        var velocity = new Vector3(Random.Range(-1f, 1f), Random.Range(0.8f, 2f), Random.Range(-1f, -2f)).normalized * Random.Range(5f, 7f);
        var gravity = new Vector3(0f, -10f, 0f);

        while (transform.position.y > -10)
        {
            var delta = Time.fixedDeltaTime;
            velocity += gravity * delta;
            transform.position += velocity * delta;

            await this.AsyncNextFixedUpdate();
        }
    }
}
