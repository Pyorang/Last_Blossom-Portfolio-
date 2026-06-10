using UnityEngine;
using System.Collections.Generic;

public class DamageTextPool : SingletonBehaviour<DamageTextPool>
{
    [SerializeField] private DamageText _prefab;
    [SerializeField] private Transform _poolParent;
    [SerializeField] private int _initialPoolSize = 10;
    [SerializeField] private float _randomOffsetRange = 0.5f;
    [SerializeField] private float _yOffset = 2f;

    private Queue<DamageText> _pool = new Queue<DamageText>();
    private Camera _mainCamera;

    public Camera MainCamera => _mainCamera;

    protected override void Init()
    {
        IsDestroyOnLoad = true;
        base.Init();

        _mainCamera = Camera.main;

        for (int i = 0; i < _initialPoolSize; i++)
        {
            CreateNewDamageText();
        }
    }

    private DamageText CreateNewDamageText()
    {
        var damageText = Instantiate(_prefab, _poolParent);
        damageText.Initialize(this);
        damageText.gameObject.SetActive(false);
        _pool.Enqueue(damageText);
        return damageText;
    }

    public void Spawn(Vector3 position, int damage, bool isEnhanced = false, bool isCritical = false, bool isShielded = false)
    {
        DamageText damageText = _pool.Count > 0 ? _pool.Dequeue() : CreateNewDamageText();

        Vector3 randomOffset = new Vector3(
            Random.Range(-_randomOffsetRange, _randomOffsetRange),
            _yOffset + Random.Range(0f, _randomOffsetRange),
            Random.Range(-_randomOffsetRange, _randomOffsetRange)
        );

        damageText.transform.position = position + randomOffset;
        damageText.gameObject.SetActive(true);

        string text = isCritical ? $"{damage}!!" : damage.ToString();

        if (isShielded)
            damageText.ShowWithShield(text);
        else if (isEnhanced)
            damageText.ShowWithHighlight(text);
        else
            damageText.Show(text);
    }

    public void Return(DamageText damageText)
    {
        damageText.gameObject.SetActive(false);
        _pool.Enqueue(damageText);
    }
}