using Apkd;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SkyShot : Attack
{
    [Inject] HealthComponent health { get; }

    [SerializeField] float shotdelay = 3;
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] Transform projectileSpawnPoint;

    [Inject.Singleton]
    private Castle castle { get; }
    private bool wasShot;

    private async void Start()
    {
        health.OnDestroy.AddListener(() => this.enabled = false);
        while(true)
        {
            gameObject.GetComponentInChildren<Animator>().SetBool("isLayingEgg", false);
            await this.AsyncDelay(shotdelay);
            gameObject.GetComponentInChildren<Animator>().SetBool("isLayingEgg", true);           
            //if (gameObject.GetComponentInChildren<Animator>().GetBool("isLayingEgg")) { Debug.Log("isLayingEgg = true"); }
            await this.AsyncDelay(0.1f);
            //gameObject.GetComponentInChildren<Animator>().SetBool("isLayingEgg", false);
            //if (this.enabled)
            //    CreateProjectile();
            
            //if (!gameObject.GetComponentInChildren<Animator>().GetBool("isLayingEgg")) { Debug.Log("isLayingEgg = false"); }

        } 
    }

    public override void DealDamage()
    {
        CreateProjectile();
    }

    void CreateProjectile()
    {
        wasShot = true;
        Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.identity);
    }


}
