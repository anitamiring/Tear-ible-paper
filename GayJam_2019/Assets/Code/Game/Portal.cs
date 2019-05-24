using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Apkd;

public class Portal : MonoBehaviour
{
    [Inject]
    ReadOnlySet<Portal> portals { get; }

    [Inject.Singleton]
    BallController Ball { get; }

    [S] public PortalType Type { get; }

    Portal nextPortal { get; set; }

    public enum State { StartPortal, EndPortal }
    public enum PortalType { Player }
    public State CurrentState { get; private set; }

    private void Start()
    {
        foreach (var portal in portals)
        {
            if (portal.Type == Portal.PortalType.Player && portal != this)
            {
                nextPortal = portal;
                return;
            }
        }
        if(nextPortal == null)
        {
            Debug.LogError("nextPortal is null");
            return;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (CurrentState == State.EndPortal)
            return;

        if (collision.gameObject == Ball.gameObject)
            TeleportBall(Ball);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        CurrentState = State.StartPortal;
    }
    
    void TeleportBall(BallController ball)
    {
        nextPortal.ChangeStateToEnd();

        var projection = Vector3.Project(transform.position - ball.transform.position, transform.right);
        var offset = ball.transform.position + projection - transform.position;
        ball.transform.position = nextPortal.transform.position + offset;

        var localDir = transform.InverseTransformDirection(ball.Velocity);
        var nextDir = nextPortal.transform.TransformDirection(localDir);
        ball.Velocity = -(nextDir+nextDir.normalized);

        

        Debug.DrawLine(ball.transform.position, ball.transform.position + new Vector3(nextDir.x, nextDir.y, 0f), Color.red);
    }

    public void ChangeStateToStart()
    {
        CurrentState = State.StartPortal;
    }

    public void ChangeStateToEnd()
    {
        CurrentState = State.EndPortal;
    }

}
