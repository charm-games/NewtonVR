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
using UnityEngine.XR;

namespace NewtonVR {

//------------------------------------------------------------------------------
// Class definition 
//------------------------------------------------------------------------------

public class NVRWindowsMRIntegration : NVRIntegration 
{
    //--------------------------------------------------------------------------
    // Private member variables
    //--------------------------------------------------------------------------

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
     
    public override void DontDestroyRenderer()
    {
        GameObject.DontDestroyOnLoad(SteamVR_Render.instance.gameObject);
    }
    
    //--------------------------------------------------------------------------

    public override void Initialize(NVRPlayer player)
    {
#if UNITY_WSA
        rigObj = player.gameObject;

        InitPoseCallback();
#endif // UNITY_WSA
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

    /**
     * Returns the projection matrix for the given eye
     */
    public override Matrix4x4 GetEyeProjectionMatrix(XRNode eye,
                                                     float  nearZ,
                                                     float  farZ)
    {
        return Matrix4x4.identity;
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
