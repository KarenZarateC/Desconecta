using UnityEditor;
using UnityEngine;
using MCPForUnity.Editor.Services;

/// <summary>Connects this project to its local development MCP server.</summary>
[InitializeOnLoad]
public static class LocalMcpConnection
{
    static LocalMcpConnection()
    {
        EditorPrefs.SetBool("MCPForUnity.SetupCompleted", true);
        EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", false);
        // Keep automatic reconnect limited to this editor session.
        if (SessionState.GetBool("Desconecta.LocalMcpEnabled", false))
            EditorApplication.delayCall += Connect;
    }

    [MenuItem("Desconecta/Connect Local MCP")]
    public static async void Connect()
    {
        SessionState.SetBool("Desconecta.LocalMcpEnabled", true);
        // The server is local and does not need a cloud account.
        EditorPrefs.SetString("MCPForUnity.HttpUrl", "http://127.0.0.1:8080");
        EditorConfigurationCache.Instance.SetUseHttpTransport(true);
        if (!MCPServiceLocator.Bridge.IsRunning)
        {
            bool connected = await MCPServiceLocator.Bridge.StartAsync();
            Debug.Log("Desconecta local MCP connected: " + connected);
        }
    }
}
