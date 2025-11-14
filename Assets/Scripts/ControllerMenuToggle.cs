using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerMenuToggle : MonoBehaviour
{
    [Header("References")]
    public GameObject menuPanel;   // The AR Menu Canvas root
    public InputActionReference menuAction; // RightHand/Menu action 

    [Header("Editor Testing")]
    public bool enableKeyboardToggle = true;
    public Key keyboardToggleKey = Key.M;   // Press 'M' in Play mode

    private bool menuVisible = false;  

    private void OnEnable()
    {
        Debug.LogWarning("[AR CMT] ControllerMenuToggle OnEnable().");
        if (menuAction != null)
        {
            menuAction.action.performed += OnMenuPressed;
            menuAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        Debug.LogWarning("[AR CMT] ControllerMenuToggle OnDisable().");
        if (menuAction != null)
            menuAction.action.performed -= OnMenuPressed;
    }

    private void OnMenuPressed(InputAction.CallbackContext ctx)
    {
        ToggleMenu();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Editor-only convenience: allow toggling with keyboard
        if (enableKeyboardToggle && Keyboard.current != null)
        {
            if (Keyboard.current[keyboardToggleKey].wasPressedThisFrame)
            {
                ToggleMenu();
            }
        }
    }

    private void ToggleMenu()
    {
        Debug.LogWarning("[AR CMT] ControllerMenuToggle ToggleMenu().");
        menuVisible = !menuVisible;

        if (menuPanel != null)
            menuPanel.SetActive(menuVisible);
    }
}
