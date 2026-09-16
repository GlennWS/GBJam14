using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public enum State { Closed, Trading, Appraise, Haggle, Restore, Sell, Summary }

    public static GameManager Instance { get; private set; }

    [SerializeField] private PlayerController player;
    [SerializeField] private Customer customer;
    [SerializeField] private Transform doorPoint;
    [SerializeField] private Transform counterPoint;
    [SerializeField] private TextMeshProUGUI messageText;

    [SerializeField] private List<ItemData> itemPool = new List<ItemData>();
    [SerializeField] private int customersPerDay = 3;
    [SerializeField] private float secondsBetweenCustomers = 4f;
    [SerializeField] private int startingMoney = 100;
    [SerializeField] private int rent = 60;
    [SerializeField] private int rentEveryDays = 3;
    [SerializeField] private float serveRadius = 2f;

    private List<Transform> browsePoints = new List<Transform>();

    public State Current { get; private set; }
    public int Money { get; private set; }
    public int Day { get; private set; } = 1;
    public List<ItemInstance> Stock { get; } = new List<ItemInstance>();

    private int customersSpawned, customersDone;
    private float spawnTimer;
    private int offer;
    private bool waitingForA;
    private bool showingPrompt;

    private void Awake()
    {
        Instance = this;
        Money = startingMoney;
    }

    private void Start()
    {
        customer.gameObject.SetActive(false);
        customer.OnLeft += _ => { customersDone++; CheckDayEnd(); };
        customer.OnGaveUp += _ => Say("The customer got tired of waiting of you being SLOOOOW!!", 2f);
        foreach (var bp in FindObjectsByType<BrowsePoint>(FindObjectsSortMode.None))
        {
            browsePoints.Add(bp.transform);
        }
        Enter(State.Closed);
    }

    private void Update()
    {
        if (waitingForA && GBInput.A.WasPressedThisFrame())
        {
            waitingForA = false;
            Advance();
            return;
        }

        switch (Current)
        {
            case State.Trading: 
                UpdateTrading(); 
                break;
            case State.Haggle: 
                HandleHaggleInput(); 
                break;
        }

        if (messageTimer > 0f && (messageTimer -= Time.deltaTime) <= 0f)
        {
            Hide();
        }
    }

    private void UpdateTrading()
    {
        if (customersSpawned < customersPerDay && !customer.gameObject.activeSelf)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                ItemData data = itemPool[Random.Range(0, itemPool.Count)];
                customer.Spawn(data, doorPoint.position, counterPoint.position, browsePoints);
                customersSpawned++;
                spawnTimer = secondsBetweenCustomers;
            }
        }

        bool canServe = customer.IsWaiting &&
                        Vector2.Distance(player.transform.position, counterPoint.position) <= serveRadius;

        if (canServe && !showingPrompt) 
        { 
            Say("~ [A] Serve Customer ~"); showingPrompt = true; 
        }
        if (!canServe && showingPrompt) 
        { 
            Hide(); showingPrompt = false; 
        }
        if (canServe && GBInput.A.WasPressedThisFrame())
        {
            showingPrompt = false;
            Enter(State.Appraise);
        }
    }

    private void CheckDayEnd()
    {
        if (Current != State.Trading)
        {
            return;
        }
        if (customersDone >= customersPerDay)
        {
            Enter(State.Summary);
        }
    }

    private void Enter(State s)
    {
        Current = s;
        player.InputEnabled = s == State.Trading;

        switch (s)
        {
            case State.Closed:
                Say($"Day: {Day}. £{Money}\n~ [A] Open the shop ~");
                waitingForA = true;
                break;

            case State.Trading:
                Hide();
                break;

            case State.Appraise:
                var it = customer.Item;
                Say($"\"{it.data.description}\"\nThe customer wants £{it.AskingPrice}.\n~ [A] Appraise ~");
                waitingForA = true;
                break;

            case State.Haggle:
                customer.Item.appraised = true;
                offer = customer.Item.AskingPrice;
                ShowHaggle();
                break;

            case State.Restore:
                Say($"Item Condition: {Mathf.RoundToInt(customer.Item.condition * 100)}%\n~ [A] Clean it up ~");
                waitingForA = true;
                break;

            case State.Sell:
                int price = customer.Item.SellValue;
                Money += price;
                Say($"Sold {customer.Item.data.displayName} for £{price}.\n~ [A] Done ~");
                waitingForA = true;
                break;

            case State.Summary:
                string rentLine = "";
                if (Day % rentEveryDays == 0) 
                { 
                    Money -= rent; 
                    rentLine = $"\nRent paid: £{rent}. Sad paying rent in game"; 
                }
                Say($"Shop closed.\nMoney: £{Money}{rentLine}\n~ [A] Next day ~");
                waitingForA = true;
                break;
        }
    }

    private void Advance()
    {
        switch (Current)
        {
            case State.Closed:
                customersSpawned = 0; 
                customersDone = 0; 
                spawnTimer = 1f;
                Enter(State.Trading);
                break;
            case State.Appraise: 
                Enter(State.Haggle); 
                break;
            case State.Restore: 
                Enter(State.Sell); 
                break;
            case State.Sell: 
                FinishServing(); 
                break;
            case State.Summary: Day++; 
                Enter(State.Closed); 
                break;
        }
    }

    private void FinishServing()
    {
        customer.Leave();
        Enter(State.Trading);
    }

    private void HandleHaggleInput()
    {
        var item = customer.Item;

        if (GBInput.Move.WasPressedThisFrame())
        {
            float y = GBInput.Direction.y;
            if (y > 0f) 
            { 
                offer += 5; 
                customer.Nudge(true); 
                ShowHaggle(); 
            }
            else
            {
                offer = Mathf.Max(1, offer - 5);
                if (!customer.Nudge(false))
                {
                    Say("The customer is so offended by your low ball offer and leaves", 2f);
                    FinishServing();
                    return;
                }
                ShowHaggle();
            }
        }

        if (GBInput.A.WasPressedThisFrame())
        {
            if (offer >= item.AskingPrice * 0.7f && offer <= Money)
            {
                Money -= offer;
                item.paidFor = offer;
                Stock.Add(item);
                Enter(State.Restore);
            }
            else if (offer > Money)
            {
                Say("You are too broke for that", 1.5f);
            }
            else
            {
                if (!customer.Nudge(false)) 
                { 
                    Say("\"Too low!\" They leave.", 2f); 
                    FinishServing(); 
                }
                else ShowHaggle();
            }
        }

        if (GBInput.B.WasPressedThisFrame())
        {
            Say("You decline the offer and tell the customer to never come back.", 1.5f);
            FinishServing();
        }
    }

    private void ShowHaggle()
    {
        string mood = customer.CurrentMood switch
        {
            Customer.Mood.Happy => ":)",
            Customer.Mood.Annoyed => ">:(",
            _ => ":|"
        };
        Say($"{customer.Item.data.displayName}  worth ~£{customer.Item.SellValue}\nOffer £{offer}  {mood}\n~ [Up/Down] Adjust  [A] Deal!  [B] Decline ~");
    }

    private float messageTimer;

    private void Say(string msg, float seconds = 0f)
    {
        messageTimer = seconds;
        if (messageText != null)
        {
            messageText.text = msg;
            messageText.enabled = true;
        }
        else
        {
            Debug.Log(msg);
        }
    }

    private void Hide()
    {
        messageTimer = 0f;
        if (messageText != null)
        {
            messageText.enabled = false;
        }
    }
}