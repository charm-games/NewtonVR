using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

#if NVR_SteamVR
using Valve.VR;

namespace NewtonVR
{
    public class NVRSteamVRIntegration : NVRIntegration
    {
        private bool initialized = false;

        public override void DontDestroyOnLoad()
        {
            GameObject.DontDestroyOnLoad(SteamVR_Render.instance.gameObject);
        }

        public override void Initialize(NVRPlayer player)
        {
            Player = player;
            Player.gameObject.SetActive(false);

            SteamVR_ControllerManager controllerManager = Player.gameObject.AddComponent<SteamVR_ControllerManager>();
            controllerManager.left = Player.LeftHand.gameObject;
            controllerManager.right = Player.RightHand.gameObject;

            //Player.gameObject.AddComponent<SteamVR_PlayArea>();

            for (int index = 0; index < Player.Hands.Length; index++)
            {
                Player.Hands[index].gameObject.AddComponent<SteamVR_TrackedObject>();
            }


            SteamVR_Camera steamVrCamera = Player.Head.gameObject.AddComponent<SteamVR_Camera>();
            Player.Head.gameObject.AddComponent<SteamVR_Ears>();
            NVRHelpers.SetField(steamVrCamera, "_head", Player.Head.transform, false);
            NVRHelpers.SetField(steamVrCamera, "_ears", Player.Head.transform, false);

            Player.Head.gameObject.AddComponent<SteamVR_TrackedObject>();

            Player.gameObject.SetActive(true);

            SteamVR_Render[] steamvr_objects = GameObject.FindObjectsOfType<SteamVR_Render>();
            for (int objectIndex = 0; objectIndex < steamvr_objects.Length; objectIndex++)
            {
                steamvr_objects[objectIndex].lockPhysicsUpdateRateToRenderFrequency = false; //this generally seems to break things :) Just make sure your Time -> Physics Timestep is set to 0.011
            }

            initialized = true;
            InvokeOnInitializedEvent();
        }

        public override void DeInitialize()
        {
            if (SteamVR_Render.instance != null) {
                // This must destroy immediately to avoid a race condition with 
                // the new rig being initialized on scene change.
                // SteamVR_Render must have the same DontDestroyOnLoad status as the 
                // PlayerRig to ensure height adjustment happens correctly through recentering.
                // If the destroy call here is delayed, we may have a race condition where
                // the next rig initialization sets the SteamVR_Render object to 
                // DontDestroyOnLoad before the Destroy is fired, thereby destroying
                // the SteamVR_Render instance that was supposed to stick around.
                // The rig and the SteamVR_Render objects must be initialized and destroyed
                // together.
                GameObject.DestroyImmediate(SteamVR_Render.instance.gameObject);
            }
        }

        public override bool IsInit()
        {
            return initialized;
        }

        private Vector3 PlayspaceBounds = Vector3.zero;
        public override Vector3 GetPlayspaceBounds()
        {
            bool initOpenVR = (!SteamVR.active && !SteamVR.usingNativeSupport);
            if (initOpenVR)
            {
                EVRInitError error = EVRInitError.None;
                OpenVR.Init(ref error, EVRApplicationType.VRApplication_Other);
            }

            CVRChaperone chaperone = OpenVR.Chaperone;
            if (chaperone != null)
            {
                chaperone.GetPlayAreaSize(ref PlayspaceBounds.x, ref PlayspaceBounds.z);
                PlayspaceBounds.y = 1;
            }

            if (initOpenVR)
                OpenVR.Shutdown();

            return PlayspaceBounds;
        }

        public override bool IsHmdPresent()
        {
            bool initOpenVR = (!SteamVR.active && !SteamVR.usingNativeSupport);
            if (initOpenVR)
            {
                EVRInitError error = EVRInitError.None;
                OpenVR.Init(ref error, EVRApplicationType.VRApplication_Other);

                if (error != EVRInitError.None)
                {
                    return false;
                }
            }

            return OpenVR.IsHmdPresent();
        }

        public override void RegisterNewPoseCallback(UnityAction callback)
        {
            SteamVR_Events.NewPosesApplied.Listen(callback);
        }

        public override void DeregisterNewPoseCallback(UnityAction callback)
        {
            SteamVR_Events.NewPosesApplied.Remove(callback);
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
            int eyeIndex = (int) XREyeIndexToSteamVREyeIndex(eye);
            Vector3 eyeOffset = SteamVR.instance.eyes[eyeIndex].pos;
            return eyeOffset;
        }

        //--------------------------------------------------------------------------

        /**
         * Returns the projection matrix for the given eye
         */
        public override Matrix4x4 GetEyeProjectionMatrix(XRNode eye,
                                                         float  nearZ,
                                                         float  farZ)
        {
            Valve.VR.EVREye eyeIndex = XREyeIndexToSteamVREyeIndex(eye);

            // Get the projection matrix for this eye from that native API
            Valve.VR.HmdMatrix44_t hmdProjMat = 
                SteamVR.instance.hmd.GetProjectionMatrix(eyeIndex, nearZ, farZ);

            // Convert to unity type
            Matrix4x4 retVal = HMDMatrix4x4ToMatrix4x4(hmdProjMat);

            return retVal;
        }

        //--------------------------------------------------------------------------

        public override void Recenter()
        {
            InputTracking.Recenter();
        }

        //--------------------------------------------------------------------------
        // Private methods 
        //--------------------------------------------------------------------------

        private Valve.VR.EVREye XREyeIndexToSteamVREyeIndex(XRNode eyeIndex)
        {
            switch (eyeIndex) {
                case XRNode.LeftEye:
                    return Valve.VR.EVREye.Eye_Left;
                case XRNode.RightEye:
                    return Valve.VR.EVREye.Eye_Right;
                default:
                    return Valve.VR.EVREye.Eye_Left;
            }
        }

        //--------------------------------------------------------------------------

        private Matrix4x4 HMDMatrix4x4ToMatrix4x4(Valve.VR.HmdMatrix44_t input) 
        {
            var m = Matrix4x4.identity;

            m[0, 0] = input.m0;
            m[0, 1] = input.m1;
            m[0, 2] = input.m2;
            m[0, 3] = input.m3;

            m[1, 0] = input.m4;
            m[1, 1] = input.m5;
            m[1, 2] = input.m6;
            m[1, 3] = input.m7;

            m[2, 0] = input.m8;
            m[2, 1] = input.m9;
            m[2, 2] = input.m10;
            m[2, 3] = input.m11;

            m[3, 0] = input.m12;
            m[3, 1] = input.m13;
            m[3, 2] = input.m14;
            m[3, 3] = input.m15;

            return m;
        }
    }
}
#else
namespace NewtonVR
{
    public class NVRSteamVRIntegration : NVRIntegration
    {
        public override void Initialize(NVRPlayer player)
        {
        }

        public override void DeInitialize()
        {
            // no-op
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

        public override void Recenter()
        {
        }

        //--------------------------------------------------------------------------
    }
}
#endif
