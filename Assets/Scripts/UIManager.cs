using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Menu")]
    [SerializeField] private CanvasGroup menuCanvasGroup;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button exportButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private TMP_Text exitTextButton;

    [Header("Popup")]
    [SerializeField] private CanvasGroup popupCanvasGroup;
    [SerializeField] private TMP_Text popupText;
    [SerializeField] private Button okButton;

    public bool IsExporting = false;

    private void Awake()
    {
        Instance = this;
    }

    public void ShowMenu(Func<IEnumerator> onExport, Action onExit, string exitMessage)
    {
        if (IsExporting) return;

        resumeButton.onClick.AddListener(() => HideMenu());
        exportButton.onClick.AddListener(() =>
        {
            HideMenu();
            StartCoroutine(onExport?.Invoke());
        });
        exitButton.onClick.AddListener(() => onExit?.Invoke());
        exitTextButton.text = exitMessage;
        HidePopupText();

        menuCanvasGroup.alpha = 1f;
        menuCanvasGroup.interactable = true;
        menuCanvasGroup.blocksRaycasts = true;
    }

    public void HideMenu()
    {
        if (IsExporting) return;

        resumeButton.onClick.RemoveAllListeners();
        exportButton.onClick.RemoveAllListeners();
        exitButton.onClick.RemoveAllListeners();

        menuCanvasGroup.alpha = 0f;
        menuCanvasGroup.interactable = false;
        menuCanvasGroup.blocksRaycasts = false;
    }

    public void ShowPopupText(string message)
    {
        okButton.onClick.RemoveAllListeners();
        okButton.onClick.AddListener(() => HidePopupText());

        popupText.text = message;
        popupCanvasGroup.alpha = 1f;
        popupCanvasGroup.interactable = true;
        popupCanvasGroup.blocksRaycasts = true;
    }

    public void HidePopupText()
    {
        popupCanvasGroup.alpha = 0f;
        popupCanvasGroup.interactable = false;
        popupCanvasGroup.blocksRaycasts = false;
    }
}
