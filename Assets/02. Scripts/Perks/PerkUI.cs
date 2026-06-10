using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class PerkUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _iconImage;

    [Header("Hover Effect")]
    [SerializeField] private float _hoverScale = 1.08f;
    [SerializeField] private float _hoverDuration = 0.2f;
    
    [Header("Select Effect")]
    [SerializeField] private float _selectPunchScale = 1.15f;
    [SerializeField] private float _selectDuration = 0.3f;
    [SerializeField] private Color _selectFlashColor = new Color(1f, 0.9f, 0.5f);

    private IPerk _perk;
    private string _perkId;
    private InGameUIController _uiController;
    private static PerkIconDatabase _iconDatabase;
    private RectTransform _rectTransform;
    private Vector3 _originalScale;
    private Color _originalBackgroundColor;
    private Tweener _scaleTweener;
    private bool _isSelected;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _originalScale = Vector3.one;
        
        if (_backgroundImage != null)
            _originalBackgroundColor = _backgroundImage.color;
    }

    public void Setup(IPerk perk, InGameUIController uiController)
    {
        _perk = perk;
        _uiController = uiController;
        _isSelected = false;

        _perkId = _perk.GetType().Name.Replace("Perk", "");
        
        if (DataTableManager.Instance.TryGetPerkData(_perkId, out var data))
        {
            _nameText.text = data.Name;
            _descriptionText.text = data.Description;
        }
        else
        {
            _nameText.text = _perkId;
            _descriptionText.text = "";
        }

        SetupIcon();
    }

    private void SetupIcon()
    {
        if (_iconImage == null) return;

        if (_iconDatabase == null)
            _iconDatabase = Resources.Load<PerkIconDatabase>("PerkIconDatabase");

        if (_iconDatabase != null)
        {
            Sprite icon = _iconDatabase.GetIcon(_perkId);
            if (icon != null)
                _iconImage.sprite = icon;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_isSelected) return;
        
        _scaleTweener?.Kill();
        _scaleTweener = _rectTransform
            .DOScale(_originalScale * _hoverScale, _hoverDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_isSelected) return;
        
        _scaleTweener?.Kill();
        _scaleTweener = _rectTransform
            .DOScale(_originalScale, _hoverDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    public void ApplyPerk()
    {
        if (_isSelected) return;
        _isSelected = true;
        
        _scaleTweener?.Kill();
        
        var selectSequence = DOTween.Sequence();
        
        selectSequence.Append(
            _rectTransform.DOPunchScale(Vector3.one * (_selectPunchScale - 1f), _selectDuration, 1, 0.5f)
        );
        
        if (_backgroundImage != null)
        {
            selectSequence.Join(
                _backgroundImage.DOColor(_selectFlashColor, _selectDuration * 0.5f)
                    .SetLoops(2, LoopType.Yoyo)
            );
        }
        
        selectSequence.SetUpdate(true);
        selectSequence.OnComplete(() =>
        {
            PerkManager.Instance.ApplyPerk(_perk, _perkId);
            _uiController.OnPerkSelected();
        });
    }

    private void OnDestroy()
    {
        _scaleTweener?.Kill();
    }
}
