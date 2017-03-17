using UnityEngine;
using System.Collections;

namespace NewtonVR
{
    public class NVRInteractableRotator : NVRInteractable
    {
        public float CurrentAngle;

        protected virtual float DeltaMagic { get { return 1f; } }
        protected Transform InitialAttachPoint;

        protected override void Awake()
        {
            base.Awake();
            this.Rigidbody.maxAngularVelocity = 100f;
        }

        /*
         * Charm Games, December 21 2016:  Added to allow subclass 
         * to override when rotational force is applied.
         */
        protected virtual bool ShouldApplyRotationForce()
        {
            return (IsAttached == true);
        }

        /*
         * Charm Games, December 21 2016:  Added to allow subclass 
         * to override how rotational force is applied.
         */
        protected virtual void ApplyRotationalForce()
        {
            Vector3 PositionDelta = (AttachedHand.transform.position - InitialAttachPoint.position) * DeltaMagic;
            this.Rigidbody.AddForceAtPosition(PositionDelta, InitialAttachPoint.position, ForceMode.VelocityChange);
        }

        protected virtual void FixedUpdate()
        {
            if (ShouldApplyRotationForce())
            {
                ApplyRotationalForce();
            }

            CurrentAngle = Quaternion.Angle(Quaternion.identity, this.transform.rotation);
        }

        public override void BeginInteraction(NVRHand hand)
        {
            base.BeginInteraction(hand);

            InitialAttachPoint = new GameObject(string.Format("[{0}] InitialAttachPoint", this.gameObject.name)).transform;
            InitialAttachPoint.position = hand.transform.position;
            InitialAttachPoint.rotation = hand.transform.rotation;
            InitialAttachPoint.localScale = Vector3.one * 0.25f;
            InitialAttachPoint.parent = this.transform;
        }

        public override void EndInteraction()
        {
            base.EndInteraction();

            if (InitialAttachPoint != null)
                Destroy(InitialAttachPoint.gameObject);
        }

    }
}
