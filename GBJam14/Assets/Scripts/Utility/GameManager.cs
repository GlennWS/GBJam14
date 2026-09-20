using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum State { Title, Closed, Trading, Appraise, Haggle, Restore, Stock, BuyerHaggle, Summary, GameOver }

    public static GameManager Instance { get; private set; }

    [SerializeField] private CleaningMinigame cleaning;
    [SerializeField] private AppraisalMinigame appraisal;

    [SerializeField] private PlayerController player;
    [SerializeField] private Customer customer;
    [SerializeField] private Transform doorPoint;
    [SerializeField] private Transform counterPoint;
    [SerializeField] private GameObject messageStrip;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private ScreenWipe wipe;
    [SerializeField] private GameObject minigameBackdrop;

    [SerializeField] private List<ItemData> itemPool = new List<ItemData>();
    [SerializeField] private int customersPerDay = 3;
    [SerializeField] private float secondsBetweenCustomers = 4f;
    [SerializeField, Range(0f, 1f)] private float buyerChance = 0.5f;
    [SerializeField] private int startingMoney = 100;
    [SerializeField] private int rent = 60;
    [SerializeField] private int rentEveryDays = 3;
    [SerializeField] private float serveRadius = 2f;

    private List<BrowsePoint> browsePoints = new List<BrowsePoint>();
    private List<Shelf> shelves = new List<Shelf>();

    public State Current { get; private set; }
    public int Money { get; private set; }
    public int Day { get; private set; } = 1;

    private int customersSpawned, customersDone;
    private float spawnTimer;
    private int offer;
    private System.Action afterText;
    private bool paging;
    private bool showingPrompt;
    private float messageTimer;

    [SerializeField] private GameObject titlePanel;
    [SerializeField] private TextMeshProUGUI titlePrompt;
    [SerializeField] private float titleBlink = 0.6f;
    private float titleBlinkTimer;

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
        browsePoints.AddRange(FindObjectsByType<BrowsePoint>(FindObjectsSortMode.None));
        shelves.AddRange(FindObjectsByType<Shelf>(FindObjectsSortMode.None));
        Enter(State.Title);
    }

    private void Update()
    {
        if (paging && !MinigameRunning() && GBInput.A.WasPressedThisFrame())
        {
            if (messageText.pageToDisplay < messageText.textInfo.pageCount)
            {
                messageText.pageToDisplay++;
            }
            else
            {
                paging = false;
                var cb = afterText;
                afterText = null;
                cb?.Invoke();
            }
            return;
        }

        switch (Current)
        {
            case State.Title:
                titleBlinkTimer += Time.deltaTime;
                if (titleBlinkTimer >= titleBlink)
                {
                    titleBlinkTimer = 0f;
                    if (titlePrompt != null) titlePrompt.enabled = !titlePrompt.enabled;
                }
                if (GBInput.Start.WasPressedThisFrame() || GBInput.A.WasPressedThisFrame())
                {
                    titlePanel.SetActive(false);
                    wipe.Play(() => Enter(State.Closed));
                }
                break;
            case State.Trading:
                UpdateTrading();
                break;
            case State.Haggle:
                HandleSellerHaggle();
                break;
            case State.BuyerHaggle:
                HandleBuyerHaggle();
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
                SpawnCustomer();
                customersSpawned++;
                spawnTimer = secondsBetweenCustomers;
            }
        }

        bool canServe = customer.IsWaiting &&
                        Vector2.Distance(player.transform.position, counterPoint.position) <= serveRadius;

        if (canServe && !showingPrompt)
        {
            Say("~ [A] Serve Customer ~");
            showingPrompt = true;
        }
        if (!canServe && showingPrompt)
        {
            Hide();
            showingPrompt = false;
        }
        if (canServe && GBInput.A.WasPressedThisFrame())
        {
            customer.WaitingPaused = true;
            showingPrompt = false;
            Enter(customer.CurrentKind == Customer.Kind.Buyer ? State.BuyerHaggle : State.Appraise);
        }
    }

    private void SpawnCustomer()
    {
        var stocked = shelves.FindAll(s => s.HasStock);
        if (stocked.Count > 0 && Random.value < buyerChance)
        {
            Shelf shelf = stocked[Random.Range(0, stocked.Count)];
            customer.SpawnBuyer(shelf.RandomItem(), shelf, doorPoint.position, counterPoint.position, browsePoints);
        }
        else
        {
            ItemData data = itemPool[Random.Range(0, itemPool.Count)];
            customer.SpawnSeller(data, doorPoint.position, counterPoint.position, browsePoints);
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
            case State.Title:
                Money = startingMoney;
                Day = 1;
                titlePanel.SetActive(true);
                break;

            case State.Closed:
                Tell($"Day: {Day}. £{Money}\n~ Open the shop ~", Advance);
                break;

            case State.Trading:
                Hide();
                break;

            case State.Appraise:
                {
                    var it = customer.Item;
                    Tell($"\"{it.data.description}\"\nThe customer wants £{it.AskingPrice}.", () =>
                        wipe.Play(
                            () =>
                            {
                                Hide();
                                paging = false;
                                afterText = null;
                                minigameBackdrop.SetActive(true);
                                appraisal.Begin(it, () => Enter(State.Haggle));
                            },
                            () => appraisal.BeginPlay()));
                    break;
                }

            case State.Haggle:
                customer.Item.appraised = true;
                offer = customer.Item.AskingPrice;
                ShowSellerHaggle();
                break;

            case State.Restore:
                Hide();
                paging = false;
                afterText = null;
                messageText.text = "";
                cleaning.Begin(customer.Item, () => WipeToShop(() => Enter(State.Stock)));
                cleaning.BeginPlay();
                break;

            case State.Stock:
                {
                    var item = customer.Item;
                    Shelf free = shelves.Find(sh => sh.HasFreeSlot);
                    if (free != null)
                    {
                        free.Place(item);
                        Tell($"{item.data.displayName} goes on the shelf.\nWorth ~£{item.SellValue} now.", Advance);
                    }
                    else
                    {
                        int scrap = Mathf.Max(1, item.SellValue / 2);
                        Money += scrap;
                        Tell($"No shelf space!\nSold {item.data.displayName} for scrap: £{scrap}.", Advance);
                    }
                    break;
                }

            case State.BuyerHaggle:
                offer = customer.Item.SellValue;
                ShowBuyerHaggle();
                break;

            case State.Summary:
                {
                    string rentLine = "";
                    if (Day % rentEveryDays == 0)
                    {
                        Money -= rent;
                        rentLine = $"\nRent paid: £{rent}. Sad paying rent in game";
                    }
                    if (Money < 0)
                    {
                        Tell($"Shop closed.\nMoney: £{Money}{rentLine}", () => Enter(State.GameOver));
                    }
                    else
                    {
                        Tell($"Shop closed.\nMoney: £{Money}{rentLine}\n~ Next day ~", Advance);
                    }
                    break;
                }

            case State.GameOver:
                Tell($"You can't pay the rent.\nThe shop is lost after {Day} days.\n~ Try again ~",
                     () => wipe.Play(() => Enter(State.Title)));
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
            case State.Stock:
                FinishServing();
                break;
            case State.Summary:
                Day++;
                Enter(State.Closed);
                break;
        }
    }

    private bool MinigameRunning() => cleaning.IsRunning || appraisal.IsRunning;

    private void FinishServing()
    {
        customer.WaitingPaused = false;
        customer.Leave();
        Enter(State.Trading);
    }

    private void HandleSellerHaggle()
    {
        var item = customer.Item;

        if (GBInput.Move.WasPressedThisFrame())
        {
            float y = GBInput.Direction.y;
            if (y > 0f)
            {
                offer += 5;
                customer.Nudge(true);
                ShowSellerHaggle();
            }
            else if (y < 0f)
            {
                offer = Mathf.Max(1, offer - 5);
                if (!customer.Nudge(false))
                {
                    Say("The customer is so offended by your low ball offer and leaves", 2f);
                    FinishServing();
                    return;
                }
                ShowSellerHaggle();
            }
        }

        if (GBInput.A.WasPressedThisFrame())
        {
            if (offer >= item.AskingPrice * 0.7f && offer <= Money)
            {
                Money -= offer;
                item.paidFor = offer;
                Enter(State.Restore);
            }
            else if (offer > Money)
            {
                Say("You are too broke for that", 1.5f);
            }
            else if (!customer.Nudge(false))
            {
                Say("\"Too low!\" They leave.", 2f);
                FinishServing();
            }
            else
            {
                ShowSellerHaggle();
            }
        }

        if (GBInput.B.WasPressedThisFrame())
        {
            Say("You decline the offer and tell the customer to never come back.", 1.5f);
            FinishServing();
        }
    }

    private void ShowSellerHaggle()
    {
        paging = false;
        messageText.pageToDisplay = 1;
        Say($"{customer.Item.data.displayName}  worth ~£{customer.Item.AppraisedValue}\nOffer £{offer}  {MoodFace()}\n~ [Up/Down] Adjust  [A] Deal!  [B] Decline ~");
    }

    private void HandleBuyerHaggle()
    {
        var item = customer.Item;
        int fair = item.SellValue;

        if (GBInput.Move.WasPressedThisFrame())
        {
            float y = GBInput.Direction.y;
            if (y > 0f)
            {
                offer += 5;
                if (!customer.Nudge(false))
                {
                    Say("\"Daylight robbery!\" They storm out.", 2f);
                    FinishServing();
                    return;
                }
                ShowBuyerHaggle();
            }
            else if (y < 0f)
            {
                offer = Mathf.Max(1, offer - 5);
                customer.Nudge(true);
                ShowBuyerHaggle();
            }
        }

        if (GBInput.A.WasPressedThisFrame())
        {
            float ceiling = customer.CurrentMood == Customer.Mood.Annoyed ? 1.1f : 1.3f;
            if (offer <= fair * ceiling)
            {
                Money += offer;
                customer.ItemShelf.Remove(item);
                Tell($"Sold {item.data.displayName} for £{offer}!\n(You paid £{item.paidFor}.)", FinishServing);
            }
            else if (!customer.Nudge(false))
            {
                Say("\"Too much!\" They leave.", 2f);
                FinishServing();
            }
            else
            {
                ShowBuyerHaggle();
            }
        }

        if (GBInput.B.WasPressedThisFrame())
        {
            Say("\"Not for sale.\"", 1.5f);
            FinishServing();
        }
    }

    private void ShowBuyerHaggle()
    {
        paging = false;
        messageText.pageToDisplay = 1;
        Say($"They want {customer.Item.data.displayName}.\nAsk £{offer}  {MoodFace()}\n~ [Up/Down] Adjust  [A] Sell  [B] Refuse ~");
    }

    private string MoodFace()
    {
        return customer.CurrentMood switch
        {
            Customer.Mood.Happy => ":)",
            Customer.Mood.Annoyed => ">:(",
            _ => ":|"
        };
    }

    private void Say(string msg, float seconds = 0f)
    {
        messageTimer = seconds;
        if (messageText != null)
        {
            messageText.text = msg;
            messageStrip.SetActive(true);
        }
        else Debug.Log(msg);
    }

    private void Hide()
    {
        messageTimer = 0f;
        if (messageStrip != null) messageStrip.SetActive(false);
    }

    private void Tell(string msg, System.Action onDone)
    {
        Say(msg);
        if (messageText != null)
        {
            messageText.pageToDisplay = 1;
            messageText.ForceMeshUpdate();
        }
        afterText = onDone;
        paging = true;
    }

    private void WipeToShop(System.Action swap)
    {
        wipe.Play(() =>
        {
            minigameBackdrop.SetActive(false);
            swap();
        });
    }
}