using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
    {
    // ALL CREDITS BELONG TO !!!BLINKACHU!!! FROM OUR DISCORD SERVER https://discord.gg/U2rcepWvuu
        public Transform vehicle;   // to get the Rigidbody, potentially in the future for other things too such as steerAngle n such
        public Transform target;    // the target the Camera Gimbal (which this script is attached to) will use for knowing what rotation it should append
        public Transform cam;       // the camera itself, which will follow the rotation smoothly of the Camera Gimbal
        private Rigidbody rb;
        public Vector3 targetPosOffset; // the position of the Camera Gimbal itself, the empty gameobject that the Camera will look at. It's an offset based on the vehicle's location
        public Vector3 camPosOffset;    // the position of the Camera itself, based on the Camera gimbal
        public float xOffset;           // the value we'll use to offset the targetPosOffset's x value based on speed and steering input
        public float smoothMove;        // how smooth should the Camera Gimbal move on the x axis? Suggested value: 5-7
        public float smoothRotate;      // how fast should the Camera itself rotate on its own axis to align with the Camera Gimbal? Suggested value: 50, since while it should be smooth, should also be fast
        public float steerRatio;        // how fast should the steerinput change? Suggest value: 5-10
        public float ratio;             // by what value do we multiply the Rigidbody's velocity magnitude? Suggested value 0.0125

        private float steer;            // Steering input, from -1 to 1
        private float steerFactor;

        void Awake()
        {
            rb = vehicle.transform.GetComponent<Rigidbody>();

        //// GET STEERING INPUT FROM NEW INPUT SYSTEM
        //vehicleInput = new VehicleInput();
        //vehicleInput.Car.Enable();
        //vehicleInput.Car.Steer.performed += ctx => steer = ctx.ReadValue<float>();
        //vehicleInput.Car.Steer.canceled += ctx => steer = 0;
        steer = Input.GetAxis("Horizontal");
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            SteerFactor(dt);
        }

        private void SteerFactor(float dt)
        {
            // GET SMOOTH STEERFACTOR
            steerFactor += steer * steerRatio * dt;
            steerFactor = Mathf.Clamp(steerFactor, -1f, 1f);
            if (steerFactor != 0 && steer == 0) steerFactor -= steerRatio * dt * Mathf.Sign(steerFactor);
    }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            AdjustPosition(dt);
            SetPosition();
            SetRotation(dt);
            CameraPosition();
            CameraRotation(dt);
        }

        private void AdjustPosition(float dt)
        {
            // GET SPEED RATIO
            float speed = rb.velocity.magnitude;
            speed = Mathf.Clamp(speed, 0f, 24f);
            float speedFactor = speed * ratio;

            // OFFSET THE targetPosOffset's X axis BY THE xOffset's VALUE (and also reset it if steering is between 0 and the deadZone)
            targetPosOffset.x = Mathf.Lerp(targetPosOffset.x, xOffset * steerFactor, smoothMove * speedFactor * dt);

            // REMOVE ANY LOW NONSENSICAL VALUES
            if (Mathf.Abs(targetPosOffset.x) < 0.001f)
                targetPosOffset.x = 0f;
        }

        private void SetPosition()
        {
            // SET POSITION OF THE CAMERA GIMBAL BASED ON CAR'S POSITION W/ OFFSET
            transform.position = target.position + target.rotation * targetPosOffset;
        }

        private void SetRotation(float dt)
        {
            // ROTATE CAMERA GIMBAL BASED ON CAR'S ROTATION
            transform.rotation = target.rotation;
        }

        private void CameraPosition()
        {
            // SET CAMERA'S POSITION BASED ON CAMERA GIMBAL'S POSITION W/ OFFSET
            cam.position = transform.position - transform.rotation * camPosOffset;
        }

        private void CameraRotation(float dt)
        {
            // ROTATE CAMERA SMOOTHLY TOWARDS CAMERA GIMBAL'S ROTATION
            Vector3 direction = transform.position - cam.position;
            Quaternion lookRotation = Quaternion.LookRotation(direction);

            cam.rotation = Quaternion.Slerp(cam.rotation, lookRotation, smoothRotate * dt);
        }
    }