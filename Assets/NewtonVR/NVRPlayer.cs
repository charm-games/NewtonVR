﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.VR;
using System.Linq;
using UnityEngine.Events;
using System;
using CharmGames.Core;

namespace NewtonVR
{
    public class NVRPlayer : MonoBehaviour
    {
        public const decimal NewtonVRVersion = 1.192m;
        public const float NewtonVRExpectedDeltaTime = 0.0111f;

        public static List<NVRPlayer> Instances = new List<NVRPlayer>();
        public static NVRPlayer Instance
        {
            get
            {
                return Instances.First(player => player != null && player.gameObject != null);
            }
        }

        [HideInInspector]
        public bool SteamVREnabled = false;
        [HideInInspector]
        public bool OculusSDKEnabled = false;
        [HideInInspector]
        public bool WindowsMREnabled = false;
        [HideInInspector]
        public bool PSVREnabled = false;

        public InterationStyle InteractionStyle;
        public bool PhysicalHands = true;
        public bool MakeControllerInvisibleOnInteraction = false;
        public bool AutomaticallySetControllerTransparency = true;
        public bool VibrateOnHover = true;
        public int VelocityHistorySteps = 3;

        [Space]
        public bool EnableEditorPlayerPreview = true;
        public Mesh EditorPlayerPreview;
        public Mesh EditorPlayspacePreview;
        public bool EditorPlayspaceOverride = false;
        public Vector2 EditorPlayspaceDefault = new Vector2(2, 1.5f);

        public Vector3 PlayspaceSize
        {
            get
            {
#if !UNITY_5_5_OR_NEWER
                if (Application.isPlaying == false)
                {
                    return Vector3.zero; //not supported in unity below 5.5.
                }
#endif


                if (Integration != null)
                {
                    return Integration.GetPlayspaceBounds();
                }
                else
                {
                    if (OculusSDKEnabled == true)
                    {
                        Integration = new NVROculusIntegration();
                        if (Integration.IsHmdPresent() == true)
                        {
                            return Integration.GetPlayspaceBounds();
                        }
                        else
                        {
                            Integration = null;
                        }
                    }

                    if (SteamVREnabled == true)
                    {
                        Integration = new NVRSteamVRIntegration();
                        if (Integration.IsHmdPresent() == true)
                        {
                            return Integration.GetPlayspaceBounds();
                        }
                        else
                        {
                            Integration = null;
                        }
                    }

                    return Vector3.zero;
                }
            }
        }

        [Space]

        [HideInInspector]
        public bool OverrideAll;
        [HideInInspector]
        public GameObject OverrideAllLeftHand;
        [HideInInspector]
        public GameObject OverrideAllLeftHandPhysicalColliders;
        [HideInInspector]
        public GameObject OverrideAllRightHand;
        [HideInInspector]
        public GameObject OverrideAllRightHandPhysicalColliders;

        [HideInInspector]
        public bool OverrideSteamVR;
        [HideInInspector]
        public GameObject OverrideSteamVRLeftHand;
        [HideInInspector]
        public GameObject OverrideSteamVRLeftHandPhysicalColliders;
        [HideInInspector]
        public GameObject OverrideSteamVRRightHand;
        [HideInInspector]
        public GameObject OverrideSteamVRRightHandPhysicalColliders;

        [HideInInspector]
        public bool OverrideOculus;
        [HideInInspector]
        public GameObject OverrideOculusLeftHand;
        [HideInInspector]
        public GameObject OverrideOculusLeftHandPhysicalColliders;
        [HideInInspector]
        public GameObject OverrideOculusRightHand;
        [HideInInspector]
        public GameObject OverrideOculusRightHandPhysicalColliders;

        [HideInInspector]
        public bool OverrideWindowsMR;
        [HideInInspector]
        public GameObject OverrideWindowsMRLeftHand;
        [HideInInspector]
        public GameObject OverrideWindowsMRLeftHandPhysicalColliders;
        [HideInInspector]
        public GameObject OverrideWindowsMRRightHand;
        [HideInInspector]
        public GameObject OverrideWindowsMRRightHandPhysicalColliders;

        [HideInInspector]
        public bool OverridePSVR;
        [HideInInspector]
        public GameObject OverridePSVRLeftHand;
        [HideInInspector]
        public GameObject OverridePSVRLeftHandPhysicalColliders;
        [HideInInspector]
        public GameObject OverridePSVRRightHand;
        [HideInInspector]
        public GameObject OverridePSVRRightHandPhysicalColliders;

        [Space]

        public NVRHead Head;
        public NVRHand LeftHand;
        public NVRHand RightHand;

        [HideInInspector]
        public NVRHand[] Hands;

        [HideInInspector]
        public NVRSDKIntegrations CurrentIntegrationType = NVRSDKIntegrations.None;

        protected NVRIntegration Integration;
        protected static event Action onIntegrationInitialized;

        private Dictionary<Collider, NVRHand> ColliderToHandMapping;

        [Space]

        public bool DEBUGEnableFallback2D = true;
        public bool DEBUGDropFrames = false;
        public int DEBUGSleepPerFrame = 13;

        public bool AutoSetFixedDeltaTime = true;
        public bool NotifyOnVersionUpdate = true;

        public int NumInitializedInputDevices
        {
            get
            {
                int numInitializedInputDevices = 0;
                foreach (NVRHand hand in Hands) {
                    if (hand.IsInputDeviceInitialized) {
                        numInitializedInputDevices++;
                    }
                }

                return numInitializedInputDevices;
            }
        }

        protected virtual void Awake()
        {
            if (AutoSetFixedDeltaTime)
            {
                Time.fixedDeltaTime = NewtonVRExpectedDeltaTime;
            }

            Instances.Add(this);

            NVRInteractables.Initialize();

            if (Head == null)
            {
                Head = this.GetComponentInChildren<NVRHead>();
            }
            Head.Initialize();

            if (LeftHand == null || RightHand == null)
            {
                Debug.LogError("[FATAL ERROR] Please set the left and right hand to a nvrhands.");
            }

            ColliderToHandMapping = new Dictionary<Collider, NVRHand>();

            SetupIntegration();

            if (Hands == null || Hands.Length == 0)
            {
                Hands = new NVRHand[] { LeftHand, RightHand };

                for (int index = 0; index < Hands.Length; index++)
                {
                    Hands[index].PreInitialize(this);
                }
            }

            if (Integration != null)
            {
                Integration.Initialize(this);
            }
        }

        protected virtual void SetupIntegration(bool logOutput = true)
        {
            CurrentIntegrationType = DetermineCurrentIntegration(logOutput);

            if (CurrentIntegrationType == NVRSDKIntegrations.Oculus)
            {
                Integration = new NVROculusIntegration();
            }
            else if (CurrentIntegrationType == NVRSDKIntegrations.SteamVR)
            {
                Integration = new NVRSteamVRIntegration();
            }
            else if (CurrentIntegrationType == NVRSDKIntegrations.FallbackNonVR)
            {
                if (logOutput == true)
                {
                    //Debug.LogError("[NewtonVR] Fallback non-vr not yet implemented.");
                }
                SetupFallbackNonVRIntegration();
                return;
            }
            else
            {
                if (logOutput == true)
                {
                    Debug.LogError("[NewtonVR] Critical Error: Oculus / SteamVR not setup properly or no headset found.");
                }
                return;
            }

            Integration.AddOnInitializedListener(OnIntegrationInitialized);
        }

        protected void OnIntegrationInitialized()
        {
            if (onIntegrationInitialized != null)
            {
                onIntegrationInitialized();
            }
        }

        protected virtual void SetupFallbackNonVRIntegration()
        {
            Integration = new MockVRIntegration();
        }

        protected virtual NVRSDKIntegrations DetermineCurrentIntegration(bool logOutput = true)
        {
            NVRSDKIntegrations currentIntegration = NVRSDKIntegrations.None;
            string resultLog = "[NewtonVR] Version : " + NewtonVRVersion + ". ";

#if UNITY_PS4
            // PSVR
            currentIntegration = NVRSDKIntegrations.PSVR;
            resultLog += "Using PSVR SDK";
#endif // UNITY_PS4

            if (UnityEngine.XR.XRDevice.isPresent == true)
            {
                resultLog += "Found VRDevice: " + UnityEngine.XR.XRDevice.model + ". ";

#if !NVR_Oculus && !NVR_SteamVR && !UNITY_PS4 && !UNITY_WSA
                string warning = "Neither SteamVR or Oculus SDK is enabled in the NVRPlayer. Please check the \"Enable SteamVR\" or \"Enable Oculus SDK\" checkbox in the NVRPlayer script in the NVRPlayer GameObject.";
                Debug.LogWarning(warning);
#endif

#if NVR_Oculus
                if (UnityEngine.XR.XRSettings.loadedDeviceName.IndexOf("oculus", System.StringComparison.CurrentCultureIgnoreCase) != -1)
                {
                    currentIntegration = NVRSDKIntegrations.Oculus;
                    resultLog += "Using Oculus SDK";
                }
#endif
                if (currentIntegration == NVRSDKIntegrations.None && UnityEngine.XR.XRSettings.loadedDeviceName == "MockHMD") {
                    currentIntegration = NVRSDKIntegrations.FallbackNonVR;
                    resultLog += "Using FallbackNonVR";
                }

                if (currentIntegration == NVRSDKIntegrations.None && UnityEngine.XR.XRSettings.loadedDeviceName == "MockHMD") {
                    currentIntegration = NVRSDKIntegrations.FallbackNonVR;
                    resultLog += "Using FallbackNonVR";
                }
                
#if NVR_SteamVR
                if (currentIntegration == NVRSDKIntegrations.None)
                { 
                    currentIntegration = NVRSDKIntegrations.SteamVR;
                    resultLog += "Using SteamVR SDK";
                }
#endif

#if UNITY_WSA
                // WindowsMR
                if (UnityEngine.XR.WSA.HolographicSettings.IsDisplayOpaque) {
                    return NVRSDKIntegrations.WindowsMR;
                }
#endif // UNITY_WSA

            }

            if (logOutput == true)
            {
                Debug.Log(resultLog);
            }

            return currentIntegration;
        }

        public void RegisterHand(NVRHand hand)
        {
            Collider[] colliders = hand.GetComponentsInChildren<Collider>();

            for (int index = 0; index < colliders.Length; index++)
            {
                if (ColliderToHandMapping.ContainsKey(colliders[index]) == false)
                {
                    ColliderToHandMapping.Add(colliders[index], hand);
                }
            }
        }

        public static void AddOnIntegrationInitializedListener(Action callback) {
            onIntegrationInitialized += callback;
        }

        public static void RemoveOnIntegrationInitializedListener(Action callback) {
            onIntegrationInitialized -= callback;
        }

        public bool IsIntegrationInitialized()
        {
            return Integration.IsInit();
        }

        public NVRHand GetHand(Collider collider)
        {
            return ColliderToHandMapping[collider];
        }

        public static void DeregisterInteractable(NVRInteractable interactable)
        {
            for (int instanceIndex = 0; instanceIndex < Instances.Count; instanceIndex++)
            {
                if (Instances[instanceIndex] != null && Instances[instanceIndex].Hands != null)
                {
                    for (int index = 0; index < Instances[instanceIndex].Hands.Length; index++)
                    {
                        if (Instances[instanceIndex].Hands[index] != null)
                        {
                            Instances[instanceIndex].Hands[index].DeregisterInteractable(interactable);
                        }
                    }
                }
            }
        }

        private void OnDestroy()
        {
            Integration.DeInitialize();
            Instances.Remove(this);
            Integration.DeInitialize();
        }

        protected virtual void Update()
        {
            if (DEBUGDropFrames == true)
            {
                System.Threading.Thread.Sleep(DEBUGSleepPerFrame);
            }
        }


#if UNITY_EDITOR
        private static System.DateTime LastRequestedSize;
        private static Vector3 CachedPlayspaceScale;
        private void OnDrawGizmos()
        {
            if (EnableEditorPlayerPreview == false)
                return;

            if (Application.isPlaying == true)
                return;

            System.TimeSpan lastRequested = System.DateTime.Now - LastRequestedSize;
            Vector3 playspaceScale;
            if (lastRequested.TotalSeconds > 1)
            {
                if (EditorPlayspaceOverride == false)
                {
                    Vector3 returnedPlayspaceSize = PlayspaceSize;
                    if (returnedPlayspaceSize == Vector3.zero)
                    {
                        playspaceScale = EditorPlayspaceDefault;
                        playspaceScale.z = playspaceScale.y;
                    }
                    else
                    {
                        playspaceScale = returnedPlayspaceSize;
                    }
                }
                else
                {
                    playspaceScale = EditorPlayspaceDefault;
                    playspaceScale.z = playspaceScale.y;
                }

                playspaceScale.y = 1f;
                LastRequestedSize = System.DateTime.Now;
            }
            else
            {
                playspaceScale = CachedPlayspaceScale;
            }
            CachedPlayspaceScale = playspaceScale;

            Color drawColor = Color.green;
            drawColor.a = 0.075f;
            Gizmos.color = drawColor;
            Gizmos.DrawWireMesh(EditorPlayerPreview, this.transform.position, this.transform.rotation, this.transform.localScale);
            drawColor.a = 0.5f;
            Gizmos.color = drawColor;
            Gizmos.DrawWireMesh(EditorPlayspacePreview, this.transform.position, this.transform.rotation, playspaceScale * this.transform.localScale.x);
        }
#endif
    }
}
