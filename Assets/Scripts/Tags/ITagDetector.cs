using System;
using UnityEngine;
public interface ITagDetector
{
    /// Raised once per detected marker observation (per frame or per update from the runtime).
    event Action<TagObservation> OnObservation;

    /// Ensure the detector is started (idempotent; safe to call multiple times).
    void EnsureStarted();

    /// Optionally stop/cleanup (no-op if not started).
    void StopDetecting();
}
