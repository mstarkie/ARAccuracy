using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HudReset : MonoBehaviour
{
    [TextArea] public string initialText = "APRILTAG HUD — if you can read this, UI is OK";

    void Start()
    {
        // Nuke old HUDs (optional)
        foreach (var c in GameObject.FindObjectsOfType<Canvas>())
            if (c.name.StartsWith("HUD_")) Destroy(c.gameObject);

        // Canvas (Overlay ignores camera/stacking/culling)
        var canvasGO = new GameObject("HUD_Canvas_Overlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Backdrop (optional, behind text)
        var bgGO = new GameObject("HUD_Backdrop", typeof(Image));
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bg = bgGO.GetComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.35f);
        var bgRT = bg.rectTransform;
        bgRT.anchorMin = new Vector2(0, 0);
        bgRT.anchorMax = new Vector2(1, 0);
        bgRT.pivot = new Vector2(0.5f, 0);
        bgRT.offsetMin = new Vector2(12, 12);
        bgRT.offsetMax = new Vector2(-12, 156);

        // Text
        var textGO = new GameObject("HUD_Text", typeof(TextMeshProUGUI));
        textGO.transform.SetParent(canvasGO.transform, false);
        var tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.text = initialText;
        tmp.fontSize = 36;
        tmp.color = new Color(1, 1, 0, 1);              // opaque yellow
        tmp.enableWordWrapping = true;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.raycastTarget = false;

        // Rect: full width, bottom
        var rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.offsetMin = new Vector2(24, 24);   // left/bottom
        rt.offsetMax = new Vector2(-24, 140); // right/top (height ~116)

        // Make sure text is on top of backdrop
        bgRT.SetAsFirstSibling();
        rt.SetAsLastSibling();
    }
}
