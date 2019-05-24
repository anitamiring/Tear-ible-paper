using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitExplosion : Attack
{
    [SerializeField] GameObject particleExplosion;

    public override void DealDamage()
    {

    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Tower")
        {
            HealthComponent health = GetComponent<HealthComponent>();
            if(health) health.DealDamage(damage);

            if (particleExplosion) Instantiate(particleExplosion, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
