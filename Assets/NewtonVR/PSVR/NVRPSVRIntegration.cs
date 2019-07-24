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

    /**
     * Set this to false to use the monitor/display as the Social Screen
     */
    private bool showHmdViewOnMonitor = true; 

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

        InitHMD();
    }

    //--------------------------------------------------------------------------

    public override void DeInitialize()
    {
        DeinitHMD();
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
        // TODO: Not sure what event to hook this to
    }

    //--------------------------------------------------------------------------

    public override void DeregisterNewPoseCallback(UnityAction callback)
    {
        // TODO: Not sure what event to hook this to
    }

    //--------------------------------------------------------------------------

    public override void MoveRig(Transform transform)
    {
        Player.transform.position   = transform.position;
        Player.transform.rotation   = transform.rotation;
        Player.transform.localScale = transform.localScale;
    }

    //--------------------------------------------------------------------------

    public override void MoveRig(Vector3 position, Quaternion orientation)
    {
        Player.transform.position = position;
        Player.transform.rotation = orientation;
    }

    //--------------------------------------------------------------------------

    public override Transform GetOrigin()
    {
        return Player.transform;
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
    // Private methods 
    //--------------------------------------------------------------------------

    private void InitHMD()
    {
#if UNITY_PS4
        // Register the callbacks needed to detect resetting the HMD
        Utility.onSystemServiceEvent += OnSystemServiceEvent;
        PlayStationVR.onDeviceEvent += OnDeviceEvent;
#endif // UNITY_PS4
        
        Debug.Log("Loading HMD device " + kPSVRDeviceName);
        XRSettings.LoadDeviceByName(kPSVRDeviceName);

        // TODO: If we move to Unity 2018.1 or greater then use the
        // XRDevice.deviceLoaded event instead of this
        CharmGames.Form.DelayedCallback.FrameDelayedCallback(OnHMDLoaded, 1);
    }

    //--------------------------------------------------------------------------

    private void OnHMDLoaded()
    {
        Debug.Log("HMD loaded. Setting XRSettings.");

        // Settings must be changed only after the HMD device is loaded
        XRSettings.enabled = true;
        XRSettings.eyeTextureResolutionScale = renderScale;
        XRSettings.showDeviceView = showHmdViewOnMonitor;

        initialized = true;
        InvokeOnInitializedEvent();
    }

    //--------------------------------------------------------------------------

    private void DeinitHMD()
    {
        Debug.Log("Unloading HMD device by loading device " + kNoneDeviceName);

        // Unload the PSVR by loading the None device
        XRSettings.LoadDeviceByName(kNoneDeviceName);

        // WORKAROUND: At the moment the device is created at the end of the frame so
        // we need to wait a frame until the VR device is changed back to 'None', and
        // then reset the Main Camera's FOV and Aspect
        // TODO: If we move to Unity 2018.1 or greater then use the
        // XRDevice.deviceLoaded event instead of this
        CharmGames.Form.DelayedCallback.FrameDelayedCallback(OnHMDUnloaded, 1);
    }

    //--------------------------------------------------------------------------

    private void OnHMDUnloaded()
    {
        Debug.Log("HMD device unloaded");

        XRSettings.enabled = false;
        XRSettings.showDeviceView = false;

#if UNITY_PS4
        // Unregister the callbacks needed to detect resetting the HMD
        Utility.onSystemServiceEvent -= OnSystemServiceEvent;
        PlayStationVR.onDeviceEvent -= OnDeviceEvent;
        PlayStationVR.SetOutputModeHMD(false, 120);
#endif

        Camera.main.fieldOfView = 60f;
        Camera.main.ResetAspect();

        initialized = false;
    }

    //--------------------------------------------------------------------------

    private void SetupHmdDevice()
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

        if (eventType == Utility.sceSystemServiceEventType.RESET_VR_POSITION) {
            InputTracking.Recenter();
        }
    }
#endif

    //--------------------------------------------------------------------------

#if UNITY_PS4
    // This handles disabling VR in the event that the HMD has been disconnected
    private bool OnDeviceEvent(PlayStationVR.deviceEventType eventType, int value)
    {
        var handledEvent = false;

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
                        SetupHmdDevice();
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
                    SetupHmdDevice();
                }
                handledEvent = true;
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
}

}
