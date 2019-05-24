using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Apkd;
using Odin = Sirenix.OdinInspector;

public class Castle : MonoBehaviour
{
    [S, Inject]
    public HealthComponent HealthComponent { get; private set; }

    [S, Inject.FromChildren]
    CastleBlock[] blocks { get; set; }

    int nextDoDrop = 0;

    private void Start()
    {
        blocks = blocks.Shuffle().ToArray();
        blocks = blocks.OrderByDescending(x => 4 * x.transform.position.y - x.transform.position.z).ToArray();
        AsyncDropBlocks();
    }

    async void AsyncDropBlocks()
    {
        while(nextDoDrop < blocks.Length)
        {

            await this.AsyncUntil(() => HealthComponent.Percentage < ((blocks.Length - nextDoDrop - 1) / (float)blocks.Length));
            DropNext();
        }
    }

    [Odin.Button]
    public void DropNext()
    {
        if (nextDoDrop < blocks.Length)
            blocks[nextDoDrop].DropThis();

        nextDoDrop++;
    }


}
