# SSR Debug Workflow

## Purpose

This document records the current Codex workflow for:

- automatic Unity screenshot capture
- automatic inspection of debug images
- repeatable SSR debugging in `Func-3`

Use this file as the handoff note for later Codex sessions.

## Project Paths

- SSR code:
  - `X:\Assets\Func-3\SSR.shader`
  - `X:\Assets\Func-3\SSRRenderPass.cs`
  - `X:\Assets\Func-3\SSRRenderFeature.cs`
- renderer asset:
  - `X:\Assets\Settings\URP-HighFidelity-Renderer.asset`
- capture system:
  - `X:\Assets\PostProcessDebugSystem\Editor\PostProcessAutoCapture.cs`
  - `X:\Assets\PostProcessDebugSystem\Editor\SSRRemoteCaptureBridge.cs`
  - `X:\Assets\PostProcessDebugSystem\Editor\EditorPlayModeCapture.cs`
- capture output:
  - `X:\Assets\DebugCaptures\`

## Current SSR Baseline

Current intent is a clean standard SSR baseline:

- projected ray from current pixel
- screen-space DDA stepping
- depth crossing hit test
- binary refine
- final blend from hit color

No object-id pass, no planar special-case reject, no custom self-hit system.

## How Auto Capture Works

### Trigger

Create the trigger file:

```powershell
Set-Content -Path 'X:\Assets\DebugCaptures\.ssr_capture_request' -Value 'capture=YYYYMMDD_HHMMSS' -Encoding ASCII
```

The editor bridge consumes this file automatically.

### Capture Output

Each capture writes:

- `SSRRenderFeature_*.png`
- `SSRRenderFeature_*_Report.txt`
- `SSRRenderFeature_*_Console.txt`

All outputs go to:

- `X:\Assets\DebugCaptures\`

### Capture Mode

The current reliable capture path is editor-side window capture, not runtime screen capture.

That means:

- Unity must be running
- single visible Unity main instance is preferred
- stale editor state sometimes requires a cold restart

## Recommended Operating Procedure

### 1. Keep a single Unity main instance

Before capture, ensure only one visible editor instance exists.

Typical check:

```powershell
Get-CimInstance Win32_Process |
  Where-Object { $_.Name -eq 'Unity.exe' -and $_.CommandLine -notmatch '-batchMode' } |
  Select-Object ProcessId, CommandLine
```

### 2. If capture state gets weird, cold restart Unity

When triggers are consumed but no new png appears, or debug/final state seems stale:

1. close all Unity processes
2. reopen only one editor instance
3. trigger capture again

Launch command:

```powershell
$exe='C:\Program Files\Unity\Hub\Editor\2021.3.9f1\Editor\Unity.exe'
[System.Diagnostics.Process]::Start($exe, '-projectPath X:\') | Out-Null
```

### 3. Trigger only one capture per change batch

Do not trigger after every tiny edit.

Use this loop instead:

1. batch a small set of SSR edits
2. trigger one capture
3. inspect image and report
4. decide the next batch

## Debug Modes

`SSRRenderPass.cs` writes `_SSRDebugMode`.

Useful values:

- `0`: final result
- `17`: reflection UV
- `18`: hit mask

If runtime asset sync is unreliable, temporarily hardcode the pass:

```csharp
int debugMode = 18;
```

Then restore:

```csharp
int debugMode = 0;
```

## What To Inspect

### Final Result

Look for:

- broken reflection shapes
- obvious self-hit on the cube
- edge streaks from screen bounds
- whether reflection disappears entirely

### Hit Mask

Use this to answer:

- are there too many hits or too few hits
- are hits concentrated near the reflector or spread into huge wedges
- did a change improve continuity or only suppress output

### Reflection UV

Use this to answer:

- where the hit is sampling from
- whether the mapping is folded, stretched, or arcing
- whether the problem is hit position, not blend strength

## Current Findings

### Stable conclusions

- This is not mainly a parameter problem.
- Main issues come from hit logic and projected screen-space mapping.
- Simplifying the scene helped isolate the problem.
- Pure standard SSR still shows strong near-surface self-hit behavior on the cube.

### Current implementation direction

- keep standard SSR only
- avoid adding non-standard fixup systems
- continue improving DDA stepping and hit acceptance

## Known Capture Caveats

- report `runtimeDebugStep` can be stale relative to the actual running pass
- when that happens, trust the image and current pass code more than the report field
- if necessary, force debug mode in `SSRRenderPass.cs`

## Suggested Next Session Start

1. open this file
2. inspect:
   - `X:\Assets\Func-3\SSR.shader`
   - `X:\Assets\Func-3\SSRRenderPass.cs`
   - latest files in `X:\Assets\DebugCaptures\`
3. verify only one Unity main instance is running
4. trigger one fresh capture
5. compare `HitMask`, `ReflectionUV`, and final image before editing
