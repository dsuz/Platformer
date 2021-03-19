using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �h���b�v�_�E���𐧌䂷��R���|�[�l���g�B�h���b�v�_�E�������������ȂǂɃA�^�b�`����B
/// �h���b�v�_�E���́u����̃R���C�_�[�Ƃ��̃I�u�W�F�N�g�̎w�肳�ꂽ�R���C�_�[�Ƃ̏Փ˂𖳌��ɂ���v���ƂŎ�������B
/// </summary>
public class IgnoreCollisionController2D : MonoBehaviour
{
    /// <summary>�Փ˂𖳌��ɂ���R���C�_�[</summary>
    [SerializeField] Collider2D[] m_targetColliders = default;

    /// <summary>
    /// m_targetColliders �𖳌��ɂ���
    /// </summary>
    /// <param name="collider2D">�Փ˂𖳌��ɂ���Ώۂ̃R���C�_�[</param>
    /// <param name="ignore">�����ɂ��鎞 true�A�L���ɖ߂��� false ���w�肷��</param>
    public void IgnoreCollision(Collider2D collider2D, bool ignore)
    {
        foreach (var c in m_targetColliders)
        {
            Physics2D.IgnoreCollision(c, collider2D, ignore);
        }
    }
}
