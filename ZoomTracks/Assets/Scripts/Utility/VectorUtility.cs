using UnityEngine;

namespace ZoomTracks {
    public static class VectorUtility {
        public static Vector3 ClampAngularSpeed_Old(float oldRotation_degrees, Vector3 newVelocity, float maxRotationSpeed_degreesPerSecond) {
            if (newVelocity == Vector3.zero) {
                return newVelocity;
            }
            Vector3 oldDirectionUnitVector = Quaternion.AngleAxis(oldRotation_degrees, Vector3.up) * Vector3.forward;
            float maxDeltaRotation_degrees = maxRotationSpeed_degreesPerSecond * Time.deltaTime;
            float signedDeltaRotation_degrees = Vector3.SignedAngle(oldDirectionUnitVector, newVelocity, Vector3.up);
            if (Mathf.Abs(signedDeltaRotation_degrees) <= maxDeltaRotation_degrees) {
                return newVelocity;
            }
            float clampedRotation_degrees = Mathf.Sign(signedDeltaRotation_degrees) * maxDeltaRotation_degrees;
            Vector3 newDirectionUnitVector = Quaternion.AngleAxis(clampedRotation_degrees, Vector3.up) * oldDirectionUnitVector;
            return newDirectionUnitVector * newVelocity.magnitude;
        }

        private const float AntiparallelEpsilon_Degrees = 0.1f;

        public static Vector3 ClampAngularSpeed(float oldRotation_degrees, Vector3 newVelocity, float maxRotationSpeed_degreesPerSecond) {
            if (newVelocity == Vector3.zero) {
                return newVelocity;
            }
            Vector3 oldDirectionUnitVector = Quaternion.AngleAxis(oldRotation_degrees, Vector3.up) * Vector3.forward;
            float maxDeltaRotation_degrees = maxRotationSpeed_degreesPerSecond * Time.deltaTime;
            float deltaRotation_degrees = Vector3.Angle(oldDirectionUnitVector, newVelocity);
            if (deltaRotation_degrees <= maxDeltaRotation_degrees) {
                return newVelocity;
            }

            float turnSign;
            if (deltaRotation_degrees >= (180f - AntiparallelEpsilon_Degrees)) {
                turnSign = 1f;
            } else {
                turnSign = Mathf.Sign(Vector3.SignedAngle(oldDirectionUnitVector, newVelocity, Vector3.up));
            }
            float clampedRotation_degrees = turnSign * maxDeltaRotation_degrees;
            Vector3 newDirectionUnitVector = Quaternion.AngleAxis(clampedRotation_degrees, Vector3.up) * oldDirectionUnitVector;
            return newDirectionUnitVector * newVelocity.magnitude;
        }
    }
}
