using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Apkd;
using Odin = Sirenix.OdinInspector;

public class HealthComponent : MonoBehaviour
{
    [S] public float MaxHealth { get; private set; }
    [S] public float CurrentHealth { get; private set; }
    public float Percentage => CurrentHealth / MaxHealth;

    [S] public UnityEvent OnDamaged { get; set; }
    [S] public UnityEvent OnDestroy { get; set; }

    public GameObject gameOverScreen;

    [Inject.Singleton] GameManager gameManager { get; }

    private void Start()
    {
        CurrentHealth = MaxHealth;
    }

    public void AddHealth(float value)
    {
        if(value < 0)
        {
            Debug.LogError("AddHealth value should be bigger than 0");
            return;
        }

        CurrentHealth += value;
        if (CurrentHealth > MaxHealth)
            CurrentHealth = MaxHealth;
    }

    public void DealDamage(float value)
    {
       
        if (value < 0)
        {
            Debug.LogError("DealDamage value should be bigger than 0");
            return;
        }

        CurrentHealth -= value;
        OnDamaged.Invoke();

        if (CurrentHealth <= 0)
            KillThis();
    }

    void KillThis()
    {
        OnDestroy.Invoke();
    }

    [Odin.Button]
    public void Add1Damage() => DealDamage(1f);
}
