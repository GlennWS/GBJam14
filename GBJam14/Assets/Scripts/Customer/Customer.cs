using System.Collections.Generic;
using UnityEngine;

public class Customer : MonoBehaviour
{
    public enum Kind { Seller, Buyer }
    public enum Mood { Happy, Neutral, Annoyed }
    public enum Phase { Entering, Browsing, WalkingToCounter, Waiting, Leaving, Gone }

    [SerializeField] private float speedPixelsPerSecond = 48f;
    [SerializeField] private int pixelsPerUnit = 8;
    [SerializeField] private Vector2 browseTimeRange = new Vector2(1.5f, 3f);
    [SerializeField] private Vector2Int browseStopsRange = new Vector2Int(1, 2);
    [SerializeField] private float patienceSeconds = 20f;

    public Kind CurrentKind { get; private set; }
    public ItemInstance Item { get; private set; }
    public Shelf ItemShelf { get; private set; }
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
    public bool WaitingPaused { get; set; }
    private Vector2 subPixel;
    private Animator anim;
    private Vector2 facing = Vector2.down;
    private static int MoveX = Animator.StringToHash("MoveX");
    private static int MoveY = Animator.StringToHash("MoveY");
    private static int Moving = Animator.StringToHash("Moving");

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void SpawnSeller(ItemData data, Vector2 doorPos, Vector2 counterPos, IList<BrowsePoint> browsePoints)
    {
        CurrentKind = Kind.Seller;
        Item = new ItemInstance(data);
        ItemShelf = null;
        Begin(doorPos, counterPos, PickStops(browsePoints, null));
    }

    public void SpawnBuyer(ItemInstance wanted, Shelf shelf, Vector2 doorPos, Vector2 counterPos, IList<BrowsePoint> browsePoints)
    {
        CurrentKind = Kind.Buyer;
        Item = wanted;
        ItemShelf = shelf;

        BrowsePoint dest = null;
        foreach (var bp in browsePoints)
        {
            if (bp.Shelf == shelf)
            {
                dest = bp;
                break;
            }
        }
        Begin(doorPos, counterPos, PickStops(browsePoints, dest));
    }

    private List<Vector2> PickStops(IList<BrowsePoint> browsePoints, BrowsePoint mustEndAt)
    {
        var stops = new List<Vector2>();
        var pool = new List<BrowsePoint>(browsePoints);
        if (mustEndAt != null)
        {
            pool.Remove(mustEndAt);
        }

        int n = Random.Range(browseStopsRange.x, browseStopsRange.y + 1);
        if (mustEndAt != null)
        {
            n = Mathf.Max(0, n - 1);
        }
        for (int i = 0; i < n && pool.Count > 0; i++)
        {
            int k = Random.Range(0, pool.Count);
            stops.Add(pool[k].transform.position);
            pool.RemoveAt(k);
        }
        if (mustEndAt != null)
        {
            stops.Add(mustEndAt.transform.position);
        }
        return stops;
    }

    private void Begin(Vector2 doorPos, Vector2 counterPos, List<Vector2> stops)
    {
        CurrentMood = Mood.Neutral;
        Patience = 3;
        door = Snap(doorPos);
        counter = Snap(counterPos);
        transform.position = door;
        gameObject.SetActive(true);

        route.Clear();
        route.AddRange(stops);
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

    public bool Nudge(bool inTheirFavour)
    {
        if (inTheirFavour)
        {
            CurrentMood = Mood.Happy;
            return true;
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
                if (WaitingPaused)
                {
                    break;
                }
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f)
                {
                    CurrentMood = Mood.Annoyed;
                    OnGaveUp?.Invoke(this);
                    Leave();
                }
                break;
        }

        if (anim != null)
        {
            anim.SetFloat(MoveX, facing.x);
            anim.SetFloat(MoveY, facing.y);
            anim.SetBool(Moving, walking);
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
            transform.position = target;
            Arrived();
            return;
        }

        if (dir.x < 0f)
        {
            facing = Vector2.left;
        }
        else if (dir.x > 0f)
        {
            facing = Vector2.right;
        }
        else if (dir.y < 0f)
        {
            facing = Vector2.down;
        }
        else if (dir.y > 0f)
        {
            facing = Vector2.up;
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