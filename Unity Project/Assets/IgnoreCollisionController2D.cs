using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ドロップダウンを制御するコンポーネント。ドロップダウンさせたい床などにアタッチすることを想定している。
/// ドロップダウンは「特定のコライダーとこのオブジェクトの指定されたコライダーとの衝突を無効にする」ことで実現する。
/// </summary>
public class IgnoreCollisionController2D : MonoBehaviour
{
    /// <summary>衝突を無効にするコライダー</summary>
    [SerializeField] Collider2D[] m_targetColliders = default;

    /// <summary>
    /// m_targetColliders を無効にする
    /// </summary>
    /// <param name="collider2D">衝突を無効にする対象のコライダー</param>
    /// <param name="ignore">無効にする時 true、有効に戻す時 false を指定する</param>
    public void IgnoreCollision(Collider2D collider2D, bool ignore)
    {
        foreach (var c in m_targetColliders)
        {
            Physics2D.IgnoreCollision(c, collider2D, ignore);
        }
    }
}
