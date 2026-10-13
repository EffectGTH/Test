using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Настройки кубиков")]
    [SerializeField] private Dice dicePrefab;     
    [SerializeField] private int diceCount = 2;         

    [Header("Настройки броска")]
    [SerializeField] private float minForce = 5f;
    [SerializeField] private float maxForce = 10f;
    [SerializeField] private float minTorque = 5f;
    [SerializeField] private float maxTorque = 15f;

    [Header("Input System")]
    [SerializeField] private InputActionAsset inputActions;
    
    private InputAction rollAction;
    private List<Dice> activeDice = new List<Dice>(); 
    private bool isRolling = false;
    private int totalScore = 0;

    void Awake()
    {
        var gameMap = inputActions.FindActionMap("Game");
        rollAction = gameMap.FindAction("Roll");
    }

    void Start()
    {
        SpawnDice(); 
    }

    void OnEnable()
    {
        rollAction.Enable();
        rollAction.performed += OnRollPerformed;
    }

    void OnDisable()
    {
        rollAction.Disable();
        rollAction.performed -= OnRollPerformed;
    }

    private void SpawnDice()
    {
        if (dicePrefab == null)
        {
            Debug.LogError("Префаб не назначен");
            return;
        }

        for (var i = 0; i < diceCount; i++)
        {
            var startPos = transform.position + new Vector3(0, i * 1.5f, 0);

            var newDice = Instantiate(dicePrefab, startPos, Quaternion.identity);
            
            activeDice.Add(newDice);
        }
    }

    private void OnRollPerformed(InputAction.CallbackContext context)
    {
        if (isRolling) return;
        StartCoroutine(RollRoutine());
    }

    private IEnumerator RollRoutine()
    {
        foreach (var die in activeDice)
        {
            die.ResetDice();
        }

        yield return new WaitForSeconds(0.2f);

        isRolling = true;
        totalScore = 0;
        Debug.Log("Бросок");

        foreach (var die in activeDice)
        {
            die.Roll(minForce, maxForce, minTorque, maxTorque);
        }

        yield return new WaitForSeconds(0.5f);

        var allSleeping = false;
        while (!allSleeping)
        {
            allSleeping = true;
            foreach (var die in activeDice)
            {
                if (!die.IsSleeping())
                {
                    allSleeping = false;
                    break;
                }
            }
            yield return new WaitForFixedUpdate();
        }

        CalculateScore();
        isRolling = false;
    }

    private void CalculateScore()
    {
        foreach (var die in activeDice)
        {
            var value = die.GetFaceValue();
            totalScore += value;
            Debug.Log($"Выпало: {value}");
        }
        Debug.Log($"Итоговый счет: {totalScore}");
    }

    public void RebindRollKey()
    {
        var action = rollAction;
        action.Disable();

        var rebindOperation = action.PerformInteractiveRebinding()
            .WithControlsExcluding("Mouse")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => {
                action.Enable();
                operation.Dispose();
                Debug.Log("Клавиша изменена");
            })
            .Start();
    }
}