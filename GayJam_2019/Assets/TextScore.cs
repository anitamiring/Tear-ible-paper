using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Apkd;
using UnityEngine.UI;

public class TextScore : MonoBehaviour
{
    [Inject.Singleton]
    GameManager gameManager { get; }

    [Inject]
    Text scoreText { get; }


    private void Update()
    {
        scoreText.text = $"Time: {gameManager.GameTime}, BestScore: {gameManager.BestTime}";
    }
}
