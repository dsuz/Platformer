using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤーの動きを制御する。
/// 接地判定の形は Box でやっている。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovementController2D : MonoBehaviour
{
    [SerializeField] float m_runSpeed = 7f;
    [SerializeField] float m_jumpPower = 5f;
    [SerializeField] LayerMask m_groundLayer;
    /// <summary>Pivot から接地判定の中心までのオフセット</summary>
    [SerializeField] Vector2 m_groundOffset = Vector2.down;
    /// <summary>接地判定をする Box のサイズ</summary>
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
    /// 接地判定
    /// </summary>
    /// <returns></returns>
    bool IsGrounded()
    {
        return Physics2D.OverlapBox((Vector2)this.transform.position + m_groundOffset, m_groundTriggerSize, 0, m_groundLayer);
    }

    void OnDrawGizmosSelected()
    {
        // 接地判定するエリアを表示する
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(this.transform.position + (Vector3)m_groundOffset, m_groundTriggerSize);
    }
}
