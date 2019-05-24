using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Apkd;

public class RotateClockArrow : MonoBehaviour
{
    [Inject.Singleton]
    GameManager gameManager { get; }

    public Animator alarmAnim;
    private float countTo60 = 0;
    private float previousTime = 0;
   

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(Vector3.forward * (gameManager.GameTime - previousTime) * -6);
        countTo60 += gameManager.GameTime - previousTime;
        previousTime = gameManager.GameTime;

        if (countTo60 >= 60)
        {
            Debug.Log("Drrrrrrr!");
            countTo60 = 0;
            alarmAnim.SetBool("alarm",true);
        }
        if (countTo60 >= 3 && alarmAnim.GetBool("alarm"))
        {
            alarmAnim.SetBool("alarm", false);
        }
    }
}
