using UnityEngine;
using TMPro;
using System.Collections;

public class DamageText : MonoBehaviour
{
    [Header("Highlight Gradient")]
    [SerializeField] private Color _highlightTopColor = new Color(1f, 1f, 0.5f, 1f);
    [SerializeField] private Color _highlightBottomColor = new Color(1f, 0.3f, 0f, 1f);
    
    [Header("Shield Gradient")]
    [SerializeField] private Color _shieldTopColor = new Color(0.5f, 0.8f, 1f, 1f);
    [SerializeField] private Color _shieldBottomColor = new Color(0.2f, 0.5f, 0.8f, 1f);
    
    [Header("Animation")]
    [SerializeField] private float _scaleDuration = 0.05f;
    [SerializeField] private float _maxScale = 1.5f;
    [SerializeField] private float _fadeOutDuration = 0.5f;

    private TextMeshPro _text;
    private Color _originalColor;
    private bool _originalGradientEnabled;
    private VertexGradient _originalGradient;
    private Vector3[][] _originalVertices;
    private Coroutine _currentCoroutine;
    private DamageTextPool _pool;

    private void Awake()
    {
        if (_text == null)
            _text = GetComponent<TextMeshPro>();
        _originalColor = _text.color;
        _originalGradientEnabled = _text.enableVertexGradient;
        _originalGradient = _text.colorGradient;
    }

    private void LateUpdate()
    {
        Camera mainCamera = _pool?.MainCamera;
        if (mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }
    }

    public void Initialize(DamageTextPool pool)
    {
        _pool = pool;
    }

    public void SetText(string text)
    {
        _text.text = text;
    }

    public void ShowWithHighlight(string text)
    {
        _text.text = text;
        _text.enableVertexGradient = true;
        _text.colorGradient = new VertexGradient(
            _highlightTopColor,
            _highlightTopColor,
            _highlightBottomColor,
            _highlightBottomColor
        );

        if (_currentCoroutine != null)
            StopCoroutine(_currentCoroutine);
        _currentCoroutine = StartCoroutine(AnimateTextCoroutine(_highlightTopColor, _highlightBottomColor));
    }

    public void ShowWithShield(string text)
    {
        _text.text = text;
        _text.enableVertexGradient = true;
        _text.colorGradient = new VertexGradient(
            _shieldTopColor,
            _shieldTopColor,
            _shieldBottomColor,
            _shieldBottomColor
        );

        if (_currentCoroutine != null)
            StopCoroutine(_currentCoroutine);
        _currentCoroutine = StartCoroutine(AnimateTextCoroutine(_shieldTopColor, _shieldBottomColor));
    }

    public void Show(string text)
    {
        _text.text = text;
        _text.enableVertexGradient = _originalGradientEnabled;
        _text.colorGradient = _originalGradient;

        if (_currentCoroutine != null)
            StopCoroutine(_currentCoroutine);
        _currentCoroutine = StartCoroutine(AnimateTextCoroutine(null, null));
    }

    private IEnumerator AnimateTextCoroutine(Color? topColor, Color? bottomColor)
    {
        _text.ForceMeshUpdate();
        TMP_TextInfo textInfo = _text.textInfo;

        CacheOriginalVertices(textInfo);

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            float elapsed = 0f;
            while (elapsed < _scaleDuration)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(1f, _maxScale, elapsed / _scaleDuration);
                ApplyCharacterScale(i, scale);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < _scaleDuration)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(_maxScale, 1f, elapsed / _scaleDuration);
                ApplyCharacterScale(i, scale);
                yield return null;
            }

            ApplyCharacterScale(i, 1f);
        }

        float fadeElapsed = 0f;
        while (fadeElapsed < _fadeOutDuration)
        {
            fadeElapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, fadeElapsed / _fadeOutDuration);
            
            if (topColor.HasValue && bottomColor.HasValue)
            {
                _text.colorGradient = new VertexGradient(
                    new Color(topColor.Value.r, topColor.Value.g, topColor.Value.b, alpha),
                    new Color(topColor.Value.r, topColor.Value.g, topColor.Value.b, alpha),
                    new Color(bottomColor.Value.r, bottomColor.Value.g, bottomColor.Value.b, alpha),
                    new Color(bottomColor.Value.r, bottomColor.Value.g, bottomColor.Value.b, alpha)
                );
            }
            else
            {
                _text.color = new Color(_originalColor.r, _originalColor.g, _originalColor.b, alpha);
            }
            yield return null;
        }

        ResetToOriginal();
        _pool?.Return(this);
    }

    private void ResetToOriginal()
    {
        _text.color = _originalColor;
        _text.enableVertexGradient = _originalGradientEnabled;
        _text.colorGradient = _originalGradient;
    }

    private void CacheOriginalVertices(TMP_TextInfo textInfo)
    {
        _originalVertices = new Vector3[textInfo.characterCount][];

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
            Vector3[] sourceVertices = textInfo.meshInfo[materialIndex].vertices;

            _originalVertices[i] = new Vector3[4];
            for (int j = 0; j < 4; j++)
            {
                _originalVertices[i][j] = sourceVertices[vertexIndex + j];
            }
        }
    }

    private void ApplyCharacterScale(int charIndex, float scale)
    {
        if (_originalVertices[charIndex] == null) return;

        TMP_TextInfo textInfo = _text.textInfo;
        int materialIndex = textInfo.characterInfo[charIndex].materialReferenceIndex;
        int vertexIndex = textInfo.characterInfo[charIndex].vertexIndex;
        Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

        Vector3 center = (_originalVertices[charIndex][0] + _originalVertices[charIndex][2]) / 2f;

        for (int j = 0; j < 4; j++)
        {
            vertices[vertexIndex + j] = center + (_originalVertices[charIndex][j] - center) * scale;
        }

        _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }
}