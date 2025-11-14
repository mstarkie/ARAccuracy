// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2024) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

[RequireComponent(typeof(ToggleGroup))]
public class WebViewTabBar : MonoBehaviour
{
    public delegate void TabEventHandler(WebViewTab tab, bool isPopup = false, ulong popupId = 0, string url = null);
    
    private GameObject tabPrefab;
    private ToggleGroup toggleGroup;

    public event TabEventHandler OnTabCreated;
    public event TabEventHandler OnTabChange;
    public event TabEventHandler OnTabClosed;

    private readonly List<WebViewTab> openTabs = new();

    public WebViewTab ActiveTab { get; private set; }

    private void Awake()
    {
        toggleGroup = GetComponent<ToggleGroup>();
        var toggle = toggleGroup.GetFirstActiveToggle();
        var firstTab = toggle.GetComponent<WebViewTab>();
        tabPrefab = Instantiate(firstTab.gameObject, transform);
        tabPrefab.SetActive(false);
        ActiveTab = firstTab;
        openTabs.Add(ActiveTab);
    }

    public void OpenNewTab()
    {
        CreateAndAddTab(null, 0, null);
    }

    public void CreateAndAddTab(MLWebView popupWebView, ulong popupId, string url)
    {
        var tabObj = Instantiate(tabPrefab, transform);
        tabObj.SetActive(true);
        tabObj.transform.SetSiblingIndex(transform.childCount - 3);

        var newTab = tabObj.GetComponent<WebViewTab>();
        openTabs.Add(newTab);
        OnTabCreated?.Invoke(newTab, (popupWebView != null), popupId, url);

        foreach (var tab in GetComponentsInChildren<WebViewTab>())
        {
            tab.ToggleCloseButton(openTabs.Count > 1);
        }

        SetTabActive(newTab, true);
    }

    public void CloseTab(WebViewTab closedTab)
    {
        closedTab.WebView.OnPopupOpened -= CreateAndAddTab;
        
        OnTabClosed?.Invoke(closedTab);
        
        Destroy(closedTab.gameObject);
        
        foreach (var tab in GetComponentsInChildren<WebViewTab>())
        {
            tab.ToggleCloseButton(openTabs.Count > 1);
        }
    }

    public void SetTabActive(WebViewTab tab, bool selected)
    {
        if (selected)
        {
            ActiveTab = tab;
            tab.GetComponent<Toggle>().isOn = true;
        }
        
        OnTabChange?.Invoke(tab);
    }
}
