using BepInEx;
using Silksong.GameObjectDump.Logging.Loggables;

namespace Silksong.GameObjectDump;

/// <summary>
/// Helper mod for dumping GameObjects to a text file.
/// </summary>
[BepInAutoPlugin(id: "io.github.syyephenomenol.gameobjectdump")]
public partial class GameObjectDumpPlugin : BaseUnityPlugin
{
    internal static GameObjectDumpPlugin? Instance { get; private set; }

    internal static void Log(string text)
    {
        if (Instance == null) return;
        Instance.Logger.LogInfo(text);
    }

    internal static void LogWarning(string text)
    {
        if (Instance == null) return;
        Instance.Logger.LogWarning(text);
    }

    internal static void LogError(string text)
    {
        if (Instance == null) return;
        Instance.Logger.LogError(text);
    }

    private void Awake()
    {
        Instance = this;
        LoggableRegistry.Initialize();
        Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
    }
}
