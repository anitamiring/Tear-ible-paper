using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Apkd;

public class DieEgg : MonoBehaviour
{
    void KillEgg()
    {
        
        Destroy(gameObject);
    }


    void ChangeLayer()
    {
        GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        gameObject.layer = 10;
    }

}
