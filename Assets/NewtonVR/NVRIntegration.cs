using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.XR;

namespace NewtonVR
{
    public abstract class NVRIntegration
    {
        protected NVRPlayer Player;

        public abstract void DontDestroyRenderer();

        public abstract void Initialize(NVRPlayer player);

        public abstract Vector3 GetPlayspaceBounds();

        public abstract bool IsHmdPresent();

        public abstract void RegisterNewPoseCallback(UnityAction callback);

        public abstract void DeregisterNewPoseCallback(UnityAction callback);

        public abstract void MoveRig(Transform transform);
    
        public abstract Transform GetOrigin();

        public abstract Vector3 GetEyeOffset(XRNode eye);

        public abstract Matrix4x4 GetEyeProjectionMatrix(XRNode eye,
                                                         float  nearZ,
                                                         float  farZ);
    }
}
