using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ballmovement : MonoBehaviour
{

    public Rigidbody rb;
    public float speed;
    public float shift;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.W))
            {
            if (Input.GetKey(KeyCode.W))
                rb.AddForce(Vector3.forward);
                if (Input.GetKey(KeyCode.LeftShift))
                {
                rb.AddForce(Vector3.forward * speed);

            }

            Debug.Log("w pressed");

        }

        if (Input.GetKey(KeyCode.S))
        {
            rb.AddForce(Vector3.back * speed);
            Debug.Log("S pressed");
            if (Input.GetKey(KeyCode.LeftShift))
            {
                rb.AddForce(Vector3.forward * speed);

            }
        }
        if (Input.GetKey(KeyCode.D))
        {
            Debug.Log("D pressed");
            rb.AddForce(Vector3.right * speed);
            if (Input.GetKey(KeyCode.LeftShift))
            {
                rb.AddForce(Vector3.forward * speed);

            }
        }
        if (Input.GetKey(KeyCode.A))
        {
            Debug.Log("A pressed");
            rb.AddForce(Vector3.left * speed);
            if (Input.GetKey(KeyCode.LeftShift))
            {
                rb.AddForce(Vector3.forward * speed);

            }
        }
    }
}
