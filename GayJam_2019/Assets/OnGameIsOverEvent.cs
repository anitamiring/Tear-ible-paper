using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Apkd;
using UnityEngine.Events;

public class OnGameIsOverEvent : MonoBehaviour
{
    [Inject.Singleton] Castle castle { get; }
    [S] public UnityEvent OnGameIsOver { get; }

    private void Awake()
    {
        castle.HealthComponent.OnDestroy.AddListener(() => OnGameIsOver.Invoke());
    }
}
