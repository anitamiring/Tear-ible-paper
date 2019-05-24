using Apkd;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OnObjectStart : MonoBehaviour
{
    [S] public UnityEvent OnGameIsStart { get; }

    private void Awake()
    {
        OnGameIsStart.Invoke();
    }
}
