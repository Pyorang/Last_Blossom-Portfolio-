using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum UISoundType
{
    Click,
    Back,
    Hover,
    Restart,
    PerkAppear,
    PerkSelect,
    PerkDisappear,
    GameStart
}

[RequireComponent(typeof(Selectable))]
public class UISoundPlayer : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler
{
    private static class SoundNames
    {
        public const string Click = "UI_클릭";
        public const string Back = "UI_뒤로가기클릭";
        public const string Hover = "UI_마우스가위에올라감";
        public const string Restart = "UI_재시작시";
        public const string PerkAppear = "UI_특전선택_나타나기";
        public const string PerkSelect = "UI_특전선택시";
        public const string PerkDisappear = "UI_특전선택이후_사라지기";
        public const string GameStart = "UI_게임시작버튼";
    }

    private static readonly string[] s_preloadSounds =
    {
        SoundNames.Click,
        SoundNames.Back,
        SoundNames.Hover,
        SoundNames.GameStart
    };

    [SerializeField] private UISoundType _clickSound = UISoundType.Click;
    [SerializeField] private bool _playHoverSound = true;

    private Selectable _selectable;
    private static bool s_isPreloaded;

    private void Awake()
    {
        _selectable = GetComponent<Selectable>();
        PreloadSounds();
    }

    private static void PreloadSounds()
    {
        if (s_isPreloaded)
        {
            return;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PreloadClips(s_preloadSounds);
            s_isPreloaded = true;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsInteractable())
        {
            return;
        }

        PlaySound(_clickSound);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_playHoverSound || !IsInteractable())
        {
            return;
        }

        PlaySound(UISoundType.Hover);
    }

    private bool IsInteractable()
    {
        return _selectable != null && _selectable.interactable;
    }

    public static void PlaySound(UISoundType soundType)
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

        string fileName = GetSoundFileName(soundType);
        AudioManager.Instance.PlaySFX(fileName, randomPitch: false);
    }

    private static string GetSoundFileName(UISoundType soundType)
    {
        return soundType switch
        {
            UISoundType.Click => SoundNames.Click,
            UISoundType.Back => SoundNames.Back,
            UISoundType.Hover => SoundNames.Hover,
            UISoundType.Restart => SoundNames.Restart,
            UISoundType.PerkAppear => SoundNames.PerkAppear,
            UISoundType.PerkSelect => SoundNames.PerkSelect,
            UISoundType.PerkDisappear => SoundNames.PerkDisappear,
            UISoundType.GameStart => SoundNames.GameStart,
            _ => SoundNames.Click
        };
    }
}
