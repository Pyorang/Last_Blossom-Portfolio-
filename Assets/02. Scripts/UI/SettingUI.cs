using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingUI : MonoBehaviour
{
    [Header("Setting UI Panel")]
    [SerializeField] private GameObject _settingUIPanel;
    [SerializeField] private GameObject _soundSettingPanel;
    [SerializeField] private GameObject _perksInfoPanel;

    [Header("Tab Toggles")]
    [SerializeField] private Toggle _soundTabToggle;

    [Header("Volume Sliders")]
    [SerializeField] private Slider _masterVolumeSlider;
    [SerializeField] private Slider _bgmVolumeSlider;
    [SerializeField] private Slider _sfxVolumeSlider;

    [Header("Volume Texts")]
    [SerializeField] private TMP_Text _masterVolumeText;
    [SerializeField] private TMP_Text _bgmVolumeText;
    [SerializeField] private TMP_Text _sfxVolumeText;

    [Header("Perk Info")]
    [SerializeField] private GameObject _perkInfoPrefab;
    [SerializeField] private Transform _perkInfoContent;
    [SerializeField] private ScrollRect _perksScrollRect;

    private UserSettingsData _userSettingsData;
    private HashSet<string> _createdPerkIds = new HashSet<string>();

    public bool IsSettingUIActive => _settingUIPanel != null && _settingUIPanel.activeSelf;

    private void Start()
    {
        InitializeSettings();
    }

    private void OnEnable()
    {
        PlayerInputHandler.OnSettingsToggle += ToggleSettingUI;
    }

    private void OnDisable()
    {
        PlayerInputHandler.OnSettingsToggle -= ToggleSettingUI;
    }

    public void ToggleSettingUI()
    {
        if (GameStateManager.Instance == null) return;
        if (GameStateManager.Instance.IsReady) return;
        if (GameStateManager.Instance.IsGameOver) return;

        if (_settingUIPanel.activeSelf)
        {
            HideSettingUIPanel();
        }
        else
        {
            ShowSettingUIPanel();
        }
    }

    private void InitializeSettings()
    {
        _userSettingsData = UserDataManager.Instance?.GetUserData<UserSettingsData>();
        if (_userSettingsData == null) return;

        _masterVolumeSlider.value = _userSettingsData.MasterValue;
        _bgmVolumeSlider.value = _userSettingsData.BGMvalue;
        _sfxVolumeSlider.value = _userSettingsData.SFXvalue;

        UpdateVolumeText(_masterVolumeText, _masterVolumeSlider.value);
        UpdateVolumeText(_bgmVolumeText, _bgmVolumeSlider.value);
        UpdateVolumeText(_sfxVolumeText, _sfxVolumeSlider.value);
    }

    #region Panel Control

    public void ShowSettingUIPanel()
    {
        _settingUIPanel.SetActive(true);
        GameStateManager.Instance?.PauseGame();

        if (_soundTabToggle != null)
        {
            _soundTabToggle.SetIsOnWithoutNotify(true);
        }

        ShowSoundSettingPanel();
    }

    public void HideSettingUIPanel()
    {
        _settingUIPanel.SetActive(false);
        Save();
        
        var inGameUI = FindObjectOfType<InGameUIController>();
        if (inGameUI == null || !inGameUI.IsPerkPanelActive)
        {
            GameStateManager.Instance?.ResumeGame();
        }
    }

    public void ShowSoundSettingPanel()
    {
        _perksInfoPanel.SetActive(false);
        _soundSettingPanel.SetActive(true);
        
        if (_soundTabToggle != null)
        {
            _soundTabToggle.SetIsOnWithoutNotify(true);
        }
    }

    public void ShowPerksInfoPanel()
    {
        _soundSettingPanel.SetActive(false);
        _perksInfoPanel.SetActive(true);
        UpdatePerkInfoItems();
        ResetPerksScroll();
    }

    private void ResetPerksScroll()
    {
        if (_perksScrollRect == null) return;
        
        Canvas.ForceUpdateCanvases();
        _perksScrollRect.normalizedPosition = new Vector2(0, 1);
    }

    #endregion

    #region Volume Control

    public void OnMasterVolumeChanged()
    {
        AudioListener.volume = _masterVolumeSlider.value;
        _userSettingsData.MasterValue = _masterVolumeSlider.value;
        UpdateVolumeText(_masterVolumeText, _masterVolumeSlider.value);
    }

    public void OnBGMVolumeChanged()
    {
        AudioManager.Instance?.SetVolume(AudioType.BGM, _bgmVolumeSlider.value);
        _userSettingsData.BGMvalue = _bgmVolumeSlider.value;
        UpdateVolumeText(_bgmVolumeText, _bgmVolumeSlider.value);
    }

    public void OnSFXVolumeChanged()
    {
        AudioManager.Instance?.SetVolume(AudioType.SFX, _sfxVolumeSlider.value);
        _userSettingsData.SFXvalue = _sfxVolumeSlider.value;
        UpdateVolumeText(_sfxVolumeText, _sfxVolumeSlider.value);
    }

    private void UpdateVolumeText(TMP_Text text, float value)
    {
        text.text = Mathf.RoundToInt(value * 100).ToString();
    }

    #endregion

    #region Save

    private void Save()
    {
        UserDataManager.Instance?.SaveUserData();
    }

    #endregion

    #region Buttons

    public void OnClickRestartButton()
    {
        Save();
        SceneLoader.Instance?.ReloadScene();
    }

    public void OnClickLobbyButton()
    {
        Save();
        SceneLoader.Instance?.LoadScene(ESceneType.Lobby);
    }

    #endregion

    #region Perk Info

    private void UpdatePerkInfoItems()
    {
        var perkHandler = FindObjectOfType<PlayerPerkHandler>();
        if (perkHandler == null) return;
        if (_perkInfoPrefab == null || _perkInfoContent == null) return;

        foreach (var perkId in perkHandler.OwnedPerkIds)
        {
            if (_createdPerkIds.Contains(perkId)) continue;

            if (DataTableManager.Instance.TryGetPerkData(perkId, out var data))
            {
                GameObject item = Instantiate(_perkInfoPrefab, _perkInfoContent);
                var perkInfoUI = item.GetComponent<PerkInfoUI>();
                if (perkInfoUI != null)
                {
                    perkInfoUI.Setup(data);
                    _createdPerkIds.Add(perkId);
                }
            }
        }
    }

    #endregion
}
