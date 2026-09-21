using System;
using System.Collections;
using UnityEngine;

public class ScreenWipe : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private float slideSeconds = 0.25f;
    [SerializeField] private float holdSeconds = 0.05f;

    public bool IsPlaying { get; private set; }

    private RectTransform parentRect;
    private Coroutine running;

    private void Awake()
    {
        parentRect = (RectTransform)panel.parent;
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = new Vector2(-9999f, 0f);
    }

    private float Width => Mathf.Max(parentRect.rect.width, 160f) + 8f;

    public void Play(Action onCovered, Action onDone = null)
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Run(onCovered, onDone));
    }

    private IEnumerator Run(Action onCovered, Action onDone)
    {
        IsPlaying = true;

        float w = Width;
        float h = Mathf.Max(parentRect.rect.height, 144f) + 8f;
        panel.sizeDelta = new Vector2(w, h);

        Vector2 offLeft = new Vector2(-w, 0f);
        Vector2 covered = Vector2.zero;
        Vector2 offRight = new Vector2(w, 0f);

        panel.anchoredPosition = offLeft;

        yield return Slide(offLeft, covered);
        panel.anchoredPosition = covered;
        yield return null;

        onCovered?.Invoke();
        if (holdSeconds > 0f) yield return new WaitForSeconds(holdSeconds);

        yield return Slide(covered, offRight);
        panel.anchoredPosition = new Vector2(-9999f, 0f);

        IsPlaying = false;
        running = null;
        onDone?.Invoke();
    }

    private IEnumerator Slide(Vector2 from, Vector2 to)
    {
        float t = 0f;
        while (t < slideSeconds)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / slideSeconds);
            Vector2 p = Vector2.Lerp(from, to, k);
            panel.anchoredPosition = new Vector2(Mathf.Round(p.x), Mathf.Round(p.y));
            yield return null;
        }
        panel.anchoredPosition = to;
    }
}