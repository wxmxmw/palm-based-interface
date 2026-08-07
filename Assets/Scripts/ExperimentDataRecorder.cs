using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public sealed class ExperimentDataRecorder : MonoBehaviour
{
    [Header("Storage")]
    [SerializeField]
    private string pcRootDirectory = @"D:\QuestData\ExperimentalData";

    [SerializeField]
    private string questDirectoryName = "ExperimentalData";

    public string SessionFilePath { get; private set; }

    private enum TimingStage
    {
        None,
        Stage1,
        Stage2,
        Stage3
    }

    private string participantId;
    private string experimentVersion;
    private string sessionStartUtc;

    private int currentRound;

    private double stageStartedAt;
    private double stage1Seconds;
    private double stage2Seconds;
    private TimingStage timingStage;
    private bool sessionStarted;

    public bool BeginSession(string newParticipantId, string newVersion)
    {
        if (!IsValidParticipantId(newParticipantId))
        {
            Debug.LogError(
                $"Invalid participant ID '{newParticipantId}'. Expected P001-P099.",
                this);
            return false;
        }

        participantId = newParticipantId;
        experimentVersion = string.IsNullOrWhiteSpace(newVersion)
            ? "V1"
            : newVersion.Trim();
        sessionStartUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);

        try
        {
            string participantDirectory = Path.Combine(
                GetRootDirectory(),
                participantId);

            Directory.CreateDirectory(participantDirectory);

            string safeVersion = MakeSafeFileName(experimentVersion);
            string timestamp = DateTime.UtcNow.ToString(
                "yyyyMMdd_HHmmss_fff",
                CultureInfo.InvariantCulture);

            SessionFilePath = Path.Combine(
                participantDirectory,
                $"{safeVersion}_{timestamp}.csv");

            const string header =
                "ParticipantId,ExperimentVersion,SessionStartUtc,Round," +
                "Stage1Seconds,Stage2Seconds,Stage3Seconds," +
                "SelectedNumber,CorrectNumber,IsCorrect,RecordedAtUtc\n";

            File.WriteAllText(
                SessionFilePath,
                header,
                new UTF8Encoding(true));

            sessionStarted = true;
            timingStage = TimingStage.None;

            Debug.Log($"Experiment data file: {SessionFilePath}", this);
            return true;
        }
        catch (Exception exception)
        {
            sessionStarted = false;
            Debug.LogError(
                $"Could not create experiment data file. {exception}",
                this);
            return false;
        }
    }

    public void BeginRound(int round)
    {
        if (!sessionStarted)
        {
            Debug.LogError("BeginSession must succeed before BeginRound.", this);
            return;
        }

        currentRound = round;
        stage1Seconds = 0d;
        stage2Seconds = 0d;
        stageStartedAt = Time.realtimeSinceStartupAsDouble;
        timingStage = TimingStage.Stage1;
    }

    public void CompleteStage1()
    {
        if (timingStage != TimingStage.Stage1)
        {
            return;
        }

        stage1Seconds = ElapsedStageSeconds();
        stageStartedAt = Time.realtimeSinceStartupAsDouble;
        timingStage = TimingStage.Stage2;
    }

    public void CompleteStage2()
    {
        if (timingStage != TimingStage.Stage2)
        {
            return;
        }

        stage2Seconds = ElapsedStageSeconds();
        stageStartedAt = Time.realtimeSinceStartupAsDouble;
        timingStage = TimingStage.Stage3;
    }

    public bool CompleteRound(int selectedNumber, int correctNumber)
    {
        if (!sessionStarted || timingStage != TimingStage.Stage3)
        {
            Debug.LogError("Cannot save the round because Stage 3 is not active.", this);
            return false;
        }

        double stage3Seconds = ElapsedStageSeconds();
        bool isCorrect = selectedNumber == correctNumber;
        string recordedAtUtc = DateTime.UtcNow.ToString(
            "O",
            CultureInfo.InvariantCulture);

        string row = string.Join(",",
            EscapeCsv(participantId),
            EscapeCsv(experimentVersion),
            EscapeCsv(sessionStartUtc),
            currentRound.ToString(CultureInfo.InvariantCulture),
            FormatSeconds(stage1Seconds),
            FormatSeconds(stage2Seconds),
            FormatSeconds(stage3Seconds),
            selectedNumber.ToString(CultureInfo.InvariantCulture),
            correctNumber.ToString(CultureInfo.InvariantCulture),
            isCorrect ? "true" : "false",
            EscapeCsv(recordedAtUtc)) + "\n";

        try
        {
            File.AppendAllText(
                SessionFilePath,
                row,
                new UTF8Encoding(false));

            timingStage = TimingStage.None;
            Debug.Log(
                $"Saved Round {currentRound}: " +
                $"selected={selectedNumber}, correct={correctNumber}, " +
                $"isCorrect={isCorrect}",
                this);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Could not save Round {currentRound}. {exception}",
                this);
            return false;
        }
    }

    private string GetRootDirectory()
    {
        return Application.platform == RuntimePlatform.Android
            ? Path.Combine(Application.persistentDataPath, questDirectoryName)
            : pcRootDirectory;
    }

    private double ElapsedStageSeconds()
    {
        return Math.Max(
            0d,
            Time.realtimeSinceStartupAsDouble - stageStartedAt);
    }

    private static bool IsValidParticipantId(string value)
    {
        if (string.IsNullOrEmpty(value) ||
            value.Length != 4 ||
            value[0] != 'P')
        {
            return false;
        }

        return int.TryParse(
                   value.Substring(1),
                   NumberStyles.None,
                   CultureInfo.InvariantCulture,
                   out int number) &&
               number >= 1 &&
               number <= 99;
    }

    private static string MakeSafeFileName(string value)
    {
        foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalidCharacter, '_');
        }

        return value;
    }

    private static string FormatSeconds(double value)
    {
        return value.ToString("F3", CultureInfo.InvariantCulture);
    }

    private static string EscapeCsv(string value)
    {
        if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
