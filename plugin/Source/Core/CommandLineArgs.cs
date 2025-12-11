using System;

namespace SilksongAgent;

public static class CommandLineArgs
{
    public static int Id { get; private set; } = 0;
    public static float TimeScale { get; private set; } = 1.0f;
    public static bool Manual { get; private set; } = false;

    public static void Parse()
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-id" && i + 1 < args.Length)
            {
                if (int.TryParse(args[i + 1], out int id))
                {
                    Id = id;
                }
            }
            else if (args[i] == "-timescale" && i + 1 < args.Length)
            {
                if (float.TryParse(args[i + 1], out float timeScale))
                {
                    TimeScale = timeScale;
                }
            }
            else if (args[i] == "-manual")
            {
                Manual = true;
            }
        }
    }
}
