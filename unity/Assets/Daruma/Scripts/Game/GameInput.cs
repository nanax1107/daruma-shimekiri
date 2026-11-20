using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

// 新しい Input System と旧 Input Manager のどちらの設定でも動くようにする入力ラッパー
public enum GKey { W, A, S, D, Left, Right, Space, Q, F, Z, X, E, Enter, Escape }

public static class GameInput
{
#if ENABLE_INPUT_SYSTEM
    static KeyControl Ctrl(GKey k)
    {
        var kb = Keyboard.current;
        if (kb == null) return null;
        switch (k)
        {
            case GKey.W: return kb.wKey;
            case GKey.A: return kb.aKey;
            case GKey.S: return kb.sKey;
            case GKey.D: return kb.dKey;
            case GKey.Left: return kb.leftArrowKey;
            case GKey.Right: return kb.rightArrowKey;
            case GKey.Space: return kb.spaceKey;
            case GKey.Q: return kb.qKey;
            case GKey.F: return kb.fKey;
            case GKey.Z: return kb.zKey;
            case GKey.X: return kb.xKey;
            case GKey.E: return kb.eKey;
            case GKey.Enter: return kb.enterKey;
            default: return kb.escapeKey;
        }
    }
    public static bool Held(GKey k) { var c = Ctrl(k); return c != null && c.isPressed; }
    public static bool Down(GKey k) { var c = Ctrl(k); return c != null && c.wasPressedThisFrame; }
    // マウスの移動量（ピクセル）
    public static Vector2 MouseDelta() => Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
    public static float Scroll() => Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
    public static bool LeftDown() => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    public static bool LeftHeld() => Mouse.current != null && Mouse.current.leftButton.isPressed;
    public static bool RightHeld() => Mouse.current != null && Mouse.current.rightButton.isPressed;
#else
    static KeyCode Code(GKey k)
    {
        switch (k)
        {
            case GKey.W: return KeyCode.W;
            case GKey.A: return KeyCode.A;
            case GKey.S: return KeyCode.S;
            case GKey.D: return KeyCode.D;
            case GKey.Left: return KeyCode.LeftArrow;
            case GKey.Right: return KeyCode.RightArrow;
            case GKey.Space: return KeyCode.Space;
            case GKey.Q: return KeyCode.Q;
            case GKey.F: return KeyCode.F;
            case GKey.Z: return KeyCode.Z;
            case GKey.X: return KeyCode.X;
            case GKey.E: return KeyCode.E;
            case GKey.Enter: return KeyCode.Return;
            default: return KeyCode.Escape;
        }
    }
    public static bool Held(GKey k) => Input.GetKey(Code(k));
    public static bool Down(GKey k) => Input.GetKeyDown(Code(k));
    public static Vector2 MouseDelta() => new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 10f;
    public static float Scroll() => Input.mouseScrollDelta.y;
    public static bool LeftDown() => Input.GetMouseButtonDown(0);
    public static bool LeftHeld() => Input.GetMouseButton(0);
    public static bool RightHeld() => Input.GetMouseButton(1);
#endif
}
