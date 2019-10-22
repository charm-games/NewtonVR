using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.XR;
using System;

namespace NewtonVR
{
    public abstract class NVRIntegration
    {
        protected NVRPlayer Player;

        protected event Action OnInitialized;

        public void AddOnInitializedListener(Action callback) {
            OnInitialized += callback;
        }

        public void RemoveOnInitializedListener(Action callback) {
            OnInitialized -= callback;
        }

        protected void InvokeOnInitializedEvent()
        {
            if (OnInitialized != null)
            {
                OnInitialized();
            }
        }

        public abstract void DontDestroyOnLoad();

        public abstract void Initialize(NVRPlayer player);

        public abstract void DeInitialize();

        public abstract bool IsInit();

        public abstract Vector3 GetPlayspaceBounds();

        public abstract bool IsHmdPresent();

        public abstract void RegisterNewPoseCallback(UnityAction callback);

        public abstract void Recenter();

        public abstract void DeregisterNewPoseCallback(UnityAction callback);

        public abstract void MoveRig(Transform transform);

        public abstract void MoveRig(Vector3 position, Quaternion orientation);
    
        public abstract Transform GetOrigin();

        public abstract Vector3 GetEyeOffset(XRNode eye);

        public abstract Matrix4x4 GetEyeProjectionMatrix(XRNode eye,
                                                         float  nearZ,
                                                         float  farZ);

        public abstract void SetHeadHeight(float headHeight);

        public virtual void Update()
        {
        }
    }
}
