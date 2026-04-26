using BepInEx;

namespace PaleAutomaton;

[BepInAutoPlugin(id: "io.github.astrum-nova.paleautomaton")]
public partial class PaleAutomatonPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
    }
}