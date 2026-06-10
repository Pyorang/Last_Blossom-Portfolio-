using UnityEngine;
using System;
using System.Collections.Generic;

public class SkillPool : SingletonBehaviour<SkillPool>
{
    [SerializeField] private GameObject _skillVFXPrefab;
    [SerializeField] private Transform _poolParent;
    [SerializeField] private int _initialPoolSize = 5;

    private Queue<SkillVFX> _pool = new Queue<SkillVFX>();

    protected override void Init()
    {
        IsDestroyOnLoad = true;
        base.Init();

        InitializePool();
    }

    private void InitializePool()
    {
        if (_skillVFXPrefab == null) return;

        for (int i = 0; i < _initialPoolSize; i++)
        {
            CreateNewSkillVFX();
        }
    }

    private SkillVFX CreateNewSkillVFX()
    {
        GameObject vfx = Instantiate(_skillVFXPrefab, _poolParent);
        SkillVFX skillVFX = vfx.GetComponent<SkillVFX>();
        vfx.SetActive(false);
        _pool.Enqueue(skillVFX);
        return skillVFX;
    }

    public SkillVFX Spawn(Vector3 position, Quaternion rotation, Vector3 direction, float attackPower, float skillCoefficient, float critRate, float critDamage, float justDamageBonus, bool isAwakened, Action onHitCallback = null, bool hasSuction = false, Action onDamageCallback = null, bool hasPiercing = false, Action<Vector3, bool> onHitVFXCallback = null)
    {
        SkillVFX skillVFX = _pool.Count > 0 ? _pool.Dequeue() : CreateNewSkillVFX();

        skillVFX.transform.position = position;
        skillVFX.transform.rotation = rotation;
        skillVFX.gameObject.SetActive(true);
        skillVFX.Activate(direction, attackPower, skillCoefficient, critRate, critDamage, justDamageBonus, isAwakened, onHitCallback, hasSuction, onDamageCallback, hasPiercing, onHitVFXCallback);

        return skillVFX;
    }

    public void Return(SkillVFX skillVFX)
    {
        skillVFX.gameObject.SetActive(false);
        _pool.Enqueue(skillVFX);
    }
}
