// AliveBeacon.cs
using UnityEngine;
public class AliveBeacon : MonoBehaviour
{
    public TMPro.TextMeshProUGUI hud;
    void Start()
    {
        Debug.Log("[ARAccuracy]->AliveBeacon: App started");
        if (hud) hud.text = "[AliveBeacon] App started\n" + hud.text;
    }
}
