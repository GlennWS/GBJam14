using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AppraisalMinigame : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Image itemImage;
    [SerializeField] private RectTransform lens;
    [SerializeField] private Sprite markerSprite;
    [SerializeField] private Image timeFill;
    [SerializeField] private TextMeshProUGUI caption;

    [SerializeField] private float duration = 8f;
    [SerializeField] private float lensPixelsPerSecond = 32f;
    [SerializeField] private float hitRadius = 5f;
    [SerializeField] private float missPenalty = 1f;

    public bool IsRunning { get; private set; }

    private ItemInstance item;
    private Action onComplete;
    private float timeLeft;
    private Vector2 lensPos;
    private Vector2 subPixel;
    private bool finishing;
    private readonly List<Image> markers = new List<Image>();

    public void Begin(ItemInstance target, Action done)
    {
        item = target;
        onComplete = done;
        timeLeft = duration;
        finishing = false;
        lensPos = Vector2.zero;
        subPixel = Vector2.zero;
        item.foundTells.Clear();

        var frames = item.data.conditionFrames;
        if (frames != null && frames.Length > 0)
        {
            int idx = Mathf.Clamp(Mathf.FloorToInt(item.condition * frames.Length), 0, frames.Length - 1);
            itemImage.sprite = frames[idx];
        }

        ClearMarkers();
        lens.anchoredPosition = lensPos;
        panel.SetActive(true);
        caption.text = "Look closely...\n[A] Inspect  [B] Done";
        IsRunning = true;
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
        if (timeFill != null)
        {
            timeFill.fillAmount = timeLeft / duration;
        }

        MoveLens();

        if (GBInput.A.WasPressedThisFrame())
        {
            Inspect();
        }

        if (timeLeft <= 0f || GBInput.B.WasPressedThisFrame() || item.foundTells.Count == item.data.tells.Count)
        {
            Finish();
        }
    }

    private void MoveLens()
    {
        Vector2 dir = GBInput.Direction;
        if (dir == Vector2.zero)
        {
            subPixel = Vector2.zero;
            return;
        }

        subPixel += dir * lensPixelsPerSecond * Time.deltaTime;
        Vector2 whole = new Vector2(Mathf.Round(subPixel.x), Mathf.Round(subPixel.y));
        subPixel -= whole;

        float half = itemImage.rectTransform.rect.width * 0.5f;
        lensPos.x = Mathf.Clamp(lensPos.x + whole.x, -half, half);
        lensPos.y = Mathf.Clamp(lensPos.y + whole.y, -half, half);
        lens.anchoredPosition = lensPos;
    }

    private void Inspect()
    {
        foreach (var tell in item.data.tells)
        {
            if (item.foundTells.Contains(tell))
            {
                continue;
            }
            if (Vector2.Distance(lensPos, ToLocal(tell.pixel)) <= hitRadius)
            {
                item.foundTells.Add(tell);
                AddMarker(tell);
                caption.text = $"{tell.Label}\n[A] Inspect  [B] Done";
                return;
            }
        }

        timeLeft -= missPenalty;
        caption.text = "Nothing there.\n[A] Inspect  [B] Done";
    }

    private Vector2 ToLocal(Vector2Int pixel)
    {
        float half = itemImage.rectTransform.rect.width * 0.5f;
        return new Vector2(pixel.x - half, half - pixel.y);
    }

    private void AddMarker(Tell tell)
    {
        var go = new GameObject("Marker", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(itemImage.transform, false);
        var img = go.GetComponent<Image>();
        img.sprite = markerSprite;
        img.raycastTarget = false;
        img.SetNativeSize();
        img.rectTransform.anchoredPosition = ToLocal(tell.pixel);
        markers.Add(img);
    }

    private void ClearMarkers()
    {
        foreach (var m in markers)
        {
            Destroy(m.gameObject);
        }
        markers.Clear();
    }

    private void Finish()
    {
        finishing = true;
        item.appraised = true;

        int found = item.foundTells.Count;
        int total = item.data.tells.Count;
        string summary = total == 0 ? "Nothing unusual." : $"Found {found} of {total} tells.";
        caption.text = $"{summary}\nLooks worth ~£{item.AppraisedValue}\n[A] Continue";
    }

    private void End()
    {
        IsRunning = false;
        panel.SetActive(false);
        onComplete?.Invoke();
    }
}
