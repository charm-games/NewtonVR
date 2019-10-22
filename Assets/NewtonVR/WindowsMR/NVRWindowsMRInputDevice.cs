/**
 * Copyright 2016, Charm Games Inc, All rights reserved.
 */

//------------------------------------------------------------------------------
// Using directives 
//------------------------------------------------------------------------------

using NewtonVR;
using System;
using System.Collections;
using UnityEngine;
#if UNITY_WSA
using UnityEngine.XR.WSA.Input;
#endif // UNITY_WSA

namespace NewtonVR {

//------------------------------------------------------------------------------
// Class definition 
//------------------------------------------------------------------------------

public class NVRWindowsMRInputDevice : NVRInputDevice 
{
    //--------------------------------------------------------------------------
    // Type definitions 
    //--------------------------------------------------------------------------

    private struct ControllerState
    {
        // Button presses
        public bool triggerPressed;   
        public bool gripPressed;
        public bool menuPressed;
        public bool touchpadPressed;
        public bool thumbstickPressed;

        // Touches
        public bool touchpadTouched;

        // Axis values
        public double triggerValue;
        public Vector2 touchpadValue;
        public Vector2 thumbstickValue;
    }

    //--------------------------------------------------------------------------
    // Private member variables
    //--------------------------------------------------------------------------

    private float kHapticIntensity = 0.1f;

#if UNITY_WSA
    private InteractionSourceHandedness handedness = 
                                        InteractionSourceHandedness.Unknown;
#endif // UNITY_WSA

    /**
     * Cached states for capturing the controller input events so they can be
     * polled by the application
     */
    private ControllerState controllerState;
    private ControllerState prevControllerState;

    private bool isTracking = false;
    private bool isInitialized = false;

    private GameObject renderModel = null;

    //--------------------------------------------------------------------------
    // Public member variables
    //--------------------------------------------------------------------------

    //--------------------------------------------------------------------------
    // Public methods 
    //--------------------------------------------------------------------------

    public override void Initialize(NVRHand hand)
    {
#if UNITY_WSA
        base.Initialize(hand);

        if (hand == Hand.Player.LeftHand) {
            handedness = InteractionSourceHandedness.Left;
        } else {
            handedness = InteractionSourceHandedness.Right;
        }

        controllerState.triggerPressed    = false;
        controllerState.gripPressed       = false;
        controllerState.menuPressed       = false;
        controllerState.touchpadPressed   = false;
        controllerState.thumbstickPressed = false;
        controllerState.touchpadTouched   = false;
        controllerState.triggerValue      = 0.0;
        controllerState.touchpadValue     = Vector2.zero;
        controllerState.thumbstickValue   = Vector2.zero;

        prevControllerState = controllerState;

        SetupInputCallbacks();

        isInitialized = true;
#endif // UNITY_WSA
    }

    //--------------------------------------------------------------------------

    public override bool IsCurrentlyTracked 
    { 
        get
        {
            return isTracking;
        }
    }

    //--------------------------------------------------------------------------

    public override bool IsInitialized
    {
        get
        {
            return isInitialized;
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
        //source.StartHaptics(kHapticIntensity, durationSeconds);
    }

    //--------------------------------------------------------------------------

    public override float GetAxis1D(NVRButtons button)
    {
        switch (button) {
            case NVRButtons.Trigger:
                return (float) controllerState.triggerValue;
            default:
                throw new ArgumentException("GetAxis1D for unsupported button");
        }
    }

    //--------------------------------------------------------------------------

    public override Vector2 GetAxis2D(NVRButtons button)
    {
        switch (button) {
            case NVRButtons.Touchpad:
                return controllerState.touchpadValue;
            case NVRButtons.Stick:
                return controllerState.thumbstickValue;
            default:
                throw new ArgumentException("GetAxis2D for unsupported button");
        }
    }

    //--------------------------------------------------------------------------

    public override bool GetPressDown(NVRButtons button)
    {
        switch (button) {
            case NVRButtons.Trigger:
                return !prevControllerState.triggerPressed && 
                                            controllerState.triggerPressed;
            case NVRButtons.Grip:
                return !prevControllerState.gripPressed && 
                                            controllerState.gripPressed;
            case NVRButtons.ApplicationMenu:
                return !prevControllerState.menuPressed &&
                                            controllerState.menuPressed;
            case NVRButtons.Touchpad:
                return !prevControllerState.touchpadPressed && 
                                            controllerState.touchpadPressed;
            case NVRButtons.Stick:
                return !prevControllerState.thumbstickPressed && 
                                            controllerState.thumbstickPressed;
            default:
                throw new ArgumentException("GetPressDown for unsupported button");
        }
    }

    //--------------------------------------------------------------------------

    public override bool GetPressUp(NVRButtons button)
    {
        switch (button) {
            case NVRButtons.Trigger:
                return prevControllerState.triggerPressed && 
                                            !controllerState.triggerPressed;
            case NVRButtons.Grip:
                return prevControllerState.gripPressed && 
                                            !controllerState.gripPressed;
            case NVRButtons.ApplicationMenu:
                return prevControllerState.menuPressed &&
                                            !controllerState.menuPressed;
            case NVRButtons.Touchpad:
                return prevControllerState.touchpadPressed && 
                                            !controllerState.touchpadPressed;
            case NVRButtons.Stick:
                return prevControllerState.thumbstickPressed && 
                                            !controllerState.thumbstickPressed;
            default:
                throw new ArgumentException("GetPressDown for unsupported button");
        }
    }

    //--------------------------------------------------------------------------

    public override bool GetPress(NVRButtons button)
    {
        switch (button) {
            case NVRButtons.Trigger:
                return controllerState.triggerPressed;
            case NVRButtons.Grip:
                return controllerState.gripPressed;
            case NVRButtons.ApplicationMenu:
                return controllerState.menuPressed;
            case NVRButtons.Touchpad:
                return controllerState.touchpadPressed;
            case NVRButtons.Stick:
                return controllerState.thumbstickPressed;
            default:
                throw new ArgumentException("Get press for unsupported button");
        }
    }

    //--------------------------------------------------------------------------

    public override bool GetTouchDown(NVRButtons button)
    {
        switch (button) {
            case NVRButtons.Touchpad:
                return !prevControllerState.touchpadTouched && 
                                                controllerState.touchpadTouched;
            default:
                throw new ArgumentException("GetTouchDown for unsupported button");
        }
    }

    //--------------------------------------------------------------------------

    public override bool GetTouchUp(NVRButtons button)
    {
        switch (button) {
            case NVRButtons.Touchpad:
                return prevControllerState.touchpadTouched && 
                                                !controllerState.touchpadTouched;
            default:
                throw new ArgumentException("GetTouchUp for unsupported button");
        }
    }

    //--------------------------------------------------------------------------

    public override bool GetTouch(NVRButtons button)
    {
        switch (button) {
            case NVRButtons.Touchpad:
                return controllerState.touchpadTouched;
            default:
                throw new ArgumentException("GetTouch for unsupported button");
        }
    }

    //--------------------------------------------------------------------------

    public override bool GetNearTouchDown(NVRButtons button)
    {
        // Near touch is unsupported
        return false;
    }

    //--------------------------------------------------------------------------

    public override bool GetNearTouchUp(NVRButtons button)
    {
        // Near touch is unsupported
        return false;
    }

    //--------------------------------------------------------------------------

    public override bool GetNearTouch(NVRButtons button)
    {
        // Near touch is unsupported
        return false;
    }

    //--------------------------------------------------------------------------
    // Private methods 
    //--------------------------------------------------------------------------

#if UNITY_WSA
    /**
     * Setup callbacks to update the internal input state
     */
    private void SetupInputCallbacks()
    {
        UnityEngine.XR.WSA.Input.InteractionManager.InteractionSourcePressed  += UpdateButtonPressed; 
        UnityEngine.XR.WSA.Input.InteractionManager.InteractionSourceReleased += UpdateButtonReleased;
        UnityEngine.XR.WSA.Input.InteractionManager.InteractionSourceUpdated  += UpdateControllerState;
        UnityEngine.XR.WSA.Input.InteractionManager.InteractionSourceDetected += ControllerDetected;
        UnityEngine.XR.WSA.Input.InteractionManager.InteractionSourceLost     += ControllerLost;
    }

    //--------------------------------------------------------------------------

    private void TeardownInputCallbacks()
    {
        UnityEngine.XR.WSA.Input.InteractionManager.InteractionSourcePressed  -= UpdateButtonPressed; 
        UnityEngine.XR.WSA.Input.InteractionManager.InteractionSourceReleased -= UpdateButtonReleased;
        UnityEngine.XR.WSA.Input.InteractionManager.InteractionSourceUpdated  -= UpdateControllerState;
        UnityEngine.XR.WSA.Input.InteractionManager.InteractionSourceDetected -= ControllerDetected;
        UnityEngine.XR.WSA.Input.InteractionManager.InteractionSourceLost     -= ControllerLost;
    }

    //--------------------------------------------------------------------------

    /**
     * Handles press events on buttons
     */
    private void UpdateButtonPressed(UnityEngine.XR.WSA.Input.InteractionSourcePressedEventArgs eventArgs)
    {
        HandleButtonPressAndRelease(eventArgs.state.source.handedness, 
                                    eventArgs.pressType,
                                    true);
    }

    //--------------------------------------------------------------------------

    /**
     * Handles release events on buttons
     */
    private void UpdateButtonReleased(UnityEngine.XR.WSA.Input.InteractionSourceReleasedEventArgs eventArgs)
    {
        HandleButtonPressAndRelease(eventArgs.state.source.handedness, 
                                    eventArgs.pressType,
                                    false);
    }

    //--------------------------------------------------------------------------

    private void HandleButtonPressAndRelease(InteractionSourceHandedness eventSourceHand, 
                                             InteractionSourcePressType  pressType, 
                                             bool                        isPressed)
    {
        // Check that this is the matching hand
        if (eventSourceHand != handedness) {
            // Don't care about other hands
            return;
        }

        // Update the internal state we are tracking
        switch (pressType) {
            case InteractionSourcePressType.Select:
                controllerState.triggerPressed = isPressed;
                break;
            case InteractionSourcePressType.Menu:
                controllerState.menuPressed = isPressed;
                break;
            case InteractionSourcePressType.Grasp:
                controllerState.gripPressed = isPressed;
                break;
            case InteractionSourcePressType.Touchpad:
                controllerState.touchpadPressed = isPressed;
                break;
            case InteractionSourcePressType.Thumbstick:
                controllerState.thumbstickPressed = isPressed;
                break;
            case InteractionSourcePressType.None:
                break;
        }
    }

    //--------------------------------------------------------------------------

    /**
     * Handles movement, touch and axis changes on controller inputs
     */
    private void UpdateControllerState(UnityEngine.XR.WSA.Input.InteractionSourceUpdatedEventArgs eventArgs)
    {
        // Check that this is the matching hand
        if (eventArgs.state.source.handedness != handedness) {
            // Don't care about other hands
            return;
        }

        // Update the internal touch and axis state we are tracking
        
        // Trigger axis
        controllerState.triggerValue = eventArgs.state.selectPressedAmount;

        // Touchpad 2d axis
        controllerState.touchpadValue = eventArgs.state.touchpadPosition;

        // Touchpad touched value
        controllerState.touchpadTouched = eventArgs.state.touchpadTouched;

        // Thumbstrick move value
        controllerState.thumbstickValue = eventArgs.state.thumbstickPosition;

        // Update position and rotation
        InteractionSourcePose sourcePose = eventArgs.state.sourcePose;

        Vector3    position;
        Quaternion rotation;
        if (sourcePose.TryGetPosition(out position, InteractionSourceNode.Grip) && 
            sourcePose.TryGetRotation(out rotation, InteractionSourceNode.Grip)) {
            transform.localPosition = position;
            transform.localRotation = rotation;
        }
    }

    //--------------------------------------------------------------------------

    private void ControllerDetected(UnityEngine.XR.WSA.Input.InteractionSourceDetectedEventArgs eventArgs)
    {
        if (eventArgs.state.source.handedness != handedness) {
            // Was for a different hand
            return;
        }

        isTracking = true;
    }

    //--------------------------------------------------------------------------

    private void ControllerLost(UnityEngine.XR.WSA.Input.InteractionSourceLostEventArgs eventArgs)
    {
        if (eventArgs.state.source.handedness != handedness) {
            // Was for a different hand
            return;
        }

        isTracking = false;
    }

#endif // UNITY_WSA

    //--------------------------------------------------------------------------
    // Messages
    //--------------------------------------------------------------------------

    /**
     * Use this for initializing internal members of this component the way you
     * would use a constructor. Also hook up components here where
     * initialization order is not important (i.e. no init dependencies)
     */
    void Awake()
    {
    }

    //--------------------------------------------------------------------------

    /** 
     * Use this to initialize order dependant external components. For instance
     * if you need to ensure component Foo's Awake call is fired before this.
     */
    void Start() 
    {

    }

    //--------------------------------------------------------------------------
    
    /** 
     * Update is called once per frame
     */
    void Update() 
    {

    }

    //--------------------------------------------------------------------------

    void LateUpdate()
    {
        // Turn over the frame once 
        prevControllerState = controllerState;
    }

    //--------------------------------------------------------------------------

    void OnDestroy()
    {
#if UNITY_WSA
        TeardownInputCallbacks();
#endif // UNITY_WSA
    }

    //--------------------------------------------------------------------------
}

}
