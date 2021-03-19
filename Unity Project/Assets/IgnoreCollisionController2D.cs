using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ドロップダウンを制御するコンポーネント。ドロップダウンさせたい床などにアタッチする。
/// ドロップダウンは「特定のコライダーとこのオブジェクトの指定されたコライダーとの衝突を無効にする」ことで実現する。
/// </summary>
public class IgnoreCollisionController2D : MonoBehaviour
{
    /// <summary>衝突を無効にするコライダー</summary>
    [SerializeField] Collider2D[] m_targetCollider = default;

    public void IgnoreCollision(Collider2D collider2D, bool ignore)
    {
        foreach (var c in m_targetCollider)
        {
            Physics2D.IgnoreCollision(c, collider2D, ignore);
        }
    }
}
