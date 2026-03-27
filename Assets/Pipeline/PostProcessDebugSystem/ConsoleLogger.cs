using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// 通用Console日志收集器
/// 自动启动，持续收集运行时日志
/// </summary>
public class ConsoleLogger : MonoBehaviour
{
    private static ConsoleLogger instance;
    private List<LogEntry> logs = new List<LogEntry>();
    private const int MAX_LOGS = 500;

    public struct LogEntry
    {
        public string message;
        public string stackTrace;
        public LogType type;
        public System.DateTime timestamp;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (instance == null)
        {
            GameObject obj = new GameObject("[ConsoleLogger]");
            obj.hideFlags = HideFlags.HideAndDontSave;
            instance = obj.AddComponent<ConsoleLogger>();
            DontDestroyOnLoad(obj);
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Application.logMessageReceived += OnLogReceived;
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= OnLogReceived;
    }

    private void OnLogReceived(string message, string stackTrace, LogType type)
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
        if (instance == null) return null;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Console Log Export");
        sb.AppendLine("=" + new string('=', 78));
        sb.AppendLine($"Time: {System.DateTime.Now}");
        sb.AppendLine($"Total: {instance.logs.Count} | Showing Last: {Mathf.Min(lastN, instance.logs.Count)}");
        sb.AppendLine("=" + new string('=', 78));
        sb.AppendLine();

        int start = Mathf.Max(0, instance.logs.Count - lastN);
        for (int i = start; i < instance.logs.Count; i++)
        {
            var log = instance.logs[i];
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
        if (instance == null) return (0, 0, 0);

        int errors = 0, warnings = 0, infos = 0;
        foreach (var log in instance.logs)
        {
            if (log.type == LogType.Error || log.type == LogType.Exception) errors++;
            else if (log.type == LogType.Warning) warnings++;
            else infos++;
        }
        return (errors, warnings, infos);
    }

    public static List<LogEntry> GetLogs(int lastN = 100)
    {
        if (instance == null) return new List<LogEntry>();

        int start = Mathf.Max(0, instance.logs.Count - lastN);
        int count = instance.logs.Count - start;
        return instance.logs.GetRange(start, count);
    }
}
