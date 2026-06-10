using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PerksSelectionUI : MonoBehaviour
{
    [Header("UI References")]
   [SerializeField] private GameObject  _perksPanel;       
   [SerializeField] private Transform   _perksButtonPanel;  
   [SerializeField] private GameObject  _perks;         

    private List<GameObject> _activeButtons = new List<GameObject>();

    [System.Serializable]
    public class PerkData
    {
        public Sprite Icon;
        public string PerkName;
        public string PerkExplain;
    }

    public void ShowPerks(params PerkData[] perks)
    {
        ClearButtons();
        foreach (var perk in perks)
        {
            CreateButton(perk);
        }

        _perksPanel.SetActive(true);
        UISoundPlayer.PlaySound(UISoundType.PerkAppear);
    }

    private void CreateButton(PerkData perk)
    {
        GameObject button = Instantiate(_perks, _perksButtonPanel);
        button.SetActive(true);

        Image iconImage = button.transform.Find("Icon_Image")?.GetComponent<Image>();
        if (iconImage != null && perk.Icon != null)
        {
            iconImage.sprite = perk.Icon;
        }

        TextMeshProUGUI nameText = button.transform.Find("Perks_Name")?.GetComponent<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = perk.PerkName;
        }

        TextMeshProUGUI explainText = button.transform.Find("Perks_Explain")?.GetComponent<TextMeshProUGUI>();
        if (explainText != null)
        {
            explainText.text = perk.PerkExplain;
        }

        _activeButtons.Add(button);
    }

    private void ClearButtons()
    {
        foreach (GameObject button in _activeButtons)
        {
            Destroy(button);
        }
        _activeButtons.Clear();
    }

    public void ClosePerksSelection()
    {
        UISoundPlayer.PlaySound(UISoundType.PerkDisappear);
        ClearButtons();
        _perksPanel.SetActive(false);
    }

}
