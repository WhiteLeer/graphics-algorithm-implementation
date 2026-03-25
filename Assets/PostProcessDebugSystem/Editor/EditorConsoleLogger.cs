using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// Editor模式专用日志收集器
/// 在Editor模式下自动启动，收集Console日志
/// </summary>
[InitializeOnLoad]
public static class EditorConsoleLogger
{
    private static List<LogEntry> logs = new List<LogEntry>();
    private const int MAX_LOGS = 500;

    public struct LogEntry
    {
        public string message;
        public string stackTrace;
        public LogType type;
        public System.DateTime timestamp;
    }

    // 静态构造函数，Editor启动时自动执行
    static EditorConsoleLogger()
    {
        // 订阅日志事件
        Application.logMessageReceived += OnLogReceived;
        Debug.Log("[EditorConsoleLogger] Editor模式日志收集已启动");
    }

    private static void OnLogReceived(string message, string stackTrace, LogType type)
    {
        logs.Add(new LogEntry
        {
            message = message,
            stackTrace = stackTrace,
            type = type,
            timestamp = System.DateTime.Now
        });

        if (logs.Count > MAX_LOGS)
            logs.RemoveAt(0);
    }

    public static string ExportLogs(string path, int lastN = 100)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Console Log Export (Editor Mode)");
        sb.AppendLine("=" + new string('=', 78));
        sb.AppendLine($"Time: {System.DateTime.Now}");
        sb.AppendLine($"Total: {logs.Count} | Showing Last: {Mathf.Min(lastN, logs.Count)}");
        sb.AppendLine("=" + new string('=', 78));
        sb.AppendLine();

        int start = Mathf.Max(0, logs.Count - lastN);
        for (int i = start; i < logs.Count; i++)
        {
            var log = logs[i];
            sb.AppendLine($"[{log.timestamp:HH:mm:ss.fff}] [{log.type}]");
            sb.AppendLine(log.message);
            if (log.type == LogType.Error && !string.IsNullOrEmpty(log.stackTrace))
            {
                sb.AppendLine("Stack:");
                sb.AppendLine(log.stackTrace);
            }
            sb.AppendLine(new string('-', 78));
        }

        File.WriteAllText(path, sb.ToString());
        return path;
    }

    public static (int errors, int warnings, int logs) GetStats()
    {
        int errors = 0, warnings = 0, infos = 0;
        foreach (var log in logs)
        {
            if (log.type == LogType.Error || log.type == LogType.Exception) errors++;
            else if (log.type == LogType.Warning) warnings++;
            else infos++;
        }
        return (errors, warnings, infos);
    }

    public static void ClearLogs()
    {
        logs.Clear();
        Debug.Log("[EditorConsoleLogger] 日志已清空");
    }
}
