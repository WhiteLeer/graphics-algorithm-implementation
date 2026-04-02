# MaterialFX Resource Layout

## Core folders
- `Assets/MaterialFX/Common/Common_LitLibrary`
- `Assets/MaterialFX/NPR/NPR-Core`
- `Assets/MaterialFX/SSS/Standard`

## NPR structure policy
- `NPR-Core`: production shader/material pipeline only.
- `Materials/Profiles/{Genshin|HSR|ZZZ}`: runtime style sets.
- `Materials/LockedProfiles/{Genshin|HSR|ZZZ}`: locked snapshot sets for rollback.
- `Textures/Generated`: source/generated maps.
- `Textures/Profiles/{Genshin|HSR|ZZZ}`: per-style output maps.

## Naming rules
- Core template material: `M_NPR3_CharacterAdvancedTemplate.mat` (legacy name kept for GUID stability).
- Converted character materials: `<slot>_NPR3.mat`
- Face-specialized material: `0_NPR3_Face_NPR6.mat`

## Tool script anchors
- `Assets/AllEffectsLab/Editor/NPR/NprStyleSwitcherWindow.cs`
- `Assets/AllEffectsLab/Editor/NPR/NprPackedLightMapGenerator.cs`
- `Assets/AllEffectsLab/Editor/NPR/NprMaskMapGenerator.cs`
- `Assets/AllEffectsLab/Editor/NPR/NprTextureChannelValidator.cs`
- `Assets/AllEffectsLab/Editor/NPR/NprStyleLockManager.cs`
- `Assets/AllEffectsLab/Editor/NPR/NprOutlineNormalBaker.cs`

