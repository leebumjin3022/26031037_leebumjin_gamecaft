namespace dicefight;
internal static class HeavyAttackTests
{
    public static int Run(List<string> report)
    {
        int failed = 0;
        void Test(string name, Action action) { try { action(); report.Add("PASS " + name); } catch (Exception e) { failed++; report.Add("FAIL " + name + ": " + e.Message); } }
        void Check(bool condition) { if (!condition) throw new Exception("조건 불일치"); }
        Combat Duel() { var c = new Combat(); c.Reset(7, 13, 100, 100); c.Players[0].X = 500; c.Players[1].X = 600; return c; }
        void Wait(Combat c, int count = 80) { for (int i = 0; i < count; i++) c.Step(default, default); }
        Test("강공격: 실제 키 입력과 상대 위치별 후방", () => {
            foreach (int player in new[] { 0, 1 }) foreach (int facing in new[] { -1, 1 }) {
                var c = Duel(); var keys = new KeyboardInput();
                c.Players[player].X = 600; c.Players[1-player].X = 600 + facing * 200;
                keys.Change(player == 0 ? (facing == 1 ? 0x1e : 0x20) : (facing == 1 ? 0x14b : 0x14d), true);
                keys.Change(player == 0 ? 0x24 : KeyboardInput.NumpadDecimal, true);
                c.Step(keys.Read(0), keys.Read(1));
                Check(c.Players[player].Charging && c.Players[player].ChargeDirection == facing);
            }
        });
        Test("강공격: 0.5초 이상 유지/이동 점프 금지/해제 피해", () => {
            var c = Duel(); c.Step(new(Move:-1, Attack:true, AttackHeld:true), default);
            for (int n=0;n<90;n++) c.Step(new(Move:1, Jump:true, AttackHeld:true), default);
            Check(c.Players[0].Charging && c.Players[0].X==500 && c.Players[0].Grounded && c.Players[1].Hp==100);
            c.Step(default, default); Check(c.Players[0].Attack?.Heavy==true);
            Wait(c,25); Check(c.Players[1].Hp==89 && c.HitEffects.Single().Heavy);
            Wait(c); Check(c.Players[1].Hp==89);
        });
        Test("강공격: 조기 해제/차징 중 피격 취소", () => {
            var c=Duel(); c.Step(new(Move:-1,Attack:true,AttackHeld:true), default); Wait(c,20);
            Check(!c.Players[0].Charging && c.Players[0].Attack==null && c.Players[1].Hp==100);
            c=Duel(); c.Step(new(Move:-1,Attack:true,AttackHeld:true),new(Attack:true));
            for(int n=0;n<22;n++) c.Step(new(AttackHeld:true),default);
            Check(!c.Players[0].Charging && c.Players[0].Hp==87); Wait(c);
            Check(c.Players[1].Hp==100);
        });
        Test("강공격: 긴 사거리/반올림/동일 넉백", () => {
            Check(GameBalance.HeavyDamage(5)==8 && GameBalance.HeavyDamage(7)==11);
            var c=Duel(); c.Players[1].X=680;
            c.Players[0].Attack=new(AttackHeight.Middle,-GameBalance.HeavyStartup,1,heavy:true);
            c.Step(default,default); Check(c.Players[1].Hp==89 && c.Players[1].Knockback==GameBalance.KnockbackSpeed);
            c=Duel(); c.Players[1].X=680; c.Step(new(Attack:true),default); Wait(c); Check(c.Players[1].Hp==100);
        });
        Test("강공격: 양측 상쇄 불가/동시 피해", () => {
            foreach(int heavyPlayer in new[]{0,1}) {
                var c=Duel();
                c.Players[0].Attack=new(AttackHeight.Middle,heavyPlayer==0?-GameBalance.HeavyStartup:-GameBalance.Startup,1,heavyPlayer==0);
                c.Players[1].Attack=new(AttackHeight.Middle,heavyPlayer==1?-GameBalance.HeavyStartup:-GameBalance.Startup,-1,heavyPlayer==1);
                c.Step(default,default);
                Check(c.Notice=="" && c.Players[0].Hp==(heavyPlayer==1?80:87) && c.Players[1].Hp==(heavyPlayer==0?89:93));
            }
        });
        Test("강공격: 웅크림 중단 회피/초기화/포커스 취소", () => {
            var c=Duel(); c.Players[0].Attack=new(AttackHeight.Middle,-GameBalance.HeavyStartup,1,heavy:true);
            c.Step(default,new(Crouch:true)); Check(c.Players[1].Hp==100 && c.HitEffects.Count==0);
            c=Duel(); c.Step(new(Move:-1,Attack:true,AttackHeld:true),default); c.CancelCharge(); Wait(c);
            Check(c.Players[0].Attack==null && c.Players[1].Hp==100);
            c.Step(new(Move:-1,Attack:true,AttackHeld:true),default); c.Reset(7,13,100,100);
            Check(c.Players.All(p=>!p.Charging && p.Attack==null));
        });
        Test("확정된 일반 공격 명령 발동", () => {
            var c=Duel(); c.Step(new(Attack:true),default); Check(c.Players[0].Attack is {Heavy:false});

        });
        return failed;
    }
}
