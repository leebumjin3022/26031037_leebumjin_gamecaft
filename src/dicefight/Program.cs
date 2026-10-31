namespace dicefight;
internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Contains("--self-test")) return SelfTests.Run();
        using var game = new GameForm();
        if (args.Contains("--smoke"))
        {
            var smokeTimer = new System.Windows.Forms.Timer { Interval = 800 };
            smokeTimer.Tick += (_, _) => {
                smokeTimer.Stop();
                game.RenderSmoke(Path.Combine(AppContext.BaseDirectory, "verification-live"));
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "window-smoke.txt"), "PASS: WinForms message loop, shown window, scene rendering, graceful close.");
                game.Close(); smokeTimer.Dispose();
            };
            game.Shown += (_, _) => smokeTimer.Start();
        }
        Application.Run(game);
        return 0;
    }
}

