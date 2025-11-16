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

    bool _starting, _started;

    void Awake()
    {
        _feature = OpenXRSettings.Instance?.GetFeature<MagicLeapMarkerUnderstandingFeature>();
        Debug.Log("[ARAccuracy MLDet] Awake. Feature present? " + (_feature != null) + ", enabled? " + _feature?.enabled);
    }

    public void StartDetecting()
    {
        if (_started || _starting) 
        { 
            return; 
        }
        _starting = true;
        StartCoroutine(StartFlow());
        Debug.Log("[ARAccuracy MLDet] StartDetecting called."); 
    }

    public void StopDetecting()
    {
        if (_feature != null && _detector != null)
        {
            try
            {
                Debug.Log("[ARAccuracy MLDet] Stopping and destroying detector...");
                StopCoroutine(StartFlow());
                _detector = null;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[ARAccuracy MLDet] StopDetecting exception: " + ex.Message);
            }
        }
        _starting = false;
        _started = false;
    }

    IEnumerator StartFlow()
    {
        // 1) Wait for XR to be initialized
        var mgr = XRGeneralSettings.Instance?.Manager;
        while (mgr == null || !mgr.isInitializationComplete || mgr.activeLoader == null) yield return null;
        yield return null; // one extra frame

        // 2) Permission
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(MarkerPermission))
        {
            Debug.Log("[ARAccuracy MLDet] Requesting MARKER_TRACKING permission…");
            UnityEngine.Android.Permission.RequestUserPermission(MarkerPermission);
            // wait for decision (poll for a short time)
            while (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(MarkerPermission))
                yield return null; // keep waiting
        }
#endif

        // 3) Create detector (struct write-back!)
        if (_feature == null || !_feature.enabled) { Debug.LogError("[ARAccuracy MLDet] Marker feature missing/disabled"); yield break; }

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

        if (_detector == null) { _starting = false; yield break; }

        // Some SDK builds require an explicit start; enable whichever exists in your API:
        // _detector.Enabled = true;
        // _feature.SetMarkerDetectorEnabled(_detector, true);
        // _feature.StartMarkerDetector(_detector);

        _started = true;
        _starting = false;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Debug.Log("[ARAccuracy MLDet]->Start");
    }

    // Update is called once per frame
    void Update()
    {
        if (_feature == null)
        {
            //Debug.Log("[ARAccuracy MagicLeapTagDetector_260 Update]->_feature is null.");
            return;
        }

        if (_detector == null)
        {
            //Debug.Log("[ARAccuracy MagicLeapTagDetector_260 Update]->_detector is null.");
            return;
        }

        // Pump all detectors first
        _feature.UpdateMarkerDetectors();

        //Debug.Log("[ARAccuracy MLDet->Update] Detector status: " + _detector?.Status);

        if (_detector.Status != MarkerDetectorStatus.Ready)
        {
            //Debug.Log("[ARAccuracy MLDet->Update] Detector not READY");
            return;
        }
        else
        {
            //Debug.Log("[ARAccuracy MLDet->Update] Status=" + _detector.Status);
        }

        // Read latest observations
        // In 2.6.0, detections are on detector.Data
        // List<...> (SDK-defined struct with MarkerPose/Number/String/Length)
        var dataList = _detector.Data;
        if (dataList == null || dataList.Count == 0) return;
        //Debug.Log("[ARAccuracy MLDet->Update] Detector Observations: " + dataList.Count);

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


}
