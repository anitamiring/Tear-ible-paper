using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Apkd;
using Odin = Sirenix.OdinInspector;

public class BlocksRain : MonoBehaviour
{
    [Inject.Singleton] GameManager gameManager { get; }
    [S, Inject.FromChildren] Transform[] children { get; }


    [Odin.PropertyRange(0f, 20f)]
    [S] float gravity { get; } = 10f;
    [Odin.PropertyRange(0f, 0.5f)]
    [S] float destroyDelay { get; } = 0.1f;
    [Odin.PropertyRange(1, 20)]
    [S] int blockPerInvoke { get; } = 1;
    [S] Vector2 dropRangeX { get; }

    Stack<GameObject> pool = new Stack<GameObject>();

    void Start()
    {
        children.ToList().Where(x => x.gameObject != gameObject).ToList().ForEach(x => PushToPool(x.gameObject));
        gameManager.OnGameIsOver.AddListener(() => gameObject.SetActive(true));
        gameObject.SetActive(false);
    }

    private void Update()
    {
       // Debug.Log("Tratata: " + pool.Count);
    }

    async void OnEnable()
    {
        var idx = 0;
        while (true)
        {
            PopFromPool();
            if (idx < blockPerInvoke)
            {
                idx++;
                continue;
            }

            idx = 0;
            await this.AsyncDelay(destroyDelay);
        }
    }

    private void PushToPool(GameObject obj)
    {
        obj.SetActive(false);
        pool.Push(obj);
    }

    private void PopFromPool()
    {
        if (pool.Count <= 0)
            return;

        var obj = pool.Pop();

        obj.transform.position = new Vector3(Random.Range(dropRangeX.x, dropRangeX.y), transform.position.y, transform.position.z);
        obj.transform.rotation = Random.rotation;
        
        obj.SetActive(true);
        DropBlock(obj);
    }

    private async void DropBlock(GameObject obj)
    {
        var velocity = new Vector3();
        while(true)
        {
            velocity += new Vector3(0f, -gravity, 0f) * Time.fixedDeltaTime;

            obj.transform.position = obj.transform.position + velocity * Time.fixedDeltaTime;

            if(obj.transform.position.y < -5)
            {
                PushToPool(obj);
                return;
            }

            await this.AsyncNextFixedUpdate();
        }
    }
}
