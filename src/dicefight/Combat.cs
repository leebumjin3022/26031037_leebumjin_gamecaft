namespace dicefight;
public enum AttackHeight { Middle, High, Low }
public readonly record struct PlayerCommand(int Move = 0, bool Jump = false, bool Attack = false,
    AttackHeight Height = AttackHeight.Middle, bool Crouch = false, bool AttackHeld = false, bool Special = false);
public sealed class AttackState(AttackHeight height, double started, int direction, bool heavy = false, bool dive = false, bool special = false)
{
    public AttackHeight Height = height;
    public double Started = started;
    public int Direction = direction; // 공격 시작 순간 고정
    public bool Consumed, ProjectileSpawned;
    public bool Heavy = heavy, Dive = dive, Special = special;
    public double LandedAt = -1;
    public double Startup => Special ? GameBalance.CastStartup : Heavy ? GameBalance.HeavyStartup : Dive ? GameBalance.DiveStartup : GameBalance.Startup;
    public double ActiveDuration => Special ? 0 : Heavy ? GameBalance.HeavyActive : Dive ? GameBalance.DiveActive : GameBalance.Active;
    public double Recovery => Special ? GameBalance.CastRecovery : Heavy ? GameBalance.HeavyRecovery : Dive ? GameBalance.DiveRecovery : GameBalance.Recovery;
    public double TotalDuration => Startup + ActiveDuration + Recovery;
}
public sealed class Fighter
{
    public float X, Y = GameBalance.Floor, VelocityY, Knockback;
    public int Facing, Power, MaxHp, Hp;
    public double Stun, SpecialCooldown;
    public bool Moving, Crouching, MovingBackward, Charging, AirAttackUsed;
    public double WalkDistance, IdleTime, CrouchTime, ChargeStarted;
    public int ChargeDirection;
    public float CurrentHeight => Crouching ? GameBalance.CrouchHeight : GameBalance.BodyHeight;
    public AttackState? Attack;
    public bool Grounded => Y >= GameBalance.Floor - .01f;
    public RectangleF Body => new(X - GameBalance.BodyWidth / 2, Y - CurrentHeight,
        GameBalance.BodyWidth, CurrentHeight);
}
public readonly record struct HitEffect(float X, float Y, int Direction, double Started, bool Heavy = false);
public sealed class Combat
{
    public List<HitEffect> HitEffects { get; } = [];
    public List<Projectile> Projectiles { get; } = [];
    private readonly AttackInputResolver[] inputResolvers = [new(),new()];
    public Fighter[] Players { get; } = [new(), new()];
    public double Time { get; private set; }
    public double ResultStarted { get; private set; } = -1;
    public string Result { get; private set; } = "";
    public string Notice { get; private set; } = "";
    public double NoticeUntil { get; private set; }
    public void Reset(int power1, int power2, int hp1, int hp2)
    {
        Players[0] = new() { X = 350, Facing = 1, Power = power1, Hp = hp1, MaxHp = hp1 };
        Players[1] = new() { X = 850, Facing = -1, Power = power2, Hp = hp2, MaxHp = hp2 };
        HitEffects.Clear(); Projectiles.Clear(); CancelInput(); ResultStarted = -1;
        Time = 0; Result = Notice = ""; NoticeUntil = 0;
    }
    public void StepInput(PlayerCommand first, PlayerCommand second)
    {
        var a=inputResolvers[0].Resolve(first,Players[0],Players[1],Time);
        var b=inputResolvers[1].Resolve(second,Players[1],Players[0],Time);
        Step(a,b);
    }
    public void CancelInput()
    {
        foreach(var resolver in inputResolvers)resolver.Clear();
        CancelCharge();
    }
    public void CancelCharge()
    {
        foreach (var player in Players) player.Charging = false;
    }
    public RectangleF AttackBox(Fighter p)
    {
        int d = p.Attack?.Direction ?? p.Facing;
        float reach = p.Attack?.Heavy == true ? GameBalance.HeavyReach : p.Attack?.Dive == true ? GameBalance.DiveReach : GameBalance.Reach;
        float x = d > 0 ? p.X : p.X - GameBalance.BodyWidth / 2 - reach;
        // 날아차기의 타격 영역은 발 주변에 두되 상단 공격으로 분류한다.
        float offset = p.Attack?.Dive == true ? 20 : GameBalance.AttackOffset(p.Attack?.Height ?? AttackHeight.Middle);
        return new(x, p.Y - offset
            - GameBalance.AttackThickness / 2, GameBalance.BodyWidth / 2 + reach,
            GameBalance.AttackThickness);
    }
    private bool CanConnect(Fighter attacker, Fighter victim) =>
        (!victim.Crouching || attacker.Attack?.Height == AttackHeight.Low) &&
        AttackBox(attacker).IntersectsWith(victim.Body);
    private bool Active(Fighter p) => p.Attack is { Consumed: false, Special: false } a &&
        Time - a.Started + 1e-8 >= a.Startup &&
        Time - a.Started < a.Startup + a.ActiveDuration &&
        (a.LandedAt < 0 || Time <= a.LandedAt + 1e-8);
    private bool Live(Fighter p) => p.Attack is { Consumed: false, Heavy: false, Special: false } a &&
        Time - a.Started < a.Startup + a.ActiveDuration &&
        (a.LandedAt < 0 || Time <= a.LandedAt + 1e-8);
    public void Step(PlayerCommand first, PlayerCommand second)
    {
        Time += GameBalance.Step;
        foreach(var player in Players) player.SpecialCooldown=Math.Max(0,player.SpecialCooldown-GameBalance.Step);
        HitEffects.RemoveAll(effect => Time - effect.Started >= ResourceMap.ShockwaveDuration);
        if (Result.Length != 0)
        {
            // 공중 KO도 바닥에 내려오며 사망 애니메이션이 끝나도록 물리만 진행.
            foreach (var player in Players) Move(player, default);
            return;
        }
        var a = Players[0]; var b = Players[1];
        // 양쪽 입력을 같은 시각으로 확정한 뒤 판정한다.
        Prepare(a, b, first); Prepare(b, a, second);
        float oldA = a.X, oldB = b.X;
        Move(a, first); Move(b, second);
        SeparateBodies(a, b);
        UpdateAnimation(a, oldA); UpdateAnimation(b, oldB);
        if (a.Attack is null && !a.Charging) a.Facing = b.X >= a.X ? 1 : -1;
        if (b.Attack is null && !b.Charging) b.Facing = a.X >= b.X ? 1 : -1;
        SpawnProjectile(0); SpawnProjectile(1);
        foreach(var shot in Projectiles)shot.X+=shot.Direction*GameBalance.ProjectileSpeed*(float)GameBalance.Step;

        bool clash = Live(a) && Live(b) && (Active(a) || Active(b)) &&
            a.Attack!.Height == b.Attack!.Height &&
            Math.Abs(a.Attack.Started - b.Attack.Started) <= GameBalance.ClashWindow + 1e-8 &&
            (a.Attack.Dive || b.Attack.Dive || (CanConnect(a, b) && CanConnect(b, a))) &&
            AttackBox(a).IntersectsWith(AttackBox(b));
        if (clash)
        {
            a.Attack!.Consumed = b.Attack!.Consumed = true;
            Notice = "상쇄!"; NoticeUntil = Time + .55;
        }
        ResolveProjectileClashes();
        bool hitB=Active(a) && CanConnect(a,b), hitA=Active(b) && CanConnect(b,a);
        // 모든 피해 후보를 먼저 모아 동시 타격을 대칭 처리한다.
        if (hitA && hitB)
        {
            double diff = (a.Attack!.Started + a.Attack.Startup) - (b.Attack!.Started + b.Attack.Startup);
            if (diff < -1e-8) hitA = false;
            else if (diff > 1e-8) hitB = false;
            else if (!GameBalance.SimultaneousDifferentHeightTrades) hitA = hitB = false;
        }
        int directionA = a.Attack?.Direction ?? a.Facing, directionB = b.Attack?.Direction ?? b.Facing;
        // 피해 적용 전에 교차 영역을 저장: 동시 적중도 양쪽 효과를 생성한다.
        if (hitB) AddHitEffect(a, b);
        if (hitA) AddHitEffect(b, a);
        int damageA = a.Attack?.Heavy == true ? GameBalance.HeavyDamage(a.Power) : a.Power;
        int damageB = b.Attack?.Heavy == true ? GameBalance.HeavyDamage(b.Power) : b.Power;
        if (hitB) a.Attack!.Consumed = true;
        if (hitA) b.Attack!.Consumed = true;
        int[] damage=[hitA?damageB:0,hitB?damageA:0];
        int[] push=[hitA?directionB:0,hitB?directionA:0];
        foreach(var shot in Projectiles.Where(s=>!s.Consumed))
        {
            int victim=1-shot.Owner;
            if(Players[victim].Crouching || !shot.Box.IntersectsWith(Players[victim].Body))continue;
            shot.Consumed=true; damage[victim]+=shot.Damage; push[victim]+=shot.Direction;
            var contact=RectangleF.Intersect(shot.Box,Players[victim].Body);
            HitEffects.Add(new(contact.Left+contact.Width/2,contact.Top+contact.Height/2,shot.Direction,Time));
            Players[shot.Owner].SpecialCooldown=Math.Max(0,Players[shot.Owner].SpecialCooldown-GameBalance.SpecialHitRefund);
        }
        for(int i=0;i<2;i++)if(damage[i]>0)
        {
            int direction=push[i]==0?(Players[i].X>=Players[1-i].X?1:-1):Math.Sign(push[i]);
            Hit(Players[i],damage[i],direction); inputResolvers[i].Clear();
        }
        Projectiles.RemoveAll(s=>s.Consumed || s.X < -GameBalance.ProjectileWidth || s.X > GameBalance.Width+GameBalance.ProjectileWidth);

        if (a.Hp == 0 && b.Hp == 0)
            Result = GameBalance.DoubleKnockoutIsDraw ? "무승부" :
                a.MaxHp == b.MaxHp ? "무승부" : a.MaxHp > b.MaxHp ? "1P 승리" : "2P 승리";
        else if (a.Hp == 0) Result = "2P 승리";
        else if (b.Hp == 0) Result = "1P 승리";
        if (Result.Length != 0 && ResultStarted < 0) { ResultStarted = Time; CancelInput(); Projectiles.Clear(); }
    }
    private void AddHitEffect(Fighter attacker, Fighter victim)
    {
        var contact = RectangleF.Intersect(AttackBox(attacker), victim.Body);
        HitEffects.Add(new(contact.Left + contact.Width / 2, contact.Top + contact.Height / 2,
            attacker.Attack!.Direction, Time, attacker.Attack.Heavy));
    }
    private void SpawnProjectile(int owner)
    {
        var player=Players[owner];
        if(player.Attack is not {Special:true,ProjectileSpawned:false} cast || Time-cast.Started+1e-8<cast.Startup)return;
        cast.ProjectileSpawned=true;
        Projectiles.Add(new(owner,player.X+cast.Direction*55,player.Y-GameBalance.AttackOffset(AttackHeight.Middle),
            cast.Direction,GameBalance.SpecialDamage(player.Power),Time));
        player.SpecialCooldown=GameBalance.SpecialCooldown;
    }
    private void ResolveProjectileClashes()
    {
        var cancelled=new HashSet<Projectile>();
        for(int i=0;i<Projectiles.Count;i++)for(int j=i+1;j<Projectiles.Count;j++)
            if(Projectiles[i].Owner!=Projectiles[j].Owner && Projectiles[i].Box.IntersectsWith(Projectiles[j].Box))
            {cancelled.Add(Projectiles[i]); cancelled.Add(Projectiles[j]);}
        foreach(var shot in cancelled)shot.Consumed=true;
        bool clashed=cancelled.Count>0;
        for(int defenderIndex=0;defenderIndex<2;defenderIndex++)
        {
            var defender=Players[defenderIndex];
            if(!Live(defender) || defender.Attack!.Height!=AttackHeight.Middle ||
                Math.Abs(Time-defender.Attack.Started-defender.Attack.Startup)>GameBalance.ClashWindow+1e-8)continue;
            var blocked=Projectiles.Where(s=>!s.Consumed && s.Owner!=defenderIndex && s.Box.IntersectsWith(AttackBox(defender))).ToArray();
            if(blocked.Length==0)continue;
            foreach(var shot in blocked)shot.Consumed=true;
            defender.Attack.Consumed=true; clashed=true;
        }
        if(clashed){Notice="상쇄!"; NoticeUntil=Time+.55;}
    }
    private void Prepare(Fighter p, Fighter other, PlayerCommand command)
    {
        p.Stun = Math.Max(0, p.Stun - GameBalance.Step);
        if (p.Attack is { } old && (Time - old.Started >= old.TotalDuration || (old.Dive && old.LandedAt >= 0 && Time - old.LandedAt >= old.Recovery)))
            p.Attack = null;
        if (p.Attack is null && !p.Charging) p.Facing = other.X >= p.X ? 1 : -1;
        if (p.Charging)
        {
            p.Crouching = p.Moving = false; p.CrouchTime = 0;
            if (!command.AttackHeld)
            {
                p.Charging = false;
                if (Time - p.ChargeStarted + 1e-8 >= GameBalance.ChargeTime)
                    p.Attack = new(AttackHeight.Middle, Time, p.ChargeDirection, heavy: true);
            }
            return;
        }
        p.Crouching = command.Crouch && p.Grounded && p.Stun <= 0 &&
            (p.Attack is null || p.Attack.Height == AttackHeight.Low);
        p.CrouchTime = p.Crouching ? p.CrouchTime + GameBalance.Step : 0;
        if (p.Stun > 0 || p.Attack is not null) return;
        if (!p.Grounded)
        {
            if (command.Attack && !p.AirAttackUsed)
            {
                p.AirAttackUsed = true; p.Crouching = false;
                p.Attack = new(AttackHeight.High, Time, p.Facing, dive: true);
                p.VelocityY = GameBalance.DiveSpeedY;
            }
            return;
        }
        // 지상에서 동시 점프+공격은 점프 우선. 이후 공중에서 새 공격 입력으로 날아차기.
        if (command.Jump) { p.Crouching = false; p.CrouchTime = 0; p.VelocityY = -GameBalance.JumpSpeed; }
        else if (command.Attack)
        {
            if(command.Special && p.SpecialCooldown<=1e-8)
            {
                p.Crouching=false; p.CrouchTime=0;
                p.Attack=new(AttackHeight.Middle,Time,p.Facing,special:true);
            }
            else if (command.Move == -p.Facing)
            {
                p.Charging = true; p.ChargeStarted = Time; p.ChargeDirection = p.Facing;
                p.Crouching = false; p.CrouchTime = 0;
            }
            else p.Attack = new(command.Height, Time, p.Facing);
        }
    }
    private void Move(Fighter p, PlayerCommand command)
    {
        float dt = (float)GameBalance.Step;
        p.Moving = command.Move != 0 && p.Stun <= 0 && p.Attack is null && !p.Crouching && !p.Charging;
        p.MovingBackward = p.Moving && Math.Sign(command.Move) != p.Facing;
        float moveX = p.Moving ? Math.Clamp(command.Move, -1, 1) * GameBalance.MoveSpeed : 0;
        if (p.Attack is { Dive: true, LandedAt: < 0 } dive && !p.Grounded)
            moveX = dive.Direction * GameBalance.DiveSpeedX;
        p.X += (moveX + p.Knockback) * dt;
        p.Knockback = Math.Sign(p.Knockback) * Math.Max(0, Math.Abs(p.Knockback) - GameBalance.KnockbackDecay * dt);
        p.VelocityY += GameBalance.Gravity * dt;
        p.Y = Math.Min(GameBalance.Floor, p.Y + p.VelocityY * dt);
        if (p.Grounded)
        {
            p.VelocityY = 0; p.AirAttackUsed = false;
            if (p.Attack is { Dive: true, LandedAt: < 0 } landed) landed.LandedAt = Time;
        }
        p.X = Math.Clamp(p.X, GameBalance.Left, GameBalance.Right);
    }
    private static void UpdateAnimation(Fighter p, float previousX)
    {
        float distance = Math.Abs(p.X - previousX);
        if (p.Moving && p.Grounded && distance > .001f && Math.Abs(p.Knockback) < .01f)
            p.WalkDistance += distance;
        else if (distance <= .001f) p.Moving = false;
        if (!p.Moving && p.Grounded && !p.Crouching && !p.Charging && p.Attack is null && p.Stun <= 0)
            p.IdleTime += GameBalance.Step;
        else p.IdleTime = 0;
    }
    private static void SeparateBodies(Fighter a, Fighter b)
    {
        if (!a.Body.IntersectsWith(b.Body)) return;
        float overlap = GameBalance.BodyWidth - Math.Abs(a.X - b.X);
        int direction = a.X <= b.X ? -1 : 1;
        a.X = Math.Clamp(a.X + direction * overlap / 2, GameBalance.Left, GameBalance.Right);
        b.X = Math.Clamp(b.X - direction * overlap / 2, GameBalance.Left, GameBalance.Right);
        float remaining = GameBalance.BodyWidth - Math.Abs(a.X - b.X);
        if (remaining > 0)
        {
            if (a.X == GameBalance.Left || a.X == GameBalance.Right)
                b.X = Math.Clamp(b.X - direction * remaining, GameBalance.Left, GameBalance.Right);
            else a.X = Math.Clamp(a.X + direction * remaining, GameBalance.Left, GameBalance.Right);
        }
    }
    private static void Hit(Fighter victim, int damage, int direction)
    {
        victim.Hp = Math.Max(0, victim.Hp - damage);
        victim.Crouching = victim.Charging = false; victim.CrouchTime = 0;
        victim.Stun = GameBalance.HitStun;
        victim.Knockback = direction * GameBalance.KnockbackSpeed;
        if (GameBalance.CancelAttackOnHit) victim.Attack = null;
    }
}






