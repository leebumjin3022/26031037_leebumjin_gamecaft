namespace dicefight;
internal static class SelfTests
{
    public static int Run()
    {
        var report = new List<string>(); int failed=0;
        void Test(string name, Action run) { try { run(); report.Add("PASS "+name); } catch(Exception e) { failed++; report.Add("FAIL "+name+": "+e.Message); } }
        void Check(bool ok) { if(!ok) throw new Exception("조건 불일치"); }
        Combat Duel(int hp=72) { var c=new Combat(); c.Reset(7,13,hp,hp); c.Players[0].X=500; c.Players[1].X=600; return c; }
        void Wait(Combat c,int n=40) { for(int i=0;i<n;i++) c.Step(default,default); }
        Test("36개 눈 조합과 곱셈 공식",()=> { for(int a=1;a<=6;a++) for(int b=1;b<=6;b++) { Check(GameBalance.AttackStat(a,b)==4+a*b); Check(GameBalance.HealthStat(a,b)==64+a*b); } });
        Test("4단계 주사위/중복입력/카운트다운/재대결",()=> {
            var s=new GameSession(42); s.Action();
            for(int i=0;i<4;i++) { Check(s.Stage==i); s.Action(); s.Action(); Check(s.Rolling && s.Stage==i);
                for(int n=0;n<140;n++) s.Step(); Check(s.Revealed && s.First is >=1 and <=6 && s.Second is >=1 and <=6);
                Check(s.Stats[i]==s.First*s.Second+(i<2?4:64)); s.Action(); }
            Check(s.Screen==Screen.Countdown); s.Step(new(Attack:true),new(Attack:true)); Check(s.Combat.Players.All(p=>p.Attack==null));
            for(int n=0;n<360;n++) s.Step(); Check(s.Screen==Screen.Battle); s.Restart();
            Check(s.Screen==Screen.Dice && s.Stage==0 && s.Stats.All(x=>x==0)); Check(s.Combat.Players.All(p=>p.Attack==null && p.Stun==0 && p.Knockback==0));
        });
        Test("동시 같은 높이 상쇄",()=> { var c=Duel(); c.Step(new(Attack:true),new(Attack:true)); Wait(c); Check(c.Players.All(p=>p.Hp==72) && c.Notice=="상쇄!"); });
        Test("허용시간 내 늦은 상쇄",()=> { var c=Duel(); c.Step(new(Attack:true),default); Wait(c,8); c.Step(default,new(Attack:true)); Wait(c); Check(c.Players.All(p=>p.Hp==72) && c.Notice=="상쇄!"); });
        Test("허용시간 밖 선공",()=> { var c=Duel(); c.Step(new(Attack:true),default); Wait(c,12); c.Step(default,new(Attack:true)); Wait(c); Check(c.Players[0].Hp==72 && c.Players[1].Hp==65 && c.Notice==""); });
        Test("다른 높이 선공/피격취소",()=> { var c=Duel(); c.Step(new(Attack:true,Height:AttackHeight.High),default); Wait(c,4); c.Step(default,new(Attack:true,Height:AttackHeight.Low)); Wait(c); Check(c.Players[0].Hp==72 && c.Players[1].Hp==65 && c.Players[1].Attack==null); });
        Test("다른 높이 동시 피해/동시 KO 무승부",()=> { var c=Duel(5); c.Step(new(Attack:true,Height:AttackHeight.High),new(Attack:true,Height:AttackHeight.Low)); Wait(c); Check(c.Players.All(p=>p.Hp==0) && c.Result=="무승부"); });
        Test("양측 선공 대칭 및 승리",()=> { var a=Duel(5); a.Step(new(Attack:true),default); Wait(a); var b=Duel(5); b.Step(default,new(Attack:true)); Wait(b); Check(a.Result=="1P 승리" && b.Result=="2P 승리"); });
        Test("사거리 밖 피해/상쇄 없음",()=> { var c=Duel(); c.Players[1].X=950; c.Step(new(Attack:true),new(Attack:true)); Wait(c); Check(c.Players.All(p=>p.Hp==72) && c.Notice==""); });
        Test("한 공격 중복 피해 없음",()=> { var c=Duel(); c.Step(new(Attack:true),default); Wait(c,100); Check(c.Players[1].Hp==65); });
        Test("이단점프 차단과 착지",()=> { var c=Duel(); c.Step(new(Jump:true),default); float vy=c.Players[0].VelocityY; c.Step(new(Jump:true),default); Check(c.Players[0].Attack==null && c.Players[0].VelocityY>vy); Wait(c,(int)Math.Ceiling(2 * GameBalance.JumpSpeed / GameBalance.Gravity / GameBalance.Step) + 2); Check(c.Players[0].Grounded); });
        Test("점프 공격영역 회피",()=> { var c=Duel(); c.Players[1].Y=350; c.Step(new(Attack:true,Height:AttackHeight.Low),default); Wait(c,20); Check(c.Players[1].Hp==72); });
        Test("상대 뛰어넘기/자동 방향",()=> { var c=Duel(); c.Step(new(Move:1,Jump:true),default); for(int n=0;n<95;n++) c.Step(new(Move:1),default); Check(c.Players[0].X>c.Players[1].X && c.Players[0].Facing==-1 && c.Players[1].Facing==1); });
        Test("동시 이동/경계",()=> { var c=Duel(); for(int n=0;n<1000;n++) c.Step(new(Move:-1),new(Move:1)); Check(c.Players[0].X==GameBalance.Left && c.Players[1].X==GameBalance.Right); });
        Test("공격방향 고정",()=> { var c=Duel(); c.Step(new(Attack:true),default); c.Players[1].X=300; Wait(c,3); Check(c.Players[0].Attack?.Direction==1 && c.Players[0].Facing==1); });
        Test("키 자동반복/동시입력/포커스 초기화",()=> { var k=new KeyboardInput(); k.Change(0x24,true); k.Change(0x53,true); Check(k.Read(0).Attack && k.Read(1).Attack); k.ClearEdges(); k.Change(0x24,true); Check(!k.Read(0).Attack); k.Clear(); Check(k.Read(0)==default && k.Read(1)==default); });
        Test("키패드 . / 일반 . / Delete 구분",()=> { Check(KeyboardInput.PhysicalCode(0x53L<<16)==KeyboardInput.NumpadDecimal); Check(KeyboardInput.PhysicalCode((0x53L<<16)|(1L<<24))!=KeyboardInput.NumpadDecimal); var k=new KeyboardInput(); k.Change(0x34,true); Check(!k.Read(1).Attack); });
        Test("30/60/144 FPS 동일 물리/긴 정지 제한",()=> { int Ticks(int fps) { int n=0; var clock=new FixedClock(); for(int i=0;i<fps*2;i++) clock.Advance(1.0/fps,()=>n++); return n; } Check(Ticks(30)==240 && Ticks(60)==240 && Ticks(144)==240); int ticks=0; new FixedClock().Advance(20,()=>ticks++); Check(ticks==12); });
        Test("제출 리소스 누락/불필요 파일 검사",()=> { AssetAudit.Verify(); });
        Test("렌더링 검증 시나리오 상태",()=> { Check(SmokeScenes.Build().All(scene=>scene.Create()!=null)); });
        Test("새 점프/웅크리기 키 양측 매핑",()=> {
            foreach(int player in new[]{0,1}) {
                var k=new KeyboardInput(); int up=player==0?0x11:0x148, down=player==0?0x1f:0x150;
                k.Change(up,true); Check(!k.Read(player).Jump); k.Clear(); k.Change(player==0?0x39:KeyboardInput.Numpad0,true); Check(k.Read(player).Jump); k.ClearEdges(); Check(!k.Read(player).Jump);
                k.Clear(); k.Change(down,true); Check(k.Read(player).Crouch); k.Change(down,false); Check(!k.Read(player).Crouch);
                k.Clear(); k.Change(up,true); k.Change(player==0?0x24:0x53,true);
                Check(k.Read(player).Attack && !k.Read(player).Jump && k.Read(player).Height==AttackHeight.High);
            }
        });
        Test("웅크리기 유지/해제 및 상단 회피",()=> {
            var c=Duel(); c.Step(default,new(Move:1,Crouch:true)); Check(c.Players[1].Crouching && c.Players[1].X==600);
            c.Step(new(Attack:true,Height:AttackHeight.High),new(Crouch:true));
            for(int i=0;i<25;i++) c.Step(default,new(Crouch:true)); Check(c.Players[1].Hp==72);
            c.Step(default,default); Check(!c.Players[1].Crouching);
        });
        Test("이동 거리 애니메이션/벽에서 정지/느린 호흡",()=> {
            var c=Duel(); c.Players[0].X=300; c.Step(new(Move:-1),default);
            Check(Math.Abs(c.Players[0].WalkDistance-GameBalance.MoveSpeed*GameBalance.Step)<.001);
            c.Players[0].X=GameBalance.Left; double old=c.Players[0].WalkDistance;
            c.Step(new(Move:-1),default); Check(c.Players[0].WalkDistance==old && !c.Players[0].Moving);
            Check(ResourceMap.IdleFrame(.2)==0 && ResourceMap.IdleFrame(1)==3);
            Check(ResourceMap.WalkFrame(ResourceMap.WalkCycleDistance)==0);
        });
        Test("적중 효과 1회 생성/만료/재대결 초기화",()=> {
            var c=Duel(); c.Step(new(Attack:true),default); Wait(c,20); Check(c.HitEffects.Count==1);
            Check(c.HitEffects[0].X>=c.Players[1].X-40); Wait(c,10); Check(c.HitEffects.Count==1);
            Wait(c,50); Check(c.HitEffects.Count==0); c.Reset(8,8,100,100); Check(c.HitEffects.Count==0);
        });
        Test("빗나감/상쇄는 충격파 없음, 동시 적중은 2개",()=> {
            var c=Duel(); c.Players[1].X=950; c.Step(new(Attack:true),default); Wait(c,20); Check(c.HitEffects.Count==0);
            c=Duel(); c.Step(new(Attack:true),new(Attack:true)); Wait(c,20); Check(c.Notice=="상쇄!" && c.HitEffects.Count==0);
            c=Duel(); c.Step(new(Attack:true,Height:AttackHeight.High),new(Attack:true,Height:AttackHeight.Low)); Wait(c,20); Check(c.HitEffects.Count==2);
        });
        Test("KO 이후에도 효과 만료",()=> {
            var c=Duel(1); c.Step(new(Attack:true),default); Wait(c,20); Check(c.Result!="" && c.HitEffects.Count==1);
            Wait(c,60); Check(c.HitEffects.Count==0);
        });
        Test("12프레임 보행/호흡 전체 도달",()=> {
            Check(ResourceMap.Frames==12);
            Check(Enumerable.Range(0,12).Select(i=>ResourceMap.WalkFrame((i+.1)*ResourceMap.WalkCycleDistance/12)).Distinct().Count()==12);
            Check(Enumerable.Range(0,12).Select(i=>ResourceMap.IdleFrame((i+.1)/ResourceMap.IdleFramesPerSecond)).Distinct().Count()==12);
        });
        Test("결과에서 시작 화면 복귀 후 새 게임",()=> {
            var s=new GameSession(42); s.Action();
            for(int i=0;i<4;i++){s.Action(); for(int n=0;n<140;n++)s.Step(); s.Action();}
            for(int n=0;n<361;n++)s.Step();
            s.Combat.Players[0].X=500; s.Combat.Players[1].X=600; s.Combat.Players[1].Hp=1;
            s.Step(new(Attack:true),default); for(int n=0;n<230;n++)s.Step(); Check(s.Screen==Screen.Result);
            s.Action(); Check(s.Screen==Screen.Main && s.Stats.All(x=>x==0) && s.Combat.HitEffects.Count==0);
            s.Action(); Check(s.Screen==Screen.Dice && s.Stage==0);
        });
        Test("공중 KO 후 착지와 결과 시각 유지",()=> {
            var c=Duel(1); c.Players[1].Y=530;
            c.Step(new(Attack:true,Height:AttackHeight.High),default); Wait(c,20); Check(c.Result!="");
            double started=c.ResultStarted; Wait(c,180); Check(c.Players[1].Grounded && c.ResultStarted==started);
        });
        Test("양측 웅크림 상중단 회피/하단 적중",()=> {
            foreach(int defender in new[]{0,1}) foreach(var height in Enum.GetValues<AttackHeight>()) {
                var c=Duel(); var attack=new PlayerCommand(Attack:true,Height:height); var duck=new PlayerCommand(Crouch:true);
                c.Step(defender==0?duck:attack,defender==0?attack:duck);
                for(int n=0;n<23;n++) c.Step(defender==0?duck:default,defender==1?duck:default);
                Check(height==AttackHeight.Low ? c.Players[defender].Hp<72 : c.Players[defender].Hp==72);
                Check(height==AttackHeight.Low ? c.HitEffects.Count==1 : c.HitEffects.Count==0);
            }
        });
        Test("하단 공격 전후 웅크림 유지/해제/전용 재생 속도",()=> {
            var c=Duel(); c.Players[1].X=950;
            for(int n=0;n<30;n++)c.Step(new(Crouch:true),default);
            c.Step(new(Attack:true,Height:AttackHeight.Low,Crouch:true),default);
            for(int n=0;n<90;n++) {
                c.Step(new(Crouch:true),default);
                Check(c.Players[0].Crouching && ResourceMap.CrouchPose(c.Players[0].CrouchTime)==ResourceMap.CrouchFrame);
            }
            c.Step(default,default); Check(!c.Players[0].Crouching && c.Players[0].CrouchTime==0);
            Check(ResourceMap.CrouchPose(.15)==ResourceMap.CrouchFrame && ResourceMap.FramesPerSecond==20);
            for(double age=0;age<.55;age+=.01)Check(ResourceMap.CrouchedAttackFrame(age) is >=4 and <=8);
        });
        Test("점프 최고점 소폭 증가",()=> {
            var c=Duel(); c.Step(new(Jump:true),default); float top=c.Players[0].Y;
            for(int n=0;n<140;n++){c.Step(default,default);top=Math.Min(top,c.Players[0].Y);}
            Check(GameBalance.Floor-top is >215 and <230 && c.Players[0].Grounded);
        });
        failed += HeavyAttackTests.Run(report);
        failed += DiveAttackTests.Run(report);
        failed += ProjectileTests.Run(report);
        report.Add($"총 {report.Count}개 / 실패 {failed}개"); File.WriteAllLines(Path.Combine(AppContext.BaseDirectory,"self-test-results.txt"),report); return failed==0?0:1;
    }
}






