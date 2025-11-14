// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2018-2024) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%
using System;
using System.Collections;
using MagicLeap.Examples;
using MagicLeap.XRKeyboard;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR.MagicLeap.Native;

[RequireComponent(typeof(MeshRenderer))]
public class WebViewScreen : MonoBehaviour
{
    public enum ScrollingMode
    {
        Touchpad,
        TriggerDrag
    }
    
    [SerializeField]
    private uint width;

    [SerializeField]
    private uint height;

    [SerializeField]
    private WebViewTabBar tabBar;

    [SerializeField]
    private Button backButton;

    [SerializeField]
    private Button forwardButton;
    
    [SerializeField]
    private Transform pointRayTransform;
    
    [SerializeField]
    private ScrollingMode scrollingMode = ScrollingMode.Touchpad;

    private MeshRenderer webViewMeshRenderer;
    private RenderTexture webViewTexture;
    private MLWebView currentWebView;
    private MLWebView.Renderer webViewRenderer;
    
    private bool isInitialized = false;
    private uint connectionCount;
    private bool isScrolling = false;
    private bool isTriggerDown = false;
    private bool pointerOverWebView = false;
    private uint previousCursorPositionX;
    private uint previousCursorPositionY;
    private uint currentCursorPositionX;
    private uint currentCursorPositionY;
    private Vector2 previousTouchpadPos = new Vector2(0.0f, 0.0f);
    private Vector2 currentTouchpadPos = new Vector2(0.0f, 0.0f);
    private bool resetTouchpadPos = false;
    private RaycastHit raycastHit;

    public ScrollingMode ScrollMode
    {
        get => scrollingMode;
        set => scrollingMode = value;
    }
    
    public uint Width => width;
    public uint Height => height;
    public bool IsConnected { get; private set; }
    
    private void Start()
    {
        webViewMeshRenderer = GetComponent<MeshRenderer>();
    }

    private void OnDestroy()
    {
        DestroyWebViewWindow();
    }

    private void OnEnable()
    {
        MagicLeapController.Instance.Touchpad += HandleOnTouchpadPosition;
        MagicLeapController.Instance.TouchpadForceApplied += HandleOnTouchpadForceDown;
        MagicLeapController.Instance.TouchpadForceCancelled += HandleOnTouchpadForceUp;
        MagicLeapController.Instance.TriggerPressed += HandleTriggerDown;
        MagicLeapController.Instance.TriggerReleased += HandleTriggerUp;
    }

    private void OnDisable()
    {
        MagicLeapController.Instance.Touchpad -= HandleOnTouchpadPosition;
        MagicLeapController.Instance.TouchpadForceApplied -= HandleOnTouchpadForceDown;
        MagicLeapController.Instance.TouchpadForceCancelled -= HandleOnTouchpadForceUp;
        MagicLeapController.Instance.TriggerPressed -= HandleTriggerDown;
        MagicLeapController.Instance.TriggerReleased -= HandleTriggerUp;
    }

    private void Update()
    {
        ProcessInput();

        if (isInitialized)
        {
            if (IsConnected)
            {
                if (currentWebView != null)
                {
                    if (webViewRenderer != null && MagicLeapNativeBindings.MLHandleIsValid(webViewRenderer.WebViewHandle))
                    {
                        webViewRenderer.Render();
                        UpdateNavButtons();
                    }
                }

                if (webViewTexture != null)
                {
                    if (!webViewTexture.IsCreated())
                    {
                        Debug.LogError("Failed to create WebViewTexture");
                    }
                }
            }
        }
    }

    private void ProcessInput()
    {
        if (Physics.Raycast(pointRayTransform.position, pointRayTransform.forward, out raycastHit) && raycastHit.collider.gameObject == gameObject)
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                pointerOverWebView = true;

                if (isInitialized)
                {
                    if (currentWebView != null)
                    {
                        var position = webViewMeshRenderer.transform.position;
                        var bounds = webViewMeshRenderer.bounds;
                        currentCursorPositionX = (uint)((raycastHit.point.x - position.x) / bounds.size.x * Width + Width / 2);
                        currentCursorPositionY = (uint)(-(raycastHit.point.y - position.y) / bounds.size.y * Height + Height / 2);

                        if (isTriggerDown)
                        {
                            if (MagicLeapNativeBindings.MLHandleIsValid(currentWebView.WebViewHandle))
                            {
                                currentWebView.InjectMouseMove(currentCursorPositionX, currentCursorPositionY,
                                    MLWebView.EventFlags.LeftMouseButton);
                            }

                            KeyboardManager.Instance.DespawnKeyboard();
                        }
                        else
                        {
                            if (MagicLeapNativeBindings.MLHandleIsValid(currentWebView.WebViewHandle))
                            {
                                currentWebView.InjectMouseMove(currentCursorPositionX, currentCursorPositionY,
                                    MLWebView.EventFlags.None);
                            }
                        }

                        if (isScrolling && scrollingMode == ScrollingMode.TriggerDrag)
                        {
                            if (MagicLeapNativeBindings.MLHandleIsValid(currentWebView.WebViewHandle))
                            {
                                currentWebView.ScrollBy(currentCursorPositionX - previousCursorPositionX,
                                    currentCursorPositionY - previousCursorPositionY);
                            }
                        }

                        previousCursorPositionX = currentCursorPositionX;
                        previousCursorPositionY = currentCursorPositionY;
                    }
                }
            }
            else
            {
                pointerOverWebView = false;
            }
        }
        else
        {
            pointerOverWebView = false;
        }

        if (isInitialized)
        {
            if (isScrolling && scrollingMode == ScrollingMode.Touchpad)
            {
                if (resetTouchpadPos)
                {
                    previousTouchpadPos = currentTouchpadPos;
                    resetTouchpadPos = false;
                }
                else
                {
                    if (MagicLeapNativeBindings.MLHandleIsValid(currentWebView.WebViewHandle))
                    {
                        currentWebView.ScrollBy((uint)(currentTouchpadPos.x - previousTouchpadPos.x),
                            (uint)(currentTouchpadPos.y - previousTouchpadPos.y));
                    }
                }
            }

            previousTouchpadPos = currentTouchpadPos;
        }
    }
    
    /// <summary>
    /// Handles the Touchpad position change events.
    /// </summary>
    private void HandleOnTouchpadPosition(InputAction.CallbackContext obj)
    {
        if (isInitialized)
        {
            currentTouchpadPos = obj.ReadValue<Vector2>();
            currentTouchpadPos.x *= Width;
            currentTouchpadPos.y *= Height;
        }
    }

    /// <summary>
    /// Handles the Touchpad force down change events.
    /// </summary>
    private void HandleOnTouchpadForceDown(InputAction.CallbackContext obj)
    {
        if (scrollingMode == ScrollingMode.Touchpad)
        {
            isScrolling = true;
        }
    }

    /// <summary>
    /// Handles the Touchpad force up change events.
    /// </summary>
    private void HandleOnTouchpadForceUp(InputAction.CallbackContext obj)
    {
        if (scrollingMode == ScrollingMode.Touchpad)
        {
            isScrolling = false;
            resetTouchpadPos = true;
        }
    }

    /// <summary>
    /// Handles the WebView Button Down events.
    /// </summary>
    private void HandleTriggerDown(InputAction.CallbackContext callbackContext)
    {
        if (pointerOverWebView && isInitialized && !isTriggerDown)
        {
            if (currentWebView != null)
            {
                currentWebView.InjectMouseButtonDown(currentCursorPositionX, currentCursorPositionY, MLWebView.EventFlags.LeftMouseButton);
                isTriggerDown = true;

                if (scrollingMode == ScrollingMode.TriggerDrag)
                {
                    isScrolling = true;
                }
            }
        }
    }

    /// <summary>
    /// Handles the WebView Button Up events.
    /// </summary>
    private void HandleTriggerUp(InputAction.CallbackContext callbackContext)
    {
        if (pointerOverWebView && isInitialized && isTriggerDown)
        {
            currentWebView?.InjectMouseButtonUp(currentCursorPositionX, currentCursorPositionY, MLWebView.EventFlags.LeftMouseButton);
            isTriggerDown = false;
        }
        if (scrollingMode == ScrollingMode.TriggerDrag)
        {
            isScrolling = false;
        }
    }

    public bool CreateWebViewWindow()
    {
        if (!isInitialized)
        {
            CreateTexture((int)width, (int) height);
            isInitialized = true;
            return true;
        }

        return false;
    }
    
    public void DestroyWebViewWindow()
    {
        if (isInitialized)
        {
            isInitialized = false;
            webViewRenderer.Cleanup();
            webViewRenderer = null;
        }
    }

    private void CreateTexture(int viewWidth, int viewHeight)
    {
        viewWidth = Mathf.Max(viewWidth, 1);
        viewHeight = Mathf.Max(viewHeight, 1);

        if (webViewTexture != null && (webViewTexture.width != viewWidth || webViewTexture.height != viewHeight))
        {
            Destroy(webViewTexture);
            webViewTexture = null;
        }

        if (webViewTexture == null)
        {
            // Create texture with given dimensions
            webViewTexture = new RenderTexture(viewWidth, viewHeight, 0, RenderTextureFormat.ARGB32);

            // Set texture on quad
            webViewMeshRenderer.material.mainTexture = webViewTexture;
            
            Debug.Log("WebViewScreen created and assigned new texture");
        }

        webViewRenderer = new MLWebView.Renderer();
        webViewRenderer.SetRenderBuffer(webViewTexture);
    }

    public void SetTab(WebViewTab tab)
    {
        currentWebView = tab.WebView;
        var viewHandle = currentWebView?.WebViewHandle ?? MagicLeapNativeBindings.InvalidHandle;
        webViewRenderer.WebViewHandle = viewHandle;
    }

    public void ServiceConnected()
    {
        connectionCount++;
        IsConnected = true;
    }

    public void ServiceDisconnected()
    {
        if (--connectionCount == 0)
        {
            IsConnected = false;
            if (webViewTexture != null)
            {
                var rt = RenderTexture.active;
                RenderTexture.active = webViewTexture;
                GL.Clear(true, true, Color.clear);
                RenderTexture.active = rt;

                webViewMeshRenderer.material.mainTexture = webViewTexture;
            }
        }
    }

    public void ReloadWebView(bool ignoreCertError)
    {
        if (currentWebView != null)
        {
            currentWebView.IgnoreCertificateError = ignoreCertError;
            var result = currentWebView.Reload();
            MLResult.DidNativeCallSucceed(result.Result, nameof(MLWebView.Reload));
        }
    }

    public void GoBack()
    {
        if (currentWebView != null)
        {
            MLResult.DidNativeCallSucceed(currentWebView.GoBack().Result, nameof(MLWebView.GoBack));
        }
    }

    public void GoForward()
    {
        if (currentWebView != null)
        {
            MLResult.DidNativeCallSucceed(currentWebView.GoForward().Result, nameof(MLWebView.GoForward));
        }
    }

    public void UpdateNavButtons()
    {
        if (currentWebView != null)
        {
            backButton.interactable = currentWebView.CanGoBack();
            forwardButton.interactable = currentWebView.CanGoForward();
        }
    }

    public void SendCharacter(char character)
    {
        if (KeyboardManager.Instance.GetKeyboard().CurrentInputField == null)
        {
            currentWebView?.InjectChar(character);
        }
    }

    public void SendDelete()
    {
        if (KeyboardManager.Instance.GetKeyboard().CurrentInputField == null)
        {
            currentWebView?.InjectKeyDown(MLWebView.KeyCode.Delete, 0);
            currentWebView?.InjectKeyUp(MLWebView.KeyCode.Delete, 0);
        }
    }

    public void SendReturn()
    {
        if (KeyboardManager.Instance.GetKeyboard().CurrentInputField == null)
        {
            currentWebView?.InjectKeyDown(MLWebView.KeyCode.Return, 0);
            currentWebView?.InjectKeyUp(MLWebView.KeyCode.Return, 0);
        }
    }
}
