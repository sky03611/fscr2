using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelControllerTFM : MonoBehaviour
{
    //common
    private Rigidbody rb;
    private RaycastHit hit;
    private float deltaTime;

    //Suspension
    [SerializeField] private float restLength = 0.65f;
    [SerializeField] private float suspensionStiffness = 50000;
    private float lastLength;
    private float currentLength;
    private float suspensionForce;
    private float fZ;

    //Damper
    [SerializeField] private float damperStiffness = 8000;
    private float damperForce;

    //WheelSettings
    [SerializeField] private GameObject visualMesh;
    [SerializeField] private float wheelRadius = 0.34f;
    [SerializeField] private float wheelMass = 15;
    private bool wheelHit;
    private float wheelInertia = 1.5f;

    //Steering 
    [SerializeField] private float steerTime;
    private float steerAngle;
    private float currentAngle;

    //Wheel Velocity
    private Vector3 linearVelocity;
    private float angularVelocity;
    private float angularAcceleration;

    //Target friction method vars
    private float targetAngularVelocity;
    private float targetFrictionTorque;
    private float maximumFrictionTorque;
    [SerializeField]
    private float longFrictionCoefficient = 1f;
    private float targetAngularAcceleration;
    private float frictionTorque;
    private float slipAngle;
    [SerializeField]
    private float slipAnglePeak = 8;
    private float sX;
    private float sY;
    private float fX;
    private float fY;

    //frictionSettings
    [SerializeField] private float relaxationLenth;
    [SerializeField]
    private float latCoeff = 1;
    [SerializeField]
    private float longCoeff = 1;

    [Header("Arcade DriveTrain Settings")]


    //Torque
    private float driveTorque;
    private float brakeTorque;

    private void Start()
    {
        rb = transform.root.GetComponent<Rigidbody>();
        wheelInertia = Mathf.Pow(wheelRadius, 2) * wheelMass;
    }

    public void Steering(float angle)
    {
        steerAngle = Mathf.Lerp(steerAngle, angle, deltaTime * steerTime);
        transform.localRotation = Quaternion.Euler(Vector3.up * steerAngle);
    }

    public void PhysicsUpdate(float dTorque, float bTorque, float dt)
    {
        driveTorque = dTorque;
        brakeTorque = bTorque;
        deltaTime = dt;
        Raycast();
        ApplyVisuals();
        SimpleDownForce();
        if (!wheelHit) { return; }
        GetSuspensionForce();
        ApplySuspensionForce();
        WheelAcceleration();
        GetSx();
        GetSy();
        AddTireForce();
    }

    public void PhysicsUpdate(float dt)
    {
        brakeTorque = Input.GetAxis("Vertical") > 0 ? 0 : -Input.GetAxis("Vertical") * 2000;
        deltaTime = dt;
        Raycast();
        ApplyVisuals();
        SimpleDownForce();
        if (!wheelHit) { return; }
        GetSuspensionForce();
        ApplySuspensionForce();
        WheelAcceleration();
        GetSx();
        GetSy();
        AddTireForce();
    }

    private void Raycast()
    {
        if (Physics.Raycast(transform.position, -transform.up, out hit, (restLength + wheelRadius)))
        {
            wheelHit = true;
            currentLength = (transform.position - (hit.point + (transform.up * wheelRadius))).magnitude;
        }
        else
        {
            wheelHit = false;
        }
    }

    private void SimpleDownForce()
    {

    }

    private void GetSuspensionForce()
    {
        suspensionForce = (restLength - currentLength) * suspensionStiffness;
        damperForce = ((lastLength - currentLength) / deltaTime) * damperStiffness;
        fZ = Mathf.Max(0, suspensionForce + damperForce);
        lastLength = currentLength;
    }

    private void ApplySuspensionForce()
    {
        rb.AddForceAtPosition((suspensionForce + damperForce) * transform.up, transform.position - (transform.up * currentLength));
        linearVelocity = transform.InverseTransformDirection(rb.GetPointVelocity(hit.point));
    }

    private void WheelAcceleration()
    {
        frictionTorque = fX * wheelRadius;
        angularAcceleration = (driveTorque - frictionTorque) / wheelInertia;
        angularVelocity += angularAcceleration * deltaTime;
        //bakes
        angularVelocity -= Mathf.Min(Mathf.Abs(angularVelocity), brakeTorque * Mathf.Sign(angularVelocity) / wheelInertia * deltaTime);
        
    }


    public void ApplySimpleDTTorque(float driveTrainInertia, float driveFrictionTorque, float _driveTorque)
    {
        driveTorque = Input.GetAxis("Vertical") * 500f;//_driveTorque;
        frictionTorque += driveFrictionTorque;
        Debug.Log("dt= " + _driveTorque);
        
    }

    private void GetSx()
    {
        targetAngularVelocity = linearVelocity.z / wheelRadius;
        targetAngularAcceleration = (angularVelocity - targetAngularVelocity) / deltaTime;
        targetFrictionTorque = targetAngularAcceleration * wheelInertia;
        maximumFrictionTorque = fZ * wheelRadius * longFrictionCoefficient;
        sX = fZ == 0 ? 0 : targetFrictionTorque / maximumFrictionTorque;
    }

    private void GetSy()
    {
        slipAngle = linearVelocity.z == 0 ? 0 : Mathf.Atan(-linearVelocity.x / Mathf.Abs(linearVelocity.z)) * Mathf.Rad2Deg;
        sY = slipAngle / slipAnglePeak;
    }

    public float GetSlipRatio()
    {
        return linearVelocity.z == 0 ? 0 : (angularVelocity * wheelRadius) - linearVelocity.z;
    }

    private void AddTireForce()
    {
        Vector3 forwardForceVectorNormalized = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;
        Vector3 sideForceVectorNormalized = Vector3.ProjectOnPlane(transform.right, hit.normal).normalized;
        Vector2 combinedForce = new Vector2(sX, sY);
        if (combinedForce.magnitude > 1)
        {
            combinedForce = combinedForce.normalized;
        }

        fX = combinedForce.x * fZ * longCoeff;
        fY = combinedForce.y * fZ * latCoeff;

        Vector3 combinedForceNorm = (forwardForceVectorNormalized * fX + sideForceVectorNormalized * fY);
        rb.AddForceAtPosition(combinedForceNorm, transform.position - (transform.up * (currentLength + wheelRadius)));
    }

    private void ApplyVisuals()
    {
        //Credits Blinkachu
        currentAngle += angularVelocity * Mathf.Rad2Deg * Time.deltaTime;
        currentAngle %= 360f;

        visualMesh.transform.position = transform.position - transform.up * currentLength;
        visualMesh.transform.localRotation = Quaternion.Euler(currentAngle, steerAngle, 0f);
    }

    public float GetWheelAngularVelocity()
    {
        return angularVelocity;
    }
    public float GetWheelInertia()
    {
        return wheelInertia;
    }

    //public float GetLoadTorque()
    //{
    //    return angularAcceleration/wheelInertia ;
    //}
    public bool GetWheelHit()
    {
        return wheelHit;
    }
}
