/**
 * Copyright 2018, Charm Games Inc, All rights reserved.
 */

//------------------------------------------------------------------------------
// Using directives 
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_PS4
using UnityEngine.PS4;
using UnityEngine.PS4.VR;
#endif // UNITY_PS4
using UnityEngine.XR;

namespace NewtonVR {

//------------------------------------------------------------------------------
// Class definition 
//------------------------------------------------------------------------------

public class NVRPSVRIntegration : NVRIntegration 
{

    //--------------------------------------------------------------------------
    // Private member variables
    //--------------------------------------------------------------------------

    private bool initialized = false;
    private GameObject rigObj = null;

    private List<UnityAction> newPosesCallbacks = null;
    private List<UnityAction> NewPosesCallbacks
    {
        get
        {
            if (newPosesCallbacks == null) 
            {
                newPosesCallbacks = new List<UnityAction>();
            }
            return newPosesCallbacks;
        }
    }

    private const string kPSVRDeviceName = "PlayStationVR";
    private const string kNoneDeviceName = "None";

    /** 
     * 1.4 is Sony's recommended scale for PlayStationVR
     */
    private float renderScale = 1.4f; 

    private int hmdHandle = 0;

    /**
     * Set this to false to use the monitor/display as the Social Screen
     */
    private bool showHmdViewOnMonitor = true; 

    private GameObject headHeightOffset = null;

    private bool trackingInitialized = false;

    private bool hmdIsInitializing = false;
    private bool requestedHMDShutdown = false;

    //--------------------------------------------------------------------------
    // Public member variables
    //--------------------------------------------------------------------------

    //--------------------------------------------------------------------------
    // Public methods 
    //--------------------------------------------------------------------------

    public override void DontDestroyOnLoad()
    {
    }

    //--------------------------------------------------------------------------
        
    public override void Initialize(NVRPlayer player)
    {
        Debug.Log("NVRPSVRIntegration.Initialize - Beginning initialization");

        rigObj = player.gameObject;

        ShowHMDSetupDialogue();
    }

    //--------------------------------------------------------------------------

    public override void DeInitialize()
    {
        UnsubscribeFromServiceAndDeviceEvents();
        initialized = false;
    }

    //--------------------------------------------------------------------------

    public override bool IsInit()
    {
        return initialized;
    }

    //--------------------------------------------------------------------------
    
    public override Vector3 GetPlayspaceBounds()
    {
        // TODO: Figure out if this is necessary for PSVR
        return new Vector3(5f, 5f, 5f);
    }

    //--------------------------------------------------------------------------

    public override bool IsHmdPresent()
    {
        return XRDevice.isPresent;
    }

    //--------------------------------------------------------------------------

    public override void RegisterNewPoseCallback(UnityAction callback)
    {
        NewPosesCallbacks.Add(callback); 
    }

    //--------------------------------------------------------------------------

    public override void DeregisterNewPoseCallback(UnityAction callback)
    {
        NewPosesCallbacks.Remove(callback);
    }

    //--------------------------------------------------------------------------

    public override void MoveRig(Transform transform)
    {
        rigObj.transform.position   = transform.position; 
        rigObj.transform.rotation   = transform.rotation;
        rigObj.transform.localScale = transform.localScale;
    }

    //--------------------------------------------------------------------------

    public override void MoveRig(Vector3 position, Quaternion orientation)
    {
        rigObj.transform.position = position;
        rigObj.transform.rotation = orientation;
    }

    //--------------------------------------------------------------------------

    public override void RotateRig(Quaternion localRotation)
    {
        // Apply the rotation additively
        rigObj.transform.localRotation = rigObj.transform.localRotation * localRotation;
    }

    //--------------------------------------------------------------------------

    public override Transform GetOrigin()
    {
        return rigObj.transform;
    }

    //--------------------------------------------------------------------------

    public override Vector3 GetEyeOffset(XRNode eye)
    {
        return Vector3.zero;
    }

    //--------------------------------------------------------------------------

    public override Matrix4x4 GetEyeProjectionMatrix(XRNode eye,
                                                     float  nearZ,
                                                     float  farZ)
    {
        return Matrix4x4.identity;
    }

    //--------------------------------------------------------------------------

    public void ToggleHmdViewOnMonitor(bool showOnMonitor)
    {
        showHmdViewOnMonitor = showOnMonitor;
        XRSettings.showDeviceView = showHmdViewOnMonitor;
    }

    //--------------------------------------------------------------------------

    public void ToggleHmdViewOnMonitor()
    {
        showHmdViewOnMonitor = !showHmdViewOnMonitor;
        XRSettings.showDeviceView = showHmdViewOnMonitor;
    }

    //--------------------------------------------------------------------------

    public void ChangeRenderScale(float scale)
    {
        XRSettings.eyeTextureResolutionScale = scale;
    }

    //--------------------------------------------------------------------------
        
    public override void Recenter()
    {
        InputTracking.Recenter();
    }

    //--------------------------------------------------------------------------

    public override void Update()
    {
        UpdateTracking();

        UpdatePoseCallbacks();
    }

    //--------------------------------------------------------------------------
    // Private methods 
    //--------------------------------------------------------------------------

    private void InitHMD()
    {
        hmdIsInitializing = true;
#if UNITY_PS4
        // Register the callbacks needed to detect resetting the HMD
        SubscribeToServiceAndDeviceEvents();
#endif // UNITY_PS4

        // No need to load device again if it's already loaded.
        // Otherwise this causes a couple seconds hiccup / black screen
        if (XRSettings.loadedDeviceName != kPSVRDeviceName)
        {
            Debug.Log("Loading HMD device " + kPSVRDeviceName);
#if UNITY_PS4
            PlayStationVRSettings.postReprojectionType = PlayStationVRPostReprojectionType.PerEye;
            PlayStationVRSettings.postReprojectionRenderScale = renderScale;
#endif // UNITY_PS4
            XRSettings.LoadDeviceByName(kPSVRDeviceName);
        }

        // TODO: If we move to Unity 2018.1 or greater then use the
        // XRDevice.deviceLoaded event instead of this
        CharmGames.Core.DelayedCallback.FrameDelayedCallback(OnHMDLoaded, 1);
    }

    //--------------------------------------------------------------------------

    private void OnHMDLoaded()
    {
#if UNITY_PS4
        Debug.Log("HMD loaded. Setting XRSettings.");

        // Settings must be changed only after the HMD device is loaded
        XRSettings.enabled = true;
        XRSettings.eyeTextureResolutionScale = renderScale;
        XRSettings.showDeviceView = showHmdViewOnMonitor;

        PlayStationVRSettings.minOutputColor = new Color(.01f, .01f, .01f);

        hmdHandle = PlayStationVR.GetHmdHandle();

        initialized = true;
        InvokeOnInitializedEvent();
#endif // UNITY_PS4
        hmdIsInitializing = false;
        if (requestedHMDShutdown)
        {
            requestedHMDShutdown = false;
            DeinitHMD();
        }
    }

    //--------------------------------------------------------------------------

    private void DeinitHMD()
    {
        if (hmdIsInitializing)
        {
            requestedHMDShutdown = true;
            return;
        }
        // TODO: Figure out the appropriate handling for PSVR only titles in
        // this situation. Loading the None device causes us to lose the headset
        // and not be able to get it back without some flat UI.
        //return;

        Debug.Log("Unloading HMD device by loading device " + kNoneDeviceName);

        // Unload the PSVR by loading the None device
        XRSettings.LoadDeviceByName(kNoneDeviceName);

        //// WORKAROUND: At the moment the device is created at the end of the frame so
        //// we need to wait a frame until the VR device is changed back to 'None', and
        //// then reset the Main Camera's FOV and Aspect
        //// TODO: If we move to Unity 2018.1 or greater then use the
        //// XRDevice.deviceLoaded event instead of this
        CharmGames.Core.DelayedCallback.FrameDelayedCallback(OnHMDUnloaded, 1);
    }

    //--------------------------------------------------------------------------

    private void OnHMDUnloaded()
    {
        Debug.Log("HMD device unloaded");

        XRSettings.enabled = false;
        XRSettings.showDeviceView = false;

#if UNITY_PS4
        // Unregister the callbacks needed to detect resetting the HMD
        UnsubscribeFromServiceAndDeviceEvents();
        PlayStationVR.SetOutputModeHMD(false, 120);
#endif

        Camera.main.fieldOfView = 60f;
        Camera.main.ResetAspect();

        initialized = false;

        ShowHMDSetupDialogue();
    }

    //--------------------------------------------------------------------------

    private void SubscribeToServiceAndDeviceEvents()
    {
#if UNITY_PS4
        Utility.onSystemServiceEvent += OnSystemServiceEvent;
        PlayStationVR.onDeviceEvent += OnDeviceEvent;
#endif
    }

    //--------------------------------------------------------------------------

    private void UnsubscribeFromServiceAndDeviceEvents()
    {
#if UNITY_PS4
        Utility.onSystemServiceEvent -= OnSystemServiceEvent;
        PlayStationVR.onDeviceEvent -= OnDeviceEvent;
#endif
    }

    //--------------------------------------------------------------------------

    private void ShowHMDSetupDialogue()
    {
#if UNITY_PS4
        // The HMD Setup Dialog is not displayed on the social screen in separate
        // mode, so we'll force it to mirror-mode first
        XRSettings.showDeviceView = true;

        // Show the HMD Setup Dialog, and specify the callback for when it's finished
        HmdSetupDialog.OpenAsync(0, OnHmdSetupDialogCompleted);
#endif
    }

    //--------------------------------------------------------------------------

#if UNITY_PS4
    // HMD recenter happens in this event
    private void OnSystemServiceEvent(Utility.sceSystemServiceEventType eventType)
    {
        Debug.LogFormat("NVRPSVRIntegration.OnSystemServiceEvent: {0}", eventType);

        if (eventType == Utility.sceSystemServiceEventType.ResetVrPosition) {
            Recenter();
        }
    }
#endif

    //--------------------------------------------------------------------------

#if UNITY_PS4
    // This handles disabling VR in the event that the HMD has been disconnected
    private bool OnDeviceEvent(PlayStationVR.deviceEventType eventType, int value)
    {
        var handledEvent = false;

        if (!initialized)
        {
            return false;
        }

        switch (eventType) {
            case PlayStationVR.deviceEventType.deviceStarted:
                Debug.LogFormat("NVRPSVRIntegration.OnDeviceEvent: " + 
                                "deviceStarted: {0}", value);
                break;
            case PlayStationVR.deviceEventType.deviceStopped:
                DeinitHMD();
                handledEvent = true;
                break;
            case PlayStationVR.deviceEventType.StatusChanged: // e.g. HMD unplugged
                VRDeviceStatus devStatus = (VRDeviceStatus) value;
                Debug.LogFormat("NVRPSVRIntegration.OnDeviceEvent: " + 
                                "VRDeviceStatus: {0}", devStatus);
                if (devStatus != VRDeviceStatus.Ready)
                {
                    // TRC R4026 suggests showing the HMD Setup Dialog if the 
                    // device status becomes non-ready
                    if (XRSettings.loadedDeviceName == kNoneDeviceName) {
                        ShowHMDSetupDialogue();
                    } else {
                        DeinitHMD();
                    }
                }
                handledEvent = true;
                break;
            case PlayStationVR.deviceEventType.MountChanged:
                VRHmdMountStatus status = (VRHmdMountStatus) value;
                Debug.LogFormat("NVRPSVRIntegration.OnDeviceEvent: " + 
                                "VRHmdMountStatus: {0}", status);
                handledEvent = true;
                break;
            case PlayStationVR.deviceEventType.CameraChanged:
                // If the event is for the camera and the value is 0, the 
                // camera has been disconnected
                Debug.LogFormat("NVRPSVRIntegration.OnDeviceEvent: " + 
                                "CameraChanged: {0}", value);
                if (value == 0) {
                    ShowHMDSetupDialogue();
                }
                handledEvent = true;
                break;
            case PlayStationVR.deviceEventType.HmdHandleInvalid:
                // Unity will handle this automatically, please see API documentation
                Debug.LogFormat("### OnDeviceEvent: HmdHandleInvalid: {0}", value);
                break;
            case PlayStationVR.deviceEventType.DeviceRestarted:
                // Unity will handle this automatically, please see API documentation
                Debug.LogFormat("### OnDeviceEvent: DeviceRestarted: {0}", value);
                break;
            case PlayStationVR.deviceEventType.DeviceStartedError:
                Debug.LogFormat("### OnDeviceEvent: DeviceStartedError: {0}", value);
                break;
            default:
                throw new ArgumentOutOfRangeException("eventType", eventType, null);
        }

        return handledEvent;
    }
#endif

    //--------------------------------------------------------------------------

#if UNITY_PS4
    // Detect completion of the HMD dialog and either proceed to setup VR, or 
    // throw a warning
    private void OnHmdSetupDialogCompleted(DialogStatus status, DialogResult result)
    {
        Debug.LogFormat("NVRPSVRIntegration.OnHmdSetupDialogCompleted: {0}, {1}", 
                        status, result);

        switch (result) {
            case DialogResult.OK:
                InitHMD();
                break;
            case DialogResult.UserCanceled:
                Debug.LogWarning("User Cancelled HMD Setup!");
                DeinitHMD();
                break;
        }
    }
#endif

    //--------------------------------------------------------------------------

    private void UpdateTracking()
    {
#if UNITY_PS4
        if (trackingInitialized) {
            return;
        }

        // Get the tracking status 
        PlayStationVRTrackingStatus trackingStatus = 
                                        PlayStationVRTrackingStatus.NotStarted;
        PlayStationVRResult result = 
            Tracker.GetTrackedDeviceStatus(hmdHandle, out trackingStatus);

        if (result != PlayStationVRResult.Ok) {
            Debug.LogError("Error getting PSVR tracking status: " + 
                            result.ToString());
            return;
        }

        PlayStationVRTrackingQuality trackingQuality = 
                                            PlayStationVRTrackingQuality.None;

        result = Tracker.GetTrackedDevicePositionQuality(hmdHandle, 
                                                         out trackingQuality);

        if (result != PlayStationVRResult.Ok) {
            Debug.LogError("Error getting PSVR tracking quality: " + 
                                result.ToString());
            return;
        }

        // Catch the first time tracking is established
        if (trackingStatus == PlayStationVRTrackingStatus.Tracking && 
                trackingQuality == PlayStationVRTrackingQuality.Full) {

            // Once tracking is established, the HMD gets moved around based on
            // inaccurate camera data. Call Recenter to reset it to the rig origin 
            Recenter();

            trackingInitialized = true;

            Debug.Log("PSVR Tracking established");
        }

#endif // UNITY_PS4
    }

    //--------------------------------------------------------------------------

    private void UpdatePoseCallbacks()
    {
        // Since we don't have a pose update callback in PSVR we fake it with an
        // update loop callback
        foreach (UnityAction action in NewPosesCallbacks) {
            action();
        }
    }

    //--------------------------------------------------------------------------
}

}
