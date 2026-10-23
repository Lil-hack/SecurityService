using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;

// One-time, request-driven configuration; normal lifecycle belongs to MCP for Unity.
[InitializeOnLoad]
public static class UnityMcpSetup
{
    const string Request = "Temp/UnityMcpSetup.request";
    static bool busy;
    static UnityMcpSetup() { EditorApplication.update += Tick; }

    static async void Tick()
    {
        if (busy || EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Request)) return;
        var locator = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("MCPForUnity.Editor.Services.MCPServiceLocator"))
            .FirstOrDefault(t => t != null);
        if (locator == null) return;
        busy = true;
        File.Delete(Request);
        try
        {
            EditorPrefs.SetBool("MCPForUnity.UseHttpTransport", true);
            EditorPrefs.SetString("MCPForUnity.HttpTransportScope", "local");
            EditorPrefs.SetString("MCPForUnity.HttpUrl", "http://127.0.0.1:8080");
            EditorPrefs.SetString("MCPForUnity.UvxPath", Path.GetFullPath(".local/unity-mcp-tools/bin/uvx.exe"));
            EditorPrefs.SetBool("MCPForUnity.TelemetryDisabled", true);
            EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", true);
            EditorPrefs.SetBool("MCPForUnity.SetupCompleted", true);
            var cacheType = locator.Assembly.GetType("MCPForUnity.Editor.Services.EditorConfigurationCache");
            var cache = cacheType.GetProperty("Instance").GetValue(null);
            cacheType.GetMethod("Refresh").Invoke(cache, null);
            var bridge = locator.GetProperty("Bridge").GetValue(null);
            var start = (Task<bool>)bridge.GetType().GetMethod("StartAsync").Invoke(bridge, null);
            bool connected = await start;
            File.WriteAllText("Temp/UnityMcpSetup.result", connected ? "Connected" : "Connection failed");
        }
        catch (Exception e) { File.WriteAllText("Temp/UnityMcpSetup.result", e.ToString()); }
        finally { busy = false; }
    }
}
