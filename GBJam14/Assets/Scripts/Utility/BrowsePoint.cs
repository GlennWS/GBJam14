using UnityEngine;

public class BrowsePoint : MonoBehaviour
{
    public Shelf Shelf { get; private set; }

    private void Awake()
    {
        Shelf = GetComponentInParent<Shelf>();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
