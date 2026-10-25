
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Dice dicePrefab;
    [SerializeField, Min(1)] private int initialDiceCount = 2;
    [SerializeField, Min(1)] private int maxDiceCount = 50;
    [SerializeField] private float diceSpacing = 1.5f;

    [SerializeField] private float minForce = 5f;
    [SerializeField] private float maxForce = 10f;
    [SerializeField] private float minTorque = 5f;
    [SerializeField] private float maxTorque = 15f;
    [SerializeField] private float maxWaitForStop = 10f;

    [SerializeField] private TMP_InputField diceCountInput;
    [SerializeField] private TMP_InputField lossMinInput;
    [SerializeField] private TMP_InputField drawMinInput;
    [SerializeField] private TMP_InputField winMinInput;

    [SerializeField] private TMP_Text diceCountText;
    [SerializeField] private TMP_Text rolledValuesText;
    [SerializeField] private TMP_Text totalScoreText;
    [SerializeField] private TMP_Text lossMinText;
    [SerializeField] private TMP_Text drawMinText;
    [SerializeField] private TMP_Text winMinText;
    [SerializeField] private TMP_Text outcomeText;

    [SerializeField] private InputActionAsset inputActions;

    private InputAction rollAction;
    private readonly List<Dice> activeDice = new List<Dice>();

    private bool isRolling;
    private int totalScore;
    private int currentLossMin;
    private int currentDrawMin;
    private int currentWinMin;

    private void Awake()
    {
        SetText(rolledValuesText, "—");
        SetText(totalScoreText, "Сумма: —");
        SetText(lossMinText, "Минимум для поражения: —");
        SetText(drawMinText, "Минимум для ничьей: —");
        SetText(winMinText, "Минимум для победы: —");
        SetText(outcomeText, "Нажмите «Перебросить кубики».");
        SetText(diceCountText, $"Количество кубиков: {initialDiceCount}");

        if (inputActions != null)
        {
            var gameMap = inputActions.FindActionMap("Game", false);

            if (gameMap != null)
                rollAction = gameMap.FindAction("Roll", false);
        }
    }

    private void OnEnable()
    {
        if (rollAction != null)
        {
            rollAction.performed += OnRollPerformed;
            rollAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (rollAction != null)
        {
            rollAction.performed -= OnRollPerformed;
            rollAction.Disable();
        }
    }

    private void Start()
    {
        if (dicePrefab == null)
        {
            Debug.LogError("GameManager: не назначен Dice Prefab!");
            SetText(outcomeText, "Ошибка: не назначен префаб кубика.");
            return;
        }

        EnsureDiceCount(Mathf.Clamp(initialDiceCount, 1, maxDiceCount));
        UpdateDiceCountText();
    }

    public void RollDiceButton()
    {
        if (isRolling)
            return;

        if (dicePrefab == null)
        {
            SetText(outcomeText, "Ошибка: назначьте префаб кубика.");
            return;
        }

        if (!TryGetDiceCount(out int requestedCount))
            return;

        if (!TryGetThresholds(
                requestedCount,
                out int lossMin,
                out int drawMin,
                out int winMin))
        {
            return;
        }

        EnsureDiceCount(requestedCount);

        currentLossMin = lossMin;
        currentDrawMin = drawMin;
        currentWinMin = winMin;
        totalScore = 0;

        UpdateDiceCountText();
        UpdateThresholdTexts();

        SetText(rolledValuesText, "Кубики бросаются...");
        SetText(totalScoreText, "Сумма: —");
        SetText(outcomeText, "Ожидание результата броска...");

        isRolling = true;
        StartCoroutine(RollRoutine());
    }

    private bool TryGetDiceCount(out int count)
    {
        count = activeDice.Count > 0
            ? activeDice.Count
            : initialDiceCount;

        string input = diceCountInput != null
            ? diceCountInput.text.Trim()
            : "";

        if (!string.IsNullOrEmpty(input))
        {
            if (!int.TryParse(input, out count))
            {
                SetText(outcomeText, "Введите целое число кубиков.");
                return false;
            }
        }

        if (count < 1 || count > maxDiceCount)
        {
            SetText(
                outcomeText,
                $"Количество кубиков должно быть от 1 до {maxDiceCount}.");
            return false;
        }

        return true;
    }

    private bool TryGetThresholds(
        int diceCount,
        out int lossMin,
        out int drawMin,
        out int winMin)
    {
        lossMin = 0;
        drawMin = 0;
        winMin = 0;

        string lossInput = ReadInput(lossMinInput);
        string drawInput = ReadInput(drawMinInput);
        string winInput = ReadInput(winMinInput);

        bool allEmpty =
            string.IsNullOrEmpty(lossInput) &&
            string.IsNullOrEmpty(drawInput) &&
            string.IsNullOrEmpty(winInput);

        int lowestPossibleScore = diceCount;
        int highestPossibleScore = diceCount * 6;

        if (allEmpty)
        {
            lossMin = Random.Range(
                lowestPossibleScore,
                highestPossibleScore - 1);

            drawMin = Random.Range(
                lossMin + 1,
                highestPossibleScore);

            winMin = Random.Range(
                drawMin + 1,
                highestPossibleScore + 1);

            return true;
        }

        if (string.IsNullOrEmpty(lossInput) ||
            string.IsNullOrEmpty(drawInput) ||
            string.IsNullOrEmpty(winInput))
        {
            SetText(
                outcomeText,
                "Заполните все три поля или очистите их для случайных значений.");
            return false;
        }

        if (!int.TryParse(lossInput, out lossMin) ||
            !int.TryParse(drawInput, out drawMin) ||
            !int.TryParse(winInput, out winMin))
        {
            SetText(outcomeText, "В полях должны быть только целые числа.");
            return false;
        }

        if (lossMin < lowestPossibleScore ||
            drawMin < lowestPossibleScore ||
            winMin > highestPossibleScore)
        {
            SetText(
                outcomeText,
                $"Значения в полях должны находиться в диапазоне от " +
                $"{lowestPossibleScore} до {highestPossibleScore}.");
            return false;
        }

        if (!(lossMin < drawMin && drawMin < winMin))
        {
            SetText(
                outcomeText,
                "Значения должны идти по возрастанию: " +
                "поражение, ничья, победа.");
            return false;
        }

        return true;
    }

    private string ReadInput(TMP_InputField input)
    {
        return input != null ? input.text.Trim() : "";
    }

    private void EnsureDiceCount(int desiredCount)
    {
        while (activeDice.Count > desiredCount)
        {
            int lastIndex = activeDice.Count - 1;
            Dice die = activeDice[lastIndex];

            activeDice.RemoveAt(lastIndex);

            if (die != null)
                Destroy(die.gameObject);
        }

        while (activeDice.Count < desiredCount)
        {
            int index = activeDice.Count;

            Vector3 spawnPosition =
                transform.position +
                new Vector3(index * diceSpacing, 0f, 0f);

            Dice newDice = Instantiate(
                dicePrefab,
                spawnPosition,
                Quaternion.identity);

            activeDice.Add(newDice);
        }
    }

    private IEnumerator RollRoutine()
    {
        foreach (Dice die in activeDice)
        {
            if (die != null)
                die.ResetDice();
        }

        yield return new WaitForFixedUpdate();

        foreach (Dice die in activeDice)
        {
            if (die != null)
                die.Roll(minForce, maxForce, minTorque, maxTorque);
        }

        yield return new WaitForSeconds(0.5f);

        float elapsed = 0f;
        bool allSleeping = false;

        while (!allSleeping && elapsed < maxWaitForStop)
        {
            allSleeping = true;

            foreach (Dice die in activeDice)
            {
                if (die != null && !die.IsSleeping())
                {
                    allSleeping = false;
                    break;
                }
            }

            if (!allSleeping)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        CalculateScore();

        if (allSleeping)
            SetText(outcomeText, GetOutcomeText());
        else
            SetText(outcomeText, GetOutcomeText() +
                "\nИстекло время ожидания остановки.");

        isRolling = false;
    }

    private void CalculateScore()
    {
        totalScore = 0;
        List<string> values = new List<string>();

        foreach (Dice die in activeDice)
        {
            if (die == null)
                continue;

            int value = die.GetFaceValue();

            totalScore += value;
            values.Add(value.ToString());
        }

        SetText(
            rolledValuesText,
            string.Join(" + ", values) + " = " + totalScore);

        SetText(totalScoreText, $"Общая сумма: {totalScore}");

        Debug.Log($"Выпавшие значения: {string.Join(", ", values)}");
        Debug.Log($"Итоговый счёт: {totalScore}");
    }

    private string GetOutcomeText()
    {
        if (totalScore >= currentWinMin)
            return "Результат: ПОБЕДА";

        if (totalScore >= currentDrawMin)
            return "Результат: НИЧЬЯ";

        if (totalScore >= currentLossMin)
            return "Результат: ПОРАЖЕНИЕ";

        return "Результат: ПОРАЖЕНИЕ. Минимум очков не набран.";
    }

    private void UpdateDiceCountText()
    {
        SetText(diceCountText, $"Количество кубиков: {activeDice.Count}");
    }

    private void UpdateThresholdTexts()
    {
        SetText(lossMinText, $"Минимум для поражения: {currentLossMin}");
        SetText(drawMinText, $"Минимум для ничьей: {currentDrawMin}");
        SetText(winMinText, $"Минимум для победы: {currentWinMin}");
    }

    private void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value;
    }

    private void OnRollPerformed(InputAction.CallbackContext context)
    {
        RollDiceButton();
    }

    public void RebindRollKey()
    {
        if (rollAction == null)
        {
            Debug.LogWarning("Бинд не настроен");
            return;
        }

        var action = rollAction;
        action.Disable();

        action.PerformInteractiveRebinding()
            .WithControlsExcluding("Mouse")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation =>
            {
                action.Enable();
                operation.Dispose();
                SetText(outcomeText, "Клавиша броска изменена");
            })
            .Start();
    }
}
