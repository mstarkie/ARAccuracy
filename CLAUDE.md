# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity AR application for Magic Leap 2 that tracks AprilTag markers to measure AR tracking accuracy and drift in real-world environments. The primary use case is shipbuilding, where precise spatial alignment between virtual objects and physical structures is critical.

**Target Platform:** Magic Leap 2 (Android-based AR headset)
**Unity Version:** 2023.x+ with Universal Render Pipeline (URP)
**Key Dependencies:**
- Magic Leap Unity SDK 2.6.0
- Unity XR Interaction Toolkit 3.0.9
- Unity XR Hands 1.5.1
- Unity Input System 1.14.2

## Building and Running

### Build for Magic Leap 2
1. Ensure Magic Leap SDK is installed and Unity is configured for Android builds
2. In Unity: File > Build Settings > Android
3. Select the main scene: `Assets/Scenes/SampleScene.unity`
4. Build Settings > Build
5. Deploy the resulting APK to Magic Leap 2 device

### Testing in Editor
The project includes XR Simulation support for basic testing without hardware, but full AprilTag detection requires the Magic Leap 2 device.

## Architecture Overview

### Core AR Tracking Pipeline

The application uses a **tag detection → coordinate transformation → drift measurement** pipeline:

1. **Tag Detection Layer** (`Assets/Scripts/Tags/`)
   - `ITagDetector` interface abstracts marker detection
   - `MagicLeapTagDetector_260.cs` implements Magic Leap 2.6.0 OpenXR Marker Understanding API
   - `TagObservation` struct contains detected tag pose, ID, size, and timestamp
   - Detection is event-driven: detector fires `OnObservation` events when tags are seen

2. **Coordinate System Management** (`Assets/Scripts/Ship/`)
   - `ShipCoordinateRegistry` (ScriptableObject) maps AprilTag IDs to known ship-frame coordinates
   - `TagPlacementController` bridges real-world tag poses to Unity world space
   - Places virtual objects (like a cube) relative to detected tags with configurable offsets
   - **Critical concept:** Maintains a baseline pose to measure drift over time

3. **Drift Measurement System**
   - When a tag is first acquired, `TagPlacementController` establishes a baseline world pose
   - On subsequent detections of the same tag, it compares:
     - **Position drift:** Linear distance between expected and actual pose (meters)
     - **Rotation drift:** Angular difference (degrees)
   - Tracks both instantaneous drift (`_lastDriftPos`, `_lastDriftDeg`) and maximum drift since acquisition
   - `DriftLogger` samples drift data at intervals and writes CSV logs to persistent storage

4. **UI and Control** (`Assets/Scripts/`)
   - `ArHudMenuController` provides Start/Stop and acquisition mode toggle (single vs continuous)
   - `DebugHudBuilder` creates runtime HUD overlays showing drift metrics and tag distance
   - `TagPlacementController.UpdateHud()` displays real-time distance from camera to tag in both meters and feet/inches

### Key Design Patterns

**Event-Driven Detection:**
```csharp
// Detector fires events when tags are observed
public interface ITagDetector {
    event Action<TagObservation> OnObservation;
    void StartDetecting();
    void StopDetecting();
}
```

**Coordinate Frame Hierarchy:**
- World Space (Unity's XR origin)
- Ship Space (ScriptableObject registry with known tag→ship mappings)
- Tag Local Space (detected tag pose)
- Placed Object Space (virtual cube/object with offset from tag)

**Drift Measurement Pattern:**
```csharp
// BEFORE moving object: measure drift from current to intended pose
Pose intended = CalculateIntendedPose(tagObservation);
Pose current = obj3d.GetPose();
Vector3 drift = intended.position - current.position;

// THEN update object to intended pose
obj3d.SetPose(intended);
```

## Common Development Tasks

### Adding Support for New AR Platforms

To support additional AR headsets (e.g., Meta Quest, HoloLens):

1. Create a new detector class implementing `ITagDetector` (see `Assets/Scripts/Tags/`)
2. Use platform-specific marker tracking APIs (ARFoundation, vendor SDK, etc.)
3. Emit `TagObservation` events with world-space poses
4. Wire the new detector to `TagPlacementController.tagDetector` in the scene

### Modifying Drift Calculation

Drift logic is in `TagPlacementController.HandleObs()` (Assets/Scripts/Ship/TagPlacementController.cs:174-225):
- Drift is measured BEFORE moving the object to the new pose
- Modify `_maxDriftPos`/`_maxDriftDeg` tracking for different statistical measures (e.g., RMS, percentiles)
- CSV logging format is in `DriftLogger.Update()` (Assets/Scripts/DriftLogger.cs:63)

### Configuring AprilTag Detection

In `MagicLeapTagDetector_260.cs`:
- `aprilTagSizeMeters`: Physical tag size (default 0.115m = 115mm)
- `tagFamily`: AprilTag dictionary (default 36h11)
- `MarkerDetectorProfile.Accuracy`: Trades speed for precision (can change to `Speed` for lower latency)

Permission required: `com.magicleap.permission.MARKER_TRACKING` (automatically requested on Android builds)

## Important Implementation Notes

### Magic Leap SDK Specifics

**Startup Flow** (MagicLeapTagDetector_260.cs:66-110):
1. Wait for XR subsystem initialization (`XRGeneralSettings.Instance.Manager.isInitializationComplete`)
2. Request Android permission for marker tracking
3. Create `MarkerDetector` with settings struct (note: AprilTag settings require **struct write-back** pattern)
4. Poll `MarkerDetectorStatus` until `Ready`
5. Call `_feature.UpdateMarkerDetectors()` every frame before reading detector data

**OpenXR Integration:**
- Uses `OpenXRSettings.Instance.GetFeature<MagicLeapMarkerUnderstandingFeature>()`
- Detector lifecycle tied to XR session (must recreate on XR restart)
- Marker pose is in world space relative to XR origin

### Unity-Specific Gotchas

**ScriptableObject Registry Pattern:**
`ShipCoordinateRegistry` must call `OnEnable()` to rebuild the dictionary cache. When editing entries in the Inspector, the cache updates on domain reload but not immediately during runtime.

**TextMeshPro HUD:**
HUD text is updated every frame in `TagPlacementController.UpdateHud()`. For performance, consider throttling updates or using dirty flags if frame rate becomes an issue.

**Coordinate Handedness:**
Unity uses left-handed Y-up coordinates. AprilTag detection typically assumes Z-forward (out of tag), X-right, Y-up. Verify tag orientation conventions if distance measurements seem inverted.

## Project Structure Highlights

```
Assets/
├── Scenes/
│   └── SampleScene.unity          # Main AR scene
├── Scripts/
│   ├── Tags/                       # Tag detection abstraction layer
│   │   ├── ITagDetector.cs        # Platform-agnostic interface
│   │   ├── TagObservation.cs      # Marker observation data struct
│   │   ├── MagicLeapTagDetector_260.cs  # ML2 implementation
│   │   └── MLMarkerBootstrap.cs   # Setup helper
│   ├── Ship/                       # Coordinate system and placement
│   │   ├── ShipCoordinateRegistry.cs    # Tag→ship pose mappings
│   │   └── TagPlacementController.cs    # Core placement & drift logic
│   ├── ArHudMenuController.cs     # UI controls (Start/Stop/Mode)
│   ├── DriftLogger.cs             # CSV logging to persistent storage
│   └── DebugHudBuilder.cs         # Runtime HUD creation
├── Samples/                        # Magic Leap SDK sample scenes
└── Packages/
    └── com.magicleap.unitysdk/    # Magic Leap vendor package
```

## Scripting Defines

The project uses these platform-specific symbols (configured in ProjectSettings):
- `USE_ML_OPENXR`: Magic Leap OpenXR features
- `MAGICLEAP`: Platform detection
- `USE_INPUT_SYSTEM_POSE_CONTROL`: New Input System for XR controllers
- `USE_STICK_CONTROL_THUMBSTICKS`: Controller input scheme

## Testing and Validation

**Measuring Accuracy:**
1. Place AprilTag at known distance (use measuring tape)
2. Start application and point headset at tag
3. Compare HUD "Tag Z distance" readout with physical measurement
4. Walk around while keeping tag in view
5. Observe "Max Drift" values in HUD
6. Review CSV logs in device persistent storage for detailed analysis

**Typical Drift Values:**
- Good tracking: <1cm position drift, <2° rotation drift
- Moderate: 1-3cm position, 2-5° rotation
- Poor: >5cm position, >10° rotation (indicates SLAM failure or occlusion)

## Permissions

Android manifest must include:
```xml
<uses-permission android:name="com.magicleap.permission.MARKER_TRACKING" />
```
Already configured in `Assets/Plugins/Android/AndroidManifest.xml` (if using custom manifest).

## Performance Considerations

- AprilTag detection runs at camera frame rate (~30Hz on ML2)
- `UpdateMarkerDetectors()` must be called every frame before reading data
- Drift calculations are lightweight (single subtraction + angle computation)
- CSV logging uses `StreamWriter.Flush()` per sample—consider buffering for high-frequency logging

## Multi-Tag Scenarios

Current implementation tracks the **most recently detected tag**. For simultaneous multi-tag tracking:
1. Modify `TagPlacementController` to maintain a dictionary of `tagId → PlacementState`
2. Create separate GameObjects for each tracked tag
3. Update drift calculations per-tag instead of globally
