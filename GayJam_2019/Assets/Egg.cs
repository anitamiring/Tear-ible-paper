using Apkd;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Egg : MonoBehaviour
{
    [SerializeField] GameObject dragonPrefab;
    [SerializeField] float yOffset = 1;
    [SerializeField] float timeToDestroy = 2.15f;

    [Inject.Singleton] SpawnManager spawnManager { get; }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "ground")
        {
            Debug.Log("HitGround");
            GetComponentInParent<Rigidbody2D>().bodyType =  RigidbodyType2D.Static;
            //GetComponentInParent<Rigidbody2D>().velocity = Vector3.zero;
            gameObject.GetComponentInParent<Animator>().SetBool("isGrounded",true);
            timeToDestroy = 2.15f;
            Invoke("DestroyEgg", timeToDestroy);
        }
    }

    void DestroyEgg()
    {
        Vector2 pos = new Vector3(transform.position.x, transform.position.y + yOffset);
        GameObject monster = Instantiate(dragonPrefab, pos, Quaternion.identity);
        spawnManager.AddMonster(monster);
        Destroy(gameObject);
    }

}
