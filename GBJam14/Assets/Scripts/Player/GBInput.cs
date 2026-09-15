using UnityEngine;
using UnityEngine.InputSystem;

public static class GBInput
{
    public static InputAction Move { get; private set; }
    public static InputAction A { get; private set; }
    public static InputAction B { get; private set; }
    public static InputAction Start { get; private set; }
    public static InputAction Select { get; private set; }

    private static void Init()
    {
        Move = new InputAction("Move", InputActionType.Value);
        A = new InputAction("A", InputActionType.Button);
        B = new InputAction("B", InputActionType.Button);
        Start = new InputAction("Start", InputActionType.Button);
        Select = new InputAction("Select", InputActionType.Button);

        Move.Enable();
        A.Enable();
        B.Enable();
        Start.Enable();
        Select.Enable();
    }
}