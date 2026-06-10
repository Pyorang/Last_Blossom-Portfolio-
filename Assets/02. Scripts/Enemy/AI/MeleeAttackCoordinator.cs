using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어를 공격할 수 있는 근접 몬스터 슬롯 관리
/// </summary>
public class MeleeAttackCoordinator : MonoBehaviour
{
    public static MeleeAttackCoordinator Instance { get; private set; }

    [Header("설정")]
    [SerializeField] private int _maxSlots = 3;

    private HashSet<EnemyController> _slotHolders = new HashSet<EnemyController>();
    private Queue<EnemyController> _waitingQueue = new Queue<EnemyController>();

    public int MaxSlots => _maxSlots;
    public int CurrentSlotCount => _slotHolders.Count;
    public bool HasAvailableSlot => _slotHolders.Count < _maxSlots;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 슬롯 획득 시도
    /// </summary>
    public bool TryAcquireSlot(EnemyController enemy)
    {
        if (enemy == null) return false;

        // 이미 슬롯 보유 중
        if (_slotHolders.Contains(enemy))
            return true;

        // 슬롯 여유 있으면 획득
        if (_slotHolders.Count < _maxSlots)
        {
            _slotHolders.Add(enemy);
            RemoveFromWaitingQueue(enemy);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 슬롯 반납 → 대기열에서 자동 승격
    /// </summary>
    public void ReleaseSlot(EnemyController enemy)
    {
        if (enemy == null) return;
        
        if (!_slotHolders.Remove(enemy)) return;

        // 대기열에서 다음 몬스터 승격
        PromoteNextFromQueue();
    }

    /// <summary>
    /// 피격 시 대기열에 추가
    /// </summary>
    public void EnqueueWaiting(EnemyController enemy)
    {
        if (enemy == null) return;
        
        // 이미 슬롯 보유 중이면 무시
        if (_slotHolders.Contains(enemy)) return;
        
        // 이미 대기열에 있으면 무시
        if (_waitingQueue.Contains(enemy)) return;

        _waitingQueue.Enqueue(enemy);
    }

    /// <summary>
    /// 슬롯 보유 여부 확인
    /// </summary>
    public bool HasSlot(EnemyController enemy)
    {
        return _slotHolders.Contains(enemy);
    }

    /// <summary>
    /// 대기열에서 다음 몬스터 승격
    /// </summary>
    private void PromoteNextFromQueue()
    {
        while (_waitingQueue.Count > 0 && _slotHolders.Count < _maxSlots)
        {
            var next = _waitingQueue.Dequeue();
            
            // 유효하지 않은 몬스터 스킵
            if (next == null || !next.gameObject.activeInHierarchy || next.IsDead)
                continue;

            _slotHolders.Add(next);
            next.OnPromotedToPlayerTarget();
            return;
        }
    }

    /// <summary>
    /// 대기열에서 제거
    /// </summary>
    private void RemoveFromWaitingQueue(EnemyController enemy)
    {
        if (!_waitingQueue.Contains(enemy)) return;

        var tempQueue = new Queue<EnemyController>();
        while (_waitingQueue.Count > 0)
        {
            var item = _waitingQueue.Dequeue();
            if (item != enemy)
            {
                tempQueue.Enqueue(item);
            }
        }
        _waitingQueue = tempQueue;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        string info = $"Slots: {_slotHolders.Count}/{_maxSlots}\nWaiting: {_waitingQueue.Count}";
        UnityEditor.Handles.Label(transform.position + Vector3.up, info);
    }
#endif
}
