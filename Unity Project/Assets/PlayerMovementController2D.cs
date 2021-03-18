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
    /// <summary>空中でのジャンプ力</summary>
    [SerializeField] float m_jumpPowerMidAir = 4f;
    /// <summary>「地面」と判定するレイヤー</summary>
    [SerializeField] LayerMask m_groundLayer;
    /// <summary>Pivot から接地判定の中心までのオフセット</summary>
    [SerializeField] Vector2 m_groundOffset = Vector2.down;
    /// <summary>接地判定をする Box のサイズ</summary>
    [SerializeField] Vector2 m_groundTriggerSize = Vector2.one;
    /// <summary>空中でジャンプできる回数</summary>
    [SerializeField] int m_maxMidAirJumpCount = 1;
    /// <summary>梯子を登る速さ</summary>
    [SerializeField] float m_climbUpLadderSpeed = 4f;
    /// <summary>梯子を降りる速さ</summary>
    [SerializeField] float m_climbDownLadderSpeed = 10f;
    /// <summary>水平方向の入力</summary>
    float m_h = 0f;
    /// <summary>垂直方向の入力</summary>
    float m_v = 0f;
    Rigidbody2D m_rb = default;
    SpriteRenderer m_sprite = default;
    /// <summary>ダッシュした時にカウントダウンされるタイマー</summary>
    float m_dashTimer = 0f;
    /// <summary>空中ジャンプした回数のカウンター</summary>
    int m_midAirJumpCount = 0;
    /// <summary>梯子と重なってるフラグ</summary>
    bool m_isOnLadder = false;
    /// <summary>梯子につかまってるフラグ</summary>
    bool m_isClimbingLadder = false;
    /// <summary>現在つかまっている梯子</summary>
    Transform m_targetLadder = default;
    /// <summary>現在立っている床のコライダー</summary>
    Collider2D m_floorStandingOn = default;
    /// <summary>飛び降りるためにこのオブジェクトとの当たり判定を無効にしたコライダー</summary>
    Collider2D m_floorCollisionDisabled = default;

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

        // 梯子につかまる
        if (m_isOnLadder)
        {
            if (m_v != 0)
            {
                CatchLadder(true);
            }
        }

        if (m_isClimbingLadder)
        {
            if (m_v > 0)
            {
                m_rb.constraints = RigidbodyConstraints2D.FreezeAll;
                this.transform.Translate(0f, m_v * m_climbUpLadderSpeed * Time.deltaTime, 0f);
            }
            else if (m_v < 0)
            {
                m_rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                m_rb.velocity = m_climbDownLadderSpeed * Vector2.down;
            }
            else
            {
                m_rb.constraints = RigidbodyConstraints2D.FreezeAll;
                this.transform.Translate(m_h * m_runSpeed * Time.deltaTime, 0, 0f);
            }
        }
        else
        {
            // 移動・ジャンプを速度で制御する
            Vector3 velocity = m_rb.velocity;

            if (IsGrounded())
            {
                if (m_midAirJumpCount > 0)
                    m_midAirJumpCount = 0;

                velocity.x = m_h * m_runSpeed; ;

                if (Input.GetButtonDown("Jump"))
                {
                    if (m_v < 0)
                    {
                        DropDownFloor();
                    }
                    else
                    {
                        velocity.y = m_jumpPower;
                    }
                }
            }
            else
            {
                // 空中ジャンプ
                if (m_midAirJumpCount < m_maxMidAirJumpCount && Input.GetButtonDown("Jump"))
                {
                    m_midAirJumpCount++;
                    velocity.y = m_jumpPowerMidAir;
                }
            }

            // 速度を決定する
            m_rb.velocity = velocity;
        }

        if (Input.GetButtonDown("Dash"))
        {
            StartCoroutine(Dash());
        }
    }

    void FixedUpdate()
    {
        // 衝突判定を無効にして飛び降りた床の判定を戻す
        if (m_floorCollisionDisabled)
        {
            if (!IsGrounded())
            {
                Physics2D.IgnoreCollision(m_floorCollisionDisabled, GetComponent<Collider2D>(), false);
                m_floorCollisionDisabled = null;
            }
        }

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
        m_floorStandingOn = Physics2D.OverlapBox((Vector2)this.transform.position + m_groundOffset, m_groundTriggerSize, 0, m_groundLayer);
        return m_floorStandingOn;
    }

    void OnDrawGizmosSelected()
    {
        // 接地判定するエリアを表示する
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(this.transform.position + (Vector3)m_groundOffset, m_groundTriggerSize);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("LadderTag"))
        {
            m_isOnLadder = true;
            m_targetLadder = collision.gameObject.transform;
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("LadderTag"))
        {
            m_isOnLadder = false;

            // 梯子につかまっていた時は、梯子を離す
            if (m_isClimbingLadder)
            {
                CatchLadder(false);
            }

            m_targetLadder = null;
        }
    }

    /// <summary>
    /// 梯子につかまる・梯子を離す時に呼ぶ
    /// </summary>
    /// <param name="isCatch">つかまる時は true, 離す時は false</param>
    void CatchLadder(bool isCatch)
    {
        if (m_midAirJumpCount > 0)
            m_midAirJumpCount = 0;

        if (isCatch)
        {
            this.transform.position = new Vector3(m_targetLadder.position.x, this.transform.position.y, this.transform.position.z);
        }

        m_isClimbingLadder = isCatch;
        m_rb.constraints = isCatch ? RigidbodyConstraints2D.FreezeAll : RigidbodyConstraints2D.FreezeRotation;
    }

    /// <summary>
    /// 床を通り抜けて飛び降りる
    /// PlatformEffector2D コンポーネントがアタッチされた床をすり抜けることができる
    /// </summary>
    void DropDownFloor()
    {
        // 自分が立っている床が一方通行の床だったら、自分との衝突判定を無効にする
        m_floorCollisionDisabled = m_floorStandingOn?.GetComponent<PlatformEffector2D>() ? m_floorStandingOn : null;

        if (m_floorCollisionDisabled)
        {
            Physics2D.IgnoreCollision(m_floorCollisionDisabled, GetComponent<Collider2D>());    // 注: プレイヤーのコライダーが一つであることを前提としている
        }
    }
}
