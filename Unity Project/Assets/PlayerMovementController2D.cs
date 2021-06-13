using System.Collections;
// using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤーの動きを制御する。衝突判定は一つの Capsule Collider 2D で行うことを前提としている。
/// 接地判定の形は Box でやっている。
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(CapsuleCollider2D))]
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
    /// <summary>しゃがんだ時の Capsule Collider 2D のオフセット値</summary>
    [SerializeField] Vector2 m_colliderOffsetOnCrouch = Vector2.one;
    /// <summary>しゃがんだ時の Capsule Collider 2D のサイズ値</summary>
    [SerializeField] Vector2 m_colliderSizeOnCrouch = Vector2.one;
    /// <summary>貼りつける壁のレイヤー</summary>
    [SerializeField] LayerMask m_stickyWallLayer = default;
    /// <summary>壁に貼りつく当たり判定のオフセット</summary>
    [SerializeField] Vector2 m_stickyAreaOffset = Vector2.right * 0.3f;
    /// <summary>壁に貼りつく当たり判定のサイズ</summary>
    [SerializeField] Vector2 m_stickyAreaSize = Vector2.one * 0.1f;
    /// <summary>アタッチされたコンポーネントのキャッシュ</summary>
    Rigidbody2D m_rb = default;
    /// <summary>アタッチされたコンポーネントのキャッシュ</summary>
    Animator m_anim = default;
    /// <summary>アタッチされたコンポーネントのキャッシュ</summary>
    SpriteRenderer m_sprite = default;
    /// <summary>アタッチされたコンポーネントのキャッシュ</summary>
    CapsuleCollider2D m_collider = default;
    /// <summary>水平方向の入力</summary>
    float m_h = 0f;
    /// <summary>垂直方向の入力</summary>
    float m_v = 0f;
    /// <summary>ダッシュした時にカウントダウンされるタイマー</summary>
    float m_dashTimer = 0f;
    /// <summary>空中ジャンプした回数のカウンター</summary>
    int m_midAirJumpCount = 0;
    /// <summary>梯子につかまってるフラグ</summary>
    bool m_isClimbingLadder = false;
    /// <summary>現在重なっている、もしくはつかまっている梯子</summary>
    Transform m_targetLadder = default;
    /// <summary>現在立っている床のコライダー</summary>
    Collider2D m_floorStandingOn = default;
    /// <summary>飛び降りるためにこのオブジェクトとの当たり判定を（一時的に）無効にした床</summary>
    IgnoreCollisionController2D m_floorCollisionDisabled = default;
    /// <summary>しゃがむ前の Capsule Collider 2D のオフセット値</summary>
    Vector2 m_colliderOffsetOnStanding = default;
    /// <summary>しゃがむ前の Capsule Collider 2D のサイズ値</summary>
    Vector2 m_colliderSizeOnStanding = default;
    /// <summary>壁にはりついているフラグ</summary>
    bool m_isStickingToWall = false;

    void Start()
    {
        m_rb = GetComponent<Rigidbody2D>();
        m_sprite = GetComponent<SpriteRenderer>();
        m_anim = GetComponent<Animator>();
        m_collider = GetComponent<CapsuleCollider2D>();
    }

    void Update()
    {
        if (m_dashTimer > 0) return;    // ダッシュ中は入力を受け付けない

        // 壁に貼りついている時にジャンプを押されたら、背中の方向の斜め上に飛ぶ
        if (m_isStickingToWall)
        {
            if (Input.GetButtonDown("Jump"))
            {
                this.transform.localScale = new Vector3(this.transform.localScale.x * -1, this.transform.localScale.y, this.transform.localScale.z);
                StickToWall(false);
                Vector3 velocity = Vector3.zero;
                velocity.y += m_jumpPower;
                velocity.x += m_runSpeed * (this.transform.localScale.x < 0 ? -1 : 1);
                m_rb.velocity = velocity;
            }
        }
        else if (!IsGrounded())
        {
            // 壁に貼りつく
            bool isTrouchingStickyWall = Physics2D.OverlapBox(this.transform.position + (Vector3) m_stickyAreaOffset * (this.transform.localScale.x > 0 ? 1 : -1), m_stickyAreaSize, 0, m_stickyWallLayer);
            StickToWall(isTrouchingStickyWall);
        }

        m_v = Input.GetAxisRaw("Vertical");
        m_h = Input.GetAxisRaw("Horizontal");
        m_h = Mathf.Round(m_h); // 入力をデジタル化する

        FlipSprite();

        // 梯子に重なった状態で上下を入力すると梯子につかまる
        if (m_targetLadder && m_v != 0)
        {
            CatchLadder(true);
        }

        // 梯子上を移動する
        if (m_isClimbingLadder)
        {
            Crouch(false);

            if (m_v > 0)
            {
                m_rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                m_rb.velocity = m_climbUpLadderSpeed * Vector2.up;
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

                if (m_v < 0)
                {
                    velocity.x = 0;

                    if (Input.GetButtonDown("Jump"))    // 下を押しながらジャンプ
                    {
                        Crouch(false);

                        if (!DropDownFloor())   // すり抜けられない床の上だった場合はジャンプする
                        {
                            velocity.y = m_jumpPower;   // ジャンプ
                        }
                    }
                    else
                    {
                        Crouch(true);
                    }
                }
                else
                {
                    Crouch(false);
                    velocity.x = m_h * m_runSpeed;

                    if (Input.GetButtonDown("Jump"))
                    {
                        velocity.y = m_jumpPower;   // ジャンプ
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
        bool isGrounded = IsGrounded();

        // 衝突判定を無効にした床を通り抜けたら、無効にした判定を戻す
        if (m_floorCollisionDisabled)
        {
            if (!isGrounded)
            {
                m_floorCollisionDisabled.IgnoreCollision(m_collider, false);
                m_floorCollisionDisabled = null;
            }
        }

        // 空中制御処理
        if (!isGrounded && ((m_h > 0 && m_rb.velocity.x < m_runSpeed) || (m_h < 0 && -1 * m_runSpeed < m_rb.velocity.x)))
        {
            m_rb.AddForce(m_h * m_movePowerInTheAir * Vector2.right);
        }
    }

    void LateUpdate()
    {
        if (m_anim)
        {
            m_anim.SetFloat("SpeedX", Mathf.Abs(m_rb.velocity.x));
            m_anim.SetFloat("SpeedY", m_rb.velocity.y);
            m_anim.SetBool("IsGrounded", IsGrounded());
            m_anim.SetBool("IsClimbingLadder", m_isClimbingLadder);
            m_anim.SetBool("IsCrouching", m_collider.offset == m_colliderOffsetOnCrouch);
            m_anim.SetBool("IsStickToWall", m_isStickingToWall);
        }
    }

    /// <summary>
    /// しゃがみ・立ち上がりを制御する
    /// </summary>
    /// <param name="crouch">true の時しゃがむ、false の時立ち上がる</param>
    void Crouch(bool crouch)
    {
        // 立っている時（飛び降り中を除く）にしゃがめと命令された場合
        if (crouch && m_collider.offset != m_colliderOffsetOnCrouch && !m_floorCollisionDisabled)    
        {
            m_colliderOffsetOnStanding = m_collider.offset;
            m_colliderSizeOnStanding = m_collider.size;
            m_collider.offset = m_colliderOffsetOnCrouch;
            m_collider.size = m_colliderSizeOnCrouch;
        }
        // しゃがんでいる時に立ち上がれと命令された場合
        else if (!crouch && m_collider.offset == m_colliderOffsetOnCrouch)
        {
            m_collider.offset = m_colliderOffsetOnStanding;
            m_collider.size = m_colliderSizeOnStanding;
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
        Vector2 velocity = this.transform.localScale.x < 0 ? -1 * m_dashSpeed * Vector2.right : m_dashSpeed * Vector2.right;

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

        // 壁に貼りつくエリアを表示する
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(this.transform.position + (Vector3)m_stickyAreaOffset * (this.transform.localScale.x > 0 ? 1 : -1), m_stickyAreaSize);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("LadderTag"))
        {
            m_targetLadder = collision.gameObject.transform;
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("LadderTag"))
        {
            // 梯子につかまっていた時は、梯子を離す
            if (m_isClimbingLadder)
            {
                CatchLadder(false);
            }

            m_targetLadder = null;
        }
    }

    /// <summary>
    /// 壁に貼りつく
    /// </summary>
    /// <param name="flag">true で貼りつき、false で離れる</param>
    void StickToWall(bool flag)
    {
        m_isStickingToWall = flag;
        m_rb.constraints = m_isStickingToWall ? RigidbodyConstraints2D.FreezeAll : RigidbodyConstraints2D.FreezeRotation;
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

        m_targetLadder.GetComponent<IgnoreCollisionController2D>()?.IgnoreCollision(m_collider, isCatch);
        m_isClimbingLadder = isCatch;
        m_rb.constraints = isCatch ? RigidbodyConstraints2D.FreezeAll : RigidbodyConstraints2D.FreezeRotation;
    }

    /// <summary>
    /// 床を通り抜けて飛び降りる
    /// IgnoreCollisionController2D コンポーネントがアタッチされた床をすり抜けることができる
    /// </summary>
    /// <returns>足下の床が通り抜けられるものだった場合 true, 飛び降りることができない床だった場合 false を返す</returns>
    bool DropDownFloor()
    {
        // 自分が立っている床がすり抜けられる床だったら、自分との衝突判定を無効にする
        m_floorCollisionDisabled = m_floorStandingOn?.GetComponent<IgnoreCollisionController2D>();
        m_floorCollisionDisabled?.IgnoreCollision(m_collider, true);   // 注: プレイヤーのコライダーが一つであることを前提としている
        return m_floorCollisionDisabled;
    }

    /// <summary>
    /// キャラクター（スプライト）の左右の向きを制御する。Scale - X が正の値の時、キャラクターが右を向いていることを前提とする。
    /// </summary>
    void FlipSprite()
    {
        // 入力方向とキャラクターの向きが逆の時
        if (m_h * this.transform.localScale.x < 0)
        {
            // Scale - X に -1 をかけて反転させる
            Vector3 scale = this.transform.localScale;
            scale.x = -1 * scale.x;
            this.transform.localScale = scale;
        }
    }
}
