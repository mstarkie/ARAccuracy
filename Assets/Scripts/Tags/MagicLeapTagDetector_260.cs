using MagicLeap.OpenXR.Features.MarkerUnderstanding;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;

// Magic Leap provider (OpenXR Marker Understanding)
// Starkie, M.
public class MagicLeapTagDetector_260 : MonoBehaviour, ITagDetector
{
    private const string MarkerPermission = "com.magicleap.permission.MARKER_TRACKING";
    public event System.Action<TagObservation> OnObservation;

    [Header("AprilTag")]
    public float aprilTagSizeMeters = 0.115f; // sample size; adjust to your print
    public AprilTagType tagFamily = AprilTagType.Dictionary_36H11;

    MagicLeapMarkerUnderstandingFeature _feature;
    MarkerDetector _detector;

    bool _initialized;     // Detector created successfully
    bool _isDetecting;     // Currently processing detections

    private Coroutine _initCoroutine;  // Track the coroutine

    void Awake()
    {
        _feature = OpenXRSettings.Instance?.GetFeature<MagicLeapMarkerUnderstandingFeature>();
        Debug.Log("[ARAccuracy MLDet] Awake. Feature present? " + (_feature != null) + ", enabled? " + _feature?.enabled);
    }

    public void StartDetecting()
    {
        if (_isDetecting) return;

        if (!_initialized)
        {
            _initCoroutine = StartCoroutine(InitializeDetector());
        }
        else
        {
            if (_feature == null)  // Add null check
            {
                 Debug.LogError("[ARAccuracy MLDet] Cannot resume: feature is null");
                 return;
            }
            _isDetecting = true;
            _feature.enabled = true;
            Debug.Log("[ARAccuracy MLDet] Resumed detecting (detector already initialized)");
        }
    }

    public void StopDetecting()
    { 
        Debug.Log($"[MLDet] StopDetecting() called. Current state: _isDetecting={_isDetecting}, _initialized={_initialized}");

        // Stop initialization if in progress
        if (_initCoroutine != null)
        {
            StopCoroutine(_initCoroutine);
            _initCoroutine = null;
        }

        _isDetecting = false;
        if (_feature != null)
        {
          _feature.enabled = false;
        }

        Debug.Log($"[MLDet] After stop: _isDetecting={_isDetecting}, _feature.enabled={_feature?.enabled}");
  }

    IEnumerator InitializeDetector()
    {
        // 1) Wait for XR to be initialized
        var mgr = XRGeneralSettings.Instance?.Manager;
        while (mgr == null || !mgr.isInitializationComplete || mgr.activeLoader == null) 
        {
            yield return null;
        }
        yield return null; // one extra frame

        // 2) Permission
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(MarkerPermission))
        {
            Debug.Log("[ARAccuracy MLDet] Requesting MARKER_TRACKING permission…");
            UnityEngine.Android.Permission.RequestUserPermission(MarkerPermission);
            // wait for decision (poll for a short time)
            while (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(MarkerPermission))
            {
                yield return null; // keep waiting
            }
        }
#endif

        // 3) Create detector (struct write-back!)
        if (_feature == null || !_feature.enabled) 
        { 
            Debug.LogError("[ARAccuracy MLDet] Marker feature missing/disabled"); 
            yield break; 
        }

        var settings = new MarkerDetectorSettings
        {
            MarkerType = MarkerType.AprilTag,
            MarkerDetectorProfile = MarkerDetectorProfile.Accuracy
        };
        var april = settings.AprilTagSettings;           // struct copy
        april.AprilTagType = tagFamily;
        april.AprilTagLength = Mathf.Max(0.01f, aprilTagSizeMeters);
        settings.AprilTagSettings = april;               // <-- write-back!

        _detector = _feature.CreateMarkerDetector(settings);
        Debug.Log($"[ARAccuracy MLDet] CreateMarkerDetector → {(_detector != null ? "OK" : "NULL")}");

        if (_detector == null) 
        { 
            yield break;
        }

        _initialized = true;
        _isDetecting = true;
        _initCoroutine = null;  // Clear reference when done

        Debug.Log("[ARAccuracy MLDet] Detector initialized and started");
    }

    // Update is called once per frame
    void Update()
    {
        // Early exit if not actively detecting
        if (!_isDetecting || _detector == null || _feature == null)
        {
            return;
        }

        Debug.Log("[MLDet] Update() processing detections"); // ← Add this

        // Pump all detectors first
        _feature.UpdateMarkerDetectors();

        //Debug.Log("[ARAccuracy MLDet->Update] Detector status: " + _detector?.Status);

        if (_detector.Status != MarkerDetectorStatus.Ready)
        {
            //Debug.Log("[ARAccuracy MLDet->Update] Detector not READY");
            return;
        }
        
        // Read latest observations
        // In 2.6.0, detections are on detector.Data
        // List<...> (SDK-defined struct with MarkerPose/Number/String/Length)
        var dataList = _detector.Data;
        if (dataList == null || dataList.Count == 0) return;

        foreach (var d in dataList)
        {
            if (!d.MarkerPose.HasValue) continue;
            var p = d.MarkerPose.Value;
            var obs = new TagObservation
            {
                Id = (ulong) d.MarkerNumber,
                WorldPose = new Pose(p.position, p.rotation),
                SizeMeters = (d.MarkerLength > 0 ? d.MarkerLength : aprilTagSizeMeters),
                Timestamp = Time.timeAsDouble,
                Valid = true
            };
            OnObservation?.Invoke(obs);
            //Debug.Log("[ARAccuracy MLDet]->Update() called");
        }
    }

      void OnDestroy()
      {
        // Stop any running initialization
        if (_initCoroutine != null)
        {
            StopCoroutine(_initCoroutine);
            _initCoroutine = null;
        }
          // Proper cleanup when component is destroyed
          if (_detector != null && _feature != null)
          {
              try
              {
                  // If the SDK supports explicit destruction:
                  // _feature.DestroyMarkerDetector(_detector);
                  _detector = null;
                  Debug.Log("[ARAccuracy MLDet] Detector destroyed on component destruction");
              }
              catch (System.Exception ex)
              {
                  Debug.LogWarning($"[ARAccuracy MLDet] Cleanup exception: {ex.Message}");
              }
          }
      }
}
