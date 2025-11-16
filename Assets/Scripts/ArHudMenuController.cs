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
    [SerializeField] private TextMeshProUGUI clearTargetLabel;

    // Internal state
    private bool _menuOpen;
    private bool _continuousAcquisition = true; // default
    public bool ContinuousAcquisition => _continuousAcquisition;
    private bool _started = false; // default
    public bool Started => _started;

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
        if (tagPlacementController != null)
        {
             _started = !_started;
            UpdateClearTargetLabel();
            if (_started)
            {
                tagPlacementController.StartDetecting();
                Debug.Log("[ARAccuracy Menu] tagPlacementController enabled!");
            } else
            {
                tagPlacementController.StopDetecting();
                tagPlacementController.ClearTargetAcquisition();
                Debug.Log("[ARAccuracy Menu] tagPlacementController disabled!");
            }
            
        }
    }

    // Req #1: Toggle single vs continuous (logic will be fleshed out next)
    public void OnToggleAcquisitionModePressed()
    {
        if (_started)
        {
            return;
        }
        _continuousAcquisition = !_continuousAcquisition;
          Debug.Log("[ARAccuracy Menu] TOGGLED! _continuousAcquisition is now: " + _continuousAcquisition + " on instance: "
   + gameObject.name + " InstanceID: " + GetInstanceID());
        UpdateAcquisitionLabel();

        Debug.Log("[ARAccuracy Menu] Acquisition mode: " +
                  (_continuousAcquisition ? "Continuous Aquisition" : "Single Aquisition"));
    }
    
    private void UpdateAcquisitionLabel()
    {
        if (acquisitionModeLabel != null)
        {
            acquisitionModeLabel.text = _continuousAcquisition
                ? "Continuous Aquisition"
                : "Single Aquisition";
        }
    }

       private void UpdateClearTargetLabel()
    {
        if (clearTargetLabel != null)
        {
            clearTargetLabel.text = _started
                ? "Stop"
                : "Start";
        }
    }
}
