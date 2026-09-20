using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CleaningMinigame : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Image itemImage;
    [SerializeField] private Sprite crackedOverlay;
    [SerializeField] private Image progressFill;
    [SerializeField] private Image strainFill;
    [SerializeField] private TextMeshProUGUI caption;

    [SerializeField] private float duration = 8f;
    [SerializeField] private int scrubsForFullClean = 24;
    [SerializeField] private float tooFastInterval = 0.09f;
    [SerializeField] private float strainPerFastPress = 0.2f;
    [SerializeField] private float strainDecayPerSecond = 0.6f;
    [SerializeField] private float crackPenalty = 0.3f;

    public bool IsRunning { get; private set; }

    private ItemInstance item;
    private Action onComplete;
    private float startCondition, grimeCleared, grimeTotal, strain, timeLeft, lastPressTime;
    private bool lastWasA, anyPress, cracked, finishing;

    public void Begin(ItemInstance target, Action done)
    {
        item = target;
        onComplete = done;
        startCondition = item.condition;
        grimeTotal = 1f - startCondition;
        grimeCleared = 0f;
        strain = 0f;
        timeLeft = duration;
        anyPress = false;
        cracked = false;
        finishing = false;

        panel.SetActive(true);
        Refresh();
        caption.text = "Alternate A and B to scrub the item!";
        IsRunning = false;
    }

    private void Update()
    {
        if (!IsRunning)
        {
            return;
        }

        if (finishing)
        {
            if (GBInput.A.WasPressedThisFrame())
            {
                End();
            }
            return;
        }

        timeLeft -= Time.deltaTime;
        strain = Mathf.Max(0f, strain - strainDecayPerSecond * Time.deltaTime);

        bool aWasPressed = GBInput.A.WasPressedThisFrame();
        bool bWasPressed = GBInput.B.WasPressedThisFrame();

        if (aWasPressed || bWasPressed)
        {
            bool isA = aWasPressed;
            float now = Time.time;

            if (anyPress && now - lastPressTime < tooFastInterval)
            {
                strain += strainPerFastPress;
            }

            if (!anyPress || isA != lastWasA)
            {
                grimeCleared = Mathf.Min(grimeTotal, grimeCleared + 1f / scrubsForFullClean);
            }

            lastWasA = isA;
            lastPressTime = now;
            anyPress = true;
        }

        if (strain >= 1f) 
        { 
            cracked = true; 
            Finish(); 
            return; 
        }
        if (timeLeft <= 0f || grimeCleared >= grimeTotal - 0.0001f) 
        { 
            Finish(); 
            return; 
        }

        Refresh();
    }

    private void Refresh()
    {
        float remaining = grimeTotal - grimeCleared;
        var frames = item.data.conditionFrames;
        if (frames != null && frames.Length > 0)
        {
            float cleanliness = grimeTotal > 0f ? grimeCleared / grimeTotal : 1f;
            int idx = Mathf.Clamp(Mathf.FloorToInt(cleanliness * frames.Length), 0, frames.Length - 1);
            itemImage.sprite = frames[idx];
        }
        if (progressFill != null)
        {
            progressFill.fillAmount = grimeTotal > 0f ? grimeCleared / grimeTotal : 1f;
        }
        if (strainFill != null)
        {
            strainFill.fillAmount = strain;
        }
    }

    private void Finish()
    {
        finishing = true;

        if (cracked)
        {
            item.condition = Mathf.Max(0f, startCondition - crackPenalty);
            caption.text = $"Too quick, the item cracks\nCondition: {Percentageify(item.condition)}\n[A] Continue";
        }
        else
        {
            item.condition = Mathf.Clamp01(startCondition + grimeCleared);
            Refresh();
            string grade = item.condition >= 0.95f ? "Mint condition" : item.condition >= 0.7f ? "Looks good" : "Still grubby";
            caption.text = $"{grade}\nCondition {Percentageify(startCondition)} → {Percentageify(item.condition)}\n[A] Continue";
        }
    }

    private void End()
    {
        IsRunning = false;
        panel.SetActive(false);
        onComplete?.Invoke();
    }
    public void BeginPlay()
    {
        IsRunning = true;
    }

    // I only have this function to make the other lines look a bit neater :')
    private static string Percentageify(float f) => $"{Mathf.RoundToInt(f * 100)}%";
}