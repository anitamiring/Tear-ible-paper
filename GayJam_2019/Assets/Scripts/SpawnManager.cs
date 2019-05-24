using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Apkd;

[System.Serializable]
class Monster
{
    public Transform[] spawn;
    public GameObject monster;
}

public class SpawnManager : MonoBehaviour
{
    [Inject.Singleton] GameManager gameManager { get; }

    [S] Monster[] monsters { get; }
    [S] AnimationCurve timeToSpawn { get; }


    private List<GameObject> spawned = new List<GameObject>();

    private int spawnedMonstersAmount = 0;
    private bool isSpawning = true;

    private async void Start()
    {
        gameManager.OnGameIsOver.AddListener(() => TurnOnWinAnimation());

        while (true)
        {
            if (gameManager.GameIsOn)
                RandomMonster();

            await this.AsyncDelay(timeToSpawn.Evaluate(gameManager.GameTime));
        }
    }

    void RandomMonster()
    {
        if (isSpawning == false) return;

        int nr = GetRandomMonsterIndeX();

        int spawnArrayLength = monsters[nr].spawn.Length;
        var pos = monsters[nr].spawn[UnityEngine.Random.Range(0, spawnArrayLength)].position;
        var monster = Instantiate(monsters[nr].monster, pos, Quaternion.identity);
        spawnedMonstersAmount++;

        spawned.Add(monster);

       // SpriteRenderer[] sprites = monster.GetComponentsInChildren<SpriteRenderer>();
       // Array.ForEach(sprites, s => s.sortingOrder += spawnedMonstersAmount * 2);
    }

    int GetRandomMonsterIndeX()
    {
        int nr = UnityEngine.Random.Range(0, 100);

        if (nr < 25) return 0;
        else if (nr >= 25 && nr <= 85) return 2;
        else return 1;
    }

    public void TurnOnWinAnimation()
    {
        isSpawning = false;
        spawned.ForEach(s => s.GetComponent<Animator>().Play("win"));
    }

    public void AddMonster(GameObject monster)
    {
        spawned.Add(monster);
    }
}
