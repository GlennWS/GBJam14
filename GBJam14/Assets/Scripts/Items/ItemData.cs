using System.Collections.Generic;
using UnityEngine;

public enum TellKind { UniqueMark, Crack, Fake }

[System.Serializable]
public class Tell
{
    public TellKind kind;
    public Vector2Int pixel;

    public float Multiplier => kind switch
    {
        TellKind.UniqueMark => 1.5f,
        TellKind.Crack => 0.6f,
        TellKind.Fake => 0.1f,
        _ => 1f
    };

    public string Label => kind switch
    {
        TellKind.UniqueMark => "A maker's mark!",
        TellKind.Crack => "A hairline crack.",
        TellKind.Fake => "It's a fake!",
        _ => ""
    };
}

[CreateAssetMenu(menuName = "Old Gold/Item", fileName = "Item_")]
public class ItemData : ScriptableObject
{
    public string displayName = "Old Ring";
    [TextArea] public string description = "A ring with a worn inscription.";

    public Sprite shelfSprite;
    public Sprite[] conditionFrames;

    public int baseValue = 50;
    [Range(0.1f, 1.5f)] public float askingRatio = 0.6f;
    public bool isKeepsake = false;

    public List<Tell> tells = new List<Tell>();

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
    public List<Tell> foundTells = new List<Tell>();

    public ItemInstance(ItemData d)
    {
        data = d;
        condition = Random.Range(d.minStartCondition, d.maxStartCondition);
    }

    public int SellValue => Value(data.tells);

    public int AppraisedValue => Value(foundTells);

    public int AskingPrice => Mathf.Max(1, Mathf.RoundToInt(data.baseValue * data.askingRatio));

    private int Value(List<Tell> tells)
    {
        float v = data.baseValue * Mathf.Lerp(0.3f, 1f, condition);
        foreach (var t in tells)
        {
            v *= t.Multiplier;
        }
        return Mathf.Max(1, Mathf.RoundToInt(v));
    }
}