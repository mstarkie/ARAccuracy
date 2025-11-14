// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2024) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MagicLeap.XRKeyboard;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR.MagicLeap.Native;

[RequireComponent(typeof(Toggle))]
public class WebViewTab : MonoBehaviour
{
    [SerializeField]
    private WebViewTabBar tabBar;
    
    [SerializeField]
    private Text label;

    [SerializeField]
    private Button closeButton;

    [SerializeField]
    private WebViewScreen webViewScreen;

    [SerializeField]
    private TMP_InputField addressBar;

    public Text Label => label;
    public string Location { get; private set; } = "https://www.google.com";

    private Toggle toggle;
    private MLWebView mlWebView;
    private bool loadOnServiceConnected;
    private bool initialized = false;

    public bool IsSelected => toggle.isOn;
    
    public MLWebView WebView => mlWebView;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
        toggle.group = GetComponentInParent<ToggleGroup>();
    }

    private void OnDestroy()
    {
        if (mlWebView != null)
        {
            mlWebView.OnLoadEnded -= WebViewOnOnLoadEnded;
            mlWebView.OnServiceConnected -= WebViewOnOnServiceConnected;
            mlWebView.OnPopupOpened -= tabBar.CreateAndAddTab;
            mlWebView.OnPopupClosed -= OnPopupClosed;

            if (webViewScreen != null)
            {
                webViewScreen.ServiceDisconnected();
            }

            if (!mlWebView.Destroy().IsOk)
            {
                Debug.LogError($"Failed to destroy WebView tab {mlWebView.WebViewHandle}");
            }
            else
            {
                mlWebView = null;
            }
        }
    }

    public void SelectTab(bool selected)
    {
        toggle.SetIsOnWithoutNotify(selected);
        tabBar.SetTabActive(this, selected);

        var kbm = KeyboardManager.Instance;
        if (kbm != null)
        {
            kbm.DespawnKeyboard();
        }
    }

    public void CloseTab()
    {
        tabBar.CloseTab(this);
    }

    public void InitWebView(bool isPopup = false, ulong popupId = 0, string popupUrl = null)
    {
        mlWebView = MLWebView.Create(webViewScreen.Width, webViewScreen.Height, isPopup, popupId);
        mlWebView.OnLoadEnded += WebViewOnOnLoadEnded;
        mlWebView.OnServiceConnected += WebViewOnOnServiceConnected;
        mlWebView.OnPopupOpened += tabBar.CreateAndAddTab;
        mlWebView.OnPopupClosed += OnPopupClosed;
        GoToLocation(popupUrl ?? Location);
        initialized = true;
    }

    private void OnPopupClosed(MLWebView webview, ulong handle)
    {
        webview.OnPopupClosed -= OnPopupClosed;
        CloseTab();
    }

    private void WebViewOnOnLoadEnded(MLWebView webview, bool ismainframe, int httpstatuscode)
    {
        StartCoroutine(UpdateTabLabel());
    }

    private void WebViewOnOnServiceConnected(MLWebView webview)
    {
        webViewScreen.ServiceConnected();
        
        if (loadOnServiceConnected)
        {
            loadOnServiceConnected = false;
            if (!string.IsNullOrEmpty(Location))
            {
                if (!mlWebView.GoTo(Location).IsOk)
                {
                    Debug.LogError($"Failed to navigate to {Location}");
                }
            }
        }
    }

    private IEnumerator UpdateTabLabel()
    {
        if (label != null && mlWebView != null)
        {
            Location = mlWebView.GetURL();
            var uri = new Uri(Location);
            label.text = uri.Host;
            
            using UnityWebRequest webRequest = UnityWebRequest.Get(uri);
            yield return webRequest.SendWebRequest();
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                var text = webRequest.downloadHandler.text;
                var regex = new Regex("<title>(.*?)</title>", RegexOptions.IgnoreCase);
                var match = regex.Match(text);
                if (match.Success)
                {
                    if (match.Groups.Count > 0)
                    {
                        label.text = match.Groups[1].Value;
                    }
                }
            }
        }
    }

    public void GoToLocation(string location)
    {
        StartCoroutine(GoToLocationAfterWait(location));
    }

    private IEnumerator GoToLocationAfterWait(string location)
    {
        yield return new WaitForEndOfFrame();
        if (mlWebView != null)
        {
            if (webViewScreen.IsConnected)
            {
                if (!location.StartsWith("http"))
                    location = "https://" + location;
                
                if (mlWebView.GoTo(location).IsOk)
                {
                    Location = location;
                }
            }
            else
            {
                Location = location;
                loadOnServiceConnected = true;
            }
        }
    }

    public void ToggleCloseButton(bool enableClose)
    {
        closeButton.interactable = enableClose;
    }
}
