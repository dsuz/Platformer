using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �v���C���[�̓����𐧌䂷��B
/// �ڒn����̌`�� Box �ł���Ă���B
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class PlayerMovementController2D : MonoBehaviour
{
    /// <summary>�n��ő��삳�ꂽ���̓�������</summary>
    [SerializeField] float m_runSpeed = 7f;
    /// <summary>�󒆂ő��삳�ꂽ���̓�����</summary>
    [SerializeField] float m_movePowerInTheAir = 5f;
    /// <summary>�_�b�V�����̃X�s�[�h</summary>
    [SerializeField] float m_dashSpeed = 15f;
    /// <summary>�_�b�V�����鎞�ԁi�P�ʁF�b�j</summary>
    [SerializeField] float m_dashTime = 0.3f;
    /// <summary>�W�����v��</summary>
    [SerializeField] float m_jumpPower = 5f;
    /// <summary>�u�n�ʁv�Ɣ��肷�郌�C���[</summary>
    [SerializeField] LayerMask m_groundLayer;
    /// <summary>Pivot ����ڒn����̒��S�܂ł̃I�t�Z�b�g</summary>
    [SerializeField] Vector2 m_groundOffset = Vector2.down;
    /// <summary>�ڒn��������� Box �̃T�C�Y</summary>
    [SerializeField] Vector2 m_groundTriggerSize = Vector2.one;
    /// <summary>���������̓���</summary>
    float m_h;
    /// <summary>���������̓���</summary>
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
        if (m_dashTimer > 0) return;    // �_�b�V�����͓��͂��󂯕t���Ȃ�

        m_h = Input.GetAxisRaw("Horizontal");
        m_v = Input.GetAxisRaw("Vertical");

        // �X�v���C�g�̌����𐧌䂷��
        if (m_h > 0) m_sprite.flipX = false;
        else if (m_h < 0) m_sprite.flipX = true;

        // �ړ��E�W�����v�𐧌䂷��
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
        // �󒆐��䏈��
        if ((m_h > 0 && m_rb.velocity.x < m_runSpeed) || (m_h < 0 && -1 * m_runSpeed < m_rb.velocity.x))
        {
            m_rb.AddForce(m_h * m_movePowerInTheAir * Vector2.right);
        }
    }

    /// <summary>
    /// �_�b�V������
    /// �_�b�V�����͏d�͂̉e�����󂯂Ȃ��Ȃ�B
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
