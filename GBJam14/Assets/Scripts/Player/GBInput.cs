using UnityEngine;
using UnityEngine.InputSystem;

public static class GBInput
{
    public static InputAction Move   { get; private set; }
    public static InputAction A      { get; private set; }
    public static InputAction B      { get; private set; }
    public static InputAction Select { get; private set; }
    public static InputAction Start  { get; private set; }

    public static Vector2 Direction => Move.ReadValue<Vector2>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        var asset = Resources.Load<InputActionAsset>("MainGBControls");
        if (asset == null)
        {
            Debug.LogError("GBInput missing");
            return;
        }

        Move   = asset.FindAction("Move",   throwIfNotFound: true);
        A      = asset.FindAction("A",      throwIfNotFound: true);
        B      = asset.FindAction("B",      throwIfNotFound: true);
        Select = asset.FindAction("Select", throwIfNotFound: true);
        Start  = asset.FindAction("Start",  throwIfNotFound: true);

        asset.Enable();
    }
}
