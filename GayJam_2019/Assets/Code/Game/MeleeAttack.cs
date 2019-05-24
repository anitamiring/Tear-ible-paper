using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Apkd;

public class MeleeAttack : Attack
{
    [Inject.Singleton]
    private Castle castle { get; }

    [Inject]
    protected HealthComponent health { get; }

    [S] float timeBetweenhits { get; } = 1f;


    private void Start()
    {
        health.OnDestroy.AddListener(() => this.enabled = false);
    }

    public override void DealDamage()
    {
        castle.HealthComponent.DealDamage(damage);
    }

    async void DealDamageToCastle()
    {
        var healthComponent = castle.HealthComponent;
        while (isAttacking)
        {
            healthComponent.DealDamage(damage);
            await this.AsyncDelay(timeBetweenhits);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == castle.gameObject)
        {
            gameObject.GetComponentInChildren<Animator>().SetBool("isFighting", true);
            if (gameObject.GetComponentInChildren<Animator>().GetBool("isFighting")) { Debug.Log("isFighting = true"); }
            isAttacking = true;
            DealDamageToCastle();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        isAttacking = false;
    }


}
