# AR Accuracy

A Unity-based AR accuracy measurement tool for Magic Leap 2 that uses AprilTag markers to quantify spatial tracking precision and drift in real-world environments.

## Overview

AR Accuracy helps evaluate how precisely augmented reality headsets maintain spatial alignment between virtual objects and physical reference points over time. Originally designed for shipbuilding applications where millimeter-level precision matters, this tool can be used anywhere accurate AR tracking needs to be validated.

### Key Features

- **Real-time AprilTag Detection** - Detects and tracks AprilTag markers using Magic Leap 2's OpenXR Marker Understanding API
- **Drift Measurement** - Quantifies positional drift (mm) and rotational drift (degrees) as you move around
- **Distance Measurement** - Real-time distance from headset to tag in meters and feet/inches
- **CSV Data Logging** - Exports drift data to CSV files for analysis
- **Single vs Continuous Acquisition** - Lock onto the first tag seen, or continuously update tracking
- **Real-time HUD** - On-screen display showing drift metrics and tag position

### Use Cases

- Validating AR tracking accuracy for industrial applications
- Comparing AR hardware platforms (Magic Leap 2, Meta Quest, HoloLens, etc.)
- Research on SLAM algorithm stability
- Quality assurance for AR experiences requiring precise spatial alignment
- Shipbuilding and construction where virtual overlays guide physical assembly

## Prerequisites

- **Unity 2023.x or later** with Universal Render Pipeline (URP)
- **Magic Leap 2 headset** with developer mode enabled
- **Magic Leap Unity SDK 2.6.0** (installed via Unity Package Manager)
- **Android Build Support** for Unity (Tools > Android > SDK & NDK Tools)
- **AprilTag markers** (36h11 family recommended) - Print at known size (default: 115mm)

## Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/ar-accuracy.git
   cd ar-accuracy
   ```

2. **Open in Unity**
   - Open Unity Hub
   - Add the project folder
   - Open with Unity 2023.x or later

3. **Install Magic Leap SDK**
   - Window > Package Manager
   - Add package from git URL: `https://registry.npmjs.org/com.magicleap.unitysdk/-/com.magicleap.unitysdk-2.6.0.tgz`
   - Or use the Magic Leap Hub to install SDK components

4. **Configure Build Settings**
   - File > Build Settings > Android
   - Switch Platform to Android
   - Set Texture Compression to ASTC
   - Minimum API Level: Android 10 (API 29)

5. **Configure XR Settings**
   - Edit > Project Settings > XR Plug-in Management
   - Enable OpenXR for Android
   - Enable Magic Leap 2 feature set

## Quick Start

### 1. Print AprilTag Markers

- Download AprilTag 36h11 markers from [AprilTag repository](https://github.com/AprilRobotics/apriltag-imgs)
- Print at a known size (default configuration expects 115mm)
- Mount on rigid backing for best results
- Place in your test environment

### 2. Build and Deploy

```bash
# In Unity, with Magic Leap 2 connected via USB
File > Build Settings > Build
# Deploy the APK to your device via Unity or adb
```

### 3. Run the Application

1. Put on the Magic Leap 2 headset
2. Launch "AR Accuracy" application
3. Press the **Start** button in the menu (trigger on controller or voice command)
4. Point the headset at an AprilTag marker
5. A virtual cube will appear at a fixed offset from the tag
6. Move around while keeping the tag in view
7. Observe drift metrics in the HUD

### 4. Review Results

- **On-device HUD** shows real-time drift and distance measurements
- **CSV logs** are saved to device storage: `/storage/emulated/0/Android/data/com.gdeb.accuracy/files/drift_*.csv`
- Retrieve logs via USB:
  ```bash
  adb pull /storage/emulated/0/Android/data/com.gdeb.accuracy/files/ ./logs/
  ```

## Usage

### UI Controls

- **Start/Stop Button** - Begin/end tag detection and tracking
- **Acquisition Mode Toggle** - Switch between:
  - **Continuous Acquisition** - Updates tracking as you move (default)
  - **Single Acquisition** - Locks to first detected tag pose

### HUD Information

The overlay displays:
- **Tag Z distance** - Distance from camera to tag (meters and feet/inches)
- **Lateral/Vertical offset** - X/Y distances in tag coordinate frame
- **Max Drift Δp** - Maximum position drift since acquisition (X, Y, Z in meters)
- **Max Drift Δθ** - Maximum rotation drift (degrees)
- **Mode** - Current acquisition mode (Single/Continuous)

### Interpreting Drift Values

| Drift Level | Position | Rotation | Quality |
|-------------|----------|----------|---------|
| Excellent   | < 5mm    | < 1°     | Production-ready for precise alignment |
| Good        | 5-10mm   | 1-2°     | Acceptable for most AR applications |
| Moderate    | 10-30mm  | 2-5°     | May drift noticeably over time |
| Poor        | > 30mm   | > 5°     | SLAM failure or severe tracking issues |

## Configuration

### Adjusting AprilTag Size

Edit `MagicLeapTagDetector_260.cs`:

```csharp
[Header("AprilTag")]
public float aprilTagSizeMeters = 0.115f; // Change to your printed tag size
public AprilTagType tagFamily = AprilTagType.Dictionary_36H11;
```

### Changing Object Placement Offset

Edit `TagPlacementController.cs`:

```csharp
[Header("Placement")]
public Vector3 objOffsetFromTag = new(0f, 0.0f, 0.2f); // 20cm forward from tag
```

### Adjusting Log Sample Rate

Edit `DriftLogger.cs`:

```csharp
[Tooltip("Seconds between log samples.")]
public float sampleInterval = 0.25f; // 4 samples per second
```

## Project Structure

```
Assets/
├── Scenes/
│   └── SampleScene.unity              # Main AR scene
├── Scripts/
│   ├── Tags/                          # Tag detection system
│   │   ├── ITagDetector.cs           # Platform-agnostic detector interface
│   │   ├── TagObservation.cs         # Detected tag data structure
│   │   └── MagicLeapTagDetector_260.cs # Magic Leap 2 implementation
│   ├── Ship/                          # Coordinate system management
│   │   ├── ShipCoordinateRegistry.cs # Tag-to-ship coordinate mappings
│   │   └── TagPlacementController.cs # Placement logic & drift calculation
│   ├── ArHudMenuController.cs        # UI controls
│   ├── DriftLogger.cs                # CSV data logging
│   └── DebugHudBuilder.cs            # Runtime HUD overlay
└── Packages/
    └── manifest.json                  # Unity package dependencies
```

## Architecture

The application follows a four-layer architecture:

1. **Detection Layer** - AprilTag marker detection via platform-specific APIs (currently Magic Leap OpenXR)
2. **Transformation Layer** - Converts tag poses to Unity world space with configurable offsets
3. **Measurement Layer** - Tracks baseline poses and computes drift over time
4. **Presentation Layer** - HUD display and CSV logging

See [CLAUDE.md](CLAUDE.md) for detailed architectural documentation.

## Extending to Other Platforms

To support additional AR headsets:

1. Implement `ITagDetector` interface for your platform
2. Use ARFoundation, vendor SDK, or custom marker tracking
3. Emit `TagObservation` events with world-space tag poses
4. Assign your detector to `TagPlacementController.tagDetector` in scene

Example platforms:
- **Meta Quest 3** - Use ARFoundation image tracking or Oculus SDK
- **HoloLens 2** - Use QR code tracking or ARFoundation
- **Android/iOS tablets** - Use ARFoundation image tracking

## Known Limitations

- Currently supports only Magic Leap 2 hardware
- Single tag tracking (multi-tag support requires code modifications)
- AprilTag 36h11 family (other families require configuration change)
- Requires well-lit environments for reliable tag detection
- Tag must remain visible to camera for continuous tracking

## Troubleshooting

### Tag Not Detected

- Ensure tag is printed clearly with high contrast
- Check lighting conditions (avoid glare/shadows on tag)
- Verify tag size matches `aprilTagSizeMeters` configuration
- Ensure MARKER_TRACKING permission granted (check device settings)

### High Drift Values

- Ensure tag is mounted on rigid, stationary surface
- Avoid rapid head movements during acquisition
- Check for reflective surfaces or visual ambiguity in environment
- Verify tag is not occluded or partially out of view

### No HUD Visible

- Check that Canvas is set to Overlay mode
- Verify TextMeshPro is installed (Window > Package Manager)
- Ensure `HudText` reference is assigned in TagPlacementController inspector

## Data Format

CSV log format:
```csv
t,tagId,cubeX,cubeY,cubeZ,driftX,driftY,driftZ,driftDeg
0.250,42,1.2340,0.5670,-0.3450,0.0012,-0.0003,0.0007,0.15
0.500,42,1.2341,0.5669,-0.3451,0.0013,-0.0004,0.0008,0.18
```

- `t` - Time in seconds since application start
- `tagId` - AprilTag ID number
- `cubeX/Y/Z` - Virtual object position in world space (meters)
- `driftX/Y/Z` - Position drift from baseline (meters)
- `driftDeg` - Rotation drift from baseline (degrees)

## Contributing

Contributions welcome! Areas for improvement:

- Support for additional AR platforms (Meta Quest, HoloLens, ARCore/ARKit)
- Multi-tag simultaneous tracking
- Statistical analysis tools (RMS drift, confidence intervals)
- Real-time drift visualization (graphs, heat maps)
- Automated test sequences and reporting

## License

[Specify your license here - MIT, Apache 2.0, etc.]

## Acknowledgments

- Built for shipbuilding AR accuracy validation
- Uses Magic Leap Unity SDK 2.6.0
- AprilTag marker system by AprilRobotics

## Contact

[Your contact information or organization]

## Citation

If you use this tool in research or publications, please cite:

```
[Your citation format]
```
