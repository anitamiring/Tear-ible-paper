using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Apkd;

public class ShowGameTime : MonoBehaviour
{
    public enum ShowGameTimeEnum { Current, Best }

    [S] ShowGameTimeEnum Type { get; }

    [Inject.Singleton] GameManager gameManager { get; }

    [S] SpriteRenderer[] fields { get; }
    [S] Sprite[] numbers { get; }

    float time;

    private void Update()
    {
        if (Type == ShowGameTimeEnum.Current)
            time = gameManager.GameTime;
        else if (Type == ShowGameTimeEnum.Best)
            time = gameManager.BestTime;

        if (time < 10000)
        {
            for (int i = 0; i < 4; i++)
            {
                fields[i].sprite = numbers[(int)time.ToString()[i]];
            }
        }

    }
}
