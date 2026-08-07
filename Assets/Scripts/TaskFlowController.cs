using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class TaskFlowController : MonoBehaviour
{
    [Header("Flow")]
    [SerializeField] private GameObject flowRoot;
    [SerializeField] private GameObject stage1;
    [SerializeField] private GameObject stage2;
    [SerializeField] private GameObject stage3;
    [SerializeField] private GameObject completionState;

    [Header("User Number")]
    [SerializeField] private GameObject userNumberStage;
    [SerializeField] private TMP_Dropdown tensDigitDropdown;
    [SerializeField] private TMP_Dropdown onesDigitDropdown;

    [Header("Experiment Data")]
    [SerializeField] private ExperimentDataRecorder dataRecorder;
    [SerializeField] private string experimentVersion = "V1";
    [SerializeField] private int[] correctNumbersByRound =
    {
       1, 3, 5, 7, 9
    };

    [Header("Round")]
    [SerializeField] private TMP_Text roundText;

    [Header("Station")]
    [SerializeField] private TMP_Text stationPrompt;
    [SerializeField] private string stationName = "Red";
    [SerializeField] private Color stationColor = Color.red;

    [Header("Number Selection")]
    [SerializeField] private Button submitButton;
    [SerializeField] private Button[] numberButtons;
    [SerializeField] private int[] numberValues = { 1, 3, 5, 7, 9 };
    [SerializeField] private Color normalNumberColor =
        new Color(0.22f, 0.22f, 0.22f);
    [SerializeField] private Color selectedNumberColor =
        new Color(0.15f, 0.55f, 0.9f);

    [Header("Completion")]
    [SerializeField] private UnityEvent<int> onSubmitted;

    public string UserNumber { get; private set; }
    public int CurrentRound { get; private set; }

    private int selectedNumber = -1;
    private bool submitted;

    public int SelectedNumber => selectedNumber;

    private void Start()
    {
        submitted = false;
        selectedNumber = -1;

        flowRoot.SetActive(true);
        userNumberStage.SetActive(true);
        completionState.SetActive(false);
        submitButton.interactable = false;
        roundText.gameObject.SetActive(false);

        UpdateNumberVisuals();
        ShowStage(0);
    }

    public void ShowStage2()
    {
        dataRecorder.CompleteStage1();
        ShowStage(2);
    }

    public void ShowStage3()
    {
        dataRecorder.CompleteStage2();
        ShowStage(3);
    }

    public void SelectNumber(int value)
    {
        if (submitted)
        {
            return;
        }

        selectedNumber = value;
        submitButton.interactable = true;
        UpdateNumberVisuals();
    }

    public void Submit()
    {
        if (selectedNumber < 0 || submitted)
        {
            return;
        }

        if (!TryGetCorrectNumber(out int correctNumber))
        {
            return;
        }

        if (!dataRecorder.CompleteRound(
            selectedNumber,
            correctNumber))
        {
            return;
        }

        submitted = true;

        Debug.Log($"Round {CurrentRound}: " + $"selected={selectedNumber}, " + $"correct={correctNumber}", this);
        onSubmitted?.Invoke(selectedNumber);

        // 当前定义：“结束”后显示restart。
        ShowStage(0);
        completionState.SetActive(true);
    }

    private void ShowStage(int stage)
    {
        stage1.SetActive(stage == 1);
        stage2.SetActive(stage == 2);
        stage3.SetActive(stage == 3);
    }

    private void UpdateStationText()
    {
        string htmlColor =
            ColorUtility.ToHtmlStringRGB(stationColor);

        stationPrompt.text =
            $"Go to <color=#{htmlColor}>{stationName}</color> station.";
    }

    private void UpdateNumberVisuals()
    {
        for (int i = 0; i < numberButtons.Length; i++)
        {
            if (numberButtons[i] == null)
            {
                continue;
            }

            bool selected =
                i < numberValues.Length &&
                numberValues[i] == selectedNumber;

            numberButtons[i].image.color =
                selected
                    ? selectedNumberColor
                    : normalNumberColor;
        }
    }


    public void StartTask()
    {
        string tens =
            tensDigitDropdown.options[
                tensDigitDropdown.value].text;

        string ones =
            onesDigitDropdown.options[
                onesDigitDropdown.value].text;

        UserNumber = $"P0{tens}{ones}";
        if (!dataRecorder.BeginSession(
            UserNumber,
            experimentVersion))
        {
            return;
        }
        Debug.Log( $"User number: {UserNumber}, " + $"version: {experimentVersion}", this);

        CurrentRound = 1;
        BeginRound();
    }

    public void RestartFlow()
    {
        CurrentRound++;
        BeginRound();
    }

    private void BeginRound()
    {
        submitted = false;
        selectedNumber = -1;

        flowRoot.SetActive(true);
        userNumberStage.SetActive(false);
        completionState.SetActive(false);
        submitButton.interactable = false;

        UpdateRoundText();
        UpdateStationText();
        UpdateNumberVisuals();
        ShowStage(1);
        
        dataRecorder.BeginRound(CurrentRound);
    }

    private void UpdateRoundText()
    {
        roundText.gameObject.SetActive(true);
        roundText.text = $"Round {CurrentRound}";
    }

    private bool TryGetCorrectNumber(out int correctNumber)
    {
        int index = CurrentRound - 1;

        if (correctNumbersByRound == null ||
            index < 0 ||
            index >= correctNumbersByRound.Length)
        {
            correctNumber = -1;

            Debug.LogError(
                $"No correct answer configured for Round {CurrentRound}.",
                this);

            return false;
        }

        correctNumber = correctNumbersByRound[index];
        //有效数字范围待定
        /*
        bool valid =
            correctNumber == 1 ||
            correctNumber == 3 ||
            correctNumber == 5 ||
            correctNumber == 7 ||
            correctNumber == 9;

        if (!valid)
        {
            Debug.LogError(
                $"Invalid correct answer {correctNumber} " +
                $"for Round {CurrentRound}.",
                this);

            return false;
        }
        */
        return true;
    }
}