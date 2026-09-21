using System.Collections.Generic;
using UnityEngine;

public class Shelf : MonoBehaviour
{
    [SerializeField] private Transform[] slotPoints;
    [SerializeField] private string iconSortingLayer = "Objects";
    [SerializeField] private int iconSortingOrder = 0;

    private ItemInstance[] items;
    private SpriteRenderer[] icons;

    public int Capacity => slotPoints.Length;
    public bool HasFreeSlot => FreeIndex() >= 0;
    public bool HasStock => Count > 0;
    public int Count { get { int n = 0; foreach (var i in items) if (i != null) n++; return n; } }

    private void Awake()
    {
        items = new ItemInstance[slotPoints.Length];
        icons = new SpriteRenderer[slotPoints.Length];
        for (int i = 0; i < slotPoints.Length; i++)
        {
            var go = new GameObject("Icon");
            go.transform.SetParent(slotPoints[i], false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.material = GetComponent<SpriteRenderer>().material;
            sr.sortingLayerName = iconSortingLayer;
            sr.sortingOrder = iconSortingOrder;
            sr.enabled = false;
            icons[i] = sr;
        }
    }

    public bool Place(ItemInstance item)
    {
        int i = FreeIndex();
        if (i < 0)
        {
            return false;
        }
        items[i] = item;
        icons[i].sprite = item.data.shelfSprite;
        icons[i].enabled = item.data.shelfSprite != null;
        return true;
    }

    public bool Remove(ItemInstance item)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != item)
            {
                continue;
            }
            items[i] = null;
            icons[i].enabled = false;
            return true;
        }
        return false;
    }

    public ItemInstance RandomItem()
    {
        var stocked = new List<ItemInstance>();
        foreach (var i in items)
        {
            if (i != null)
            {
                stocked.Add(i);
            }
        }
        return stocked.Count == 0 ? null : stocked[Random.Range(0, stocked.Count)];
    }

    private int FreeIndex()
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
            {
                return i;
            }
        }
        return -1;
    }
}
