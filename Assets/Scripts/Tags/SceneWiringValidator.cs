// SceneWiringValidator.cs
using UnityEngine;

public class SceneWiringValidator : MonoBehaviour
{
    [SerializeField] MagicLeapTagDetector_260 detector;
    [SerializeField] TagPlacementController controller;
    [SerializeField] DriftLogger logger;
    [SerializeField] TMPro.TextMeshProUGUI hud;
    [SerializeField] Transform shipRoot;
    [SerializeField] Transform obj3d;
    [SerializeField] UnityEngine.Camera xrCamera;

    void Awake()
    {
        // Auto-find if fields are empty (safe convenience).
        detector ??= GetComponent<MagicLeapTagDetector_260>();
        controller ??= GetComponent<TagPlacementController>();
        logger ??= GetComponent<DriftLogger>();
        shipRoot ??= GameObject.Find("ShipRoot")?.transform;
        obj3d ??= GameObject.Find("obj3d")?.transform;
        hud ??= GameObject.Find("HUD Text")?.GetComponent<TMPro.TextMeshProUGUI>();
        xrCamera ??= Camera.main;

        // Push refs into components if they’re null.
        if (controller)
        {
            if (controller.GetType().GetField("tagDetector") != null && controller.tagDetector == null)
                controller.tagDetector = detector;
            if (controller.shipRoot == null) controller.shipRoot = shipRoot;
            if (controller.obj3d == null) controller.obj3d = obj3d;
            if (controller.hudText == null) controller.hudText = hud;
        }
        if (logger && logger.controller == null) logger.controller = controller;
    }

    void Start()
    {
        Debug.Log("[ARAccuracy]->Start");
        var ok = true;
        ok &= Assert(detector, "Missing MagicLeapTagDetector_260 on TagSystem.");
        ok &= Assert(controller, "Missing TagPlacementController on TagSystem.");
        ok &= Assert(logger, "Missing DriftLogger on TagSystem.");
        ok &= Assert(hud, "HUD Text (TextMeshProUGUI) not assigned/found.");
        ok &= Assert(shipRoot, "ShipRoot Transform not found.");
        ok &= Assert(obj3d, "Cube Transform not found.");
        ok &= Assert(xrCamera, "XR camera not found. Ensure XR Origin has a child Camera tagged MainCamera.");

        // Show one-line “wired” status on HUD.
        if (hud) hud.text = (ok ? "<color=green>Scene wiring OK</color>" : "<color=red>Scene wiring errors</color>") + "\n" + hud.text;
    }

    bool Assert(Object o, string msg)
    {
        if (o) return true;
        Debug.LogError("[Wiring] " + msg);
        if (hud) hud.text = "<color=red>" + msg + "</color>\n" + hud.text;
        return false;
    }
}

