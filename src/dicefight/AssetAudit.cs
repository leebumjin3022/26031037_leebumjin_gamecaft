namespace dicefight;
internal static class AssetAudit
{
    public static string[] Required=>File.ReadAllLines(Path.Combine(AppContext.BaseDirectory,"assets.txt"))
        .Select(s=>s.Trim()).Where(s=>s.Length>0).ToArray();
    public static void Verify()
    {
        var expected=Required.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var actual=Directory.EnumerateFiles(ResourceMap.Root,"*",SearchOption.AllDirectories)
            .Select(p=>Path.GetRelativePath(ResourceMap.Root,p).Replace('\\','/')).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if(!expected.SetEquals(actual))throw new InvalidDataException("누락: "+string.Join(",",expected.Except(actual))+" / 불필요: "+string.Join(",",actual.Except(expected)));
    }
}
