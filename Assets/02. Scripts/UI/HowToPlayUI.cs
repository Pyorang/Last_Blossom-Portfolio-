using TMPro;
using UnityEngine;

public class HowToPlayUI : MonoBehaviour
{
    [Header("How To Play UI Panel")]
    [SerializeField] private GameObject _background;
    [SerializeField] private GameObject[] _pages;
    [SerializeField] private TextMeshProUGUI _pageText;
    [SerializeField] private int _pageCount = 1;
    [SerializeField] private GameObject _gameStartButton;


    public void ShowHowToPlay()
    {
        _background.SetActive(true);
        ShowPage(1);
    }

    public void ShowPage(int pageIndex)
    {
        _pageCount = pageIndex;

        for (int i = 0; i < _pages.Length; i++)
        {
            _pages[i].SetActive(false);
        }

        if (pageIndex > 0 && pageIndex <= _pages.Length)
        {
            _pages[pageIndex - 1].SetActive(true);
        }

        UpdatePageText();

        if (_gameStartButton != null)
        {
            _gameStartButton.SetActive(pageIndex == _pages.Length);
        }
    }

    private void UpdatePageText()
    {
        if (_pageText != null)
        {
            _pageText.text = $"{_pageCount} / {_pages.Length}";
        }
    }

    public void NextPage()
    {
        if (_pageCount < _pages.Length)
        {
            ShowPage(_pageCount + 1);
        }
    }

    public void PreviousPage()
    {
        if (_pageCount > 1)
        {
            ShowPage(_pageCount - 1);
        }
    }

    public void Exit()
    {
        _background.SetActive(false);
    }
}