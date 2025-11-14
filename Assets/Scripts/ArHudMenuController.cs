using TMPro;
using UnityEngine;

public class ArHudMenuController : MonoBehaviour
{
    [Header("Menu Root")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Target / Drift")]
    [SerializeField] private TagPlacementController tagPlacementController;

    [Header("Acquisition Mode UI")]
    [SerializeField] private TextMeshProUGUI acquisitionModeLabel;

    // Internal state
    private bool _menuOpen;
    private bool _continuousAcquisition = true; // default
    public bool ContinuousAcquisition => _continuousAcquisition;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        // Start with menu visible; no keyboard / legacy Input used
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        UpdateAcquisitionLabel();
    }

    private void Update()
    {
       
    }

    public void OnClearTargetPressed()
    {
         Debug.Log("[AR Menu] Clear Target button pressed.");
        if (tagPlacementController != null)
        {
            Debug.Log("[AR Menu] Calling TagPlacementController.ClearTargetAcquisition()");
            tagPlacementController.ClearTargetAcquisition();
        }
        else
        {
            Debug.LogWarning("[AR Menu] TagPlacementController not assigned.");
        }
    }

    // Req #1: Toggle single vs continuous (logic will be fleshed out next)
    public void OnToggleAcquisitionModePressed()
    {
        _continuousAcquisition = !_continuousAcquisition;
        UpdateAcquisitionLabel();

        Debug.Log("[AR Menu] Acquisition mode: " +
                  (_continuousAcquisition ? "Continuous" : "Single"));

        // TODO: in the next step we’ll hook this into the tag detector /
        // TagPlacementController so they respect this mode.
    }
    
    private void UpdateAcquisitionLabel()
    {
        if (acquisitionModeLabel != null)
        {
            acquisitionModeLabel.text = _continuousAcquisition
                ? "Mode: Continuous"
                : "Mode: Single";
        }
    }
}
