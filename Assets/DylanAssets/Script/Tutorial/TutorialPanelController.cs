using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialPanelController : MonoBehaviour
{
    [Serializable]
    public class TutorialPage
    {
        [TextArea(5, 12)]
        public string description;

        public Sprite screenshot;
        public string title;
    }

    [Header("Panel Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Page UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image screenshotImage;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Navigation")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button closeButton;

    [Header("Tutorial Pages")]
    [SerializeField] private List<TutorialPage> pages = new List<TutorialPage>();

    [Header("Controls To Lock While Open")]
    [Tooltip("Drag in movement / look scripts here, e.g. newWalkController, FlyoverController, etc.")]
    [SerializeField] private MonoBehaviour[] behavioursToDisable;

    [Header("Cursor")]
    [SerializeField] private bool unlockCursorWhenOpen = true;
    [SerializeField] private bool relockCursorWhenClosed = true;

    private int currentPageIndex;
    private bool isOpen;

    public bool IsOpen => isOpen;

    public event Action<bool> OnPanelStateChanged;

    private void Awake()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(NextPage);

        if (prevButton != null)
            prevButton.onClick.AddListener(PreviousPage);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        if (panelRoot == null)
            panelRoot = gameObject;

        panelRoot.SetActive(false);
    }

    public void OpenPanel()
    {
        if (pages == null || pages.Count == 0)
        {
            Debug.LogWarning("TutorialPanelController: No tutorial pages have been assigned.");
            return;
        }

        isOpen = true;
        currentPageIndex = Mathf.Clamp(currentPageIndex, 0, pages.Count - 1);

        panelRoot.SetActive(true);
        SetGameplayLocked(true);
        RefreshPage();

        OnPanelStateChanged?.Invoke(true);
    }

    public void ClosePanel()
    {
        isOpen = false;

        panelRoot.SetActive(false);
        SetGameplayLocked(false);

        OnPanelStateChanged?.Invoke(false);
    }

    public void NextPage()
    {
        if (pages == null || pages.Count == 0)
            return;

        currentPageIndex++;

        if (currentPageIndex >= pages.Count)
            currentPageIndex = pages.Count - 1;

        RefreshPage();
    }

    public void PreviousPage()
    {
        if (pages == null || pages.Count == 0)
            return;

        currentPageIndex--;

        if (currentPageIndex < 0)
            currentPageIndex = 0;

        RefreshPage();
    }

    public void GoToPage(int index)
    {
        if (pages == null || pages.Count == 0)
            return;

        currentPageIndex = Mathf.Clamp(index, 0, pages.Count - 1);
        RefreshPage();
    }

    private void RefreshPage()
    {
        if (pages == null || pages.Count == 0)
            return;

        TutorialPage page = pages[currentPageIndex];

        if (titleText != null)
        {
            titleText.text = string.IsNullOrWhiteSpace(page.title)
                ? $"Tutorial {currentPageIndex + 1}"
                : page.title;
        }

        if (descriptionText != null)
            descriptionText.text = page.description ?? string.Empty;

        if (screenshotImage != null)
        {
            if (page.screenshot != null)
            {
                screenshotImage.sprite = page.screenshot;
                screenshotImage.enabled = true;
                screenshotImage.preserveAspect = true;
            }
            else
            {
                screenshotImage.sprite = null;
                screenshotImage.enabled = false;
            }
        }

        if (prevButton != null)
            prevButton.interactable = currentPageIndex > 0;

        if (nextButton != null)
            nextButton.interactable = currentPageIndex < pages.Count - 1;
    }

    private void SetGameplayLocked(bool locked)
    {
        if (behavioursToDisable != null)
        {
            for (int i = 0; i < behavioursToDisable.Length; i++)
            {
                if (behavioursToDisable[i] != null)
                    behavioursToDisable[i].enabled = !locked;
            }
        }

        if (locked && unlockCursorWhenOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (!locked && relockCursorWhenClosed)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}