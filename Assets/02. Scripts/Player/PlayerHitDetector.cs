using System;
using UnityEngine;

public class PlayerHitDetector : MonoBehaviour
{
    [Header("Normal Attack Hitbox")]
    [SerializeField] private Vector3 _normalAttack1Size = new Vector3(2f, 2f, 2f);
    [SerializeField] private Vector3 _normalAttack1Offset = new Vector3(0f, 0f, 1f);
    [SerializeField] private Vector3 _normalAttack1Rotation = Vector3.zero;

    [Space(10)]
    [SerializeField] private Vector3 _normalAttack2Size = new Vector3(2.5f, 2f, 2.5f);
    [SerializeField] private Vector3 _normalAttack2Offset = new Vector3(0f, 0f, 1.2f);
    [SerializeField] private Vector3 _normalAttack2Rotation = Vector3.zero;

    [Space(10)]
    [SerializeField] private Vector3 _normalAttack3Size = new Vector3(3f, 2f, 3f);
    [SerializeField] private Vector3 _normalAttack3Offset = new Vector3(0f, 0f, 1.5f);
    [SerializeField] private Vector3 _normalAttack3Rotation = Vector3.zero;

    [Header("Ultimate Hitbox")]
    [SerializeField] private Vector3 _ultimateAttackSize = new Vector3(5f, 3f, 5f);
    [SerializeField] private Vector3 _ultimateAttackOffset = new Vector3(0f, 0f, 2f);
    [SerializeField] private Vector3 _ultimateAttackRotation = Vector3.zero;

    [Header("Settings")]
    [SerializeField] private LayerMask _enemyLayerMask;
    [SerializeField] private bool _showGizmo = false;

    private Collider[] _hitBuffer = new Collider[16];

    public event Action<Collider[], int, float, bool, PlayerHitType> OnHitDetected;

    public void DetectNormalAttack1(float coefficient, bool isEnhanced)
    {
        Detect(_normalAttack1Size, _normalAttack1Offset, _normalAttack1Rotation, coefficient, isEnhanced, PlayerHitType.Normal1);
    }

    public void DetectNormalAttack2(float coefficient, bool isEnhanced)
    {
        Detect(_normalAttack2Size, _normalAttack2Offset, _normalAttack2Rotation, coefficient, isEnhanced, PlayerHitType.Normal2);
    }

    public void DetectNormalAttack3(float coefficient, bool isEnhanced)
    {
        Detect(_normalAttack3Size, _normalAttack3Offset, _normalAttack3Rotation, coefficient, isEnhanced, PlayerHitType.Normal3);
    }

    public void DetectUltimate(float coefficient)
    {
        Detect(_ultimateAttackSize, _ultimateAttackOffset, _ultimateAttackRotation, coefficient, true, PlayerHitType.Ultimate);
    }

    private void Detect(Vector3 size, Vector3 offset, Vector3 rotation, float coefficient, bool isEnhanced, PlayerHitType hitType)
    {
        Vector3 center = transform.position + transform.TransformDirection(offset);
        Quaternion rot = transform.rotation * Quaternion.Euler(rotation);

        int hitCount = Physics.OverlapBoxNonAlloc(center, size / 2f, _hitBuffer, rot, _enemyLayerMask);
        
        if (hitCount > 0)
        {
            OnHitDetected?.Invoke(_hitBuffer, hitCount, coefficient, isEnhanced, hitType);
        }
    }

    public int DetectSphere(Vector3 center, float radius, out Collider[] hits)
    {
        int count = Physics.OverlapSphereNonAlloc(center, radius, _hitBuffer, _enemyLayerMask);
        hits = _hitBuffer;
        return count;
    }

    #region Gizmos
    
    private void OnDrawGizmos()
    {
        if (!_showGizmo) return;
        
        DrawGizmo(_normalAttack1Size, _normalAttack1Offset, _normalAttack1Rotation, new Color(1f, 0f, 0f, 0.3f));
        DrawGizmo(_normalAttack2Size, _normalAttack2Offset, _normalAttack2Rotation, new Color(0f, 1f, 0f, 0.3f));
        DrawGizmo(_normalAttack3Size, _normalAttack3Offset, _normalAttack3Rotation, new Color(0f, 0f, 1f, 0.3f));
        DrawGizmo(_ultimateAttackSize, _ultimateAttackOffset, _ultimateAttackRotation, new Color(1f, 1f, 0f, 0.3f));
    }

    private void DrawGizmo(Vector3 size, Vector3 offset, Vector3 rotation, Color color)
    {
        Gizmos.color = color;
        Vector3 center = transform.position + transform.TransformDirection(offset);
        Quaternion rot = transform.rotation * Quaternion.Euler(rotation);
        Gizmos.matrix = Matrix4x4.TRS(center, rot, Vector3.one);
        Gizmos.DrawCube(Vector3.zero, size);
        Gizmos.DrawWireCube(Vector3.zero, size);
    }
    
    #endregion
}
