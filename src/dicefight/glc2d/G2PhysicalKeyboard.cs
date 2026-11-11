// DiceFight extension: preserve numpad scan codes regardless of Num Lock.
using System.Windows.Forms;
sealed class G2PhysicalKeyboard : NativeWindow, IDisposable
{
    private readonly HashSet<int> held=[],pending=[],down=[];
    public G2PhysicalKeyboard(Form window)=>AssignHandle(window.Handle);
    protected override void WndProc(ref Message m)
    {
        if(m.Msg is 0x100 or 0x104 or 0x101 or 0x105)
        {
            long value=m.LParam.ToInt64();
            int code=(int)((value>>16)&255)|((value&(1L<<24))!=0?256:0);
            if(m.Msg is 0x100 or 0x104){if(held.Add(code))pending.Add(code);}
            else held.Remove(code);
        }
        if(m.Msg==0x0008)Reset();
        base.WndProc(ref m);
    }
    public void Update(){down.Clear();down.UnionWith(pending);pending.Clear();}
    public bool IsHeld(int code)=>held.Contains(code);
    public bool IsDown(int code)=>down.Contains(code);
    public void Reset(){held.Clear();pending.Clear();down.Clear();}
    public void Dispose(){Reset();ReleaseHandle();}
}
