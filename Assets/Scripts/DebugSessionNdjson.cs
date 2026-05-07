using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// NDJSON append para sesión de depuración (no registrar secretos).
/// </summary>
public static class DebugSessionNdjson
{
    const string SessionId = "f20961";

    static string LogPath => Path.Combine(Application.dataPath, "..", "debug-f20961.log");

    public static void Write(string hypothesisId, string location, string message, string dataJsonObject)
    {
        // #region agent log
        try
        {
            long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var sb = new StringBuilder(256);
            sb.Append("{\"sessionId\":\"").Append(SessionId).Append("\",\"hypothesisId\":\"")
                .Append(EscapeJson(hypothesisId)).Append("\",\"location\":\"")
                .Append(EscapeJson(location)).Append("\",\"message\":\"")
                .Append(EscapeJson(message)).Append("\",\"data\":");
            sb.Append(string.IsNullOrEmpty(dataJsonObject) ? "{}" : dataJsonObject);
            sb.Append(",\"timestamp\":").Append(ts).Append("}\n");
            File.AppendAllText(LogPath, sb.ToString());
        }
        catch { }
        // #endregion
    }

    static string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ");
    }
}
