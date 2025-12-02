using System.IO;
using UnityEngine;

public class DriftLogger : MonoBehaviour
{
    [Tooltip("Assign the TagPlacementController on TagSystem (or leave blank to auto-find).")]
    public TagPlacementController controller;

    [Tooltip("Seconds between log samples.")]
    public float sampleInterval = 0.25f;

    StreamWriter _sw;
    string _path;
    double _nextSampleTime;

    void Awake()
    {
        Debug.Log("[ARAccuracy DriftLogger]->Awake");
        // Auto-find for convenience
        controller ??= GetComponent<TagPlacementController>()
                  ?? FindFirstObjectByType<TagPlacementController>();
        if (!controller)
        {
            Debug.Log("[ARAccuracy DriftLogger]->Awake: ERROR: Controller is NULL");
        }
    }

    void Start()
    {
        Debug.Log("[ARAccuracy DriftLogger]->Start");
        _path = Path.Combine(Application.persistentDataPath,
            $"drift_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");
        _sw = new StreamWriter(_path);
        _sw.WriteLine("t,tagId,cubeX,cubeY,cubeZ,frameDriftX,frameDriftY,frameDriftZ,frameDriftDeg,totalDriftX,totalDriftY,totalDriftZ,totalDriftDeg");
        _sw.Flush();
        Debug.Log("[ARAccuracy DriftLogger] Logging to: " + _path);
    }

    void Update()
    {
        //Debug.Log("[ARAccuracy DriftLogger]->Update");
        if (controller == null)
        {
            Debug.Log("[ARAccuracy DriftLogger]->Update: ERROR: Controller is NULL");
            return;
        }
        if (controller.obj3d == null)
        {
            Debug.Log("[ARAccuracy DriftLogger]->Update: ERROR: controller.obj3d is NULL");
            return;
        }

        if (Time.timeAsDouble < _nextSampleTime) return;
        _nextSampleTime = Time.timeAsDouble + sampleInterval;

        var p = controller.obj3d.position;
        var frameD = controller.FrameToFrameDriftPos;
        var frameA = controller.FrameToFrameDriftDeg;
        var totalD = controller.TotalDriftPos;
        var totalA = controller.TotalDriftDeg;
        ulong id = controller.LastTagId;

        Debug.Log("[ARAccuracy DriftLogger]->Update: SAMPLED!");

        _sw.WriteLine($"{Time.timeAsDouble:F3},{id},{p.x:F4},{p.y:F4},{p.z:F4},{frameD.x:F4},{frameD.y:F4},{frameD.z:F4},{frameA:F2},{totalD.x:F4},{totalD.y:F4},{totalD.z:F4},{totalA:F2}");
        _sw.Flush();
    }

    void OnDestroy()
    {
        Debug.Log("[ARAccuracy DriftLogger]->OnDestroy");
        if (_sw != null) { _sw.Flush(); _sw.Close(); _sw.Dispose(); }
    }
}
