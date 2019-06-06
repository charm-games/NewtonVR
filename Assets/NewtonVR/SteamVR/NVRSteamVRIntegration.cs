using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

#if NVR_SteamVR
using Valve.VR;

namespace NewtonVR
{
    public class NVRSteamVRIntegration : NVRIntegration
    {
        private bool initialized = false;

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
            // no-op
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
    }
}
#endif
