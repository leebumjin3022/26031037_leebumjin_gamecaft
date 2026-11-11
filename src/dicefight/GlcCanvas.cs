using System.Numerics;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;
using Color=System.Drawing.Color;
namespace dicefight;

// glc2d 렌더 타깃 위의 작은 그리기 도우미. 모든 이미지/문자는 G2Texture/G2Font를 통한다.
internal sealed class GlcCanvas : IDisposable
{
    private readonly G2AppBase app;
    private readonly ID2D1SolidColorBrush brush;
    private readonly Dictionary<(float,bool),G2Font> fonts=[];
    private ID2D1HwndRenderTarget Target=>app.RenderTarget;
    public int SpriteDraws { get; private set; }
    public int TextDraws { get; private set; }
    public GlcCanvas(G2AppBase app){this.app=app;brush=Target.CreateSolidColorBrush(new Color4(1,1,1,1));}
    public void BeginViewport()
    {
        float width=app.ScreenSize.Width*app.ScaleX,height=app.ScreenSize.Height*app.ScaleY;
        float scale=Math.Min(app.ScaleX,app.ScaleY);
        Target.Transform=Matrix3x2.CreateScale(scale)*Matrix3x2.CreateTranslation((width-GameBalance.Width*scale)/2,(height-GameBalance.Height*scale)/2);
    }
    public PointF MouseToScene(PointF glcMouse)
    {
        var before=Target.Transform;BeginViewport();var matrix=Target.Transform;Target.Transform=before;
        Matrix3x2.Invert(matrix,out var inverse);
        var p=Vector2.Transform(new(glcMouse.X*app.ScaleX,glcMouse.Y*app.ScaleY),inverse);
        return new(p.X,p.Y);
    }
    private static Rect Rect(RectangleF r)=>new(r.X,r.Y,r.Width,r.Height);
    private ID2D1SolidColorBrush Brush(Color c){brush.Color=new(c.R/255f,c.G/255f,c.B/255f,c.A/255f);return brush;}
    public Matrix3x2 Save()=>Target.Transform;
    public void Restore(Matrix3x2 matrix)=>Target.Transform=matrix;
    public void TranslateTransform(float x,float y)=>Target.Transform=Matrix3x2.CreateTranslation(x,y)*Target.Transform;
    public void ScaleTransform(float x,float y)=>Target.Transform=Matrix3x2.CreateScale(x,y)*Target.Transform;
    public void DrawImage(GameTexture texture,RectangleF destination)=>DrawImage(texture,destination,new(0,0,texture.Width,texture.Height));
    public void DrawImage(GameTexture texture,RectangleF destination,RectangleF source)
    {texture.Texture.Draw(Rect(destination),Rect(source));SpriteDraws++;}
    public void FillRectangle(Color color,RectangleF rect)=>Target.FillRectangle(Rect(rect),Brush(color));
    public void FillRectangle(Color color,float x,float y,float width,float height)=>FillRectangle(color,new(x,y,width,height));
    public void FillEllipse(Color color,float x,float y,float width,float height)=>Target.FillEllipse(new Ellipse(new Vector2(x+width/2,y+height/2),width/2,height/2),Brush(color));
    public void DrawRectangle(Color color,float x,float y,float width,float height,float stroke=3)=>Target.DrawRectangle(new Rect(x,y,width,height),Brush(color),stroke);
    public void DrawLine(Color color,float x1,float y1,float x2,float y2,float stroke=2)=>Target.DrawLine(new Vector2(x1,y1),new Vector2(x2,y2),Brush(color),stroke);
    public void Clear(Color color)=>Target.Clear(new Color4(color.R/255f,color.G/255f,color.B/255f,color.A/255f));
    public void Text(string text,float x,float y,float size,Color color,bool centered=false)
    {
        var key=(size,centered);
        if(!fonts.TryGetValue(key,out var font))fonts[key]=font=new G2Font("맑은 고딕",size,FontWeight.Bold,textAlignment:centered?TextAlignment.Center:TextAlignment.Leading);
        font.DrawText(text,new Rect(centered?x-600:x,y,1200,size*3),new Color4(color.R/255f,color.G/255f,color.B/255f,color.A/255f));TextDraws++;
    }
    public void Dispose(){foreach(var font in fonts.Values)font.Dispose();brush.Dispose();}
}
