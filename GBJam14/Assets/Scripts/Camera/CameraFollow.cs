using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private SpriteRenderer boundsSource;
    [SerializeField] private int pixelsPerUnit = 8;
    [SerializeField] private float smoothTime = 0f;

    private Camera mainCamera;
    private Vector3 velocity;

    private void Awake()
    {
        mainCamera = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);

        if (boundsSource != null)
        {
            Bounds b = boundsSource.bounds;
            float halfH = mainCamera.orthographicSize;
            float halfW = halfH * mainCamera.aspect;
            float pad = 1f / pixelsPerUnit;

            desired.x = b.size.x <= halfW * 2f ? b.center.x : Mathf.Clamp(desired.x, b.min.x + halfW + pad, b.max.x - halfW - pad);
            desired.y = b.size.y <= halfH * 2f ? b.center.y : Mathf.Clamp(desired.y, b.min.y + halfH + pad, b.max.y - halfH - pad);
        }

        Vector3 next = smoothTime > 0f ? Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime) : desired;

        float s = pixelsPerUnit;
        next.x = Mathf.Round(next.x * s) / s;
        next.y = Mathf.Round(next.y * s) / s;
        transform.position = next;
    }

    public void SetTarget(Transform t)
    {
        target = t;
    }
}