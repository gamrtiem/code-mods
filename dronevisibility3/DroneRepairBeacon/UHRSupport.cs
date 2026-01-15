#if DEBUG
using System.Reflection;

namespace DroneRepairBeacon;

internal static class UHRSupport
{
    internal static void hotReload(Assembly assembly, string path)
    {
        UnityHotReloadNS.UnityHotReload.LoadNewAssemblyVersion(assembly, path);
    }
}
#endif