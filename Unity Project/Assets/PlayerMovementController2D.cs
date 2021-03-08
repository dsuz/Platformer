using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤーの動きを制御する。
/// 接地判定の形は Box でやっている。
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class PlayerMovementController2D : MonoBehaviour
{
    /// <summary>地上で操作された時の動く速さ</summary>
    [SerializeField] float m_runSpeed = 7f;
    /// <summary>空中で操作された時の動く力</summary>
    [SerializeField] float m_movePowerInTheAir = 5f;
    /// <summary>ダッシュ時のスピード</summary>
    [SerializeField] float m_dashSpeed = 15f;
    /// <summary>ダッシュする時間（単位：秒）</summary>
    [SerializeField] float m_dashTime = 0.3f;
    /// <summary>ジャンプ力</summary>
    [SerializeField] float m_jumpPower = 5f;
    /// <summary>「地面」と判定するレイヤー</summary>
    [SerializeField] LayerMask m_groundLayer;
    /// <summary>Pivot から接地判定の中心までのオフセット</summary>
    [SerializeField] Vector2 m_groundOffset = Vector2.down;
    /// <summary>接地判定をする Box のサイズ</summary>
    [SerializeField] Vector2 m_groundTriggerSize = Vector2.one;
    /// <summary>水平方向の入力</summary>
    float m_h;
    /// <summary>垂直方向の入力</summary>
    float m_v;
    Rigidbody2D m_rb = default;
    SpriteRenderer m_sprite = default;
    float m_dashTimer;

    void Start()
    {
        m_rb = GetComponent<Rigidbody2D>();
        m_sprite = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (m_dashTimer > 0) return;    // ダッシュ中は入力を受け付けない

        m_h = Input.GetAxisRaw("Horizontal");
        m_v = Input.GetAxisRaw("Vertical");

        // スプライトの向きを制御する
        if (m_h > 0) m_sprite.flipX = false;
        else if (m_h < 0) m_sprite.flipX = true;

        // 移動・ジャンプを制御する
        Vector3 velocity = m_rb.velocity;

        if (IsGrounded())
        {
            velocity.x = m_h * m_runSpeed; ;

            if (Input.GetButtonDown("Jump"))
            {
                velocity.y = m_jumpPower;
            }
        }

        m_rb.velocity = velocity;

        if (Input.GetButtonDown("Dash"))
        {
            StartCoroutine(Dash());
        }
    }

    void FixedUpdate()
    {
        // 空中制御処理
        if ((m_h > 0 && m_rb.velocity.x < m_runSpeed) || (m_h < 0 && -1 * m_runSpeed < m_rb.velocity.x))
        {
            m_rb.AddForce(m_h * m_movePowerInTheAir * Vector2.right);
        }
    }

    /// <summary>
    /// ダッシュ処理
    /// ダッシュ中は重力の影響を受けなくなる。
    /// </summary>
    /// <returns></returns>
    IEnumerator Dash()
    {
        m_dashTimer = m_dashTime;
        float savedGravityScale = m_rb.gravityScale;
        m_rb.gravityScale = 0f;
        Vector2 velocity = m_sprite.flipX ? -1 * m_dashSpeed * Vector2.right : m_dashSpeed * Vector2.right;

        while (m_dashTimer > 0)
        {
            m_dashTimer -= Time.deltaTime;
            m_rb.velocity = velocity;
            yield return new WaitForEndOfFrame();            
        }

        m_rb.velocity = Vector2.zero;
        m_rb.gravityScale = savedGravityScale;
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
