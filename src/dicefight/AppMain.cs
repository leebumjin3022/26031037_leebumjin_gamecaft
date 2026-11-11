namespace dicefight;
internal static class AppMain
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize(); // glc2d RenderForm의 DPI/창 초기화
        try
        {
            if(args.Contains("--self-test"))return SelfTests.Run();
            using var game=new GameMain(args.Contains("--smoke"));
            game.Run();
            return 0;
        }
        catch(Exception error)
        {
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"startup-error.txt"),error.ToString());
            if(!args.Contains("--smoke") && !args.Contains("--self-test"))
                MessageBox.Show(error.Message,"게임 실행 오류",MessageBoxButtons.OK,MessageBoxIcon.Error);
            return 1;
        }
    }
}
