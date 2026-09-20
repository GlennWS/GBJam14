using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenWipe : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private float slideSeconds = 0.25f;
    [SerializeField] private float holdSeconds = 0.05f;

    public bool IsPlaying { get; private set; }

    private float width;

    private void Awake()
    {
        width = panel.rect.width;
        panel.anchoredPosition = new Vector2(-width, 0f);
    }

    public void Play(Action onCovered, Action onDone = null)
    {
        StartCoroutine(Run(onCovered, onDone));
    }

    private IEnumerator Run(Action onCovered, Action onDone)
    {
        IsPlaying = true;

        yield return Slide(-width, 0f);
        onCovered?.Invoke();
        if (holdSeconds > 0f)
        {
            yield return new WaitForSeconds(holdSeconds);
        }
        yield return Slide(0f, width);

        panel.anchoredPosition = new Vector2(-width, 0f);
        IsPlaying = false;
        onDone?.Invoke();
    }

    private IEnumerator Slide(float fromX, float toX)
    {
        float t = 0f;
        while (t < slideSeconds)
        {
            t += Time.deltaTime;
            float x = Mathf.Round(Mathf.Lerp(fromX, toX, t / slideSeconds));
            panel.anchoredPosition = new Vector2(x, 0f);
            yield return null;
        }
        panel.anchoredPosition = new Vector2(toX, 0f);
    }
}
