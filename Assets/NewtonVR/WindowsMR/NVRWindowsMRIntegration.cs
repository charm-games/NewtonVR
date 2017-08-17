/**
 * Copyright 2016, Charm Games Inc, All rights reserved.
 */

//------------------------------------------------------------------------------
// Using directives 
//------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VR.WSA;
using UnityEngine.VR.WSA.Input;

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

    //--------------------------------------------------------------------------
    // Public member variables
    //--------------------------------------------------------------------------

    //--------------------------------------------------------------------------
    // Public methods 
    //--------------------------------------------------------------------------

    public override void Initialize(NVRPlayer player)
    {
        rigObj = player.gameObject;

        // Set the player rig's tracking position in real space
        InitFloor();

        InitPoseCallback();
    }

    //--------------------------------------------------------------------------

    public override Vector3 GetPlayspaceBounds()
    {
        return new Vector3(5f, 5f, 5f);
    }

    //--------------------------------------------------------------------------

    public override bool IsHmdPresent()
    {
        return HolographicSettings.IsDisplayOpaque;
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
    // Private methods 
    //--------------------------------------------------------------------------

    private void InitFloor()
    {
        GameObject stageRootObject = new GameObject("StageRoot");

        // Add a stage root to set the floor position on the rig object.
        // StageRoot will anchor to the floor automatically once added.
        StageRoot stageRoot = stageRootObject.AddComponent<StageRoot>();

        // Move the rig to the stage root
        PlaceRig(stageRoot);

        // Set up to hear about tracking resets
        stageRoot.OnTrackingChanged += OnTrackingChanged;
        
    }

    //--------------------------------------------------------------------------

    /**
     * Place the rig in relation to where the floor (stageroot) is
     */
    private void PlaceRig(StageRoot stageRoot)
    {
        // The head position is given in world space, not relative to the floor.
        // Wr figure out the offset of our player rig from the floor and apply
        // that to the rig so that it is added to the head and hand transforms.

        float floorToRigVerticalOffset = 
            rigObj.transform.position.y - stageRoot.transform.position.y;

        rigObj.transform.position = 
            new Vector3(rigObj.transform.position.x,
                        rigObj.transform.position.y + floorToRigVerticalOffset,
                        rigObj.transform.position.z);
    }

    //--------------------------------------------------------------------------

    private void OnTrackingChanged(StageRoot stageRoot, bool located)
    {
        PlaceRig(stageRoot);
    }

    //--------------------------------------------------------------------------

    private void NewPoseCallbackInternal(InteractionManager.SourceEventArgs eventArgs)
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
        InteractionManager.SourceUpdated += NewPoseCallbackInternal;
    }

    //--------------------------------------------------------------------------
    
}

}
