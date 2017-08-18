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

    /**
     * The world space y that the rig starts the scene at. Used to reanchor the
     * rig everytime a new floor is set by the tracking.
     */
    private float initialRigHeight = 0f;

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
        // Get the initial rig height before we move the rig
        initialRigHeight = rigObj.transform.position.y;

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

        // Figure out how far from the floor the rig should be
        float floorToRigVerticalOffset = 
            initialRigHeight - stageRoot.transform.position.y;

        // Now we move the rig up by that distance which is why we add twice the
        // rig offset to the floor, once to get back to original height, and
        // once more to offset the deficit built into the hands and head
        // positions coming from the hmd.
        rigObj.transform.position = 
            new Vector3(rigObj.transform.position.x,
                        stageRoot.transform.position.y + 2 * floorToRigVerticalOffset,
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
