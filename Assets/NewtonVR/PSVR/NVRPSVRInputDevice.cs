/**
 * Copyright 2018, Charm Games Inc, All rights reserved.
 */

//------------------------------------------------------------------------------
// Using directives 
//------------------------------------------------------------------------------

using System;
using System.Collections;
using UnityEngine;
#if UNITY_PS4
using UnityEngine.PS4;
using UnityEngine.PS4.VR;
#endif // UNITY_PS4

namespace NewtonVR {

//------------------------------------------------------------------------------
// Class definition 
//------------------------------------------------------------------------------

#if UNITY_PS4
public class NVRPSVRInputDevice : NVRInputDevice 
{
    //--------------------------------------------------------------------------
    // Type definitions 
    //--------------------------------------------------------------------------

    private enum MoveControllerButtonMap
    {
        ButtonSelect        = 1 << 0,
        ButtonT             = 1 << 1,
        ButtonMove          = 1 << 2,
        ButtonStart         = 1 << 3,
        ButtonTriangle      = 1 << 4,
        ButtonCircle        = 1 << 5,
        ButtonCross         = 1 << 6,
        ButtonSquare        = 1 << 7,
        ButtonIntercepted   = 1 << 15 
    }

    //--------------------------------------------------------------------------
    // Private member variables
    //--------------------------------------------------------------------------

    private int deviceHandle = -1;
    private int deviceIndex = -1;
    private int deviceSlot = 0;
    
    private GameObject renderModel = null;

    private bool isInitialized = false;

    private int buttonState = 0;
    private int prevButtonState = 0;

#if UNITY_PS4
        public PlayStationVRTrackingType trackingType = 
                                            PlayStationVRTrackingType.Absolute;
        public PlayStationVRTrackerUsage trackerUsageType = 
                                 PlayStationVRTrackerUsage.OptimizedForHmdUser;
#endif

    //--------------------------------------------------------------------------
    // Public member variables
    //--------------------------------------------------------------------------

    public override bool IsInitialized
    {
        get
        {
            return isInitialized;
        }
    }

    //--------------------------------------------------------------------------
    // Public methods 
    //--------------------------------------------------------------------------

    public override void Initialize(NVRHand hand)
    {
        Debug.Log("NVRPSVRInputDevice.Initialize for hand " + 
                  (hand.IsLeft ? "Left" : "Right"));

        base.Initialize(hand);
            
        StartCoroutine(InitializeCoroutine());
    }

    //--------------------------------------------------------------------------

    public override bool IsCurrentlyTracked 
    { 
        get
        {
            PlayStationVRTrackingStatus status;
            Tracker.GetTrackedDeviceStatus(deviceHandle, out status);

            return status == PlayStationVRTrackingStatus.Tracking;
        }
    }

    //--------------------------------------------------------------------------

    public override Collider[] SetupDefaultPhysicalColliders(Transform ModelParent)
    {
        // Unsupported currently
        return null;
    }

    //--------------------------------------------------------------------------

    public override GameObject SetupDefaultRenderModel()
    {
        // Unsupported currently
        renderModel = new GameObject("DefaultRenderModel");

        renderModel.name = "Render Model for " + Hand.gameObject.name;
        renderModel.transform.parent = Hand.transform;
        renderModel.transform.localPosition = Vector3.zero;
        renderModel.transform.localRotation = Quaternion.identity;
        renderModel.transform.localScale = Vector3.one;

        return renderModel;
    }

    //--------------------------------------------------------------------------

    public override bool ReadyToInitialize()
    {
        return true;
    }

    //--------------------------------------------------------------------------

    public override Collider[] SetupDefaultColliders()
    {
        Collider[] Colliders = null;
        
        SphereCollider handCollider = renderModel.AddComponent<SphereCollider>();
        handCollider.isTrigger = true;
        handCollider.radius = 0.15f;

        Colliders = new Collider[] { handCollider };

        return Colliders;
    }

    //--------------------------------------------------------------------------

    public override string GetDeviceName()
    {
        if (Hand.HasCustomModel == true) {
            return "Custom";
        } else {
            // No name method in the API at the moment so use the game object
            return gameObject.ToString();
        }
    }

    //--------------------------------------------------------------------------

    public override void TriggerHapticPulse(ushort durationMicroSec = 500, 
                                            NVRButtons button = NVRButtons.Touchpad)
    {
        float durationSeconds = ((float)durationMicroSec) / 10e6f;

        // TODO: Implement haptics
    }

    //--------------------------------------------------------------------------

    public override float GetAxis1D(NVRButtons button)
    {
        const float kMaxButtonPress = 255;
        float buttonVal = PS4Input.MoveGetAnalogButton(deviceSlot, deviceIndex);
        float normalizedVal = (buttonVal / kMaxButtonPress);

        // This API is supposed to return button values from 0.5 to 1 so convert
        // our normalized value to this
        float convertedVal = (normalizedVal / 2f) + 0.5f;
        return convertedVal;
    }

    //--------------------------------------------------------------------------

    public override Vector2 GetAxis2D(NVRButtons button)
    {
        return Vector2.zero;
    }

    //--------------------------------------------------------------------------

    public override bool GetPressDown(NVRButtons button)
    {
        buttonState = PS4Input.MoveGetButtons(deviceSlot, deviceIndex);

        switch (button) {
            case NVRButtons.Trigger:
                return (!WasPressed(MoveControllerButtonMap.ButtonT) &&  
                            IsPressed(MoveControllerButtonMap.ButtonT));
            case NVRButtons.Grip:
                return (!WasPressed(MoveControllerButtonMap.ButtonT) &&  
                            IsPressed(MoveControllerButtonMap.ButtonT));
            case NVRButtons.Touchpad:
                return (!WasPressed(MoveControllerButtonMap.ButtonTriangle) &&  
                            IsPressed(MoveControllerButtonMap.ButtonTriangle));
            case NVRButtons.ApplicationMenu:
                return (!WasPressed(MoveControllerButtonMap.ButtonMove) &&  
                            IsPressed(MoveControllerButtonMap.ButtonMove));
            default:
                throw new ArgumentException("Get press down for unsupported button");
        }
    }

    //--------------------------------------------------------------------------
    
    public override bool GetPressUp(NVRButtons button)
    {
        buttonState = PS4Input.MoveGetButtons(deviceSlot, deviceIndex);

        switch (button) {
            case NVRButtons.Trigger:
                return (WasPressed(MoveControllerButtonMap.ButtonT) &&  
                            !IsPressed(MoveControllerButtonMap.ButtonT));
            case NVRButtons.Grip:
                return (WasPressed(MoveControllerButtonMap.ButtonT) &&  
                            !IsPressed(MoveControllerButtonMap.ButtonT));
            case NVRButtons.Touchpad:
                return (WasPressed(MoveControllerButtonMap.ButtonTriangle) &&  
                            !IsPressed(MoveControllerButtonMap.ButtonTriangle));
            case NVRButtons.ApplicationMenu:
                return (WasPressed(MoveControllerButtonMap.ButtonMove) &&  
                            !IsPressed(MoveControllerButtonMap.ButtonMove));
            default:
                throw new ArgumentException("Get press up for unsupported button");
        }
    }

    //--------------------------------------------------------------------------

    public override bool GetPress(NVRButtons button)
    {
        buttonState = PS4Input.MoveGetButtons(deviceSlot, deviceIndex);

        switch (button) {
            case NVRButtons.Trigger:
                return IsPressed(MoveControllerButtonMap.ButtonT);
            case NVRButtons.Grip:
                return IsPressed(MoveControllerButtonMap.ButtonT);
            case NVRButtons.Touchpad:
                return IsPressed(MoveControllerButtonMap.ButtonTriangle);
            case NVRButtons.ApplicationMenu:
                return IsPressed(MoveControllerButtonMap.ButtonMove);
            default:
                throw new ArgumentException("Get press for unsupported button");
        }
    }

    //--------------------------------------------------------------------------

    public override bool GetTouchDown(NVRButtons button)
    {
        return false;
    }

    //--------------------------------------------------------------------------

    public override bool GetTouchUp(NVRButtons button)
    {
        return false;
    }

    //--------------------------------------------------------------------------

    public override bool GetTouch(NVRButtons button)
    {
        return false;
    }

    //--------------------------------------------------------------------------

    public override bool GetNearTouchDown(NVRButtons button)
    {
        return false;
    }

    //--------------------------------------------------------------------------

    public override bool GetNearTouchUp(NVRButtons button)
    {
        return false;
    }

    //--------------------------------------------------------------------------

    public override bool GetNearTouch(NVRButtons button)
    {
        return false;
    }

    //--------------------------------------------------------------------------
    // Private methods 
    //--------------------------------------------------------------------------

    private IEnumerator InitializeCoroutine()
    {
        // Keep waiting until we have a VR Device available
        while (!UnityEngine.XR.XRDevice.isPresent) {
            yield return new WaitForSeconds(1.0f);
        }

        if (PS4Input.MoveIsConnected(0, 0) == false) {
            Debug.LogError("Trying to register the primary Move device, but it is not connected!");
            yield break;
        }

        if (PS4Input.MoveIsConnected(0, 1) == false) {
            Debug.LogError("Trying to register the secondary Move device, but it is not connected!");
            yield break;
        }

        // Get the handle for this move controller
        int[] primaryHandles = new int[1];
        int[] secondaryHandles = new int[1];
        PS4Input.MoveGetUsersMoveHandles(1, primaryHandles, secondaryHandles);

        int numInitializedInputDevices = NVRPlayer.Instance.NumInitializedInputDevices;

        if (numInitializedInputDevices == 0) {
            // This is the first move controller so check the primary handles
            deviceHandle = primaryHandles[0];
            deviceIndex = 0;
        } else {
            // This is the second move controller so check the secondary handles
            deviceHandle = secondaryHandles[0];
            deviceIndex = 1;
        }

        // Get the tracking for the primary Move device, and wait for it to start
        PlayStationVRResult result = 
            Tracker.RegisterTrackedDevice(PlayStationVRTrackedDevice.DeviceMove, 
                                          deviceHandle, 
                                          trackingType, 
                                          trackerUsageType);

        if (result == PlayStationVRResult.Ok)
        {
            isInitialized = true;

            PlayStationVRTrackingStatus trackingStatusPrimary = 
                                        new PlayStationVRTrackingStatus();

            while (trackingStatusPrimary == PlayStationVRTrackingStatus.NotStarted)
            {
                Debug.Log("NVRPSVRInputDevice - Waiting for device for hand " + 
                          (Hand.IsLeft ? "Left" : "Right") + " to start tracking");
                Tracker.GetTrackedDeviceStatus(deviceHandle, 
                                               out trackingStatusPrimary);
                yield return null;
            }

            Debug.Log("NVRPSVRInputDevice - Device for hand " + 
                      (Hand.IsLeft ? "Left" : "Right") + " is tracking");
        } else {
            throw new Exception("Could not register tracked device for hand " + 
                                (Hand.IsLeft ? "Left" : "Right"));
        }
    }

    //--------------------------------------------------------------------------

    private void UpdateTransforms()
    {
        Vector3 position = Vector3.zero;
        Quaternion orientation = Quaternion.identity;

        PlayStationVRResult result = 
            Tracker.GetTrackedDevicePosition(deviceHandle, out position);
        if (result == PlayStationVRResult.Ok) {
            transform.localPosition = position;
        }

        result = 
            Tracker.GetTrackedDeviceOrientation(deviceHandle, out orientation);
        if (result == PlayStationVRResult.Ok) {
            transform.localRotation = orientation;
        }
    }

    //--------------------------------------------------------------------------

    private bool IsPressed(MoveControllerButtonMap button)
    {
        return (buttonState & ((int) button)) != 0;
    }

    //--------------------------------------------------------------------------

    private bool WasPressed(MoveControllerButtonMap button)
    {
        return (prevButtonState & ((int) button)) != 0;
    }

    //--------------------------------------------------------------------------

    private void UnregisterDevice()
    {
        Debug.Log("NVRPSVRInputDevice.UnregisterDevice - Unregistering tracked device for hand " + 
                  (Hand.IsLeft ? "Left" : "Right"));

        Tracker.UnregisterTrackedDevice(deviceHandle);
    }

    //--------------------------------------------------------------------------
    // Messages
    //--------------------------------------------------------------------------

    /**
     * Use this for initializing internal members of this component the way you
     * would use a constructor. Also hook up components here where
     * initialization order is not important (i.e. no init dependencies)
     */
    private void Awake()
    {
    }

    //--------------------------------------------------------------------------

    /** 
     * Use this to initialize order dependant external components. For instance
     * if you need to ensure component Foo's Awake call is fired before this.
     */
    private void Start() 
    {

    }

    //--------------------------------------------------------------------------
    
    /** 
     * Update is called once per frame
     */
    private void Update() 
    {
        // TODO: This needs to move to a non time-dependent update method
        UpdateTransforms(); 
    }

    //--------------------------------------------------------------------------

    void LateUpdate()
    {
        // Turn over the frame once 
        prevButtonState = buttonState;
    }

    //--------------------------------------------------------------------------

    private void OnDisable()
    {
        UnregisterDevice();
    }
    
    //--------------------------------------------------------------------------

    private void OnDestroy()
    {
        UnregisterDevice();
    }

    //--------------------------------------------------------------------------
}
#else

public class NVRPSVRInputDevice : NVRInputDevice 
{
    public override bool IsCurrentlyTracked
    {
        get
        {
            throw new NotImplementedException();
        }
    }
    
    public override bool IsInitialized
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public override float GetAxis1D(NVRButtons button)
    {
        throw new NotImplementedException();
    }

    public override Vector2 GetAxis2D(NVRButtons button)
    {
        throw new NotImplementedException();
    }

    public override string GetDeviceName()
    {
        throw new NotImplementedException();
    }

    public override bool GetNearTouch(NVRButtons button)
    {
        throw new NotImplementedException();
    }

    public override bool GetNearTouchDown(NVRButtons button)
    {
        throw new NotImplementedException();
    }

    public override bool GetNearTouchUp(NVRButtons button)
    {
        throw new NotImplementedException();
    }

    public override bool GetPress(NVRButtons button)
    {
        throw new NotImplementedException();
    }

    public override bool GetPressDown(NVRButtons button)
    {
        throw new NotImplementedException();
    }

    public override bool GetPressUp(NVRButtons button)
    {
        throw new NotImplementedException();
    }

    public override bool GetTouch(NVRButtons button)
    {
        throw new NotImplementedException();
    }

    public override bool GetTouchDown(NVRButtons button)
    {
        throw new NotImplementedException();
    }

    public override bool GetTouchUp(NVRButtons button)
    {
        throw new NotImplementedException();
    }

    public override bool ReadyToInitialize()
    {
        throw new NotImplementedException();
    }

    public override Collider[] SetupDefaultColliders()
    {
        throw new NotImplementedException();
    }

    public override Collider[] SetupDefaultPhysicalColliders(Transform ModelParent)
    {
        throw new NotImplementedException();
    }

    public override GameObject SetupDefaultRenderModel()
    {
        throw new NotImplementedException();
    }

    public override void TriggerHapticPulse(ushort durationMicroSec = 500, 
                                            NVRButtons button = NVRButtons.Touchpad)
    {
        throw new NotImplementedException();
    }
}
#endif // UNITY_PS4

}
