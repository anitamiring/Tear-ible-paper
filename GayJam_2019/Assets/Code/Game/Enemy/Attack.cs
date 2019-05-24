using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Apkd;

public abstract class Attack : MonoBehaviour
{

    [S] protected float damage { get; }

    public abstract void DealDamage();

    protected bool isAttacking;

    public void StopAttack()
    {
        isAttacking = false;
    }
}
