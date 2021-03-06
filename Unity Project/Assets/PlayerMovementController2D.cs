using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovementController2D : MonoBehaviour
{
    [SerializeField] float m_runSpeed = 7f;
    [SerializeField] float m_jumpPower = 5f;
    Rigidbody2D m_rb = default;

    void Start()
    {
        m_rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector2 velocity = Vector2.right * h * m_runSpeed;
        velocity.y = m_rb.velocity.y;

        if (Input.GetButtonDown("Jump"))
        {
            velocity.y = m_jumpPower;
        }

        m_rb.velocity = velocity;
    }
}
