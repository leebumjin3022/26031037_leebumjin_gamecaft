namespace dicefight;
// 모든 장면은 glc2d의 G2Texture/G2Font와 Direct2D 기본 도형으로 그린다.
internal sealed class GameRenderer(GameSession session, GameAssets resources)
{
    public GameSession Session { get; set; }=session;
    public static readonly RectangleF ButtonArea=new(440,540,320,64);
    private static readonly Color Blue=Color.FromArgb(92,179,255), Red=Color.FromArgb(255,113,114);
    public void DrawScene(GlcCanvas g)
    {
        string background = Session.Screen == Screen.Main ? ResourceMap.Main : Session.Screen == Screen.Dice ? ResourceMap.Dice : ResourceMap.Arena;
        if (!Picture(g, background, new(0, 0, 1200, 720)))
        {
            g.Clear(Color.FromArgb(36, 43, 61));
            g.FillRectangle(Color.FromArgb(98, 70, 48), 0, 600, 1200, 120);
        }
        g.FillRectangle(Color.FromArgb(Session.Screen is Screen.Main or Screen.Dice ? 110 : 45, 0, 0, 0), 0, 0, 1200, 720);
        if (Session.Screen == Screen.Main)
        {
            Panel(g, new(240, 80, 720, 410));
            if (!Picture(g, ResourceMap.Title, new(300, 105, 600, 200))) Label(g, "더 다이스 파이트", 600, 155, 44, Color.White, true);
            Label(g, "두 개의 주사위, 한 번의 승부", 600, 335, 26, Color.White, true);
            Button(g, "게임 시작");
        }
        else if (Session.Screen == Screen.Dice)
        {
            Panel(g, new(180, 55, 840, 450));
            Label(g, $"능력치 결정  {Session.Stage + 1} / 4", 600, 80, 18, Color.Gainsboro, true);
            Label(g, Session.StageName, 600, 120, 34, Session.Stage % 2 == 0 ? Blue : Red, true);
            DrawDie(g, 365, Session.First, 0); DrawDie(g, 665, Session.Second, 4);
            string result = Session.Revealed ? $"{Session.StageName}  {Session.Stats[Session.Stage]}" :
                Session.Rolling ? "주사위를 굴리는 중…" : "던지기 버튼을 눌러 주세요";
            if (Session.Revealed)
            {
                Label(g, Session.Stage < 2 ? "결정된 공격력" : "결정된 최대 체력", 600, 399, 18, Color.White, true);
                DrawNumber(g, Session.Stats[Session.Stage].ToString(), 600, 421, 72);
            }
            else Label(g, result, 600, 405, 25, Color.White, true);
            Button(g, Session.Rolling ? "굴리는 중…" : Session.Revealed ? Session.Stage == 3 ? "경기장으로" : "다음 능력치" : "던지기", !Session.Rolling);
        }
        else
        {
            g.DrawLine(Color.FromArgb(170, 255, 255, 255), 30, GameBalance.Floor, 1170, GameBalance.Floor);
            DrawFighter(g, 0); DrawFighter(g, 1); DrawProjectiles(g); DrawHitEffects(g); DrawHud(g, 0); DrawHud(g, 1);
            if (Session.Screen == Screen.Countdown)
            {
                Panel(g, new(420, 240, 360, 170));
                Label(g, "잠시 후 시작", 600, 255, 22, Color.White, true);
                DrawNumber(g, Math.Max(1, (int)Math.Ceiling(Session.Remaining)).ToString(), 600, 287, 118);
            }
            if (Session.Combat.Time < Session.Combat.NoticeUntil)
                Picture(g, ResourceMap.ClashImage, new(430, 175, 340, 170));
            if (Session.Screen == Screen.Result && Session.Combat.Time - Session.Combat.ResultStarted >= ResourceMap.ResultDelay)
            {
                Panel(g, new(300, 190, 600, 310));
                string? logo = Session.Combat.Result.StartsWith("1P") ? ResourceMap.BlueWin : Session.Combat.Result.StartsWith("2P") ? ResourceMap.RedWin : null;
                if (logo != null) Picture(g, logo, new(405, 215, 390, 150));
                Label(g, Session.Combat.Result, 600, 380, 44, Color.White, true);
                Button(g, "재대결 · 시작 화면으로");
            }
        }
        Panel(g, new(0, 644, 1200, 76));
        Label(g, "1P  A / D 이동   Space 점프   S 웅크리기   J 공격   W+J 상단   S+J 하단   뒤+J 모은 후 떼기: 강공격   공중 J: 날아차기   J 두 번: 장풍", 26, 654, 14, Blue);
        Label(g, "2P  ← / → 이동   숫자패드0 점프   ↓ 웅크리기   숫자패드. 공격   ↑+. 상단   ↓+. 하단   뒤+. 모은 후 떼기: 강공격   공중 .: 날아차기   . 두 번: 장풍", 26, 683, 14, Red);
    }
    private void DrawDie(GlcCanvas g, int x, int value, int offset)
    {
        string path = Session.Rolling ? ResourceMap.Rotation(((int)((GameBalance.DiceDuration - Session.Remaining) * 18) + offset) % ResourceMap.RotationFrames) : ResourceMap.Face(value);
        if (!Picture(g, path, new(x, 205, 170, 170)))
        {
            g.FillRectangle(Color.Ivory, x, 205, 170, 170);
            Label(g, (Session.Rolling ? (int)(Session.Remaining * 20 + offset) % 6 + 1 : value).ToString(), x + 85, 245, 54, Color.Black, true);
        }
        if (!Session.Rolling) DrawNumber(g, value.ToString(), x + 85, 363, 34);
    }
    // 숫자 그림은 원본 비율을 유지하고, 슬래시 등 구분자는 글꼴로 표시한다.
    private void DrawNumber(GlcCanvas g, string value, float centerX, float y, float height)
    {
        float advance = height * .60f;
        float x = centerX - value.Length * advance / 2;
        foreach (char character in value)
        {
            GameTexture? digit = char.IsAsciiDigit(character) ? resources.Get(ResourceMap.Digit(character)) : null;
            if (digit != null)
                g.DrawImage(digit, new RectangleF(x - height * .10f, y, height * .8f, height));
            else Label(g, character.ToString(), x + advance / 2, y + height * .2f, height * .60f, Color.Gold, true);
            x += advance;
        }
    }
    private void DrawHealthBar(GlcCanvas g, int player, int hp, int maxHp, RectangleF outer)
    {
        float ratio = Math.Clamp(hp / (float)Math.Max(1, maxHp), 0, 1);
        var map = ResourceMap.HpInterior;
        var inner = new RectangleF(outer.X + outer.Width * map.X, outer.Y + outer.Height * map.Y,
            outer.Width * map.Width, outer.Height * map.Height);
        g.FillRectangle(Color.Black, inner);
        var fill = resources.Get(ResourceMap.HpFill(player));
        if (ratio > 0)
        {
            float width = inner.Width * ratio;
            float targetX = player == 0 ? inner.X : inner.Right - width;
            if (fill != null)
            {
                float sourceWidth = fill.Width * ratio;
                float sourceX = player == 0 ? 0 : fill.Width - sourceWidth;
                g.DrawImage(fill, new RectangleF(targetX, inner.Y, width, inner.Height),
                    new RectangleF(sourceX, 0, sourceWidth, fill.Height));
            }
            else { g.FillRectangle(player == 0 ? Blue : Red, targetX, inner.Y, width, inner.Height); }
        }
        if (!Picture(g, ResourceMap.HpFrame, outer))
        { g.DrawRectangle(Color.Goldenrod, inner.X, inner.Y, inner.Width, inner.Height); }
    }
    private void DrawHud(GlcCanvas g, int player)
    {
        var p = Session.Combat.Players[player]; float x = player == 0 ? 30 : 735; Color color = player == 0 ? Blue : Red;
        Panel(g, new(x, 25, 435, 130));
        Label(g, $"{player + 1}P   공격력", x + 18, 37, 23, color);
        DrawNumber(g, p.Power.ToString(), x + 225, 28, 48);
        var bar = new RectangleF(x + 8, 66, 419, 419f * 204 / 1024);
        DrawHealthBar(g, player, p.Hp, p.MaxHp, bar);
        DrawNumber(g, $"{p.Hp}/{p.MaxHp}", x + 217, 86, 38);
        Panel(g,new(x+8,152,230,28));
        Label(g,p.SpecialCooldown<=0?"장풍 준비 완료":$"장풍 대기 {p.SpecialCooldown:0.0}초",x+20,155,17,color);
    }
    private void DrawProjectiles(GlcCanvas g)
    {
        foreach(var shot in Session.Combat.Projectiles)
        {
            int frame=(int)((Session.Combat.Time-shot.Started)*24)%ResourceMap.Frames;
            float size=ResourceMap.ProjectileSize;
            var saved=g.Save();g.TranslateTransform(shot.X,shot.Y);g.ScaleTransform(shot.Direction,1);
            if(!Picture(g,ResourceMap.Projectile(shot.Owner,frame),new(-size/2,-size/2,size,size)))
            { g.FillEllipse(shot.Owner==0?Blue:Red,-29,-22,58,44); }
            g.Restore(saved);
        }
    }
    private void DrawHitEffects(GlcCanvas g)
    {
        foreach (var effect in Session.Combat.HitEffects)
        {
            int frame = (int)((Session.Combat.Time - effect.Started) * ResourceMap.ShockwaveFps);
            if (frame < 0 || frame >= ResourceMap.ShockwaveFrames) continue;
            float size = effect.Heavy ? ResourceMap.HeavyImpactSize : ResourceMap.ShockwaveSize;
            var saved = g.Save(); g.TranslateTransform(effect.X, effect.Y);
            g.ScaleTransform(effect.Direction, 1);
            Picture(g, effect.Heavy ? ResourceMap.HeavyImpact(frame) : ResourceMap.Shockwave(frame), new(-size / 2, -size / 2, size, size));
            g.Restore(saved);
        }
    }
    private readonly string[] animationStates = ["", ""];
    private readonly double[] animationStarts = new double[2];
    private void DrawFighter(GlcCanvas g, int player)
    {
        var p = Session.Combat.Players[player]; Color color = player == 0 ? Blue : Red;
        string state = p.Hp == 0 ? "11_defeat" : p.Stun > 0 ? (p.Grounded ? "09_ground_hit" : "10_air_hit") : p.Charging ? "13_heavy_charge" : p.Attack is { Heavy: true } ? "14_heavy_attack" : p.Attack is { Dive: true } ? "15_jump_attack" : p.Attack is { Special: true } ? "16_special_cast" : p.Attack is { } atk ? atk.Height switch
        { AttackHeight.High => "05_attack_high", AttackHeight.Low => "06_attack_low", _ => "04_attack_middle" } :
            !p.Grounded ? "07_jump" : p.Crouching ? "08_duck" : p.Moving ? (p.MovingBackward ? "12_backwalk" : "02_move_right") : "01_idle";
        if (animationStates[player] != state || Session.Combat.Time < animationStarts[player])
        { animationStates[player] = state; animationStarts[player] = Session.Combat.Time; }
        double animationAge = Session.Combat.Time - animationStarts[player];
        int frame = Math.Min(ResourceMap.Frames - 1, (int)(animationAge * ResourceMap.FramesPerSecond));
        if (state == "01_idle") frame = ResourceMap.IdleFrame(p.IdleTime);
        if (state is "02_move_right" or "12_backwalk") frame = ResourceMap.WalkFrame(p.WalkDistance);
        if (state == "08_duck") frame = ResourceMap.CrouchPose(p.CrouchTime);
        if (p.Hp == 0) frame = Math.Min(ResourceMap.Frames - 1,
            (int)((Session.Combat.Time - Session.Combat.ResultStarted) * ResourceMap.DefeatFps));
        if (p.Hp > 0 && p.Attack is { } attack) frame = Math.Min(ResourceMap.Frames - 1, (int)((Session.Combat.Time - attack.Started) / (GameBalance.Startup + GameBalance.Active + GameBalance.Recovery) * ResourceMap.Frames));
        if (p.Hp > 0 && p.Charging) frame = Math.Min(11, (int)((Session.Combat.Time - p.ChargeStarted) / GameBalance.ChargeTime * 12));
        if (p.Hp > 0 && p.Attack is { Heavy: true } heavy) frame = ResourceMap.HeavyFrame(Session.Combat.Time - heavy.Started);
        if (p.Hp > 0 && p.Attack is { Dive: true } dive) frame = ResourceMap.DiveFrame(dive, Session.Combat.Time);
        if (p.Hp > 0 && p.Attack is { Special: true } cast) frame = ResourceMap.CastFrame(Session.Combat.Time - cast.Started);
        if (p.Hp > 0 && p.Crouching && p.Attack is { Height: AttackHeight.Low } lowAttack)
            frame = ResourceMap.CrouchedAttackFrame(Session.Combat.Time - lowAttack.Started);
        var image = resources.Get(ResourceMap.Fighter(player, state, frame));
        g.FillEllipse(Color.FromArgb(100, 0, 0, 0), p.X - 55, GameBalance.Floor - 8, 110, 16);
        if (image == null)
        { g.FillRectangle(color, p.Body); }
        else
        {
            // 원본의 공통 캔버스/발 기준을 유지. 피격 상자 높이와 표시 배율은 독립.
            var saved = g.Save(); g.TranslateTransform(p.X, p.Y);
            g.ScaleTransform(p.Facing == ResourceMap.SourceFacing ? 1 : -1, 1);
            g.DrawImage(image, ResourceMap.SpriteRectangle); g.Restore(saved);
        }
        Label(g, $"{player + 1}P", p.X, p.Y + 10, 18, color, true);
    }
    private bool Picture(GlcCanvas g, string path, RectangleF rect)
    {
        var image = resources.Get(path); if (image == null) return false;
        g.DrawImage(image, rect); return true;
    }
    private static void Panel(GlcCanvas g, RectangleF rect)
    { g.FillRectangle(Color.FromArgb(215, 16, 23, 36), rect); }
    private static void Label(GlcCanvas g, string text, float x, float y, float size, Color color, bool centered = false)
    { g.Text(text,x,y,size,color,centered); }
    private static void Button(GlcCanvas g, string text, bool enabled = true)
    {
        var color = enabled ? Color.FromArgb(218, 167, 66) : Color.FromArgb(90, 90, 90);
        g.FillRectangle(color, ButtonArea); Label(g, text, 600, 558, 24, Color.FromArgb(20, 24, 32), true);
    }
}
