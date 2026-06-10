using UnityEngine;

public class DebugCheatManager : SingletonBehaviour<DebugCheatManager>
{
    private const KeyCode CheatKey_UnlockUltimate = KeyCode.F1;
    private const KeyCode CheatKey_HealPlayer = KeyCode.F2;
    private const KeyCode CheatKey_HealSpiritTree = KeyCode.F3;
    private const KeyCode CheatKey_RestoreEnergy = KeyCode.F4;
    private const KeyCode CheatKey_FillAwakening = KeyCode.F5;
    private const KeyCode CheatKey_AllCheat = KeyCode.F6;
    private const KeyCode CheatKey_PlayerDead = KeyCode.F7;

    private PlayerAttack _playerAttack;
    private HealthComponent _playerHealth;
    private EnergyComponent _playerEnergy;
    private AwakeningComponent _playerAwakening;
    private SpiritTreeController _spiritTree;

    private bool _isInitialized;

#if UNITY_EDITOR || DEVELOPMENT_BUILD

    protected override void Init()
    {
        base.Init();
        IsDestroyOnLoad = true;
    }

    private void Update()
    {
        if (!_isInitialized)
        {
            TryInitializeReferences();
        }

        if (!_isInitialized) return;

        ProcessCheatInputs();
    }

    private void TryInitializeReferences()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        _playerAttack = player.GetComponent<PlayerAttack>();
        _playerHealth = player.GetComponent<HealthComponent>();
        _playerEnergy = player.GetComponent<EnergyComponent>();
        _playerAwakening = player.GetComponent<AwakeningComponent>();
        _spiritTree = FindAnyObjectByType<SpiritTreeController>();

        _isInitialized = _playerAttack != null
                      && _playerHealth != null
                      && _playerEnergy != null
                      && _playerAwakening != null
                      && _spiritTree != null;
    }

    private void ProcessCheatInputs()
    {
        if (Input.GetKeyDown(CheatKey_UnlockUltimate))
        {
            UnlockUltimate();
        }

        if (Input.GetKeyDown(CheatKey_HealPlayer))
        {
            HealPlayer();
        }

        if (Input.GetKeyDown(CheatKey_HealSpiritTree))
        {
            HealSpiritTree();
        }

        if (Input.GetKeyDown(CheatKey_RestoreEnergy))
        {
            RestoreEnergy();
        }

        if (Input.GetKeyDown(CheatKey_FillAwakening))
        {
            FillAwakening();
        }

        if (Input.GetKeyDown(CheatKey_AllCheat))
        {
            ApplyAllCheats();
        }

        if (Input.GetKeyDown(CheatKey_PlayerDead))
        {
            PlayerDead();
        }
    }

    private void UnlockUltimate()
    {
        if (_playerAttack.IsUltimateLocked)
        {
            _playerAttack.UnlockUltimate();
            Debug.Log("[DebugCheat] 궁극기 해금 완료");
        }
        else
        {
            Debug.Log("[DebugCheat] 궁극기가 이미 해금되어 있습니다");
        }
    }

    private void HealPlayer()
    {
        float healAmount = _playerHealth.MaxHP - _playerHealth.CurrentHP;
        if (healAmount > 0)
        {
            _playerHealth.Heal(healAmount);
            Debug.Log($"[DebugCheat] 플레이어 HP 회복 완료 ({_playerHealth.CurrentHP}/{_playerHealth.MaxHP})");
        }
        else
        {
            Debug.Log("[DebugCheat] 플레이어 HP가 이미 최대입니다");
        }
    }

    private void HealSpiritTree()
    {
        float previousHP = _spiritTree.CurrentHP;
        _spiritTree.RestoreFullHealth();
        float healed = _spiritTree.CurrentHP - previousHP;
        
        if (healed > 0)
        {
            Debug.Log($"[DebugCheat] 영목 HP 회복 완료 ({_spiritTree.CurrentHP}/{_spiritTree.MaxHP})");
        }
        else
        {
            Debug.Log("[DebugCheat] 영목 HP가 이미 최대입니다");
        }
    }

    private void RestoreEnergy()
    {
        float restoreAmount = _playerEnergy.MaxEnergy - _playerEnergy.CurrentEnergy;
        if (restoreAmount > 0)
        {
            _playerEnergy.RestoreEnergy(restoreAmount);
            Debug.Log($"[DebugCheat] 스태미나 회복 완료 ({_playerEnergy.CurrentEnergy}/{_playerEnergy.MaxEnergy})");
        }
        else
        {
            Debug.Log("[DebugCheat] 스태미나가 이미 최대입니다");
        }
    }

    private void FillAwakening()
    {
        if (_playerAwakening.IsAwakened)
        {
            Debug.Log("[DebugCheat] 각성 상태 중에는 게이지를 채울 수 없습니다");
            return;
        }

        float fillAmount = _playerAwakening.MaxAwakening - _playerAwakening.CurrentAwakening;
        if (fillAmount > 0)
        {
            _playerAwakening.AddAwakening(fillAmount);
            Debug.Log($"[DebugCheat] 각성 게이지 충전 완료 ({_playerAwakening.CurrentAwakening}/{_playerAwakening.MaxAwakening})");
        }
        else
        {
            Debug.Log("[DebugCheat] 각성 게이지가 이미 최대입니다");
        }
    }

    private void PlayerDead()
    {
        if (_playerHealth.CurrentHP > 0)
        {
            _playerHealth.TakeDamage(_playerHealth.CurrentHP);
            Debug.Log("[DebugCheat] 플레이어 즉사 완료");
        }
        else
        {
            Debug.Log("[DebugCheat] 플레이어가 이미 죽어 있습니다");
        }
    }

    private void ApplyAllCheats()
    {
        UnlockUltimate();
        HealPlayer();
        HealSpiritTree();
        RestoreEnergy();
        FillAwakening();
        Debug.Log("[DebugCheat] ===== 모든 치트 적용 완료 =====");
    }

#endif
}
