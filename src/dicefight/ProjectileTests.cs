namespace dicefight;
internal static class ProjectileTests
{
    public static int Run(List<string> report)
    {
        int failed=0;
        void Test(string name,Action action){try{action();report.Add("PASS "+name);}catch(Exception e){failed++;report.Add("FAIL "+name+": "+e.Message);}}
        void Check(bool condition){if(!condition)throw new Exception("조건 불일치");}
        Combat Duel(){var c=new Combat();c.Reset(7,13,100,100);c.Players[0].X=500;c.Players[1].X=600;return c;}
        void Wait(Combat c,int n=80){for(int i=0;i<n;i++)c.Step(default,default);}
        void RawWait(Combat c,int n){for(int i=0;i<n;i++)c.StepInput(default,default);}
        Test("장풍 입력: 단타 대기/피해 한 번/원래 공격 높이",()=>{
            var c=Duel();c.StepInput(new(Attack:true,Height:AttackHeight.Low),default);Check(c.Players[0].Attack==null);
            RawWait(c,26);Check(c.Players[0].Attack is {Special:false,Heavy:false,Height:AttackHeight.Low});
            RawWait(c,80);Check(c.Players[1].Hp==93 && c.Projectiles.Count==0);
            c=Duel();c.StepInput(new(Attack:true),default);
            for(int n=0;n<26;n++)c.StepInput(new(Move:-1),default);
            Check(!c.Players[0].Charging && c.Players[0].Attack is {Heavy:false});
        });
        Test("장풍 입력: 양측 실제 키 두 번/일반공격 중복 없음",()=>{
            for(int player=0;player<2;player++){
                var c=Duel();var keys=new KeyboardInput();int key=player==0?0x24:KeyboardInput.NumpadDecimal;
                keys.Change(key,true);c.StepInput(keys.Read(0),keys.Read(1));keys.ClearEdges();
                keys.Change(key,false);RawWait(c,10);keys.Change(key,true);c.StepInput(keys.Read(0),keys.Read(1));keys.ClearEdges();
                Check(c.Players[player].Attack is {Special:true});RawWait(c,60);
                Check(c.Players[1-player].Hp==100-GameBalance.SpecialDamage(c.Players[player].Power));
            }
        });
        Test("장풍 입력: 자동반복 무시/쿨다운 중 일반 공격",()=>{
            var keys=new KeyboardInput();keys.Change(0x24,true);keys.ClearEdges();keys.Change(0x24,true);Check(!keys.Read(0).Attack);
            var c=Duel();c.Players[0].SpecialCooldown=4;c.StepInput(new(Attack:true),default);RawWait(c,8);c.StepInput(new(Attack:true),default);
            Check(c.Players[0].Attack is {Special:false,Heavy:false});RawWait(c,70);Check(c.Players[1].Hp==93 && c.Projectiles.Count==0);
        });
        Test("장풍 입력: 강공격/공중 발차기 즉시 우선",()=>{
            var c=Duel();c.StepInput(new(Move:-1,Attack:true,AttackHeld:true),default);Check(c.Players[0].Charging);
            c=Duel();c.Players[0].Y=430;c.StepInput(new(Attack:true),default);Check(c.Players[0].Attack is {Dive:true,Special:false});
        });
        Test("장풍: 발사 시 5초/적중 2.5초 환급/동일 넉백",()=>{
            var c=Duel();c.Players[1].X=950;c.Step(new(Attack:true,Special:true),default);Wait(c,29);
            Check(c.Projectiles.Count==1 && c.Players[0].SpecialCooldown>4.9);
            c.Players[1].X=c.Projectiles[0].X+25;c.Step(default,default);
            Check(c.Players[1].Hp==94 && c.Projectiles.Count==0 && c.Players[0].SpecialCooldown is >2.4 and <=2.5);
            Check(c.Players[1].Knockback==GameBalance.KnockbackSpeed);Wait(c);Check(c.Players[1].Hp==94);
            Check(GameBalance.SpecialDamage(5)==4 && GameBalance.SpecialDamage(7)==6 && GameBalance.SpecialDamage(13)==10);
        });
        Test("장풍: 시전 중 피격 취소/발사된 장풍 유지",()=>{
            var c=Duel();c.Step(new(Attack:true,Special:true),new(Attack:true));Wait(c,35);
            Check(c.Players[0].Hp==87 && c.Players[0].SpecialCooldown==0 && c.Projectiles.Count==0);
            c=Duel();c.Players[1].X=1000;c.Step(new(Attack:true,Special:true),default);Wait(c,30);
            c.Players[1].X=400;c.Players[1].Attack=new(AttackHeight.Middle,c.Time-GameBalance.Startup,1);
            c.Step(default,default);Check(c.Players[0].Hp==87 && c.Projectiles.Count==1);
        });
        Test("장풍: 서로 상쇄/쿨다운 환급 없음",()=>{
            var c=Duel();c.Players[0].SpecialCooldown=4;c.Players[1].SpecialCooldown=3;
            c.Projectiles.Add(new(0,300,300,1,6,0));c.Projectiles.Add(new(1,340,300,-1,10,-2));c.Step(default,default);
            Check(c.Projectiles.Count==0 && c.Notice=="상쇄!" && c.Players.All(p=>p.Hp==100));
            Check(c.Players[0].SpecialCooldown>3.9 && c.Players[1].SpecialCooldown>2.9 && c.HitEffects.Count==0);
        });
        Test("장풍: 중단 상쇄/상하단 및 강공격 상쇄 불가",()=>{
            foreach(var height in Enum.GetValues<AttackHeight>()){
                var c=Duel();c.Players[1].Attack=new(height,-GameBalance.Startup,-1);c.Projectiles.Add(new(0,550,518,1,6,0));c.Step(default,default);
                Check(height==AttackHeight.Middle?c.Notice=="상쇄!" && c.Players[1].Hp==100:c.Notice=="" && c.Players[1].Hp==94);
            }
            var heavy=Duel();heavy.Players[1].Attack=new(AttackHeight.Middle,-GameBalance.HeavyStartup,-1,heavy:true);
            heavy.Projectiles.Add(new(0,550,518,1,6,0));heavy.Step(default,default);Check(heavy.Notice=="" && heavy.Players[1].Hp==94);
        });
        Test("장풍: 웅크림/점프 회피",()=>{
            var c=Duel();c.Projectiles.Add(new(0,550,518,1,6,0));c.Step(default,new(Crouch:true));Check(c.Players[1].Hp==100 && c.Projectiles.Count==1);
            c=Duel();c.Players[1].Y=350;c.Projectiles.Add(new(0,550,518,1,6,0));c.Step(default,default);Check(c.Players[1].Hp==100);
        });
        Test("장풍: 여러 발 유지/화면 밖 제거",()=>{
            var c=Duel();for(int i=0;i<5;i++)c.Projectiles.Add(new(0,50+i*60,150,1,6,0));c.Step(default,default);
            Check(c.Projectiles.Count==5);Wait(c,400);Check(c.Projectiles.Count==0);
        });
        Test("장풍: 근접 상쇄 중에도 장풍 적중/동시 KO",()=>{
            var c=Duel();c.Players[0].Attack=new(AttackHeight.High,-GameBalance.Startup,1);c.Players[1].Attack=new(AttackHeight.High,-GameBalance.Startup,-1);
            c.Projectiles.Add(new(0,610,518,1,6,0));c.Step(default,default);Check(c.Notice=="상쇄!" && c.Players[1].Hp==94);
            c=Duel();c.Players[0].Hp=c.Players[1].Hp=5;c.Projectiles.Add(new(0,610,518,1,6,0));c.Projectiles.Add(new(1,490,518,-1,6,0));
            c.Step(default,default);Check(c.Result=="무승부" && c.Projectiles.Count==0);
        });
        Test("장풍: 재대결/포커스/피격 시 보류 입력 제거",()=>{
            var c=Duel();c.StepInput(new(Attack:true),default);c.CancelInput();RawWait(c,100);Check(c.Players[1].Hp==100);
            c.Players[0].SpecialCooldown=3;c.Projectiles.Add(new(0,10,10,1,6,0));c.Reset(7,13,100,100);
            Check(c.Projectiles.Count==0 && c.Players.All(p=>p.SpecialCooldown==0));
            c.Players[0].X=500;c.Players[1].X=600;c.Players[1].Attack=new(AttackHeight.Middle,-GameBalance.Startup,-1);
            c.StepInput(new(Attack:true),default);RawWait(c,100);Check(c.Players[0].Hp==87 && c.Players[1].Hp==100);
        });
        Test("장풍: 다중 상쇄 순서 독립/자기 장풍끼리 유지",()=>{
            foreach(bool reverse in new[]{false,true}){
                var c=Duel();c.Players[1].Attack=new(AttackHeight.Middle,-GameBalance.Startup,-1);
                var shots=new[]{new Projectile(0,548,518,1,6,0),new Projectile(0,550,518,1,6,0)};
                c.Projectiles.AddRange(reverse?shots.Reverse():shots);c.Step(default,default);Check(c.Projectiles.Count==0 && c.Players[1].Hp==100);
            }
            var same=Duel();same.Projectiles.Add(new(0,50,150,1,6,0));same.Projectiles.Add(new(0,50,150,1,6,0));same.Step(default,default);Check(same.Projectiles.Count==2);
        });
        Test("장풍: 양측 시전/투사체 12프레임 리소스",()=>{
            for(int p=0;p<2;p++)for(int f=0;f<12;f++){
                Check(File.Exists(Path.Combine(ResourceMap.Root,ResourceMap.Fighter(p,"16_special_cast",f))));
                Check(File.Exists(Path.Combine(ResourceMap.Root,ResourceMap.Projectile(p,f))));
            }
        });
        return failed;
    }
}
