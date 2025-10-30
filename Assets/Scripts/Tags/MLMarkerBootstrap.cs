// MLMarkerBootstrap.cs  (replace the try/catch block with this)
using UnityEngine;
using UnityEngine.XR.OpenXR;
using MagicLeap.OpenXR.Features.MarkerUnderstanding;

public class MLMarkerBootstrap : MonoBehaviour
{
    [SerializeField] TMPro.TextMeshProUGUI hud;

    const string MarkerPermission = "com.magicleap.permission.MARKER_TRACKING";

    void Start()
    {
        Debug.Log("[ARAccuracy MLMarkerBootstrap]->Start");
        var feat = OpenXRSettings.Instance?.GetFeature<MagicLeapMarkerUnderstandingFeature>();
        Log($"[Bootstrap] Marker feature present? {feat != null}, enabled? {feat?.enabled}");

        if (feat == null)
        {
            Debug.Log("[ARAccuracy MLMarkerBootstrap]->Start feature is null");
            Log("<color=red>Marker Understanding feature missing/disabled.</color>\n" +
                "Project Settings → XR Plug-in Management → OpenXR (Android) → enable Magic Leap 2 Marker Understanding.");
            return;
        }

        if (!feat.enabled)
        {
            Debug.Log("[ARAccuracy MLMarkerBootstrap]->Start feature not enabled");
            Log("<color=red>Marker Understanding feature missing/disabled.</color>\n" +
                "Project Settings → XR Plug-in Management → OpenXR (Android) → enable Magic Leap 2 Marker Understanding.");
            return;
        }

        // Optional runtime ask (older ML2 builds won’t need this if manifest has it)
#if UNITY_ANDROID && !UNITY_EDITOR
        const string MarkerPermission = "com.magicleap.permission.MARKER_TRACKING";
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(MarkerPermission))
        {
            Log("[Bootstrap] Requesting MARKER_TRACKING...");
            UnityEngine.Android.Permission.RequestUserPermission(MarkerPermission);
        }
        else
        {
            Log("[Bootstrap] MARKER_TRACKING already granted.");
        }
#endif
    }

    void Log(string msg) { Debug.Log(msg); if (hud) hud.text = msg + "\n" + hud.text; }
}
