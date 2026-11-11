namespace dicefight;
internal sealed class GlcGameInput
{
    private readonly KeyboardInput keys=new();
    private static readonly int[] Codes=[0x1e,0x20,0x39,0x24,0x11,0x1f,0x14b,0x14d,0x52,0x53,0x148,0x150];
    public void Poll(G2InputContext input)
    {foreach(int code in Codes)keys.Change(code,input.IsPhysicalKeyHeld(code)||input.IsPhysicalKeyDown(code));}
    public PlayerCommand Read(int player)=>keys.Read(player);
    public void ClearEdges()=>keys.ClearEdges();
    public void Clear()=>keys.Clear();
}
