using UnityEngine;

public class HelloProbe : MonoBehaviour
{
    void Awake() { Debug.Log("[ARAccuracy->HelloProbe] AWAKE"); }
    void Start() { Debug.Log("[ARAccuracy->HelloProbe] START"); }

    // IMGUI draws without any Canvas/TMP
    void OnGUI()
    {
        GUI.color = Color.white;
        GUI.Label(new Rect(12, 12, 1600, 60), "[HelloProbe] OnGUI alive");
    }
}

