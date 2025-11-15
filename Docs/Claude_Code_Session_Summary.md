# AR Accuracy Project - Claude Code Session Summary

**Last Updated:** 2025-11-15
**Session Focus:** Comprehensive project analysis and Magic Leap integration planning

---

## Project Overview

### Purpose
Magic Leap 2 AR spatial accuracy evaluation project designed to measure tracking precision and drift characteristics using AprilTag markers. Intended for **shipbuilding applications** where precise AR alignment is critical.

### Core Functionality
1. **AprilTag Detection** - Detects AprilTag markers in physical environment
2. **Virtual Object Placement** - Places cubes at configurable offsets from tags
3. **Drift Measurement** - Tracks positional (meters) and angular (degrees) drift over time
4. **Data Logging** - Exports drift measurements to CSV files
5. **Distance Measurement** - Real-time Z/X/Y distance from camera to tags in metric & imperial

---

## Key Custom Scripts

### Core AR Accuracy System

#### **TagPlacementController.cs** (`Assets/Scripts/Ship/`)
- **Primary accuracy measurement script**
- Bridges physical AprilTag and virtual Unity objects
- Calculates drift: `intended position - current position`
- Two acquisition modes:
  - **Continuous**: Updates position based on live tag detections
  - **Single**: Locks target once for drift measurement
- HUD display: Tag distance (Z/X/Y), max drift, acquisition mode
- Configurable cube offset from tag

#### **MagicLeapTagDetector_260.cs** (`Assets/Scripts/Tags/`)
- OpenXR-based AprilTag detector for Magic Leap 2
- Uses `MagicLeapMarkerUnderstandingFeature`
- Default: AprilTag 36h11 dictionary, 0.115m (11.5cm) tag size
- Accuracy profile for precise detection
- Fires `OnObservation` event with tag ID, pose, size, timestamp

#### **DriftLogger.cs** (`Assets/Scripts/`)
- CSV data logger for drift analysis
- Configurable log interval (default: 0.25s)
- Columns: timestamp, tagId, cubeX/Y/Z, driftX/Y/Z, driftDeg
- Saves to: `Application.persistentDataPath/drift_yyyyMMdd_HHmmss.csv`

#### **ShipCoordinateRegistry.cs** (`Assets/Scripts/Ship/`)
- ScriptableObject for ship coordinate mapping
- Maps AprilTag IDs to known ship frame positions
- **Currently placeholder** - not actively used
- Designed for future multi-tag ship alignment

### UI and Control System

#### **ArHudMenuController.cs**
- Toggle acquisition modes (Continuous/Single)
- Clear target acquisition button
- Menu visibility control

#### **ControllerMenuToggle.cs**
- Toggles AR menu with Magic Leap controller menu button
- Editor testing: Press 'M' key

#### **DebugHudBuilder.cs**
- Programmatically creates HUD overlay
- Yellow text on semi-transparent black background
- Fallback when scene-based UI fails

### Validation

#### **SceneWiringValidator.cs**
- Auto-finds and validates required components on startup
- Logs errors if anything missing
- Prevents runtime failures from missing references

---

## Scene Setup

### SampleScene.unity
**Key GameObjects:**
- **TagSystem** - Core scripts: MagicLeapTagDetector_260, TagPlacementController, DriftLogger, SceneWiringValidator
- **ShipRoot** - Transform representing ship coordinate frame
- **Cube (obj3d)** - Virtual object placed relative to detected tags
- **XR Origin** - Standard AR Foundation setup
- **AR Menu Canvas** - Button_ClearTarget, Toggle Acquisition Mode
- **HUD Text** - TextMeshPro status display

---

## Magic Leap 2 Patterns & Best Practices

### 1. Permission-First Workflow
```csharp
Permissions.RequestPermission(Permissions.SomePerm, OnPermissionGranted, OnPermissionDenied);
// Initialize features only after permission granted
```

### 2. OpenXR Feature Access
```csharp
feature = OpenXRSettings.Instance.GetFeature<MagicLeapSomeFeature>();
```

### 3. Controller Integration
```csharp
MagicLeapController.Instance.BumperPressed += Handler;
MagicLeapController.Instance.MenuPressed += Handler;
```

### 4. Subsystem Initialization
```csharp
yield return new WaitUntil(Utils.AreSubsystemsLoaded<XRSubsystem>);
```

### 5. Efficient UI Updates
- Use `StringBuilder` for status text in Update() to avoid GC
- Cache component references
- Only update UI when values change

### 6. Proper Cleanup
```csharp
private void OnDestroy() {
    Controller.BumperPressed -= Handler;
    if (feature.Created) feature.Destroy();
}
```

---

## Available Magic Leap Features (from Examples)

### Tier 1 - High Priority for AR Accuracy

#### **1. Spatial Anchors** (SpatialAnchorsExample.cs)
**Capabilities:**
- Publish local anchors to persistent storage
- Query stored anchors by radius (10m example)
- Localization map tracking with confidence monitoring
- Export/import maps between devices
- Anchor persistence across sessions

**AR Accuracy Benefits:**
- Persistent accuracy testing across days/weeks
- Baseline comparison: tag detection vs stored anchor positions
- Multi-session drift analysis
- Map quality evaluation (localization confidence)
- Ship environment mapping with persistent coordinates

**Integration Pattern:**
```csharp
// When tag stable:
1. Create ARAnchor at tag location
2. PublishSpatialAnchorsToStorage([anchor], tagId)
3. On next visit: QueryStoredSpatialAnchors(position, radius)
4. Compare anchor.position vs current tag detection
5. Log "anchor drift" vs "tag drift" separately
```

**Key API:**
- `storageFeature.PublishSpatialAnchorsToStorage(List<ARAnchor>, expiration)`
- `storageFeature.QueryStoredSpatialAnchors(Vector3 position, float radius)`
- `storageFeature.CreateSpatialAnchorsFromStorage(List<string> anchorIds)`
- `localizationMapFeature.RequestMapLocalization(string mapUUID)`
- `localizationMapFeature.GetLatestLocalizationMapData(out LocalizationEventData)`

#### **2. Advanced Marker Tracking** (MarkerTrackingExample.cs)
**Capabilities:**
- Multiple marker types: AprilTag (36h11, 25h9, 16h5), ArUco, QR
- Detector profiles: Default, Speed, **Accuracy**, Custom
- Custom settings: FPS hint, resolution, camera hint, corner/edge refinement
- Multiple simultaneous detectors
- Marker length estimation vs fixed size

**AR Accuracy Benefits:**
- A/B test detector profiles for optimal accuracy
- Multi-dictionary performance comparison
- QR code metadata alongside AprilTags
- Fine-tune detection for lighting conditions
- Performance benchmarking

**Integration Pattern:**
```csharp
// Create multiple detectors for comparison:
MarkerDetectorSettings settings1 = new() {
    MarkerDetectorProfile = MarkerDetectorProfile.Accuracy,
    MarkerType = MarkerType.AprilTag,
    AprilTagSettings = { AprilTagType = AprilTagType.AprilTag_36h11 }
};
markerFeature.CreateMarkerDetector(settings1);

// Track which detector performs best
```

**Key API:**
- `markerFeature.CreateMarkerDetector(MarkerDetectorSettings)`
- `markerFeature.UpdateMarkerDetectors()`
- `markerFeature.DestroyAllMarkerDetectors()`
- Access: `markerFeature.MarkerDetectors` (list of active detectors)

#### **3. Voice Intents** (VoiceIntentsExample.cs)
**Capabilities:**
- Custom voice commands with slot-based values
- Runtime configuration switching
- Start/stop processing on demand
- System intents integration

**AR Accuracy Benefits:**
- Hands-free operation for shipbuilding (hands may hold tools)
- Voice commands: "Clear target", "Start logging", "Single mode"
- Query status vocally: "Show drift", "Show distance"
- Safety: Hands-free in industrial environment

**Integration Pattern:**
```csharp
MLVoiceIntentsConfiguration config;
config.VoiceCommandsToAdd.Add(new() { Value = "Clear Target", Id = 1 });
config.VoiceCommandsToAdd.Add(new() { Value = "Continuous Mode", Id = 2 });
config.VoiceCommandsToAdd.Add(new() { Value = "Single Mode", Id = 3 });
config.VoiceCommandsToAdd.Add(new() { Value = "Start Logging", Id = 4 });

MLVoice.SetupVoiceIntents(config);
MLVoice.OnVoiceEvent += (success, intentEvent) => {
    switch(intentEvent.EventID) {
        case 1: tagPlacement.ClearTarget(); break;
        case 2: tagPlacement.SetMode(Continuous); break;
        // etc.
    }
};
```

**Key API:**
- `MLVoice.SetupVoiceIntents(MLVoiceIntentsConfiguration)`
- `MLVoice.OnVoiceEvent` event
- `MLVoice.Stop()` / resume

### Tier 2 - Moderate Priority

#### **4. Light Estimation** (LightEstimationExample.cs)
- Directional light color/direction
- HDR environment cubemap (3 resolutions)
- Spherical harmonics ambient lighting

**AR Accuracy Benefits:**
- Log lighting conditions alongside drift data
- Correlate poor lighting with detection failures
- Environmental documentation

#### **5. Plane Detection** (PlaneExample.cs)
- Semantic classification (floor, ceiling, wall, table)
- Horizontal/vertical detection
- Min area filtering, max results config

**AR Accuracy Benefits:**
- Validate tags are on detected surfaces
- Compare tag rotation vs plane normal
- Assess tracking on different surface types

#### **6. Pixel Sensors** (PixelSensorExample.cs)
- Access World, Eye, Picture, Depth cameras
- Stream configuration and real-time data

**AR Accuracy Benefits:**
- Capture frames when tag detection fails
- Compare depth sensor vs tag distance
- Computer vision research/debugging

### Tier 3 - Nice to Have

- **Occlusion** - Realistic AR rendering
- **Spatial Meshing** - Environmental geometry visualization
- **Gaze Tracking** - Hands-free tag selection, attention monitoring

---

## Architecture Patterns

### Interface-Based Design
`ITagDetector` interface allows multiple implementations:
- Current: `MagicLeapTagDetector_260` (OpenXR)
- Future: ARCore, ARKit, Vuforia adapters

### Event-Driven
```csharp
public event Action<TagObservation> OnObservation;
// TagPlacementController subscribes
```

### Struct-Based Data
```csharp
public struct TagObservation {
    public ulong Id;
    public Pose WorldPose;
    public float SizeMeters;
    public double Timestamp;
    public bool Valid;
}
```

### Auto-Wiring with Validation
`SceneWiringValidator` prevents missing references

---

## Current Project State

### ✅ Production Ready
- AprilTag detection with Magic Leap 2
- Spatial drift measurement and CSV logging
- Distance measurement (metric + imperial)
- Acquisition mode switching (continuous/single)
- Clear target functionality
- Real-time HUD display

### 🚧 Partially Implemented
- Ship coordinate system mapping (ScriptableObject exists but unused)
- Multi-tag spatial registration

### 📋 Example Features Available
Complete ML2 feature showcase with proper patterns

---

## Build Configuration

- **Platform:** Android
- **Product Name:** AR Accuracy
- **Company:** GDEB
- **Rendering:** URP
- **Unity Version:** Unity 6
- **XR Plugin:** OpenXR with Magic Leap 2

---

## Recommended Next Steps

1. **Spatial Anchors Integration** - Persistent anchor storage at tag locations for long-term drift studies
2. **Detector Profile Testing** - UI to switch Accuracy/Speed/Custom profiles
3. **Voice Control** - Hands-free commands for shipbuilding operators
4. **Light Estimation Logging** - Correlate environmental conditions with accuracy
5. **Ship Coordinate Registry** - Activate multi-tag ship frame mapping

---

## Quick Reference: Key File Paths

```
Assets/
├── Scripts/
│   ├── Ship/
│   │   ├── TagPlacementController.cs         # Main accuracy controller
│   │   └── ShipCoordinateRegistry.cs          # Ship coordinate mapping
│   ├── Tags/
│   │   ├── MagicLeapTagDetector_260.cs        # AprilTag detector
│   │   ├── TagObservation.cs                  # Data struct
│   │   └── ITagDetector.cs                    # Interface
│   ├── DriftLogger.cs                         # CSV logger
│   ├── ArHudMenuController.cs                 # Menu UI
│   ├── ControllerMenuToggle.cs                # Controller input
│   └── [Magic Leap Examples]/                 # ML2 feature demos
├── Scenes/
│   └── SampleScene.unity                      # Main scene
└── Docs/
    └── [Project documentation]
```

---

## Notes from Current Session

- User wants to make tweaks to AR Accuracy scene
- Next: Determine specific scene modifications needed
- Ready to implement changes using Magic Leap patterns

---

*This document was auto-generated by Claude Code to preserve session context across restarts.*
