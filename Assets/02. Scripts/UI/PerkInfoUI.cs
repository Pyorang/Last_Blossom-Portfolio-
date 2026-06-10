using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PerkInfoUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private Image _iconImage;

    private static PerkIconDatabase _iconDatabase;

    public void Setup(PerkDataModel data)
    {
        _nameText.text = data.Name;
        _descriptionText.text = data.Description;

        SetupIcon(data.ID);
    }

    private void SetupIcon(string perkId)
    {
        if (_iconImage == null) return;

        if (_iconDatabase == null)
            _iconDatabase = Resources.Load<PerkIconDatabase>("PerkIconDatabase");

        if (_iconDatabase != null)
        {
            Sprite icon = _iconDatabase.GetIcon(perkId);
            if (icon != null)
                _iconImage.sprite = icon;
        }
    }
}
