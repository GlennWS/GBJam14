using System.Collections.Generic;
using UnityEngine;

public class Customer : MonoBehaviour
{
    public enum Mood { Happy, Neutral, Annoyed }
    public enum Phase { Entering, Browsing, WalkingToCounter, Waiting, Leaving, Gone }

    [SerializeField] private float speedPixelsPerSecond = 48f;
    [SerializeField] private int pixelsPerUnit = 8;
    [SerializeField] private Vector2 browseTimeRange = new Vector2(1.5f, 3f);
    [SerializeField] private Vector2Int browseStopsRange = new Vector2Int(1, 2);
    [SerializeField] private float patienceSeconds = 20f;

    public ItemInstance Item { get; private set; }
    public Mood CurrentMood { get; private set; } = Mood.Neutral;
    public Phase CurrentPhase { get; private set; } = Phase.Gone;
    public int Patience { get; private set; } = 3;
    public bool IsWaiting => CurrentPhase == Phase.Waiting;
    public event System.Action<Customer> OnGaveUp;
    public event System.Action<Customer> OnLeft;

    private Vector2 door, counter;
    private List<Vector2> route = new List<Vector2>();
    private int routeIndex;
    private Vector2 target;
    private bool walking;
    private float pauseTimer;
    private float waitTimer;
    private Vector2 subPixel;

    public void Spawn(ItemData data, Vector2 doorPos, Vector2 counterPos, IList<Transform> browsePoints)
    {
        Item = new ItemInstance(data);
        CurrentMood = Mood.Neutral;
        Patience = 3;
        door = Snap(doorPos);
        counter = Snap(counterPos);
        transform.position = door;
        gameObject.SetActive(true);
        route.Clear();

        int stops = Random.Range(browseStopsRange.x, browseStopsRange.y + 1);
        var pool = new List<Transform>(browsePoints);
        for (int i = 0; i < stops && pool.Count > 0; i++)
        {
            int k = Random.Range(0, pool.Count);
            route.Add(pool[k].position);
            pool.RemoveAt(k);
        }
        routeIndex = 0;

        CurrentPhase = Phase.Entering;
        NextLeg();
    }

    private void NextLeg()
    {
        if (routeIndex < route.Count)
        {
            CurrentPhase = Phase.Browsing;
            WalkTo(route[routeIndex++]);
        }
        else
        {
            CurrentPhase = Phase.WalkingToCounter;
            WalkTo(counter);
        }
    }

    public void Leave()
    {
        CurrentPhase = Phase.Leaving;
        WalkTo(door);
    }

    public bool Nudge(bool towardsTheirPrice)
    {
        if (towardsTheirPrice) 
        { 
            CurrentMood = Mood.Happy; return true; 
        }
        Patience--;
        CurrentMood = Patience <= 1 ? Mood.Annoyed : Mood.Neutral;
        return Patience > 0;
    }

    private void WalkTo(Vector2 pos)
    {
        target = Snap(pos);
        walking = true;
    }

    private void Update()
    {
        switch (CurrentPhase)
        {
            case Phase.Browsing:
            case Phase.WalkingToCounter:
            case Phase.Leaving:
                if (walking)
                {
                    Step();
                }
                else if (CurrentPhase == Phase.Browsing)
                {
                    pauseTimer -= Time.deltaTime;
                    if (pauseTimer <= 0f) 
                    {
                        NextLeg();
                    }
                }
                break;

            case Phase.Waiting:
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f)
                {
                    CurrentMood = Mood.Annoyed;
                    OnGaveUp?.Invoke(this);
                    Leave();
                }
                break;
        }
    }

    private void Arrived()
    {
        walking = false;
        switch (CurrentPhase)
        {
            case Phase.Browsing:
                pauseTimer = Random.Range(browseTimeRange.x, browseTimeRange.y);
                break;
            case Phase.WalkingToCounter:
                CurrentPhase = Phase.Waiting;
                waitTimer = patienceSeconds;
                break;
            case Phase.Leaving:
                CurrentPhase = Phase.Gone;
                gameObject.SetActive(false);
                OnLeft?.Invoke(this);
                break;
        }
    }

    private void Step()
    {
        Vector2 pos = transform.position;
        Vector2 delta = target - pos;

        Vector2 dir = Mathf.Abs(delta.x) > 0.001f ? new Vector2(Mathf.Sign(delta.x), 0f)
                    : Mathf.Abs(delta.y) > 0.001f ? new Vector2(0f, Mathf.Sign(delta.y))
                    : Vector2.zero;

        if (dir == Vector2.zero) 
        { 
            transform.position = target; Arrived(); return; 
        }

        subPixel += dir * speedPixelsPerSecond * Time.deltaTime;
        Vector2 whole = new Vector2(Mathf.Round(subPixel.x), Mathf.Round(subPixel.y));
        subPixel -= whole;

        Vector2 step = whole / pixelsPerUnit;
        if (Mathf.Abs(step.x) > Mathf.Abs(delta.x))
        {
            step.x = delta.x;
        }
        if (Mathf.Abs(step.y) > Mathf.Abs(delta.y))
        {
            step.y = delta.y;
        }
        transform.position = pos + step;
    }

    private Vector2 Snap(Vector2 p)
    {
        float s = pixelsPerUnit;
        return new Vector2(Mathf.Round(p.x * s) / s, Mathf.Round(p.y * s) / s);
    }
}