using System.Collections;
using TMPro;
using UnityEngine;

public class LobbyUIController : MonoBehaviour
{
    [Header("씬 전환 연출")]
    [SerializeField] private float _duration = 1.0f;
    [SerializeField] private GameObject _loadingImage;
    [SerializeField] private TextMeshProUGUI _loadingText;

    [Header("플레이어 아이콘 애니메이션")]
    [SerializeField] private RectTransform _playerIcon;
    [SerializeField] private float _iconRotateAngle = -10f;
    [SerializeField] private float _iconRotateInterval = 0.15f;

    private const string TitleBGM = "로비BGM";

    public void Start()
    {
        AudioManager.Instance.Play(AudioType.BGM, TitleBGM);
    }

    private IEnumerator ProcessLoadingEffect()
    {
        AsyncOperation loadingOperation = SceneLoader.Instance.LoadSceneAsync(ESceneType.InGame);

        if (loadingOperation == null)
        {
            yield break;
        }

        loadingOperation.allowSceneActivation = false;
        _loadingImage.SetActive(true);

        Color textColor = _loadingText.color;
        textColor.a = 0f;
        _loadingText.color = textColor;

        float totalTimeElapsed = 0f;
        float fadeTimeElapsed = 0f;
        float iconRotateTimer = 0f;
        bool isRotated = false;
        bool isFadingIn = true;
        float minimumLoadingTime = 2f;

        while (loadingOperation.progress < 0.9f || totalTimeElapsed < minimumLoadingTime)
        {
            totalTimeElapsed += Time.deltaTime;

            if (isFadingIn)
            {
                if (fadeTimeElapsed < _duration)
                {
                    float ratio = fadeTimeElapsed / _duration;
                    textColor.a = ratio;
                    _loadingText.color = textColor;
                    fadeTimeElapsed += Time.deltaTime;
                }
                else
                {
                    textColor.a = 1f;
                    _loadingText.color = textColor;
                    fadeTimeElapsed = 0f;
                    isFadingIn = false;
                }
            }
            else
            {
                if (fadeTimeElapsed < _duration)
                {
                    fadeTimeElapsed += Time.deltaTime;
                }
                else
                {
                    textColor.a = 0f;
                    _loadingText.color = textColor;
                    fadeTimeElapsed = 0f;
                    isFadingIn = true;
                }
            }

            iconRotateTimer += Time.deltaTime;
            if (iconRotateTimer >= _iconRotateInterval)
            {
                iconRotateTimer = 0f;
                isRotated = !isRotated;
                float angle = isRotated ? _iconRotateAngle : 0f;
                _playerIcon.localRotation = Quaternion.Euler(0f, 0f, angle);
            }

            yield return null;
        }

        _playerIcon.localRotation = Quaternion.identity;
        loadingOperation.allowSceneActivation = true;
    }

    public void OnClickPlayButton()
    {
        StartCoroutine(ProcessLoadingEffect());
    }

    public void OnClickExitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}