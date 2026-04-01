# MaterialFX Resource Layout

## Core folders
- `Assets/MaterialFX/Common/Common_LitLibrary`
- `Assets/MaterialFX/NPR/NPR-Core`
- `Assets/MaterialFX/SSS/SSS-09_Standard`

## NPR structure policy
- `NPR-Core`: only the production shader/material pipeline (current base: former NPR-3).
- `NPR Look`: parameter preset operations (menu/scripts), no separate shader folder.
- `NPR Tools`: workflow operations (lighting presets, texture import optimization, turntable capture, scene fusion).

## Naming rules
- Core template material: `M_NPR3_CharacterAdvancedTemplate.mat` (legacy name kept for GUID stability).
- Converted character materials: `<slot>_NPR3.mat`
- Face-specialized material: `0_NPR3_Face_NPR6.mat`

## Cleanup rule
- Removed legacy core branches: `NPR-0`, `NPR-1`, `NPR-2`.
- Keep exactly one active face material variant; remove suffix chains.

## Script anchors
- `Assets/AllEffectsLab/Editor/DynamicFxLabBuilder.cs`
- `Assets/AllEffectsLab/Editor/Npr4StylePresets.cs`
- `Assets/AllEffectsLab/Editor/NprAdvancedWorkflowTools.cs`
- `Assets/AllEffectsLab/Editor/NprPreviewControlPanel.cs`
