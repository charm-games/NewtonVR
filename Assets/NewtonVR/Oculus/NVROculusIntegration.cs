using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

#if NVR_Oculus
namespace NewtonVR
{
    public class NVROculusIntegration : NVRIntegration
    {
        private bool initialized = false;
        private OVRBoundary boundary;
        private OVRBoundary Boundary
        {
            get
            {
                if (boundary == null)
                {
                    boundary = new OVRBoundary();
                }
                return boundary;
            }
        }

        private OVRDisplay display;
        private OVRDisplay Display
        {
            get
            {
                if (display == null)
                {
                    display = new OVRDisplay();
                }
                return display;
            }
        }

        private OVRTracker tracker;
        private OVRTracker Tracker
        {
            get
            {
                if (tracker == null)
                {
                    tracker = new OVRTracker();
                }
                return tracker;
            }
        }

        /**
         * A list of all external callbacks that should be fired on receiving
         * the new poses event from the hmd
         */
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
        
        public override void DontDestroyOnLoad()
        {
        }

        public override void Initialize(NVRPlayer player)
        {
            Player = player;
            Player.gameObject.SetActive(false);

            OVRManager manager = Player.gameObject.AddComponent<OVRManager>();
            manager.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;

            OVRCameraRig rig = Player.gameObject.AddComponent<OVRCameraRig>();

            NVRHelpers.SetProperty(rig, "trackingSpace", Player.transform, true);
            NVRHelpers.SetProperty(rig, "leftHandAnchor", Player.LeftHand.transform, true);
            NVRHelpers.SetProperty(rig, "rightHandAnchor", Player.RightHand.transform, true);
            NVRHelpers.SetProperty(rig, "centerEyeAnchor", Player.Head.transform, true);

            Player.gameObject.SetActive(true);

            // Register our internal callback to listen for updated poses
            Action<OVRCameraRig> internalCallback = NewPoseCallbackInternal;

            rig.UpdatedAnchors += internalCallback;

            initialized = true;
            InvokeOnInitializedEvent();
        }

        public override void DeInitialize()
        {
            // no-op
        }

        public override bool IsInit()
        {
            return initialized;
        }

        private Vector3 PlayspaceBounds = Vector3.zero;
        public override Vector3 GetPlayspaceBounds()
        {
            bool configured = Boundary.GetConfigured();
            if (configured == true)
            {
                PlayspaceBounds = Boundary.GetDimensions(OVRBoundary.BoundaryType.OuterBoundary);
            }

            return PlayspaceBounds;
        }

        public override bool IsHmdPresent()
        {
            if (Application.isPlaying == false) //try and enable vr if we're in the editor so we can get hmd present
            {
                if (UnityEngine.XR.XRSettings.enabled == false)
                {
                    UnityEngine.XR.XRSettings.enabled = true;
                }

                if (Display == null)

                {
                    return false;
                }

                if (Tracker == null)
                {
                    return false;
                }
            }

            return OVRPlugin.hmdPresent;
        }

        
        public override void RegisterNewPoseCallback(UnityAction callback)
        {
            // Store the external callback to be called from the internal
            // callback. This is because we want the external callback to always
            // be of type UnityAction.
            NewPosesCallbacks.Add(callback);
        }

        private void NewPoseCallbackInternal(OVRCameraRig rig)
        {
            foreach (UnityAction callback in NewPosesCallbacks) {
                callback();
            }
        }

        public override void DeregisterNewPoseCallback(UnityAction callback)
        {
            NewPosesCallbacks.Remove(callback);
        }

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
    }

}
#else
namespace NewtonVR
{
    public class NVROculusIntegration : NVRIntegration
    {
        public override void Initialize(NVRPlayer player)
        {
        }

        public override void DeInitialize()
        {
        }

        public override bool IsInit()
        {
            return false;
        }

        public override void DontDestroyOnLoad()
        {
        }

        public override Vector3 GetPlayspaceBounds()
        {
            return Vector3.zero;
        }

        public override bool IsHmdPresent()
        {
            return false;
        }

        public override void RegisterNewPoseCallback(UnityAction callback)
        {
        }

        public override void DeregisterNewPoseCallback(UnityAction callback)
        {
        }

        public override void MoveRig(Transform transform)
        {
        }

        public override void MoveRig(Vector3 position, Quaternion orientation)
        {
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
    }
}
#endif
