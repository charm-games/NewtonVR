using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
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

        public abstract void Initialize(NVRPlayer player);

        public abstract void DeInitialize();

        public abstract bool IsInit();

        public abstract Vector3 GetPlayspaceBounds();

        public abstract bool IsHmdPresent();

        public abstract void RegisterNewPoseCallback(UnityAction callback);

        public abstract void DeregisterNewPoseCallback(UnityAction callback);

        public abstract void MoveRig(Transform transform);
    }
}
