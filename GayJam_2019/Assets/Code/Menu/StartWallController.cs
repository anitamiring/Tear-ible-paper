using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Apkd;
using Odin = Sirenix.OdinInspector;

public class StartWallController : MonoBehaviour
{
    [Inject.Singleton] GameManager gameManager { get; }

    [S, Inject.FromChildren] CastleBlock[] fragments { get; set; }

    [Odin.PropertyRange(0f, 0.5f)]
    [S] float destroyDelay { get; } = 0.1f;
    [Odin.PropertyRange(1, 20)]
    [S] int blockPerInvoke { get; } = 1;

    Plane plane = new Plane(Vector3.forward, new Vector3());
    private Vector2 dragStart;
    private Vector2 dragEnd;
    private float enter;
    private float timer;

    private void Start()
    {
        fragments = fragments.OrderByDescending(x => x.transform.position.y).ToArray();
    }

    async void DestroyWall(float force, Vector3 position, float radius)
    {
        var idx = 0;
        foreach (var item in fragments)
        {
            item.DropThis();
            if (idx < blockPerInvoke)
            {    
                idx++;
            }
            else
            {
                idx = 0;
                await this.AsyncDelay(destroyDelay);
            }
        }
        await this.AsyncDelay(0.2f);
        gameManager.GameIsOn = true;
        gameObject.SetActive(false);
    }

    private void OnMouseUp()
    {
        DestroyWall(timer, dragStart, Vector3.Distance(dragStart, dragEnd));
    }





}
