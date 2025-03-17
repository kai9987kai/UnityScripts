using UnityEngine;

public class TeleportBall : MonoBehaviour
{
    public Transform redSphere;  
    private Rigidbody ballRigidbody;

    void Start()
    {
        ballRigidbody = GetComponent<Rigidbody>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            TeleportBallToRedSphere(other.gameObject);
        }
    }
    void OnTriggerEnter(BoxCollider other)
    {
        if (other.CompareTag("Ball"))
        {
            TeleportBallToRedSphere(other.gameObject);
        }
    }
    void TeleportBallToRedSphere(GameObject ball)
    {
        ballRigidbody.isKinematic = true;
        ball.transform.position = redSphere.position;
        ballRigidbody.velocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;
        ballRigidbody.isKinematic = false;
    }
}
