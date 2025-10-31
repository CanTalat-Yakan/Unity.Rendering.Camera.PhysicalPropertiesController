# Unity Essentials

This module is part of the Unity Essentials ecosystem and follows the same lightweight, editor-first approach.
Unity Essentials is a lightweight, modular set of editor utilities and helpers that streamline Unity development. It focuses on clean, dependency-free tools that work well together.

All utilities are under the `UnityEssentials` namespace.

```csharp
using UnityEssentials;
```

## Installation

Install the Unity Essentials entry package via Unity's Package Manager, then install modules from the Tools menu.

- Add the entry package (via Git URL)
    - Window → Package Manager
    - "+" → "Add package from git URL…"
    - Paste: `https://github.com/CanTalat-Yakan/UnityEssentials.git`

- Install or update Unity Essentials packages
    - Tools → Install & Update UnityEssentials
    - Install all or select individual modules; run again anytime to update

---

# Camera Physical Properties Controller

> Quick overview: Preset‑driven mapping from normalized multipliers to physical camera settings (aperture, focal length, ISO, shutter), applied to Cinemachine Lens or the built‑in Physical Camera, with optional volume effects for ISO noise, zoom vignette, and lens distortion.

Physical camera parameters are derived each frame from normalized inputs using a preset. Values are applied to either Cinemachine’s Lens Physical Properties or the built‑in camera’s physical properties, while associated post‑processing volumes are weighted to reflect ISO noise, zoom, and focal‑length‑dependent distortion.

![screenshot](Documentation/Screenshot.png)

## Features
- Preset‑driven physical ranges
  - `CameraPresetData` defines Sensor Size, F‑stop range, Focal Length range, ISO range, and a Shutter Speed “1/x” range, plus a Lens Distortion toggle
- Normalized controls (0..1)
  - `ZoomMultiplier`, `IsoMultiplier`, `ShutterSpeedMultiplier` with global `EffectsStrength`
- Outputs and applied values
  - Physical readouts: `FStop`, `FocalLength`, `ISO`, `ShutterSpeed` (seconds), `SensorSize`
  - Applied to Cinemachine (`CinemachineCamera.Lens.*`) if present, otherwise to the built‑in `Camera` (with `usePhysicalProperties = true`)
  - Field of View computed from focal length and sensor height
- Volume integration (optional)
  - A `Resources` prefab `UnityEssentials_Prefab_CameraNoiseVolumes` is instantiated and expected to contain child `Volume`s:
    - "Iso Volume" (grain/noise) weight = `IsoMultiplier * EffectsStrength * isoNoiseWeight(ISO)` (0 at ISO ≤ 400, 1 at ISO ≥ 6400)
    - "Zoom Volume" weight = `ZoomMultiplier * EffectsStrength`
    - "Fov Volume" weight = `(LensDistortion ? 1 - ZoomMultiplier : 0) * focalLengthFactor * EffectsStrength` (more weight at wider focal lengths)
- Cinemachine optional
  - Works with or without a `CinemachineCamera` on the same GameObject

## Requirements
- Unity 6000.0+ (per package manifest)
- A Camera on the same GameObject (enforced by `[RequireComponent]`)
- A `CameraPresetData` asset assigned to the controller
- Optional: Cinemachine (for Lens PhysicalProperties application)
- Optional: Volume system (URP/HDRP) and a prefab at `Resources/UnityEssentials_Prefab_CameraNoiseVolumes` with child volumes named "Iso Volume", "Zoom Volume", and "Fov Volume"
  - Without this prefab, the code expects those `Volume` references; ensure it exists when volume effects are desired

## Usage
1) Prepare a preset
   - Create and configure a `CameraPresetData` asset with:
     - Sensor Size (Vector2 width×height)
     - Ranges: F‑stop, Focal Length, ISO, and Shutter Speed (expressed as a “1/x” range)
     - Lens Distortion toggle (enables FOV distortion weighting)
2) Add the controller
   - Add `CameraPhysicalPropertiesController` to a Camera and assign the preset
   - If using Cinemachine 3, ensure a `CinemachineCamera` component is present on the same GameObject
3) Optional: Add volume effects
   - Place a prefab named `UnityEssentials_Prefab_CameraNoiseVolumes` in a `Resources/` folder with child `Volume`s named exactly:
     - "Iso Volume", "Zoom Volume", and "Fov Volume"
   - Configure those volumes with your desired post‑processing overrides (grain/vignette/distortion)
4) Drive multipliers
   - Adjust `ZoomMultiplier`, `IsoMultiplier`, `ShutterSpeedMultiplier`, and `EffectsStrength` at runtime or via script
   - Read physical outputs (e.g., `FStop`, `ISO`) for UI/telemetry if needed

## How It Works
- Initialization
  - Enables `usePhysicalProperties` on the built‑in Camera and caches a `CinemachineCamera` if present
  - Instantiates the `UnityEssentials_Prefab_CameraNoiseVolumes` prefab and finds child volumes by name
- Mapping to physical values
  - F‑stop = preset.FStopRange.Lerp(ZoomMultiplier)
  - Focal Length = max(1, preset.FocalLengthRange.Lerp(ZoomMultiplier))
  - ISO = round(preset.ISORange.Lerp(IsoMultiplier))
  - Shutter Speed = 1 / (1 − preset.ShutterSpeed1OverXRange.Slerp(ShutterSpeedMultiplier, 0.5))
  - Field of View = `Camera.FocalLengthToFieldOfView(FocalLength, SensorSize.y)`
- Applying values
  - If Cinemachine is present, values are written to `CinemachineCamera.Lens` (including PhysicalProperties)
  - Otherwise, values are set on `Camera` (aperture, focalLength, iso, `shutterSpeed = ShutterSpeedUnscaled`)
- Volume weights
  - ISO Noise weight scales from 0 at ISO ≤ 400 to 1 at ISO ≥ 6400 (linear in between) and is multiplied by `IsoMultiplier * EffectsStrength`
  - Zoom volume uses `ZoomMultiplier * EffectsStrength`
  - FOV/Lens Distortion weight is `(LensDistortion ? 1 − ZoomMultiplier : 0)` times a focal‑length factor that increases toward wide angles, times `EffectsStrength`

## Notes and Limitations
- Resources dependency: The controller expects a `Resources` prefab named `UnityEssentials_Prefab_CameraNoiseVolumes` with specific child names. If it is absent, attempting to animate volume weights will fail; include the prefab or guard accesses
- Pipeline dependency for volumes: Post‑processing `Volume`s require URP/HDRP (or a compatible volume system) and appropriate overrides
- Application precedence: If both Cinemachine and the built‑in camera are present, Cinemachine values are written; ensure only one system controls these properties
- Performance: Values are updated every frame; consider driving multipliers only when necessary to limit work
- Units and realism: Ranges in the preset determine realism; choose values appropriate for your art direction and pipeline

## Files in This Package
- `Runtime/CameraPhysicalPropertiesController.cs` – Normalized‑to‑physical mapping and application (Cinemachine or built‑in Camera)
- `Runtime/CameraPresetData.cs` – Preset asset with physical ranges and options (sensor size, f‑stop, focal length, ISO, shutter, lens distortion)
- `Runtime/UnityEssentials.CameraPhysicalPropertiesController.asmdef` – Runtime assembly definition
- `Resources/UnityEssentials_Prefab_CameraNoiseVolumes` – Optional volume prefab (expected child names: Iso/Zoom/Fov Volume)

## Tags
unity, camera, physical, aperture, f‑stop, focal‑length, iso, shutter, exposure, cinemachine, volume, lens‑distortion, runtime
