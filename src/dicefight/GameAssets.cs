using System.Buffers.Binary;
namespace dicefight;

// PNG는 glc2d의 WIC/Direct2D 텍스처로 직접 로드한다. GDI+ 렌더링은 사용하지 않는다.
internal sealed class GameTexture : IDisposable
{
    public G2Texture Texture { get; }
    public int Width { get; }
    public int Height { get; }
    public GameTexture(string path)
    {
        using(var file=File.OpenRead(path))
        {
            Span<byte> header=stackalloc byte[24]; file.ReadExactly(header);
            Width=BinaryPrimitives.ReadInt32BigEndian(header[16..20]);
            Height=BinaryPrimitives.ReadInt32BigEndian(header[20..24]);
        }
        Texture=new G2Texture(path);
    }
    public void Dispose()=>Texture.Dispose();
}
internal sealed class GameAssets : IDisposable
{
    private readonly Dictionary<string,GameTexture> textures=[];
    private G2Audio? music;
    public int TextureCount=>textures.Count;
    public string AudioStatus { get; private set; }="not started";
    public GameTexture Get(string relative)
    {
        if(!textures.TryGetValue(relative,out var texture))
            textures[relative]=texture=new GameTexture(Path.Combine(ResourceMap.Root,relative));
        return texture;
    }
    public void StartMusic()
    {
        if(G2AudioContext.Instance?.Audio==null){AudioStatus="no output device";return;}
        music=new G2Audio(Path.Combine(ResourceMap.Root,ResourceMap.Music));
        G2AudioContext.Instance.MasteringVoice?.SetVolume(.35f);
        music.Play(isLooping:true);AudioStatus="G2Audio playing";
    }
    public void Dispose()
    {
        music?.Dispose();
        foreach(var texture in textures.Values)texture.Dispose();
        textures.Clear();
    }
}
