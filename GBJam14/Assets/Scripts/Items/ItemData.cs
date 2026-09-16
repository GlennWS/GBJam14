using UnityEngine;

[CreateAssetMenu(menuName = "Old Gold/Item", fileName = "Item_")]
public class ItemData : ScriptableObject
{
    public string displayName = "";
    [TextArea] public string description = "";

    public Sprite shelfSprite;
    public Sprite closeUpSprite;

    public int baseValue = 50;
    [Range(0.1f, 1.5f)] public float askingRatio = 0.6f;

    [Range(0, 3)] public int tells = 1;
    public bool isFake = false;
    public bool isKeepsake = false;

    [Range(0f, 1f)] public float minStartCondition = 0.2f;
    [Range(0f, 1f)] public float maxStartCondition = 0.8f;
}

[System.Serializable]
public class ItemInstance
{
    public ItemData data;
    [Range(0f, 1f)] public float condition;
    public bool appraised;
    public int paidFor;

    public ItemInstance(ItemData d)
    {
        data = d;
        condition = Random.Range(d.minStartCondition, d.maxStartCondition);
    }

    public int SellValue => data.isFake ? Mathf.RoundToInt(data.baseValue * 0.1f)
                                        : Mathf.RoundToInt(data.baseValue * Mathf.Lerp(0.3f, 1f, condition));

    public int AskingPrice => Mathf.Max(1, Mathf.RoundToInt(data.baseValue * data.askingRatio));
}
