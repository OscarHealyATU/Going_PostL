using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DaySceneUI : MonoBehaviour
{
    [Header("Clock")]
    [SerializeField] private GameObject clockRoot;
    [SerializeField] private TMP_Text clockText;
    [SerializeField] private TMP_Text dayText;

    [Header("Buttons")]
    [SerializeField] private GameObject endDayButtonObject;
    [SerializeField] private Button endDayButton;
    [SerializeField] private Button startNextDayButton;
    [SerializeField] private Button closeSummaryButton;

    [Header("Summary")]
    [SerializeField] private GameObject daySummaryPanel;
    [SerializeField] private TMP_Text summaryTitleText;
    [SerializeField] private TMP_Text packagesDeliveredText;
    [SerializeField] private TMP_Text moneyEarnedText;
    [SerializeField] private TMP_Text moneySpentText;
    [SerializeField] private TMP_Text totalRevenueText;
    [SerializeField] private TMP_Text experienceEarnedText;

    [Header("Transition")]
    [SerializeField] private CanvasGroup dayTransitionFadeGroup;
    [SerializeField] private TMP_Text dayTransitionText;

    public GameObject ClockRoot => clockRoot;
    public TMP_Text ClockText => clockText;
    public TMP_Text DayText => dayText;

    public GameObject EndDayButtonObject => endDayButtonObject;
    public Button EndDayButton => endDayButton;
    public Button StartNextDayButton => startNextDayButton;
    public Button CloseSummaryButton => closeSummaryButton;

    public GameObject DaySummaryPanel => daySummaryPanel;
    public TMP_Text SummaryTitleText => summaryTitleText;
    public TMP_Text PackagesDeliveredText => packagesDeliveredText;
    public TMP_Text MoneyEarnedText => moneyEarnedText;
    public TMP_Text MoneySpentText => moneySpentText;
    public TMP_Text TotalRevenueText => totalRevenueText;
    public TMP_Text ExperienceEarnedText => experienceEarnedText;

    public CanvasGroup DayTransitionFadeGroup => dayTransitionFadeGroup;
    public TMP_Text DayTransitionText => dayTransitionText;

    private void Start()
    {
        if (DayManager.Instance != null)
            DayManager.Instance.BindSceneUI(this);
    }

    private void OnEnable()
    {
        if (DayManager.Instance != null)
            DayManager.Instance.BindSceneUI(this);
    }
}