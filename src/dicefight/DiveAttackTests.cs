namespace dicefight;
internal static class DiveAttackTests
{
    public static int Run(List<string> report)
    {
        int failed=0;
        void Test(string name, Action action) { try { action(); report.Add("PASS "+name); } catch(Exception e) { failed++; report.Add("FAIL "+name+": "+e.Message); } }
        void Check(bool condition) { if(!condition) throw new Exception("조건 불일치"); }
        Combat Duel() { var c=new Combat(); c.Reset(7,13,100,100); c.Players[0].X=500; c.Players[1].X=600; return c; }
        void Wait(Combat c,int n=80) { for(int i=0;i<n;i++)c.Step(default,default); }
        Test("점프 공격: 양측 실제 점프/공격 키와 좌우 방향",()=> {
            foreach(int player in new[]{0,1}) foreach(int facing in new[]{-1,1}) {
                var c=Duel(); var k=new KeyboardInput(); c.Players[player].X=600; c.Players[1-player].X=600+facing*300;
                k.Change(player==0?0x39:KeyboardInput.Numpad0,true); c.Step(k.Read(0),k.Read(1)); k.Clear(); Wait(c,24);
                float x=c.Players[player].X, y=c.Players[player].Y;
                k.Change(player==0?0x24:KeyboardInput.NumpadDecimal,true); c.Step(k.Read(0),k.Read(1));
                Check(c.Players[player].Attack is {Dive:true, Heavy:false, Height:AttackHeight.High});
                Check((c.Players[player].X-x)*facing>0 && c.Players[player].Y>y && !c.Players[player].Charging);
            }
        });
        Test("점프 공격: 일반 공격력 피해/한 번 적중/동일 넉백",()=> {
            foreach(int player in new[]{0,1}) {
                var c=Duel(); c.Players[player].Y=430;
                c.Step(player==0?new(Attack:true):default,player==1?new(Attack:true):default);
                int victim=1-player;
                for(int n=0;n<40 && c.Players[victim].Hp==100;n++)c.Step(default,default);
                Check(c.Players[victim].Hp==100-c.Players[player].Power && Math.Abs(c.Players[victim].Knockback)==GameBalance.KnockbackSpeed);
                Check(c.HitEffects.Count==1 && !c.HitEffects[0].Heavy);
                Wait(c); Check(c.Players[victim].Hp==100-c.Players[player].Power);
            }
        });
        Test("점프 공격: 상단 상쇄/중하단 상쇄 제외",()=> {
            foreach(var height in new[]{AttackHeight.High,AttackHeight.Middle,AttackHeight.Low}) {
                var c=Duel(); c.Players[0].Y=470;
                c.Players[0].Attack=new(AttackHeight.High,-GameBalance.DiveStartup,1,dive:true);
                c.Players[1].Attack=new(height,-GameBalance.Startup,-1);
                c.Step(default,default);
                Check(height==AttackHeight.High ? c.Notice=="상쇄!" && c.Players.All(p=>p.Hp==100) : c.Notice=="");
            }
        });
        Test("점프 공격: 웅크림 회피/헛발질 효과 없음",()=> {
            var c=Duel(); c.Players[0].Y=430;
            c.Step(new(Attack:true),new(Crouch:true));
            for(int n=0;n<70;n++)c.Step(default,new(Crouch:true));
            Check(c.Players[1].Hp==100 && c.HitEffects.Count==0);
            c=Duel(); c.Players[0].Y=430; c.Players[1].X=1000; c.Step(new(Attack:true),default); Wait(c);
            Check(c.Players[1].Hp==100 && c.HitEffects.Count==0);
        });
        Test("점프 공격: 공중 중복 입력 차단/착지 후 재사용",()=> {
            var c=Duel(); c.Players[1].X=1100; c.Step(new(Jump:true),default); Wait(c,25);
            c.Step(new(Attack:true),default); var first=c.Players[0].Attack;
            c.Step(new(Attack:true,Jump:true,Move:-1),default);
            Check(ReferenceEquals(first,c.Players[0].Attack) && c.Players[0].AirAttackUsed && c.Players[0].VelocityY>0);
            Wait(c); Check(c.Players[0].Grounded && !c.Players[0].AirAttackUsed && c.Players[0].Attack==null);
            c.Step(new(Jump:true),default); Wait(c,15); c.Step(new(Attack:true),default);
            Check(c.Players[0].Attack is {Dive:true} && !ReferenceEquals(first,c.Players[0].Attack));
        });
        Test("점프 공격: 착지 후 공격 판정 종료/회복/프레임",()=> {
            var c=Duel(); c.Players[0].Y=599; c.Players[1].X=950;
            c.Step(new(Attack:true),default); var attack=c.Players[0].Attack!;
            Check(c.Players[0].Grounded && attack.LandedAt>=0 && ResourceMap.DiveFrame(attack,c.Time)==9);
            c.Players[1].X=c.Players[0].X+100; Wait(c,15);
            Check(c.Players[1].Hp==100 && c.Players[0].Attack!=null);
            Check(ResourceMap.DiveFrame(attack,attack.LandedAt+attack.Recovery)==11);
            Wait(c,15); Check(c.Players[0].Attack==null);
        });
        Test("점프 공격: 피격 시 취소/경기 초기화",()=> {
            var c=Duel(); c.Players[0].Y=c.Players[1].Y=430;
            c.Players[1].Attack=new(AttackHeight.Middle,-GameBalance.Startup,-1);
            c.Step(new(Attack:true),default);
            Check(c.Players[0].Hp==87 && c.Players[0].Attack==null && c.Players[0].AirAttackUsed);
            c.Reset(7,13,100,100); Check(c.Players.All(p=>!p.AirAttackUsed && p.Attack==null));
        });
        Test("점프 공격: 양측 12프레임 리소스",()=> {
            for(int player=0;player<2;player++) for(int frame=0;frame<12;frame++)
                Check(File.Exists(Path.Combine(ResourceMap.Root,ResourceMap.Fighter(player,"15_jump_attack",frame))));
        });
        return failed;
    }
}
