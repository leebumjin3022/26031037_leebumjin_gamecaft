namespace dicefight;
internal static class SmokeScenes
{
    public sealed record Scene(string Name,Func<GameSession> Create);
    private static void Wait(GameSession s,int count){for(int n=0;n<count;n++)s.Step();}
    private static GameSession Battle()
    {
        var s=new GameSession(42);s.Action();
        for(int n=0;n<4;n++){s.Action();Wait(s,140);s.Action();}
        Wait(s,361);s.Combat.Reset(7,13,100,100);return s;
    }
    public static List<Scene> Build()=>[
        new("main",()=>new GameSession()),
        new("dice",()=>{var s=new GameSession();s.Action();return s;}),
        new("dice-rolling",()=>{var s=new GameSession();s.Action();s.Action();Wait(s,60);return s;}),
        new("dice-result",()=>{var s=new GameSession();s.Action();s.Action();Wait(s,140);return s;}),
        new("battle",Battle),
        new("crouch",()=>{var s=Battle();for(int n=0;n<30;n++)s.Step(new(Crouch:true),new(Crouch:true));return s;}),
        new("walk",()=>{var s=Battle();for(int n=0;n<30;n++)s.Step(new(Move:1),new(Move:-1));return s;}),
        new("jump",()=>{var s=Battle();s.Step(new(Jump:true),default);Wait(s,25);return s;}),
        new("heavy-charge",()=>{var s=Battle();s.Step(new(Move:-1,Attack:true,AttackHeld:true),default);for(int n=0;n<65;n++)s.Step(new(AttackHeld:true),default);return s;}),
        new("heavy-hit",()=>{var s=Battle();var c=s.Combat;c.Players[0].X=500;c.Players[1].X=660;c.Players[0].Attack=new(AttackHeight.Middle,-GameBalance.HeavyStartup,1,heavy:true);c.Step(default,default);Wait(s,4);return s;}),
        new("dive",()=>{var s=Battle();s.Combat.Players[0].Y=350;s.Step(new(Attack:true),default);Wait(s,12);return s;}),
        new("projectile-blue",()=>{var s=Battle();s.Step(new(Attack:true),default);Wait(s,10);s.Step(new(Attack:true),default);Wait(s,45);return s;}),
        new("projectile-red",()=>{var s=Battle();s.Step(default,new(Attack:true));Wait(s,10);s.Step(default,new(Attack:true));Wait(s,45);return s;}),
        new("clash",()=>{var s=Battle();s.Combat.Players[0].X=500;s.Combat.Players[1].X=600;s.Step(new(Attack:true),new(Attack:true));Wait(s,48);return s;}),
        new("blue-win",()=>{var s=Battle();s.Combat.Players[0].X=500;s.Combat.Players[1].X=600;s.Combat.Players[1].Hp=1;s.Step(new(Attack:true),default);Wait(s,240);return s;}),
        new("red-win",()=>{var s=Battle();s.Combat.Players[0].X=500;s.Combat.Players[1].X=600;s.Combat.Players[0].Hp=1;s.Step(default,new(Attack:true));Wait(s,240);return s;}),
        new("rematch",()=>{var s=Battle();s.Combat.Players[0].X=500;s.Combat.Players[1].X=600;s.Combat.Players[1].Hp=1;s.Step(new(Attack:true),default);Wait(s,240);s.Action();return s;})
    ];
}
