using System;
using UnityEngine;

// Common interface for marker detection
// Starkie, M.
public struct TagObservation
{
    public ulong Id;             // AprilTag ID
    public Pose WorldPose;       // Tag pose in Unity world space
    public float SizeMeters;     // Edge length if known
    public double Timestamp;     // Seconds
    public bool Valid;
}
