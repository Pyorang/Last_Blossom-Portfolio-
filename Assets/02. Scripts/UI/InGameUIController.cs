using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Cinemachine;
using DG.Tweening;

public class InGameUIController : MonoBehaviour
{
    #region Player Stat UI
    [Header("Player Stat UI")]
    [SerializeField] private GameObject _playerStatPanel;
    [SerializeField] private Image _profileImage;
    [SerializeField] private Sprite _normalProfileSprite;
    [SerializeField] private Sprite _awakenedProfileSprite;
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Slider _healthDamageSlider;
    [SerializeField] private float _healthDamageDuration = 0.5f;
    [SerializeField] private Slider _staminaSlider;
    [SerializeField] private TextMeshProUGUI _healthText;
    
    private Tweener _healthDamageTweener;
    #endregion

    #region Spirit Tree UI
    [Header("Spirit Tree UI")]
    [SerializeField] private GameObject _spiritTreePanel;
    [SerializeField] private Slider _spiritTreeHealthSlider;
    [SerializeField] private Slider _spiritTreeDamageSlider;
    [SerializeField] private float _spiritTreeDamageDuration = 0.5f;
    [SerializeField] private TextMeshProUGUI _spiritTreeHealthText;
    
    private Tweener _spiritTreeDamageTweener;
    [SerializeField] private SpiritTreeController _spiritTreeController;
    private readonly StringBuilder _spiritTreeHealthSB = new StringBuilder(32);
    #endregion

    #region Awakening Icon UI
    [Header("Awakening Icon UI")]
    [SerializeField] private Image _awakeningImage;
    #endregion

    #region Skill Icon UI
    [Header("Skill Icon UI")]
    [SerializeField] private GameObject _skillIconPanel;
    [SerializeField] private GameObject _evasionEnableImage;
    [SerializeField] private GameObject _eSkillEnableImage;
    [SerializeField] private GameObject _qSkillEnableImage;
    [SerializeField] private GameObject _qSkillLockImage;
    #endregion

    #region Wave Info UI
    [Header("Wave Info UI")]
    [SerializeField] private GameObject _waveInfoPanel;
    [SerializeField] private TextMeshProUGUI _waveInfoText;
    [SerializeField] private float _waveInfoSlideDuration = 0.5f;
    [SerializeField] private Ease _waveInfoEaseIn = Ease.OutBack;
    [SerializeField] private Ease _waveInfoEaseOut = Ease.InBack;
    
    [Header("Monster Count Effect")]
    [SerializeField] private float _monsterCountPunchScale = 1.2f;
    [SerializeField] private float _monsterCountPunchDuration = 0.3f;
    [SerializeField] private Color _monsterCountHighlightColor = Color.red;
    [SerializeField] private float _colorFlashDuration = 0.3f;

    private RectTransform _waveInfoRect;
    private Vector2 _waveInfoShowPos;
    private Vector2 _waveInfoHidePos;
    private Tweener _waveInfoTweener;
    private Tweener _monsterCountScaleTweener;
    private Tweener _monsterCountColorTweener;
    private Color _originalWaveInfoColor;
    #endregion

    #region Notification UI
    [Header("Notification UI")]
    [SerializeField] private GameObject _notificationPanel;
    [SerializeField] private TextMeshProUGUI _notificationText;
    [SerializeField] private float _notificationDisplayDuration = 2f;
    [SerializeField] private float _notificationFadeDuration = 1f;
    [SerializeField] private Sprite _defaultNotificationSprite;
    [SerializeField] private Sprite _waveClearNotificationSprite;

    private Coroutine _currentNotificationCoroutine;
    private Image _notificationPanelImage;
    private Color _originalPanelColor;
    private Color _originalTextColor;

    private WaitForSeconds _notificationDisplayWait;

    private readonly StringBuilder _healthSB = new StringBuilder(32);
    private readonly StringBuilder _waveInfoSB = new StringBuilder(32);

    private readonly string _notificationMessage1 = "영목이 공격받고 있습니다!";
    private readonly string _notificationMessage2 = "적들이 몰려옵니다!";
    private readonly string _notificationMessage3 = "웨이브 클리어!";
    private readonly string _notificationMessage4 = "방어 실패...";
    private readonly string _notificationMessage5 = "모든 웨이브 클리어!";

    private float _lastAttackNotificationTime = -999f;
    private const float ATTACK_NOTIFICATION_COOLDOWN = 4f;
    #endregion

    #region Perk Selection UI
    [Header("Perk Selection UI")]
    [SerializeField] private GameObject _perkPanel;
    [SerializeField] private RectTransform _perkContainer;
    [SerializeField] private GameObject _perkUIPrefab;
    [SerializeField] private Image _perkTitleBackground;
    [SerializeField] private TextMeshProUGUI _perkTitleText;
    [SerializeField] private float _perkFadeDuration = 0.3f;
    [SerializeField] private float _perkSlideDuration = 0.4f;
    [SerializeField] private SettingUI _settingUI;

    private List<PerkUI> _spawnedPerkUIs = new List<PerkUI>();
    private Coroutine _perkAnimationCoroutine;
    private Vector2 _perkContainerShowPos;
    private Vector2 _perkContainerHidePos;
    private bool _perkContainerInitialized;
    private Color _perkTitleBackgroundOriginalColor;
    private Color _perkTitleTextOriginalColor;
    private bool _perkTitleColorsInitialized;
    
    public bool IsPerkPanelActive => _perkPanel != null && _perkPanel.activeSelf;
    #endregion

    #region Game Over UI
    [Header("Game Over UI")]
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private CanvasGroup _lobbyButtonGroup;
    [SerializeField] private CanvasGroup _restartButtonGroup;
    [SerializeField] private Button _lobbyButton;
    [SerializeField] private Button _restartButton;
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private float _gameOverButtonFadeDuration = 0.5f;
    [SerializeField] private float _resultTextStampDuration = 0.6f;
    [SerializeField] private float _resultTextStartScale = 2.5f;
    [SerializeField] private float _resultTextRotationCount = 1f;

    private readonly string _victoryText = "축하합니다!";
    private readonly StringBuilder _resultSB = new StringBuilder(64);
    
    private RectTransform _resultTextRect;
    #endregion

    #region Cutscene Bar UI
    [Header("Cutscene Bar UI")]
    [SerializeField] private RectTransform _topBar;
    [SerializeField] private RectTransform _bottomBar;
    [SerializeField] private CinemachineBrain _cinemachineBrain;
    [SerializeField] private AnimationCurve _revealCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("UI Slide Animation")]
    [SerializeField] private float _uiSlideDuration = 0.5f;
    [SerializeField] private float _hpFillDuration = 0.8f;
    [SerializeField] private Ease _uiSlideEase = Ease.OutCubic;
    [SerializeField] private Ease _hpFillEase = Ease.OutQuad;

    private Vector2 _topBarStartPos;
    private Vector2 _bottomBarStartPos;
    private Vector2 _topBarEndPos;
    private Vector2 _bottomBarEndPos;
    private bool _isRevealing;
    private bool _isHPFillAnimating;

    private RectTransform _playerStatRect;
    private RectTransform _spiritTreeRect;
    private RectTransform _skillIconRect;
    private Vector2 _playerStatOriginalPos;
    private Vector2 _spiritTreeOriginalPos;
    private Vector2 _skillIconOriginalPos;

    public static event Action OnCutsceneRevealComplete;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        _notificationDisplayWait = new WaitForSeconds(_notificationDisplayDuration);
        InitializeCutsceneBars();
        InitializeWaveInfoPanel();
    }

    private void Start()
    {
        if (_notificationPanel != null)
        {
            _notificationPanelImage = _notificationPanel.GetComponent<Image>();
            if (_notificationPanelImage != null)
            {
                _originalPanelColor = _notificationPanelImage.color;
            }
        }

        if (_notificationText != null)
        {
            _originalTextColor = _notificationText.color;
        }

        if (_waveInfoText != null)
        {
            _originalWaveInfoColor = _waveInfoText.color;
        }

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStart += HandleWaveStart;
            WaveManager.Instance.OnMonsterCountChanged += UpdateWaveInfoUI;
            WaveManager.Instance.OnWaveCleared += HandleWaveCleared;
            WaveManager.Instance.OnWaveFailed += HandleWaveFailed;
            WaveManager.Instance.OnAllWavesCleared += HandleAllWavesCleared;
        }

        IntroCameraController.OnIntroComplete += RevealUI;

        // Inspector에서 주입받음
        if (_spiritTreeController != null)
        {
            _spiritTreeController.OnHealthChanged += UpdateSpiritTreeHealthUI;
            _spiritTreeController.OnSpiritTreeDamaged += HandleSpiritTreeDamaged;
            UpdateSpiritTreeHealthUI(_spiritTreeController.CurrentHP, _spiritTreeController.MaxHP, _spiritTreeController.CurrentHealthState);
        }
    }

    private void OnDestroy()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStart -= HandleWaveStart;
            WaveManager.Instance.OnMonsterCountChanged -= UpdateWaveInfoUI;
            WaveManager.Instance.OnWaveCleared -= HandleWaveCleared;
            WaveManager.Instance.OnWaveFailed -= HandleWaveFailed;
            WaveManager.Instance.OnAllWavesCleared -= HandleAllWavesCleared;
        }

        IntroCameraController.OnIntroComplete -= RevealUI;

        if (_spiritTreeController != null)
        {
            _spiritTreeController.OnHealthChanged -= UpdateSpiritTreeHealthUI;
            _spiritTreeController.OnSpiritTreeDamaged -= HandleSpiritTreeDamaged;
        }
    }
    #endregion

    #region Player Stat Methods
    public void UpdateHealthUI(float current, float max)
    {
        if (_isHPFillAnimating) return;

        float targetValue = current / max;
        
        if (_healthSlider != null)
        {
            _healthSlider.value = targetValue;
        }

        if (_healthDamageSlider != null)
        {
            if (targetValue < _healthDamageSlider.value)
            {
                _healthDamageTweener?.Kill();
                _healthDamageTweener = _healthDamageSlider
                    .DOValue(targetValue, _healthDamageDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(false);
            }
            else
            {
                _healthDamageTweener?.Kill();
                _healthDamageSlider.value = targetValue;
            }
        }

        if (_healthText != null)
        {
            int currentInt = Mathf.CeilToInt(current);
            int maxInt = Mathf.CeilToInt(max);

            _healthSB.Clear();
            _healthSB.Append(currentInt);
            _healthSB.Append(" / ");
            _healthSB.Append(maxInt);

            _healthText.SetText(_healthSB);
        }
    }

    public void UpdateStaminaUI(float current, float max)
    {
        if (_staminaSlider != null)
        {
            _staminaSlider.value = current / max;
        }
    }
    #endregion

    #region Spirit Tree Methods
    public void UpdateSpiritTreeHealthUI(float current, float max, SpiritTreeHealthState state)
    {
        if (_isHPFillAnimating) return;

        float targetValue = current / max;
        
        if (_spiritTreeHealthSlider != null)
        {
            _spiritTreeHealthSlider.value = targetValue;
        }

        if (_spiritTreeDamageSlider != null)
        {
            if (targetValue < _spiritTreeDamageSlider.value)
            {
                _spiritTreeDamageTweener?.Kill();
                _spiritTreeDamageTweener = _spiritTreeDamageSlider
                    .DOValue(targetValue, _spiritTreeDamageDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(false);
            }
            else
            {
                _spiritTreeDamageTweener?.Kill();
                _spiritTreeDamageSlider.value = targetValue;
            }
        }

        if (_spiritTreeHealthText != null)
        {
            int currentInt = Mathf.CeilToInt(current);
            int maxInt = Mathf.CeilToInt(max);

            _spiritTreeHealthSB.Clear();
            _spiritTreeHealthSB.Append(currentInt);
            _spiritTreeHealthSB.Append(" / ");
            _spiritTreeHealthSB.Append(maxInt);

            _spiritTreeHealthText.SetText(_spiritTreeHealthSB);
        }
    }

    private void HandleSpiritTreeDamaged(float damage, float currentHP, float maxHP)
    {
        if (Time.time - _lastAttackNotificationTime >= ATTACK_NOTIFICATION_COOLDOWN)
        {
            _lastAttackNotificationTime = Time.time;
            ShowMonsterAttackNotification();
        }
    }
    #endregion

    #region Ascend Methods
    public void UpdateAscendUI(float current, float max)
    {
        if (_awakeningImage != null)
        {
            _awakeningImage.fillAmount = current / max;
        }
    }
    #endregion

    #region Skill Icon Methods
    public void EvasionEnableSkill()
    {
        if (_evasionEnableImage != null)
        {
            _evasionEnableImage.SetActive(true);
        }
    }

    public void EvasionDisableSkill()
    {
        if (_evasionEnableImage != null)
        {
            _evasionEnableImage.SetActive(false);
        }
    }

    public void ESkillEnable()
    {
        if (_eSkillEnableImage != null)
        {
            _eSkillEnableImage.SetActive(true);
        }
    }

    public void ESkillDisable()
    {
        if (_eSkillEnableImage != null)
        {
            _eSkillEnableImage.SetActive(false);
        }
    }

    public void QSkillEnable()
    {
        if (_qSkillEnableImage != null)
        {
            _qSkillEnableImage.SetActive(true);
        }
    }

    public void QSkillDisable()
    {
        if (_qSkillEnableImage != null)
        {
            _qSkillEnableImage.SetActive(false);
        }
    }

    public void SetSkillIconState(SkillType skillType, bool isEnabled)
    {
        switch (skillType)
        {
            case SkillType.Evasion:
                if (_evasionEnableImage != null) _evasionEnableImage.SetActive(isEnabled);
                break;
            case SkillType.ESkill:
                if (_eSkillEnableImage != null) _eSkillEnableImage.SetActive(isEnabled);
                break;
            case SkillType.QSkill:
                if (_qSkillEnableImage != null) _qSkillEnableImage.SetActive(isEnabled);
                break;
        }
    }
    #endregion

    #region Wave Info Methods
    private void InitializeWaveInfoPanel()
    {
        if (_waveInfoPanel != null)
        {
            _waveInfoRect = _waveInfoPanel.GetComponent<RectTransform>();
            _waveInfoShowPos = _waveInfoRect.anchoredPosition;
            
            // 화면 우측 바깥으로 숨김 위치 계산
            float hideOffset = _waveInfoRect.rect.width + 100f;
            _waveInfoHidePos = _waveInfoShowPos + new Vector2(hideOffset, 0f);
            
            // 시작 시 숨김 위치로 설정
            _waveInfoRect.anchoredPosition = _waveInfoHidePos;
        }
    }

    public void ShowWaveInfoPanel()
    {
        if (_waveInfoPanel == null || _waveInfoRect == null) return;
        
        _waveInfoTweener?.Kill();
        _waveInfoPanel.SetActive(true);
        
        _waveInfoTweener = _waveInfoRect
            .DOAnchorPos(_waveInfoShowPos, _waveInfoSlideDuration)
            .SetEase(_waveInfoEaseIn)
            .SetUpdate(false);
    }

    public void HideWaveInfoPanel()
    {
        if (_waveInfoPanel == null || _waveInfoRect == null) return;
        
        _waveInfoTweener?.Kill();
        
        _waveInfoTweener = _waveInfoRect
            .DOAnchorPos(_waveInfoHidePos, _waveInfoSlideDuration)
            .SetEase(_waveInfoEaseOut)
            .SetUpdate(false)
            .OnComplete(() => _waveInfoPanel.SetActive(false));
    }

    public void UpdateWaveInfoUI(int wave, int remainingMonsters)
    {
        if (_waveInfoText != null)
        {
            _waveInfoSB.Clear();
            _waveInfoSB.Append("Wave ");
            _waveInfoSB.Append(wave);
            _waveInfoSB.Append("\n남은 적 수 : ");
            _waveInfoSB.Append(remainingMonsters);

            _waveInfoText.SetText(_waveInfoSB);
            
            PunchMonsterCount();
        }
    }

    private void PunchMonsterCount()
    {
        // 스케일 펀치 효과
        if (_waveInfoRect != null)
        {
            _monsterCountScaleTweener?.Kill();
            _waveInfoRect.localScale = Vector3.one;
            
            _monsterCountScaleTweener = _waveInfoRect
                .DOPunchScale(Vector3.one * (_monsterCountPunchScale - 1f), _monsterCountPunchDuration, 1, 0.5f)
                .SetUpdate(false);
        }
        
        // 색상 플래시 효과
        if (_waveInfoText != null)
        {
            _monsterCountColorTweener?.Kill();
            _waveInfoText.color = _monsterCountHighlightColor;
            
            _monsterCountColorTweener = _waveInfoText
                .DOColor(_originalWaveInfoColor, _colorFlashDuration)
                .SetUpdate(false);
        }
    }

    private void HandleWaveStart(int waveId)
    {
        ShowWaveInfoPanel();
    }

    private void HandleWaveCleared(int waveId)
    {
        HideWaveInfoPanel();
        AudioManager.Instance.PlaySFX("웨이브클리어", randomPitch: false);
        ShowNotificationInternal(_notificationMessage3, () => ShowPerks(waveId), _waveClearNotificationSprite);
    }

    private void HandleWaveFailed(int waveId)
    {
        HideWaveInfoPanel();
        ShowNotificationWithCallback(_notificationMessage4, () => ShowGameOverUI(false));
    }

    private void HandleAllWavesCleared()
    {
        ShowNotificationInternal(_notificationMessage5, () => ShowGameOverUI(true), _waveClearNotificationSprite);
    }
    #endregion

    #region Notification Methods
    public void ShowNotification(string message)
    {
        ShowNotificationInternal(message, null, null);
    }

    public void ShowNotificationWithCallback(string message, Action onComplete)
    {
        ShowNotificationInternal(message, onComplete, null);
    }

    private void ShowNotificationInternal(string message, Action onComplete, Sprite overrideSprite)
    {
        if (_currentNotificationCoroutine != null)
        {
            StopCoroutine(_currentNotificationCoroutine);
        }

        _currentNotificationCoroutine = StartCoroutine(DisplayNotificationCoroutine(message, onComplete, overrideSprite));
    }

    public void HideNotification()
    {
        if (_currentNotificationCoroutine != null)
        {
            StopCoroutine(_currentNotificationCoroutine);
        }

        if (_notificationPanel != null)
        {
            _notificationPanel.SetActive(false);
        }
    }

    private IEnumerator DisplayNotificationCoroutine(string message, Action onComplete, Sprite overrideSprite)
    {
        if (_notificationPanelImage != null)
        {
            _notificationPanelImage.color = _originalPanelColor;
            _notificationPanelImage.sprite = overrideSprite != null ? overrideSprite : _defaultNotificationSprite;
        }
        if (_notificationText != null)
        {
            _notificationText.color = _originalTextColor;
            _notificationText.text = message;
        }

        if (_notificationPanel != null)
        {
            _notificationPanel.SetActive(true);
        }

        yield return _notificationDisplayWait;

        float elapsed = 0f;
        while (elapsed < _notificationFadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / _notificationFadeDuration);

            if (_notificationPanelImage != null)
            {
                var color = _originalPanelColor;
                color.a = _originalPanelColor.a * alpha;
                _notificationPanelImage.color = color;
            }
            if (_notificationText != null)
            {
                var color = _originalTextColor;
                color.a = _originalTextColor.a * alpha;
                _notificationText.color = color;
            }

            yield return null;
        }

        if (_notificationPanel != null)
        {
            _notificationPanel.SetActive(false);
        }

        if (_notificationPanelImage != null && _defaultNotificationSprite != null)
        {
            _notificationPanelImage.sprite = _defaultNotificationSprite;
        }

        _currentNotificationCoroutine = null;
        
        onComplete?.Invoke();
    }

    public void ShowMonsterAttackNotification() => ShowNotification(_notificationMessage1);
    public void ShowNextWaveNotification() => ShowNotification(_notificationMessage2);
    public void ShowWaveCompleteNotification() => ShowNotification(_notificationMessage3);
    public void ShowWaveFailedNotification() => ShowNotification(_notificationMessage4);
    public void ShowAllWavesClearNotification() => ShowNotification(_notificationMessage5);
    #endregion

    #region Perk Selection Methods
    public void ShowPerks(int waveId)
    {
        var perkIds = DataTableManager.Instance.GetPerkIdsByWave(waveId);
        if (perkIds == null || perkIds.Count == 0)
        {
            ShowNotificationWithCallback(_notificationMessage2, () => WaveManager.Instance?.StartNextWave());
            return;
        }

        GameStateManager.Instance.PauseGame();
        ClearPerkUIs();
        
        if (_perkAnimationCoroutine != null)
            StopCoroutine(_perkAnimationCoroutine);

        // 컨테이너 위치 초기화 (최초 1회)
        if (!_perkContainerInitialized && _perkContainer != null)
        {
            _perkContainerShowPos = _perkContainer.anchoredPosition;
            float screenWidth = Screen.width;
            _perkContainerHidePos = _perkContainerShowPos + new Vector2(screenWidth, 0f);
            _perkContainerInitialized = true;
        }

        // 타이틀 원본 색상 저장 (최초 1회)
        if (!_perkTitleColorsInitialized)
        {
            if (_perkTitleBackground != null)
                _perkTitleBackgroundOriginalColor = _perkTitleBackground.color;
            if (_perkTitleText != null)
                _perkTitleTextOriginalColor = _perkTitleText.color;
            _perkTitleColorsInitialized = true;
        }

        // 타이틀 배경/텍스트 초기화 (투명)
        if (_perkTitleBackground != null)
        {
            Color c = _perkTitleBackgroundOriginalColor;
            c.a = 0f;
            _perkTitleBackground.color = c;
        }
        if (_perkTitleText != null)
        {
            Color c = _perkTitleTextOriginalColor;
            c.a = 0f;
            _perkTitleText.color = c;
        }
        
        // 컨테이너를 화면 밖(우측)에 배치
        if (_perkContainer != null)
            _perkContainer.anchoredPosition = _perkContainerHidePos;

        // 퍽 UI 생성
        foreach (var perkId in perkIds)
        {
            IPerk perk = PerkFactory.Create(perkId);
            if (perk != null)
            {
                GameObject perkUIObj = Instantiate(_perkUIPrefab, _perkContainer);
                PerkUI perkUI = perkUIObj.GetComponent<PerkUI>();
                perkUI.Setup(perk, this);
                _spawnedPerkUIs.Add(perkUI);
            }
        }

        if (_perkPanel != null)
            _perkPanel.SetActive(true);

        UISoundPlayer.PlaySound(UISoundType.PerkAppear);
        _perkAnimationCoroutine = StartCoroutine(PerkShowCoroutine());
    }

    private IEnumerator PerkShowCoroutine()
    {
        // 1. 타이틀 페이드인 (unscaled time - Paused 상태에서 실행)
        float elapsed = 0f;
        while (elapsed < _perkFadeDuration)
        {
            if (_settingUI != null && _settingUI.IsSettingUIActive)
            {
                yield return null;
                continue;
            }
            
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / _perkFadeDuration);
            
            if (_perkTitleBackground != null)
            {
                Color c = _perkTitleBackgroundOriginalColor;
                c.a = _perkTitleBackgroundOriginalColor.a * t;
                _perkTitleBackground.color = c;
            }
            if (_perkTitleText != null)
            {
                Color c = _perkTitleTextOriginalColor;
                c.a = _perkTitleTextOriginalColor.a * t;
                _perkTitleText.color = c;
            }
            
            yield return null;
        }
        
        // 2. 컨테이너 슬라이드 인 (unscaled time)
        elapsed = 0f;
        Vector2 startPos = _perkContainerHidePos;
        while (elapsed < _perkSlideDuration)
        {
            if (_settingUI != null && _settingUI.IsSettingUIActive)
            {
                yield return null;
                continue;
            }
            
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / _perkSlideDuration);
            float easedT = EaseOutBack(t);
            
            if (_perkContainer != null)
                _perkContainer.anchoredPosition = Vector2.LerpUnclamped(startPos, _perkContainerShowPos, easedT);
            
            yield return null;
        }
        
        if (_perkContainer != null)
            _perkContainer.anchoredPosition = _perkContainerShowPos;
        
        _perkAnimationCoroutine = null;
    }

    public void OnPerkSelected()
    {
        if (_perkAnimationCoroutine != null)
            StopCoroutine(_perkAnimationCoroutine);
        
        if(_settingUI != null && !_settingUI.IsSettingUIActive)
        {
            GameStateManager.Instance.ResumeGame();
        }

        UISoundPlayer.PlaySound(UISoundType.PerkDisappear);
        _perkAnimationCoroutine = StartCoroutine(PerkHideCoroutine());
    }

    private IEnumerator PerkHideCoroutine()
    {
        float screenWidth = Screen.width;
        Vector2 leftHidePos = _perkContainerShowPos - new Vector2(screenWidth, 0f);
        
        // 1. 컨테이너 슬라이드 아웃
        float elapsed = 0f;
        Vector2 startPos = _perkContainer != null ? _perkContainer.anchoredPosition : _perkContainerShowPos;
        while (elapsed < _perkSlideDuration)
        {
            if (_settingUI != null && _settingUI.IsSettingUIActive)
            {
                yield return null;
                continue;
            }
            
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / _perkSlideDuration);
            float easedT = EaseInBack(t);
            
            if (_perkContainer != null)
                _perkContainer.anchoredPosition = Vector2.LerpUnclamped(startPos, leftHidePos, easedT);
            
            yield return null;
        }
        
        // 2. 타이틀 페이드아웃
        elapsed = 0f;
        while (elapsed < _perkFadeDuration)
        {
            if (_settingUI != null && _settingUI.IsSettingUIActive)
            {
                yield return null;
                continue;
            }
            
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / _perkFadeDuration);
            
            if (_perkTitleBackground != null)
            {
                Color c = _perkTitleBackgroundOriginalColor;
                c.a = _perkTitleBackgroundOriginalColor.a * (1f - t);
                _perkTitleBackground.color = c;
            }
            if (_perkTitleText != null)
            {
                Color c = _perkTitleTextOriginalColor;
                c.a = _perkTitleTextOriginalColor.a * (1f - t);
                _perkTitleText.color = c;
            }
            
            yield return null;
        }
        
        // 완료 처리
        if (_perkPanel != null)
            _perkPanel.SetActive(false);

        GameStateManager.Instance.ResumeGame();

        ClearPerkUIs();
        _perkAnimationCoroutine = null;
        
        AudioManager.Instance.PlaySFX("웨이브시작시", randomPitch: false);
        ShowNotificationWithCallback(_notificationMessage2, () => WaveManager.Instance?.StartNextWave());
    }

    private float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private float EaseInBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return c3 * t * t * t - c1 * t * t;
    }

    private void ClearPerkUIs()
    {
        foreach (var perkUI in _spawnedPerkUIs)
        {
            if (perkUI != null)
                Destroy(perkUI.gameObject);
        }
        _spawnedPerkUIs.Clear();
    }
    #endregion

    #region Cutscene Bar Methods
    private void InitializeCutsceneBars()
    {
        if (_topBar != null)
        {
            _topBarStartPos = _topBar.anchoredPosition;
            _topBarEndPos = _topBarStartPos + new Vector2(0f, _topBar.rect.height + 100f);
        }

        if (_bottomBar != null)
        {
            _bottomBarStartPos = _bottomBar.anchoredPosition;
            _bottomBarEndPos = _bottomBarStartPos - new Vector2(0f, _bottomBar.rect.height + 100f);
        }

        InitializeUIPanelPositions();
        HideGameUI(true);
    }

    private void InitializeUIPanelPositions()
    {
        if (_playerStatPanel != null)
        {
            _playerStatRect = _playerStatPanel.GetComponent<RectTransform>();
            _playerStatOriginalPos = _playerStatRect.anchoredPosition;
        }

        if (_spiritTreePanel != null)
        {
            _spiritTreeRect = _spiritTreePanel.GetComponent<RectTransform>();
            _spiritTreeOriginalPos = _spiritTreeRect.anchoredPosition;
        }

        if (_skillIconPanel != null)
        {
            _skillIconRect = _skillIconPanel.GetComponent<RectTransform>();
            _skillIconOriginalPos = _skillIconRect.anchoredPosition;
        }
    }

    public void HideGameUI(bool forIntro = false)
    {
        if (_playerStatPanel != null) _playerStatPanel.SetActive(false);
        if (_spiritTreePanel != null) _spiritTreePanel.SetActive(false);
        if (_skillIconPanel != null) _skillIconPanel.SetActive(false);
        if (_waveInfoPanel != null) _waveInfoPanel.SetActive(false);
        if (_notificationPanel != null) _notificationPanel.SetActive(false);
        if (_perkPanel != null) _perkPanel.SetActive(false);

        if (forIntro)
        {
            _isHPFillAnimating = true;

            if (_playerStatRect != null)
            {
                float hideOffset = _playerStatRect.rect.width + 100f;
                _playerStatRect.anchoredPosition = _playerStatOriginalPos - new Vector2(hideOffset, 0f);
            }

            if (_spiritTreeRect != null)
            {
                float hideOffset = _spiritTreeRect.rect.width + 100f;
                _spiritTreeRect.anchoredPosition = _spiritTreeOriginalPos + new Vector2(hideOffset, 0f);
            }

            if (_skillIconRect != null)
            {
                float hideOffset = _skillIconRect.rect.width + 100f;
                _skillIconRect.anchoredPosition = _skillIconOriginalPos + new Vector2(hideOffset, 0f);
            }

            if (_healthSlider != null) _healthSlider.value = 0f;
            if (_healthDamageSlider != null) _healthDamageSlider.value = 0f;
            if (_spiritTreeHealthSlider != null) _spiritTreeHealthSlider.value = 0f;
            if (_spiritTreeDamageSlider != null) _spiritTreeDamageSlider.value = 0f;
        }
    }

    public void ShowGameUI(bool withIntroAnimation = false)
    {
        if (_playerStatPanel != null) _playerStatPanel.SetActive(true);
        if (_spiritTreePanel != null) _spiritTreePanel.SetActive(true);
        if (_skillIconPanel != null) _skillIconPanel.SetActive(true);
        
        if (_waveInfoPanel != null && WaveManager.Instance != null && WaveManager.Instance.IsWaveActive)
            _waveInfoPanel.SetActive(true);

        if (withIntroAnimation)
        {
            PlayUISlideAnimation();
        }
        else
        {
            _isHPFillAnimating = false;

            if (_playerStatRect != null)
                _playerStatRect.anchoredPosition = _playerStatOriginalPos;
            if (_spiritTreeRect != null)
                _spiritTreeRect.anchoredPosition = _spiritTreeOriginalPos;
            if (_skillIconRect != null)
                _skillIconRect.anchoredPosition = _skillIconOriginalPos;
        }
    }

    private void PlayUISlideAnimation()
    {
        StartCoroutine(UISlideAnimationCoroutine());
    }

    private IEnumerator UISlideAnimationCoroutine()
    {
        if (_playerStatRect != null)
        {
            _playerStatRect
                .DOAnchorPos(_playerStatOriginalPos, _uiSlideDuration)
                .SetEase(_uiSlideEase);
        }

        if (_spiritTreeRect != null)
        {
            _spiritTreeRect
                .DOAnchorPos(_spiritTreeOriginalPos, _uiSlideDuration)
                .SetEase(_uiSlideEase);
        }

        if (_skillIconRect != null)
        {
            _skillIconRect
                .DOAnchorPos(_skillIconOriginalPos, _uiSlideDuration)
                .SetEase(_uiSlideEase);
        }

        yield return new WaitForSeconds(_uiSlideDuration);

        StartCoroutine(HPFillAnimationCoroutine());
    }

    private IEnumerator HPFillAnimationCoroutine()
    {
        _healthDamageTweener?.Kill();
        _spiritTreeDamageTweener?.Kill();

        if (_healthSlider != null)
        {
            _healthSlider.DOValue(1f, _hpFillDuration).SetEase(_hpFillEase);
        }

        if (_healthDamageSlider != null)
        {
            _healthDamageSlider.DOValue(1f, _hpFillDuration).SetEase(_hpFillEase);
        }

        if (_spiritTreeHealthSlider != null)
        {
            _spiritTreeHealthSlider.DOValue(1f, _hpFillDuration).SetEase(_hpFillEase);
        }

        if (_spiritTreeDamageSlider != null)
        {
            _spiritTreeDamageSlider.DOValue(1f, _hpFillDuration).SetEase(_hpFillEase);
        }

        yield return new WaitForSeconds(_hpFillDuration);

        _isHPFillAnimating = false;
    }

    public void RevealUI()
    {
        if (_isRevealing) return;
        StartCoroutine(RevealCoroutine());
    }

    private IEnumerator RevealCoroutine()
    {
        _isRevealing = true;

        float revealDuration = _cinemachineBrain != null 
            ? _cinemachineBrain.DefaultBlend.Time 
            : 1f;

        float elapsed = 0f;

        while (elapsed < revealDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / revealDuration;
            float curveValue = _revealCurve.Evaluate(progress);

            if (_topBar != null)
            {
                _topBar.anchoredPosition = Vector2.Lerp(_topBarStartPos, _topBarEndPos, curveValue);
            }

            if (_bottomBar != null)
            {
                _bottomBar.anchoredPosition = Vector2.Lerp(_bottomBarStartPos, _bottomBarEndPos, curveValue);
            }

            yield return null;
        }

        if (_topBar != null) _topBar.anchoredPosition = _topBarEndPos;
        if (_bottomBar != null) _bottomBar.anchoredPosition = _bottomBarEndPos;

        if (_topBar != null) _topBar.gameObject.SetActive(false);
        if (_bottomBar != null) _bottomBar.gameObject.SetActive(false);

        ShowGameUI(true);

        _isRevealing = false;
        OnCutsceneRevealComplete?.Invoke();
        
        AudioManager.Instance.PlaySFX("웨이브시작시", randomPitch: false);
        ShowNotificationWithCallback(_notificationMessage2, () => WaveManager.Instance?.StartWave(1));
    }

    public void SkipCutscene()
    {
        StopAllCoroutines();

        if (_topBar != null) _topBar.gameObject.SetActive(false);
        if (_bottomBar != null) _bottomBar.gameObject.SetActive(false);

        ShowGameUI(true);

        _isRevealing = false;
        OnCutsceneRevealComplete?.Invoke();
        
        AudioManager.Instance.PlaySFX("웨이브시작시", randomPitch: false);
        ShowNotificationWithCallback(_notificationMessage2, () => WaveManager.Instance?.StartWave(1));
    }
    #endregion

    #region Perk UI Methods

    public void UnlockUltimateUI()
    {
        if (_qSkillLockImage != null)
            _qSkillLockImage.SetActive(false);
    }

    #endregion

    #region Game Over Methods
    public void ShowGameOverUI(bool isVictory)
    {
        if (_gameOverPanel == null) return;

        // 알림 끝난 후 TimeScale 정지
        TimeScaleManager.Instance?.Pause();

        AudioManager.Instance.PlaySFX("효과음_게임오버시", randomPitch: false);

        // 초기 상태 설정
        InitializeGameOverUI(isVictory);
        
        _gameOverPanel.SetActive(true);

        // 버튼 페이드인 (동시에)
        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        
        sequence.Append(_lobbyButtonGroup.DOFade(1f, _gameOverButtonFadeDuration));
        sequence.Join(_restartButtonGroup.DOFade(1f, _gameOverButtonFadeDuration));
        
        // 버튼 페이드인 완료 후 interactable 활성화
        sequence.AppendCallback(() =>
        {
            if (_lobbyButton != null) _lobbyButton.interactable = true;
            if (_restartButton != null) _restartButton.interactable = true;
        });
        
        // 결과 텍스트 스탬프 애니메이션 (스케일 + X축 회전)
        sequence.Append(_resultTextRect
            .DOScale(1f, _resultTextStampDuration)
            .SetEase(Ease.OutBack));
        sequence.Join(_resultTextRect
            .DORotate(Vector3.zero, _resultTextStampDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.OutBack));
        sequence.Join(_resultText.DOFade(1f, _resultTextStampDuration * 0.4f));
    }

    private void InitializeGameOverUI(bool isVictory)
    {
        // 버튼 CanvasGroup 초기화
        if (_lobbyButtonGroup != null)
        {
            _lobbyButtonGroup.alpha = 0f;
        }
        if (_restartButtonGroup != null)
        {
            _restartButtonGroup.alpha = 0f;
        }
        
        // 버튼 interactable 비활성화
        if (_lobbyButton != null) _lobbyButton.interactable = false;
        if (_restartButton != null) _restartButton.interactable = false;
        
        // 결과 텍스트 초기화
        if (_resultText != null)
        {
            int currentWave = WaveManager.Instance != null ? WaveManager.Instance.CurrentWaveId : 0;
            int maxWave = WaveManager.Instance != null ? WaveManager.Instance.MaxWave : 20;
            
            _resultSB.Clear();
            if (isVictory)
            {
                _resultSB.Append(_victoryText);
                _resultSB.Append("\n");
            }
            _resultSB.Append(currentWave);
            _resultSB.Append("/");
            _resultSB.Append(maxWave);
            
            _resultText.text = _resultSB.ToString();
            Color c = _resultText.color;
            c.a = 0f;
            _resultText.color = c;
            
            if (_resultTextRect == null)
            {
                _resultTextRect = _resultText.GetComponent<RectTransform>();
            }
            _resultTextRect.localScale = Vector3.one * _resultTextStartScale;
            _resultTextRect.localRotation = Quaternion.Euler(_resultTextRotationCount * 360f, 0f, 0f);
        }
    }

    public void OnLobbyButtonClicked()
    {
        SceneLoader.Instance?.LoadScene(ESceneType.Lobby);
    }

    public void OnRestartButtonClicked()
    {
        SceneLoader.Instance?.ReloadScene();
    }
    #endregion

    #region Profile Methods

    public void SetAwakenedProfile()
    {
        if (_profileImage != null && _awakenedProfileSprite != null)
            _profileImage.sprite = _awakenedProfileSprite;
    }

    public void SetNormalProfile()
    {
        if (_profileImage != null && _normalProfileSprite != null)
            _profileImage.sprite = _normalProfileSprite;
    }

    #endregion
}

public enum SkillType
{
    Evasion,
    ESkill,
    QSkill
}
