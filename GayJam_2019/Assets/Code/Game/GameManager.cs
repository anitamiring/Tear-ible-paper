using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Apkd;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Inject.Singleton]
    BallController ball { get; }

    [Inject.Singleton]
    Castle castle { get; }

    public bool IsFreeze { get; private set; }
    public bool GameIsOn { get; set; }
    public bool GameIsOver { get; set; }

    float gameTime;
    public float GameTime => gameTime;
    float lastBestTime;
    public float BestTime => GameTime > lastBestTime ? GameTime : lastBestTime;
    float saveTime;


    [S] public UnityEvent OnGameIsOver { get; }
    public object Aplication { get; private set; }

    private void Start()
    {       
        if (PlayerPrefs.HasKey("BestTime")) { lastBestTime = PlayerPrefs.GetFloat("BestTime"); }
        else { lastBestTime = 0; }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            FreezeUnfreezeBall();
        }

        if(!GameIsOver && castle.HealthComponent.CurrentHealth <= 0)
        {
            GameIsOver = true;
            GameIsOn = false;
            OnGameIsOver.Invoke();
        }

        if(GameIsOn && !GameIsOver)
        {
            gameTime += Time.deltaTime;
        }

    }

    public void StartGame()
    {
        if (GameIsOn)
            return;

        GameIsOn = true;
        gameTime = 0f;
    }


    private void FreezeUnfreezeBall()
    {
        if (IsFreeze)
        {
            ball.Unfreeze();
        }
        else
        {
            ball.Freeze();
        }

        IsFreeze = !IsFreeze;
    }

    void SaveBestTime()
    {

        PlayerPrefs.SetFloat("BestTime", BestTime);

    }

    public void GameReset()
    {
        SaveBestTime();
        SceneManager.LoadSceneAsync(0);
    }

    public void GameEnd()
    {
        SaveBestTime();
        Application.Quit();
    }


}
