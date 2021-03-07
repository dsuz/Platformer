using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �v���C���[�̓����𐧌䂷��B
/// �ڒn����̌`�� Box �ł���Ă���B
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovementController2D : MonoBehaviour
{
    [SerializeField] float m_runSpeed = 7f;
    [SerializeField] float m_jumpPower = 5f;
    [SerializeField] LayerMask m_groundLayer;
    /// <summary>Pivot ����ڒn����̒��S�܂ł̃I�t�Z�b�g</summary>
    [SerializeField] Vector2 m_groundOffset = Vector2.down;
    /// <summary>�ڒn��������� Box �̃T�C�Y</summary>
    [SerializeField] Vector2 m_groundTriggerSize = Vector2.one;
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

        if (Input.GetButtonDown("Jump") && IsGrounded())
        {
            velocity.y = m_jumpPower;
        }

        m_rb.velocity = velocity;
    }

    /// <summary>
    /// �ڒn����
    /// </summary>
    /// <returns></returns>
    bool IsGrounded()
    {
        return Physics2D.OverlapBox((Vector2)this.transform.position + m_groundOffset, m_groundTriggerSize, 0, m_groundLayer);
    }

    void OnDrawGizmosSelected()
    {
        // �ڒn���肷��G���A��\������
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(this.transform.position + (Vector3)m_groundOffset, m_groundTriggerSize);
    }
}
