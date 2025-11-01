using MagicLeap.Android.NDK.Camera.Metadata;
using System.Text;
using TMPro;
using UnityEngine;

/*
 * Starkie, M.
 * The Big Picture:
 * When you wear an AR or VR headset, there are two coordinate worlds in play:
 *   1. The real world — where physical things exist, like your AprilTag taped to the wall or table.
 *   2. The virtual world — where Unity places 3D objects, like cubes, labels, or holograms.
 * The TagPlacementController is the bridge between these two worlds.
 * Its job is to:
 *   1. Listen for when the headset’s camera detects an AprilTag.
 *   2. Use the tag’s position and orientation to figure out where a virtual object 
 *      should appear in Unity’s world space.
 *   3. Keep the virtual object “attached” to that tag, so when you move your head around, it stays 
 *      correctly positioned relative to the tag.
 *   4. Measure how much the virtual object “drifts” when the system has to correct its estimate of 
 *      where things are in space.
 */

public class TagPlacementController : MonoBehaviour
{
    [Header("Refs")]
    public MagicLeapTagDetector_260 tagDetector;
    public Transform shipRoot;     // root of ship coordinate frame (v1: place at world origin)
    public Transform obj3d;        // the virtual object to place
    public TextMeshProUGUI hudText;           // simple UI Text at bottom of view

    [Header("Placement")]
    public Vector3 objOffsetFromTag = new(0f, 0.0f, 0.2f); // 20 cm in tag local (forward)

    // Drift tracking
    Vector3 _lastDriftPos;   // meters
    float _lastDriftDeg;     // degrees
    public float LastDriftDeg => _lastDriftDeg;
    public Vector3 LastDriftPos => _lastDriftPos;

    Vector3 _maxDriftPos;   // meters
    float _maxDriftDeg;     // degrees
    public float MaxDriftDeg => _maxDriftDeg;
    public Vector3 MaxDriftPos => _maxDriftPos;
   
    public ulong LastTagId { get; private set; }

    // Establish a “baseline”: expected 3D Object pose after first stable detection
    bool _hasBaseline;
    Pose _baselineCubePoseWorld;

    bool _subscribed;

    static string ToFeetInches(float meters)
    {
        const float IN_PER_M = 39.3700787f;
        float totalIn = Mathf.Abs(meters) * IN_PER_M;
        int feet = Mathf.FloorToInt(totalIn / 12f);
        float inches = totalIn - feet * 12f;
        return $"{feet} ft {inches:0.0} in";
    }

    public void ResetMaxDrift()
    {
        _maxDriftPos = Vector3.zero;
        _maxDriftDeg = 0f;
        _hasBaseline = false;
    }

    void Awake()
    {
        // Auto-find if not assigned
        tagDetector ??= GetComponent<MagicLeapTagDetector_260>();
        shipRoot ??= GameObject.Find("ShipRoot")?.transform;
        obj3d ??= GameObject.Find("Cube")?.transform;
        hudText ??= GameObject.Find("HUD Text")?.GetComponent<TextMeshProUGUI>();
        Debug.Log("[ARAccuracy TPC] Awake");
    }

    void OnEnable() 
    {
        if (tagDetector == null) { Debug.LogError("[ARAccuracy TPC] tagDetector is null"); return; }
        if (!_subscribed) 
        { 
            tagDetector.OnObservation += HandleObs; 
            _subscribed = true; 
            Debug.Log("[ARAccuracy TPC] OnEnable"); 
        }
        tagDetector.EnsureStarted(); // idempotent
        Debug.Log("[ARAccuracy TPC] EnsureStarted called");
    }  
  
    void OnDisable()
    {
        if (_subscribed && tagDetector != null)
        {
            Debug.Log("[ARAccuracy TPC]->OnDisable");
            tagDetector.OnObservation -= HandleObs;
            _subscribed = false;
        }
       
    }

    /*
     * @See TagPlacementController which subscribes to this event.
     * Tag Detected (TagObservation)
     * The headset’s sensors and cameras see the AprilTag and calculate
     *   "This tag is 1.2 meters in front of me, and it’s tilted slightly upward."
     *   
     * Takes the pose (position + orientation) of the tag and decides where to put a
     * virtual object relative to it.
     * 
     * Track and report drift:
     *   As you walk around and the headset’s tracking system refines its understanding of the world, 
     *   Unity sometimes needs to slightly adjust where objects appear.
     *   
     *   When that happens:
     *      The cube might jump a few millimeters or rotate a tiny bit.
     *      The script measures that correction and calls it drift.
     *      
     *      Drift tells you how stable the AR platform’s coordinate system is.
     *      If the drift is small (a few millimeters), the tracking is good.
     *      If it’s large (several centimeters), the AR platform is struggling to 
     *      maintain consistent spatial alignment.
     *   
     *   Why this matters:
     *      In shipbuilding, each AprilTag will represent a known point in the ship’s coordinate system 
     *      (like a bulkhead corner or mounting bracket).
     *      By placing virtual objects relative to those tags and watching how they drift over time, you can:
     *         Measure how precisely the AR headset maintains spatial consistency.
     *         Evaluate which hardware (Magic Leap 2, Meta Quest 3, Android tablet, etc.) best aligns virtual 
     *           content with the real ship structure.
     */
    void HandleObs(TagObservation tag)
    {
        Debug.Log("[ARAccuracy TPC]->HandleObs");

        // (1) Ship/tag pose (placeholder)
        Pose shipTagPose = new Pose(Vector3.zero, Quaternion.identity);

        // (2) Intended cube pose from tag
        var intendedPos = tag.WorldPose.position + tag.WorldPose.rotation * objOffsetFromTag;
        var intendedRot = tag.WorldPose.rotation;
        var intended    = new Pose(intendedPos, intendedRot);

        // (3) Drift versus current cube pose (BEFORE moving it)
        var current     = new Pose(obj3d.position, obj3d.rotation);
        Vector3 posDrift = intended.position - current.position;
        float   angDrift = Quaternion.Angle(intended.rotation, current.rotation);

        _lastDriftPos = posDrift;
        _lastDriftDeg = angDrift;

        // (4) Update max drift
        if (_hasBaseline)
        {
            if (_lastDriftPos.sqrMagnitude > _maxDriftPos.sqrMagnitude)
                _maxDriftPos = _lastDriftPos;

            if (_lastDriftDeg > _maxDriftDeg)
                _maxDriftDeg = _lastDriftDeg;
        }

        LastTagId = tag.Id;

        // (5) Move cube to intended pose
        obj3d.SetPositionAndRotation(intended.position, intended.rotation);

        // (6) Establish baseline once
        if (!_hasBaseline)
        {
            _baselineCubePoseWorld = intended;
            _hasBaseline = true;
        }

        // (7) HUD
        UpdateHud(tag, intended, _maxDriftPos, _maxDriftDeg);
    }


    void UpdateHud(TagObservation obs, Pose intended, Vector3 posDrift, float angDrift)
    {
        if (!hudText) return;
        Debug.Log("[ARAccuracy TPC]->UpdateHud");
        var sb = new StringBuilder();
        //sb.AppendLine($"Tag Id: {obs.Id}   size: {obs.SizeMeters:F3} m");
        //sb.AppendLine($"Tag@World   p=({obs.WorldPose.position.x:F3},{obs.WorldPose.position.y:F3},{obs.WorldPose.position.z:F3})");

        var e = obs.WorldPose.rotation.eulerAngles;

        //sb.AppendLine($"             r=({e.x:F1},{e.y:F1},{e.z:F1})°");
        //sb.AppendLine($"Cube@World  p=({intended.position.x:F3},{intended.position.y:F3},{intended.position.z:F3})");

        var ce = intended.rotation.eulerAngles;

        
        // Tag world pose
        Vector3 tagPos = obs.WorldPose.position;
        Quaternion tagRot = obs.WorldPose.rotation;

        // Tag axes in world
        Vector3 tagZ = tagRot * Vector3.forward;  // out of the tag
        Vector3 tagX = tagRot * Vector3.right;
        Vector3 tagY = tagRot * Vector3.up;

        // Camera position
        var camTr = Camera.main.transform;
        Vector3 camPos = camTr.position;
        Vector3 delta = camPos - tagPos;

        // Signed distances in meters
        float zMeters = Vector3.Dot(delta, tagZ);  // along tag Z (what you want to compare with tape)
        float xMeters = Vector3.Dot(delta, tagX);  // right (+) / left (-)
        float yMeters = Vector3.Dot(delta, tagY);  // up (+) / down (-)

        // If your readout seems negative when you stand in front of the tag,
        // your tag Z may be flipped relative to your detector's convention.
        // In that case, just invert tagZ once:
        // tagZ = -tagZ; zMeters = Vector3.Dot(delta, tagZ);

        string zFeetIn = ToFeetInches(zMeters);
        string side = zMeters >= 0 ? "in front of" : "behind";

        string distText = $"Tag Z distance: {Mathf.Abs(zMeters):0.000} m  ({zFeetIn})  {side} tag\n" +
           $"Lateral (X): {xMeters:0.000} m   Vertical (Y): {yMeters:0.000} m";
     
        sb.AppendLine(distText);
        sb.AppendLine($"Max Drift Δp (m)=({posDrift.x:+0.000;-0.000;+0.000},{posDrift.y:+0.000;-0.000;+0.000},{posDrift.z:+0.000;-0.000;+0.000})");
        sb.AppendLine($"Max Drift Δθ (deg)={_maxDriftDeg:0.00}");

        hudText.text = sb.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        if (!hudText)
        {
            Debug.Log("[ARAccuracy TPC]->Update() - hudText is NULL");
        }
    }
}
