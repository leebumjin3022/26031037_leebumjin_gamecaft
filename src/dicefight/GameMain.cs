using System.Text.Json;
namespace dicefight;
// 수업 glc2d 실행 구조: G2AppBase.Run → Initialize / Update / Render.
internal sealed class GameMain(bool smoke=false) : G2AppBase
{
    private GameSession session=new();
    private readonly FixedClock clock=new();
    private readonly GlcGameInput controls=new();
    private GameAssets? assets;
    private GlcCanvas? canvas;
    private GameRenderer? renderer;
    private bool focused;
    private int smokeIndex=-1,renderedFrames;
    private List<SmokeScenes.Scene> scenes=[];
    private double nextScene;
    private bool closing;
    public override Size ScreenSize=>GameGlobal.ScreenSize;
    public override string GameName=>GameGlobal.GameName;
    protected override void Initialize()
    {
        AssetAudit.Verify();
        assets=new GameAssets();canvas=new GlcCanvas(this);renderer=new GameRenderer(session,assets);
        assets.StartMusic();
        if(smoke)scenes=SmokeScenes.Build();
    }
    protected override void Update()
    {
        if(smoke)
        {
            if(closing){Close();return;}
            if(TotalTime<nextScene)return;
            if(++smokeIndex>=scenes.Count)
            {
                // 모든 제출 이미지를 glc2d의 실제 텍스처 로더로 검증한다.
                foreach(string path in AssetAudit.Required.Where(p=>p.EndsWith(".png")))assets!.Get(path);
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"glc2d-smoke.json"),JsonSerializer.Serialize(new{
                    Result="PASS",Engine="G2AppBase / Direct2D",Scenes=scenes.Select(s=>s.Name),Frames=renderedFrames,
                    G2TextureDraws=canvas!.SpriteDraws,G2FontDraws=canvas.TextDraws,LoadedTextures=assets!.TextureCount,Audio=assets.AudioStatus
                },new JsonSerializerOptions{WriteIndented=true}));
                closing=true;return;
            }
            session=scenes[smokeIndex].Create();renderer!.Session=session;nextScene=TotalTime+.15;return;
        }
        bool active=WindowFocused;
        if(!active || active!=focused)
        {
            controls.Clear();Input.Reset();session.Combat.CancelInput();clock.Reset();focused=active;
            return;
        }
        if(Input.IsPhysicalKeyDown(0x01)){Close();return;}
        if(Input.IsButtonDown(MouseButtons.Left) && GameRenderer.ButtonArea.Contains(canvas!.MouseToScene(Input.MousePosition)))
        {session.Action();controls.Clear();clock.Reset();return;}
        controls.Poll(Input);
        clock.Advance(DeltaTime,()=>{
            var before=session.Screen;session.Step(controls.Read(0),controls.Read(1));controls.ClearEdges();
            if(before!=session.Screen)controls.Clear();
        });
    }
    protected override void Render()
    {
        if(closing)return;
        canvas!.BeginViewport();renderer!.DrawScene(canvas);renderedFrames++;
        if(!smoke && !focused)
        {
            canvas.FillRectangle(Color.FromArgb(215,16,23,36),new(320,300,560,100));
            canvas.Text("일시 정지 · 창을 클릭하여 계속",600,330,24,Color.White,true);
        }
    }
    public override void Dispose(){canvas?.Dispose();assets?.Dispose();base.Dispose();}
}
