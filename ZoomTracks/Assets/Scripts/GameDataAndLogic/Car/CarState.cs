using UnityEngine;
using UnityEngine.InputSystem;

namespace ZoomTracks {
    public class CarState {
        private const float MaxDriftVisualIndicator_Degrees = 80f;
        private const float MaxDriftSpeed_DegreesPerSecond = 45f;

        private Transform PlaceholderCarTransform { get; }
        private TrackSwitcher TrackSwitcher { get; }
        private CarSwitcher CarSwitcher { get; }
        private CameraController CameraController { get; }
        private InputManager InputManager { get; }

        /// <summary>
        /// World space
        /// </summary>
        public Vector3 Position { get; private set; }

        /// <summary>
        /// World space
        /// </summary>
        private Vector3 Velocity { get; set; }

        private float PreviousRotation { get; set; }

        private Quaternion RotationQuaternion {
            get {
                if (this.Velocity.sqrMagnitude <= 0f) {
                    return Quaternion.Euler(0f, this.PreviousRotation, 0f);
                } else {
                    return Quaternion.LookRotation(this.Velocity, Vector3.up);
                }
            }
        }

        private float Rotation => this.RotationQuaternion.eulerAngles.y;

        private float DriftInput { get; set; }

        private float VisualRotation { get; set; }

        public CarState(Transform placeholderCarTransform, TrackSwitcher trackSwitcher, CarSwitcher carSwitcher, CameraController cameraController, InputManager inputManager) {
            this.PlaceholderCarTransform = placeholderCarTransform;
            this.TrackSwitcher = trackSwitcher;
            this.CarSwitcher = carSwitcher;
            this.CameraController = cameraController;
            this.InputManager = inputManager;
            this.Reset();
        }

        // cameraTransformEulerAngleY must be in world space
        // cameraTransformEulerAngleY = GameObject.Find("Camera").GetComponent<Camera>().transform.eulerAngles.y
        public void ReadInputAndUpdateState() {
            Gamepad gamepad = this.InputManager.Gamepad;
            if (gamepad == null) {
                return;
            }

            float brakeInput = gamepad.bButton.ReadValue();
            Vector2 accelerationInput_xyPlane = gamepad.rightStick.ReadValue();
            CarDynamic carDynamic = this.CarSwitcher.CurrentCarDynamic;
            float cameraTransformEulerAngleY = this.CameraController.CameraYawWorldSpace;

            if (brakeInput <= 0) {
                if (accelerationInput_xyPlane.magnitude > 0) {
                    Vector3 accelerationInput_xzPlane = new(accelerationInput_xyPlane.x, 0, accelerationInput_xyPlane.y);
                    Vector3 accelerationInput_worldSpace = Quaternion.Euler(0, cameraTransformEulerAngleY, 0) * accelerationInput_xzPlane;
                    Vector3 accelerationInput_carSpace = Quaternion.Inverse(this.RotationQuaternion) * accelerationInput_worldSpace;

                    Vector3 accelerationOutput_carSpace = default;
                    if (accelerationInput_carSpace.x > 0) {
                        accelerationOutput_carSpace.x = accelerationInput_carSpace.x * carDynamic.AccelerationMap.Right;
                    } else if (accelerationInput_carSpace.x < 0) {
                        accelerationOutput_carSpace.x = accelerationInput_carSpace.x * carDynamic.AccelerationMap.Left;
                    } else {
                        accelerationOutput_carSpace.x = 0;
                    }
                    if (accelerationInput_carSpace.z > 0) {
                        accelerationOutput_carSpace.z = accelerationInput_carSpace.z * carDynamic.AccelerationMap.Forward;
                    } else if (accelerationInput_carSpace.z < 0) {
                        accelerationOutput_carSpace.z = accelerationInput_carSpace.z * carDynamic.AccelerationMap.Reverse;
                    } else {
                        accelerationOutput_carSpace.z = 0;
                    }
                    accelerationOutput_carSpace.y = 0;

                    Vector3 accelerationOutput_worldSpace = this.RotationQuaternion * accelerationOutput_carSpace;
                    Vector3 deltaVelocity_worldSpace = Time.deltaTime * accelerationOutput_worldSpace;
                    deltaVelocity_worldSpace.y = 0;
                    this.Velocity += deltaVelocity_worldSpace;
                } else {
                    // Brake and acceleration are zero, so do nothing
                }
            } else {
                if (this.Velocity.sqrMagnitude <= 0f) {
                    // Brake is non-zero, but velocity is already zero, so do nothing
                } else {
                    Vector3 opposingVec = (-1 * this.Velocity).normalized;
                    Vector3 velocityDelta = carDynamic.AccelerationMap.Reverse * brakeInput * Time.deltaTime * opposingVec;
                    if (velocityDelta.magnitude >= this.Velocity.magnitude) {
                        this.Velocity = Vector3.zero;
                    } else {
                        this.Velocity += velocityDelta;
                    }
                }
            }

            if (this.Velocity.sqrMagnitude > 0f) {
                // Get drift input
                Vector2 driftInput_xyPlane = gamepad.leftStick.ReadValue();
                Vector3 driftInput_xzPlane = new(driftInput_xyPlane.x, 0f, driftInput_xyPlane.y);
                Vector3 driftInput_worldSpace = Quaternion.Euler(0f, cameraTransformEulerAngleY, 0f) * driftInput_xzPlane;
                Vector3 driftInput_carSpace = Quaternion.Inverse(this.RotationQuaternion) * driftInput_worldSpace;
                float driftInput = driftInput_carSpace.x;
                this.DriftInput = driftInput;
                float driftDeltaAngle = MaxDriftSpeed_DegreesPerSecond * Time.deltaTime * driftInput;
                this.Velocity = Quaternion.Euler(0f, driftDeltaAngle, 0f) * this.Velocity;
            } else {
                this.DriftInput = 0f;
            }

            if (this.Velocity.sqrMagnitude > 0f) {
                this.PreviousRotation = this.Rotation;
            }

            // this.TranslationalDrift(gamepad, cameraTransformEulerAngleY);

            //if (carDynamic.VelocityLimiter >= 0) {
            //    // Limit velocity
            //    this.Velocity = Vector3.ClampMagnitude(this.Velocity, carDynamic.VelocityLimiter);
            //}
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0051:Remove unused private members", Justification = "<Pending>")]
        private void TranslationalDrift(Gamepad gamepad, float cameraTransformEulerAngleY) {
            if (this.Velocity.magnitude > 0f) {
                // Get drift input
                Vector2 driftInput_xyPlane = gamepad.leftStick.ReadValue();
                Vector3 driftInput_xzPlane = new(driftInput_xyPlane.x, 0f, driftInput_xyPlane.y);
                Vector3 driftInput_worldSpace = Quaternion.Euler(0f, cameraTransformEulerAngleY, 0f) * driftInput_xzPlane;
                Vector3 driftInput_carSpace = Quaternion.Inverse(this.RotationQuaternion) * driftInput_worldSpace;
                float driftInput = driftInput_carSpace.x;
                float maxDriftSpeed_metersPerSecond = 10;
                float driftDistance = driftInput * maxDriftSpeed_metersPerSecond * Time.deltaTime;
                Vector3 unitLeft = (Quaternion.Euler(0f, 90f, 0f) * this.Velocity).normalized;
                this.Position += unitLeft * driftDistance;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0051:Remove unused private members", Justification = "<Pending>")]
        private Vector3 PreventRotationJitter(Vector3 velocityDelta) {
            float minVelocityForRotation = this.CarSwitcher.CurrentCarDynamic.MinVelocityForRotation;
            if (minVelocityForRotation == 0) {
                minVelocityForRotation = this.TrackSwitcher.CurrentTrackJson.MinVelocityForRotation;
            }
            if (this.Velocity.magnitude < minVelocityForRotation) {
                Vector3 carSpaceVelocityDelta = Quaternion.Inverse(this.RotationQuaternion) * velocityDelta;
                // Zero out the Vector3.left and Vector3.right (with respect to the car yaw) component of the velocity delta
                carSpaceVelocityDelta.x = 0;
                Vector3 newVelocityDelta = this.RotationQuaternion * carSpaceVelocityDelta;
                return newVelocityDelta;
            } else {
                return velocityDelta;
            }
        }

        public void ApplyVelocityToPositionAndRotation() {
            // Apply velocity to position
            this.Position += this.Velocity * Time.deltaTime;

            if (this.Velocity != Vector3.zero) {
                // Rotate to match the velocity direction
                this.VisualRotation = this.Rotation;

                // Apply rotation offset based on drift input
                this.VisualRotation += this.DriftInput * MaxDriftVisualIndicator_Degrees;
            }
        }

        public void ApplyStateToGameObject() {
            this.CarSwitcher.CurrentCarTransform.SetPositionAndRotation(
                this.Position,
                Quaternion.Euler(0f, this.VisualRotation, 0f));
        }

        public void Reset() {
            this.Position = this.PlaceholderCarTransform.position;
            this.PreviousRotation = this.PlaceholderCarTransform.rotation.eulerAngles.y;
            this.VisualRotation = this.PlaceholderCarTransform.rotation.eulerAngles.y;
            this.Velocity = Vector3.zero;
            this.DriftInput = 0f;
        }
    }
}
