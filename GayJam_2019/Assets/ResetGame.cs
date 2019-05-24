using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Apkd;

public class ResetGame : MonoBehaviour
{
    [Inject.Singleton]
    GameManager gameManager { get; }

    [S] bool end { get; } = false; //TODO: hack

    void OnMouseDown()
    {
        if (end)
        {
            gameManager.GameEnd();
        }
        else
        {
            gameManager.GameReset();
        }
    }
}
