namespace dicefight;
// Windows 키 메시지의 물리 스캔 코드 사용: Num Lock과 무관하게 키패드를 식별.
public sealed class KeyboardInput
{
    private readonly HashSet<int> held = [], pressed = [];
    public const int Numpad0 = 0x52, NumpadDecimal = 0x53;
    public static int PhysicalCode(long lParam) => (int)((lParam >> 16) & 0xff) | ((lParam & (1L << 24)) != 0 ? 0x100 : 0);
    public void Change(int code, bool down)
    {
        if (down) { if (held.Add(code)) pressed.Add(code); }
        else held.Remove(code);
    }
    public void Clear() { held.Clear(); pressed.Clear(); }
    public void ClearEdges() => pressed.Clear();
    private bool Held(int key) => held.Contains(key);
    private bool Pressed(int key) => pressed.Contains(key);
    public PlayerCommand Read(int player)
    {
        int left = player == 0 ? 0x1e : 0x14b, right = player == 0 ? 0x20 : 0x14d;
        int jump = player == 0 ? 0x39 : Numpad0, attack = player == 0 ? 0x24 : NumpadDecimal;
        int high = player == 0 ? 0x11 : 0x148, low = player == 0 ? 0x1f : 0x150;
        return new((Held(right) ? 1 : 0) - (Held(left) ? 1 : 0), Pressed(jump), Pressed(attack),
            Held(high) && !Held(low) ? AttackHeight.High : Held(low) && !Held(high) ? AttackHeight.Low : AttackHeight.Middle, Held(low) && !Held(high), AttackHeld: Held(attack));
    }
}


