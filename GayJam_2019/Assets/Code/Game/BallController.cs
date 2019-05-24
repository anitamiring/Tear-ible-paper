using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Apkd;
using Odin = Sirenix.OdinInspector;

public class BallController : MonoBehaviour
{
    [Inject.Singleton]
    private Castle castle { get; }

    [S, Inject]
    new Rigidbody2D rigidbody { get; }

    [Odin.BoxGroup("Settings")]
    [S] float maxVelocity { get; }
    [Odin.BoxGroup("Settings")]
    [S] AnimationCurve damageFromVelocity { get; }

    bool isFrozen { get; set; }

    public Vector2 Velocity { get => rigidbody.velocity; set => rigidbody.velocity = value; }

    Vector3 rotationVector = new Vector3();

    private void Update()
    {
        transform.Rotate(Velocity);

        transform.right = rigidbody.velocity.normalized; //TODO: Uncomment if object rotation is necessary
        if (Velocity.magnitude > maxVelocity)
        {
            rigidbody.velocity = Velocity.normalized * maxVelocity;
        }
    }

    [Odin.Button]
    public void Freeze() => rigidbody.simulated = false;

    [Odin.Button]
    public void Unfreeze() => rigidbody.simulated = true;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        rotationVector = new Vector3(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)).normalized;

        if (collision.gameObject == castle.gameObject)
            return;

        HealthComponent health;
        if(health = collision.gameObject.GetComponent<HealthComponent>())
        {
            health.DealDamage(damageFromVelocity.Evaluate(Velocity.magnitude));
        }
    }
}
