namespace dicefight;
// 두 번 누름과 단타를 확정한 뒤 Combat.Step에 전달한다.
public sealed class AttackInputResolver
{
    private double? firstPress;
    private AttackHeight pendingHeight;
    public void Clear() => firstPress=null;
    public PlayerCommand Resolve(PlayerCommand raw, Fighter player, Fighter other, double now)
    {
        var command=raw with { Attack=false, Special=false };
        if(player.Stun>0 || player.Charging || player.Attack!=null || raw.Jump)
        { Clear(); return command; }
        if(!player.Grounded)
        { Clear(); return command with { Attack=raw.Attack }; }
        if(raw.Attack)
        {
            int facing=other.X>=player.X?1:-1;
            if(raw.Move==-facing)
            { Clear(); return command with { Attack=true }; }
            if(firstPress is double first && now-first<=GameBalance.DoubleTapWindow+1e-8)
            {
                Clear();
                return command with { Attack=true, Move=0, Special=player.SpecialCooldown<=1e-8 };
            }
            if(firstPress!=null)
            { Clear(); return command with { Attack=true, Move=0, Height=pendingHeight }; }
            firstPress=now; pendingHeight=raw.Height;
        }
        if(firstPress is double started && now-started>=GameBalance.DoubleTapWindow-1e-8)
        { Clear(); return command with { Attack=true, Move=0, Height=pendingHeight }; }
        return command;
    }
}
