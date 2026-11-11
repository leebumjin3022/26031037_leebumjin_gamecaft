
namespace dicefight;
// 실제 경로, 프레임 수, 속도, 원본 방향을 이곳에서 교체합니다.
public static class ResourceMap
{
    public static readonly string Root = Path.Combine(AppContext.BaseDirectory, "resource");
    public const string Main = "bcakground/mainground.png", Dice = "bcakground/diceground.png", Arena = "bcakground/battleground.png";
    public const string Title = "logo/title.png";
    public const string BlueWin = "logo/bluewin.png", RedWin = "logo/redwin.png";
    public const string Music = "bgm/Asian_Drums-Kevin_Maclead.mp3";
    public const int Frames = 12, RotationFrames = 12;
    public const double FramesPerSecond = 20;
    public const double IdleFramesPerSecond = 3.6; // 순환 호흡 주기 약 3.33초
    public const int CrouchFrame = 6; // 7번 프레임: 가장 낮게 웅크린 자세 유지
    public const double CrouchFps = 40; // 웅크림 전용, 최저 자세까지 .15초 (기존 .30초)
    public static int CrouchPose(double seconds) => Math.Min(CrouchFrame, (int)(seconds * CrouchFps));
    // 아래 키 유지 중에는 하단 공격의 서 있는 시작/끝 프레임을 사용하지 않는다.
    public static int CrouchedAttackFrame(double age)
    {
        if (age < GameBalance.Startup) return 4;
        if (age < GameBalance.Startup + GameBalance.Active)
            return 5 + Math.Min(1, (int)((age - GameBalance.Startup) / GameBalance.Active * 2));
        return 7 + Math.Min(1, (int)((age - GameBalance.Startup - GameBalance.Active) / GameBalance.Recovery * 2));
    }
    public const double WalkCycleDistance = 140; // 걷기 12프레임 한 주기의 실제 이동 거리 px
    public static int IdleFrame(double seconds)
    {
        return (int)(seconds * IdleFramesPerSecond) % Frames;
    }
    public static int WalkFrame(double distance) => (int)(distance / WalkCycleDistance * Frames) % Frames;
    public static string Digit(char digit) => $"ui/numbers/{digit}.png";
    public const string HpFrame = "ui/health/hp_frame.png";
    public static string HpFill(int player) => player == 0 ? "ui/health/hp_fill_blue.png" : "ui/health/hp_fill_red.png";
    public static readonly RectangleF HpInterior = new(64f / 1024, 48f / 204, 896f / 1024, 112f / 204);
    public const float HeavyImpactSize = 185;
    public static string HeavyImpact(int frame) => $"effects/heavy_impact/frame_{frame + 1:00}.png";
    public static int HeavyFrame(double age)
    {
        if (age < GameBalance.HeavyStartup) return Math.Min(3, (int)(age / GameBalance.HeavyStartup * 4));
        if (age < GameBalance.HeavyStartup + GameBalance.HeavyActive)
            return 4 + Math.Min(2, (int)((age - GameBalance.HeavyStartup) / GameBalance.HeavyActive * 3));
        return 7 + Math.Min(4, (int)((age - GameBalance.HeavyStartup - GameBalance.HeavyActive) / GameBalance.HeavyRecovery * 5));
    }
    public static int DiveFrame(AttackState attack, double time)
    {
        double age = Math.Max(0, time - attack.Started);
        if (attack.LandedAt >= 0)
            return 9 + Math.Min(2, (int)((time - attack.LandedAt) / attack.Recovery * 3));
        return age < attack.Startup ? Math.Min(2, (int)(age / attack.Startup * 3)) : Math.Min(8, 3 + (int)((age - attack.Startup) * 18));
    }
    public const float ProjectileSize=100;
    public static string Projectile(int owner,int frame) => $"effects/projectile_{(owner==0?"blue":"red")}/frame_{frame+1:00}.png";
    public static int CastFrame(double age) => age<GameBalance.CastStartup
        ? Math.Min(5,(int)(age/GameBalance.CastStartup*6))
        : 6+Math.Min(5,(int)((age-GameBalance.CastStartup)/GameBalance.CastRecovery*6));
    public const string ClashImage = "logo/cancel.png";
    public const int ShockwaveFrames = 12;
    public const double ShockwaveFps = 32;
    public const double ShockwaveDuration = ShockwaveFrames / ShockwaveFps;
    public const float ShockwaveSize = 140;
    public static string Shockwave(int frame) => $"effects/shockwave/frame_{frame + 1:00}.png";
    public const float SpriteCanvasSize = 200; // 512px 원본 캔버스를 모든 자세에서 같은 200px로 표시
    public const float SpriteFootY = 478f / 512; // 원본 공통 바닥 기준 (알파 하단 476px)
    public static RectangleF SpriteRectangle => new(-SpriteCanvasSize / 2,
        -SpriteCanvasSize * SpriteFootY, SpriteCanvasSize, SpriteCanvasSize);
    public const double DefeatFps = 12; // 사망 12장 약 1초, 마지막 자세 유지
    public const double ResultDelay = 1.15; // 사망 동작이 보인 뒤 결과 패널 표시
    public const int SourceFacing = 1; // 원본은 오른쪽을 봄. 왼쪽은 반전.
    public static string Face(int value) => $"dice/faces/face_{value}.png";
    public static string Rotation(int frame) => $"dice/rotation/rotation_{frame + 1:00}.png";
    public static string Fighter(int player, string state, int frame) => $"team {(player == 0 ? "blue" : "red")}/{state}/frame_{frame + 1:00}.png";
}
