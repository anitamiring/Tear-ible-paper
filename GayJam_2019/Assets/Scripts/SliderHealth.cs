using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Apkd;



public class SliderHealth : MonoBehaviour
{
    [Inject.Singleton]
    GameManager gameManager { get; }

    [Inject] new Renderer renderer { get; set; } 

    void Update()
    {
        var percentTime = gameManager.GameTime / gameManager.BestTime;
        renderer.material.SetFloat("Vector1_A48DD67C", Mathf.Lerp(-0.9f, 0.6f, percentTime));
    }
}
