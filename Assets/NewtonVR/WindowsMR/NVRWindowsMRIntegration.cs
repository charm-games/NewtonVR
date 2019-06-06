﻿/**
 * Copyright 2016, Charm Games Inc, All rights reserved.
 */

//------------------------------------------------------------------------------
// Using directives 
//------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NewtonVR {

//------------------------------------------------------------------------------
// Class definition 
//------------------------------------------------------------------------------

public class NVRWindowsMRIntegration : NVRIntegration 
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

    /**
     * The world space y that the rig starts the scene at. Used to reanchor the
     * rig everytime a new floor is set by the tracking.
     */
    private float initialRigHeight = 0f;

    private float floorToRigVerticalOffset = 0f;

    //--------------------------------------------------------------------------
    // Public member variables
    //--------------------------------------------------------------------------

    //--------------------------------------------------------------------------
    // Public methods 
    //--------------------------------------------------------------------------

    public override void Initialize(NVRPlayer player)
    {
#if UNITY_WSA
        rigObj = player.gameObject;

        InitPoseCallback();
        initialized = true;
        InvokeOnInitializedEvent();
#endif // UNITY_WSA
    }

    //--------------------------------------------------------------------------

    public override void DeInitialize()
    {
        // no-op
    }

    //--------------------------------------------------------------------------

    public override bool IsInit()
    {
        return initialized;
    }

    //--------------------------------------------------------------------------

    public override Vector3 GetPlayspaceBounds()
    {
        return new Vector3(5f, 5f, 5f);
    }

    //--------------------------------------------------------------------------

    public override bool IsHmdPresent()
    {
#if UNITY_WSA
        return UnityEngine.XR.WSA.HolographicSettings.IsDisplayOpaque;
#else 
        return false;
#endif // UNITY_WSA
    }

    //--------------------------------------------------------------------------

    public override void RegisterNewPoseCallback(UnityAction callback)
    {
        // Store the external callback to be called from the internal
        // callback. This is because we want the external callback to always
        // be of type UnityAction.
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
    // Private methods 
    //--------------------------------------------------------------------------

#if UNITY_WSA
    private void NewPoseCallbackInternal(UnityEngine.XR.WSA.Input.InteractionSourceUpdatedEventArgs eventArgs)
    {
        foreach (UnityAction callback in NewPosesCallbacks) {
            callback();
        }
    }

    //--------------------------------------------------------------------------

    private void InitPoseCallback()
    {
        // Setup our internal callback to listen to the WindowsMR pose udpated
        // callback
        UnityEngine.XR.WSA.Input.InteractionManager.InteractionSourceUpdated += NewPoseCallbackInternal;
    }
#endif // UNITY_WSA

    //--------------------------------------------------------------------------
}

}
