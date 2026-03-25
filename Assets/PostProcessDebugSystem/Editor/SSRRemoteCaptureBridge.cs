using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Allows external tools to trigger SSR auto-capture in the already running Unity editor
/// by creating Assets/DebugCaptures/.ssr_capture_request.
/// </summary>
[InitializeOnLoad]
public static class SSRRemoteCaptureBridge
{
    private const double PollInterval = 0.5;
    private const double CompileSettleSeconds = 1.5;
    private const string AutoCapturePendingKey = "PostProcessAutoCapture.Pending";
    private const double WaitTimeoutSeconds = 240.0;
    private const double IdlePendingResetSeconds = 8.0;

    private static readonly string TriggerFilePath =
        Path.Combine(Application.dataPath, "DebugCaptures", ".ssr_capture_request");

    private static double s_LastPollTime;
    private static double s_LastBusyTime;
    private static bool s_WaitingForCaptureFinish;
    private static double s_WaitStartTime;

    static SSRRemoteCaptureBridge()
    {
        Debug.Log("[SSRRemoteCapture] Initialized.");
        EditorApplication.update += OnEditorUpdate;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.delayCall += ForceInitialPoll;
    }

    private static void ForceInitialPoll()
    {
        s_LastPollTime = 0.0;
        OnEditorUpdate();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            s_WaitingForCaptureFinish = false;
            s_WaitStartTime = 0.0;
        }
    }

    private static void OnEditorUpdate()
    {
        if (EditorApplication.timeSinceStartup - s_LastPollTime < PollInterval)
            return;
        s_LastPollTime = EditorApplication.timeSinceStartup;

        if (s_WaitingForCaptureFinish)
        {
            if (!SessionState.GetBool(AutoCapturePendingKey, false))
            {
                s_WaitingForCaptureFinish = false;
                s_WaitStartTime = 0.0;
            }
            else if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode &&
                     File.Exists(TriggerFilePath) &&
                     s_WaitStartTime > 0.0 &&
                     EditorApplication.timeSinceStartup - s_WaitStartTime > IdlePendingResetSeconds)
            {
                Debug.LogWarning("[SSRRemoteCapture] Pending capture is idle while a new trigger is waiting. Clearing stale pending state.");
                SessionState.SetBool(AutoCapturePendingKey, false);
                s_WaitingForCaptureFinish = false;
                s_WaitStartTime = 0.0;
            }
            else if (s_WaitStartTime > 0.0 && EditorApplication.timeSinceStartup - s_WaitStartTime > WaitTimeoutSeconds)
            {
                Debug.LogWarning("[SSRRemoteCapture] Stale pending capture detected. Resetting pending flag.");
                SessionState.SetBool(AutoCapturePendingKey, false);
                s_WaitingForCaptureFinish = false;
                s_WaitStartTime = 0.0;
            }
            return;
        }

        if (SessionState.GetBool(AutoCapturePendingKey, false))
        {
            s_WaitingForCaptureFinish = true;
            s_WaitStartTime = EditorApplication.timeSinceStartup;
            return;
        }

        bool editorBusy = EditorApplication.isCompiling || EditorApplication.isUpdating;
        if (editorBusy)
        {
            s_LastBusyTime = EditorApplication.timeSinceStartup;
            return;
        }

        if (EditorApplication.timeSinceStartup - s_LastBusyTime < CompileSettleSeconds)
            return;

        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (!File.Exists(TriggerFilePath))
            return;

        if (!TryConsumeTriggerFile())
            return;

        s_WaitingForCaptureFinish = true;
        s_WaitStartTime = EditorApplication.timeSinceStartup;
        Debug.Log("[SSRRemoteCapture] Trigger detected. Starting SSR auto capture.");
        PostProcessAutoCapture.CaptureSSRPlayMenu();
    }

    private static bool TryConsumeTriggerFile()
    {
        try
        {
            File.Delete(TriggerFilePath);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }
}
