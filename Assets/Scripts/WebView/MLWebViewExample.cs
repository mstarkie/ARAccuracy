// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2024) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%
using System.Collections;
using System.Text;
using MagicLeap.Android;
using MagicLeap.Examples;
using MagicLeap.XRKeyboard;
using MagicLeap.XRKeyboard.Component;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;
using Keyboard = MagicLeap.XRKeyboard.Keyboard;

public class MLWebViewExample : MonoBehaviour
{
    [SerializeField]
    private string startLocation = "https://magicleap.com";
    
    [SerializeField]
    private WebViewTabBar tabBar;

    [SerializeField]
    private TMP_InputField locationInput;
    
    [SerializeField]
    private WebViewScreen webViewScreen;

    [SerializeField]
    private RectTransform certificateErrorPopup;

    [SerializeField]
    private Text status;
    
    private string loadStatus = "";
    private Keyboard webViewKeyboard;
    private bool locationEditing = false;
    
    private IEnumerator Start()
    {
        certificateErrorPopup.gameObject.SetActive(false);
        
        tabBar.OnTabCreated += HandleTabCreated;
        tabBar.OnTabChange += HandleTabChange;
        tabBar.OnTabClosed += TabBarOnOnTabClosed;
        
        locationInput.onSelect.AddListener(HandleLocationSelect);
        locationInput.onSubmit.AddListener(HandleLocationChange);

        yield return null;

        locationInput.onFocusSelectAll = true;
     
        if (Permissions.CheckPermission(Permissions.WebView))
        {
            CreateWebViewRenderer();
        
            HandleTabCreated(tabBar.ActiveTab, false, 0, null);
            
            tabBar.ActiveTab.GoToLocation(startLocation);
            webViewScreen.SetTab(tabBar.ActiveTab);

            MagicLeapController.Instance.BumperPressed += OnBumper;
        }
        else
        {
            Debug.LogError($"Missing permission {Permissions.WebView}");
        }
    }

    private void Update()
    {
        UpdateStatus();

        if (!locationEditing && tabBar.ActiveTab.WebView != null)
        {
            locationInput.text = tabBar.ActiveTab.WebView.GetURL();
        }
    }

    private void OnDestroy()
    {
        tabBar.OnTabCreated -= HandleTabCreated;
        tabBar.OnTabChange -= HandleTabChange;
        locationInput.onSubmit.RemoveListener(HandleLocationChange);
        MagicLeapController.Instance.BumperPressed -= OnBumper;
    }
    
    private void OnBumper(InputAction.CallbackContext obj)
    {
        webViewScreen.ScrollMode = webViewScreen.ScrollMode == WebViewScreen.ScrollingMode.Touchpad ? 
            WebViewScreen.ScrollingMode.TriggerDrag : 
            WebViewScreen.ScrollingMode.Touchpad;
    }

    private void CreateWebViewRenderer()
    {
        if (!webViewScreen.CreateWebViewWindow())
        {
            Debug.LogError("Failed to create web view window");
        }
    }

    private void HandleTabCreated(WebViewTab newTab, bool isPopup, ulong popupId, string popupUrl)
    {
        newTab.InitWebView(isPopup, popupId, popupUrl);
        newTab.WebView.OnLoadEnded += WebViewOnOnLoadEnded;
        newTab.WebView.OnErrorLoaded += WebViewOnOnErrorLoaded;
        newTab.WebView.OnCertificateErrorLoaded += WebViewOnOnCertificateErrorLoaded;
        newTab.WebView.OnKeyboardShown += WebViewOnKeyboardShown;
        newTab.WebView.OnKeyboardDismissed += WebViewOnKeyboardDismissed;
    }

    private void TabBarOnOnTabClosed(WebViewTab tab, bool ispopup, ulong popupid, string url)
    {
        tab.WebView.OnLoadEnded -= WebViewOnOnLoadEnded;
        tab.WebView.OnErrorLoaded -= WebViewOnOnErrorLoaded;
        tab.WebView.OnCertificateErrorLoaded -= WebViewOnOnCertificateErrorLoaded;
        tab.WebView.OnKeyboardShown -= WebViewOnKeyboardShown;
        tab.WebView.OnKeyboardDismissed -= WebViewOnKeyboardDismissed;
    }

    private void WebViewOnKeyboardShown(MLWebView webview, MLWebView.InputFieldData keyboardShowData)
    {
        var contentType = (keyboardShowData.TextInputType == MLWebView.TextInputType.Password) ? 
            TMP_InputField.ContentType.Password : 
            TMP_InputField.ContentType.Standard;
        
        locationInput.GetComponent<TMPInputFieldTextReceiver>().EndEdit();
        
        webViewKeyboard = KeyboardManager.Instance.ShowKeyboard(null, contentType, TouchScreenKeyboardType.Default);
        webViewKeyboard.OnKeyUp -= OnWebViewKeyUpEvt;
        webViewKeyboard.OnKeyUp += OnWebViewKeyUpEvt;
    }

    private void OnWebViewKeyUpEvt(Event keyEvent)
    {
        switch (keyEvent.keyCode)
        {
            case KeyCode.Backspace:
                webViewScreen.SendDelete();
                break;
            case KeyCode.Return:
                webViewScreen.SendReturn();
                break;
            default:
                webViewScreen.SendCharacter(keyEvent.character);
                break;
        }
    }

    private void WebViewOnKeyboardDismissed(MLWebView webview)
    {
        if (webViewKeyboard != null)
        {
            webViewKeyboard.OnKeyUp -= OnWebViewKeyUpEvt;
            webViewKeyboard.EndEdit();
        }

        webViewKeyboard = null;
        KeyboardManager.Instance.DespawnKeyboard();
    }

    private void HandleTabChange(WebViewTab tab, bool isPopup = false, ulong popupId = 0, string url = null)
    {
        if (tab.IsSelected)
        {
            webViewScreen.SetTab(tab);
            locationInput.text = tab.Location;
        }
    }

    private void WebViewOnOnLoadEnded(MLWebView webview, bool ismainframe, int httpStatusCode)
    {
        webview.IgnoreCertificateError = false;
        loadStatus = $"Success - {httpStatusCode.ToString()}";
    }

    private void WebViewOnOnErrorLoaded(MLWebView webview, bool ismainframe, int httpStatusCode, string errorStr, string failedurl)
    {
        Debug.LogError($"WebView load error. status: {httpStatusCode} error: {errorStr}");
        loadStatus = $"Failed - {httpStatusCode.ToString()} - {errorStr}";
    }

    private void WebViewOnOnCertificateErrorLoaded(MLWebView webview, int errorCode, string url, string errorMessage, string details, bool ignored)
    {
        Debug.LogError($"Certification Error ({errorCode} {(ignored ? "[IGNORED]" : string.Empty)}): {errorMessage}\nDetails:\n\t{details}");
        if (!ignored)
        {
            if (certificateErrorPopup != null)
            {
                certificateErrorPopup.gameObject.SetActive(true);
            }
        }
        loadStatus = $"Cert Error - {errorCode.ToString()} - {errorMessage}";
    }

    private void HandleLocationChange(string newLocation)
    {
        tabBar.ActiveTab.GoToLocation(newLocation);
        locationEditing = false;
        KeyboardManager.Instance.GetKeyboard().EndEdit();
    }

    private void HandleLocationSelect(string val)
    {
        locationEditing = true;
    }
    
    private void UpdateStatus()
    {
        status.text = $"<color=#B7B7B8><b>Web View Data</b></color>\n";
        StringBuilder strBuilder = new StringBuilder();
        strBuilder.Append($"Scrolling Mode: <i>{webViewScreen.ScrollMode.ToString()}</i>\n\n");
        strBuilder.Append($"Load Status: <i>{loadStatus}</i>\n");
        status.text += strBuilder.ToString();
    }

    private void OnValidate()
    {
        if (Application.isEditor && locationInput != null)
            locationInput.text = startLocation;
    }
}
