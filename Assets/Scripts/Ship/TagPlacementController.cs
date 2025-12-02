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
    public Vector3 objOffsetFromTag = new(0, 0, 0); // .1 = 1 cm

    // Drift tracking
    Vector3 _frameToFrameDriftPos;   // meters
    float _frameToFrameDriftDeg;     // degrees
    public float FrameToFrameDriftDeg => _frameToFrameDriftDeg;
    public Vector3 FrameToFrameDriftPos => _frameToFrameDriftPos;

    Vector3 _maxFrameToFrameDriftPos; 
    float _maxFrameToFrameDriftDeg;   
    public Vector3 MaxFrameToFrameDriftPos => _maxFrameToFrameDriftPos;  
    public float MaxFrameToFrameDriftDeg => _maxFrameToFrameDriftDeg;  

    private Pose _lastTagPose;  // Add as class member
    private bool _hasTagPose;   // Track if valid
    Vector3 _totalDriftPos;   // meters
    float _totalDriftDeg;     // degrees
    public float TotalDriftDeg => _totalDriftDeg;
    public Vector3 TotalDriftPos => _totalDriftPos;
    public ulong LastTagId { get; private set; }
    [SerializeField] private ArHudMenuController menuController;  // set in Inspector
    public ArHudMenuController MenuController => menuController;
    bool _hasBaseline;
    Pose _baselineCubePoseWorld;
    bool _subscribed;
    [Header("Debug Visualization")]
    public bool showTagAxes = true;
    private LineRenderer[] _axisLines = new LineRenderer[3];
    private TextMeshPro[] _axisLabels = new TextMeshPro[3];

    void CreateAxisVisualizers()
    {
        string[] labels = { "X", "Y", "Z" };
        Color[] colors = { Color.red, Color.green, Color.blue };

        for (int i = 0; i < 3; i++)
        {
            GameObject axisObj = new GameObject($"TagAxis_{i}");
            axisObj.transform.SetParent(transform);
            LineRenderer lr = axisObj.AddComponent<LineRenderer>();
            lr.startWidth = 0.005f;
            lr.endWidth = 0.005f;
            lr.positionCount = 2;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = i == 0 ? Color.red : i == 1 ? Color.green : Color.blue;
            lr.endColor = lr.startColor;
            _axisLines[i] = lr;

            // Add label
            GameObject labelObj = new GameObject($"Label_{labels[i]}");
            labelObj.transform.SetParent(axisObj.transform);
            TextMeshPro tmp = labelObj.AddComponent<TextMeshPro>();
            tmp.text = labels[i];
            tmp.fontSize = 0.5f;
            tmp.color = colors[i];
            tmp.alignment = TextAlignmentOptions.Center;
            _axisLabels[i] = tmp;
        }
    }
    static string ToFeetInches(float meters)
    {
        const float IN_PER_M = 39.3700787f;
        float totalIn = Mathf.Abs(meters) * IN_PER_M;
        int feet = Mathf.FloorToInt(totalIn / 12f);
        float inches = totalIn - feet * 12f;
        return $"{feet} ft {inches:0.0} in";
    }

    public void ResetDrift()
    {
        _totalDriftPos = Vector3.zero;
        _totalDriftDeg = 0f;
        _frameToFrameDriftPos = Vector3.zero;
        _frameToFrameDriftDeg = 0f;
        _maxFrameToFrameDriftPos = Vector3.zero;
        _maxFrameToFrameDriftDeg = 0f;
        _hasBaseline = false;
    }

    public void ClearTargetAcquisition()
    {
        Debug.Log("[ARAccuracy TPC] ClearTargetAcquisition() called.");
        ResetDrift();

        LastTagId = 0;

        if (obj3d != null)
        {
            obj3d.localPosition = Vector3.zero;
            obj3d.localRotation = Quaternion.identity;
        }

        if (hudText != null)
        {
            hudText.text =
            "Target cleared.\n\n" +
            "Point at an AprilTag to acquire a new target.";
        }

        Debug.Log("[ARAccuracy TPC] ClearTargetAcquisition: drift & baseline reset.");
    }

    void Awake()
    {
        // Auto-find if not assigned
        tagDetector ??= GetComponent<MagicLeapTagDetector_260>();
        shipRoot ??= GameObject.Find("ShipRoot")?.transform;
        obj3d ??= GameObject.Find("Cube")?.transform;
        hudText ??= GameObject.Find("HUD Text")?.GetComponent<TextMeshProUGUI>();
        if (showTagAxes) CreateAxisVisualizers();
        Debug.Log("[ARAccuracy TPC] Awake");
    }

    public void Enable() 
    {
        if (tagDetector == null) 
        { 
            Debug.LogError("[ARAccuracy TPC] tagDetector is null"); 
            return; 
        }
        if (!_subscribed) 
        { 
            tagDetector.OnObservation += HandleObs; 
            _subscribed = true; 
            Debug.Log("[ARAccuracy TPC] Enable"); 
        }
       
    }

    public void StartDetecting()
    {
        Enable();
        tagDetector.StartDetecting(); // idempotent
        Debug.Log("[ARAccuracy TPC] StartDetecting called");
    }

     public void StopDetecting()
    {
        Disable();
        tagDetector.StopDetecting(); 
        Debug.Log("[ARAccuracy TPC] StopDetecting called");
    }

    public void Disable()
    {
       if (_subscribed && tagDetector != null)
        {
            Debug.Log("[ARAccuracy TPC]->Disable");
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
        //Debug.Log("[ARAccuracy TPC]->HandleObs");

        _lastTagPose = tag.WorldPose;  // Store for debugging
        _hasTagPose = true;

        var intendedPos = tag.WorldPose.position + tag.WorldPose.rotation * objOffsetFromTag;
        var intendedRot = tag.WorldPose.rotation;
        var intended    = new Pose(intendedPos, intendedRot);

        Debug.Log($"[HandleObs] Offset applied: {objOffsetFromTag}");
        Debug.Log($"[HandleObs] Tag world position: {tag.WorldPose.position}");
        Debug.Log($"[HandleObs] Tag world rotation: {tag.WorldPose.rotation}");
        Debug.Log($"[HandleObs] Tag world offset: {tag.WorldPose.rotation * objOffsetFromTag}");
        Debug.Log($"[HandleObs] Intended cube world position: {intendedPos}");
        Debug.Log($"[HandleObs] Cube will position from {obj3d.position} to {intendedPos}");
        Debug.Log($"[HandleObs] Cube will rotate from {obj3d.rotation} to {intendedRot}");

        // (1) Ship/tag pose (placeholder)
        //Pose shipTagPose = new Pose(Vector3.zero, Quaternion.identity);

        // DEBUG: Print tag coordinate axes
        Vector3 tagRight = tag.WorldPose.rotation * Vector3.right;      // X (red)
        Vector3 tagUp = tag.WorldPose.rotation * Vector3.up;            // Y (green)
        Vector3 tagForward = tag.WorldPose.rotation * Vector3.forward;  // Z (blue)

        Debug.Log($"[HandleObs] Tag axes in world space:");
        Debug.Log($"[HandleObs]   Right   (X): {tagRight}");
        Debug.Log($"[HandleObs]   Up      (Y): {tagUp}");
        Debug.Log($"[HandleObs]   Forward (Z): {tagForward}");
        
        // (3) Drift versus current cube pose (BEFORE moving it)
        var current = new Pose(obj3d.position, obj3d.rotation);
        Vector3 posDrift = intended.position - current.position;
        float angDrift = Quaternion.Angle(intended.rotation, current.rotation);

        _frameToFrameDriftPos = posDrift;
        _frameToFrameDriftDeg = angDrift;

        // Track maximum frame drift
        if (_frameToFrameDriftPos.magnitude > _maxFrameToFrameDriftPos.magnitude)
            _maxFrameToFrameDriftPos = _frameToFrameDriftPos;
        if (_frameToFrameDriftDeg > _maxFrameToFrameDriftDeg)
            _maxFrameToFrameDriftDeg = _frameToFrameDriftDeg;

        LastTagId = tag.Id;

        // (5) Move cube to intended pose
        obj3d.SetPositionAndRotation(intended.position, intended.rotation);

         // DEBUG: Verify the cube actually moved
        Debug.Log($"[HandleObs] After SetPositionAndRotation:");
        Debug.Log($"[HandleObs] new cube world position: {obj3d.position}");
        Debug.Log($"[HandleObs] new cube local position: {obj3d.localPosition}");
        Debug.Log($"[HandleObs] obj3d.parent: {(obj3d.parent != null ? obj3d.parent.name : "null")}");
        if (obj3d.parent != null)
        {
            Debug.Log($"[HandleObs] parent.position: {obj3d.parent.position}");
        }

        if (!_hasBaseline)
        {
            _baselineCubePoseWorld = intended;
            _hasBaseline = true;
        }
        else
        {
            _totalDriftPos = intended.position - _baselineCubePoseWorld.position;
            _totalDriftDeg = Quaternion.Angle(intended.rotation, _baselineCubePoseWorld.rotation);
        }
        

        Debug.Log("[HandleObs] Reading menuController: " + menuController.gameObject.name + " InstanceID: " +
            menuController.GetInstanceID() + " ContinuousAcquisition=" + menuController.ContinuousAcquisition);
        Debug.Log("[HandleObs] menuController=" + (menuController != null) + " ContinuousAcquisition=" +
            (menuController != null ? menuController.ContinuousAcquisition : false) + " _hasBaseline=" + _hasBaseline);
        UpdateHud(tag, intended);
        if (menuController != null && !menuController.ContinuousAcquisition)
        {
            Debug.Log("!!! [HandleObs] SHOULD STOP DETECTING NOW !!!");
            StopDetecting();
        }
    }


    void UpdateHud(TagObservation obs, Pose intended)
    {
        if (!hudText) return;
        //Debug.Log("[ARAccuracy TPC]->UpdateHud");
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

        tagZ = -tagZ; zMeters = Vector3.Dot(delta, tagZ);

        string zFeetIn = ToFeetInches(zMeters);
        string side = zMeters >= 0 ? "in front of" : "behind";

        Debug.Log($"[UpdateHud] Camera position: {camPos}");
        Debug.Log($"[UpdateHud] Tag position: {tagPos}");
        Debug.Log($"[UpdateHud] Delta (cam - tag): {delta}");
        Debug.Log($"[UpdateHud] zMeters: {zMeters}");
        Debug.Log($"[UpdateHud] zFeetIn: {zFeetIn}");
        Debug.Log($"[UpdateHud] side: {side}");

        //string distText = $"Tag Z distance: {Mathf.Abs(zMeters):0.000} m  ({zFeetIn})  {side} tag\n" +
           //$"Lateral (X): {xMeters:0.000} m   Vertical (Y): {yMeters:0.000} m";

        string distText = $"Dist Z: {Mathf.Abs(zMeters):0.000}m ({zFeetIn}) {side}\n" +
            $"X: {xMeters:0.000}m  Y: {yMeters:0.000}m";
        sb.AppendLine(distText);
        
        sb.AppendLine($"Tag ID: {obs.Id}"); 
        sb.AppendLine($"Total Drift: Δp={_totalDriftPos.magnitude:0.000}m Δθ={_totalDriftDeg:0.00}°");
        sb.AppendLine($"Max Drift: Δp={_maxFrameToFrameDriftPos.magnitude:0.000}mΔθ={_maxFrameToFrameDriftDeg:0.00}°");
        var modeStr = (menuController == null || menuController.ContinuousAcquisition) ? "Continuous" : "Single";
        sb.AppendLine($"Mode: {modeStr}");

        hudText.text = sb.ToString();
        Debug.Log("[ARAccuracy TPC]->UpdateHud()");
    }

    // Update is called once per frame
    void Update()
    {
        if (!hudText)
        {
            Debug.Log("[ARAccuracy TPC]->Update() - hudText is NULL");
        }

        if (showTagAxes && _axisLines[0] != null)
        {
            //bool visible = _hasTagPose && _subscribed; // Only show when detecting
            bool visible = _hasTagPose;
            float axisLength = 0.3f;

            for (int i = 0; i < 3; i++)
            {
              _axisLines[i].enabled = visible;
              if (_axisLabels[i] != null)
              {
                _axisLabels[i].gameObject.SetActive(visible);
              }
            }

            if (visible)
            {
                Vector3[] directions = { Vector3.right, Vector3.up, Vector3.forward };

                for (int i = 0; i < 3; i++)
                {
                    Vector3 start = _lastTagPose.position;
                    Vector3 end = start + _lastTagPose.rotation * directions[i] * axisLength;

                    _axisLines[i].SetPosition(0, start);
                    _axisLines[i].SetPosition(1, end);

                    if (_axisLabels[i] != null)
                    {
                        _axisLabels[i].transform.position = end;
                        _axisLabels[i].transform.rotation = Camera.main.transform.rotation; 
                    }
                }
            }
        }
    }
}
