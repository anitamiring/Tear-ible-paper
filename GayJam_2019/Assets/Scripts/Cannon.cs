using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cannon : MonoBehaviour
{
    [SerializeField] float speed;

    private float currentRotation = 0;


    private void Update()
    {
        currentRotation += Time.deltaTime * speed;

        CheckDirection();
        float z = transform.eulerAngles.z;
        z += Time.deltaTime * speed;

        transform.rotation = Quaternion.Euler(0, 0, z);
    }

    void CheckDirection()
    {
        if (currentRotation > 180)
        {
            currentRotation = 180;
            speed *= -1;
        }
        else if (currentRotation < 0)
        {
            currentRotation = 0;
            speed *= -1;
        }
    }
}
