using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Suspension{
    public float restLength;
    public float stiffness;
    [HideInInspector]
    public float lastLength;
    [HideInInspector]
    public float currentLength;
    [HideInInspector]
    public float force;
    [HideInInspector]
    public float fZ;
}

[System.Serializable]
public class Wheel {
    public GameObject visualMesh;
    public float radius;
    public float mass;
    public float steerTime;
    [HideInInspector]
    public bool hit;
    [HideInInspector]
    public float inertia;
    [HideInInspector]
    public float steerAngle;
    public float currentAngle;
}

[System.Serializable]
public class Damper{
    public float stiffness;
    [HideInInspector]
    public float force;
}
[System.Serializable]
public class WheelVelocity{
    public Vector3 linear;
    public float angular;
    public float ecsessive;
};

[System.Serializable]
public class Friction{
    public bool usePacejkaMethod;
    [HideInInspector]
    public float slipSpeed;
    public float slipAngle;
    [HideInInspector]
    public float longForceNormalized;
    [HideInInspector]
    public float longForce;
    [HideInInspector]
    public float sideForce;
    [HideInInspector]
    public float sideForceNormalized;
    [HideInInspector]
    public float torque;
    public AnimationCurve longtitudinalCurve;
    public AnimationCurve lateralCurve;
    public float longForceCoefficient = 1.3f;
    public float lateralForceCoefficient = 1.3f;
    [Tooltip("For arcady style controls")]
    public float additionalCoefficientLong = 1f;
    [Tooltip("For arcady style controls")]
    public float additionalCoefficientLateral = 1f;
    [HideInInspector]
    public float lateralTransientForceMax;
    [HideInInspector]
    public float lateralTransientCoefficient;
    [HideInInspector]
    public float longTransientCoefficient;
    [HideInInspector]
    public float longTransientForceMax;
    [HideInInspector]
    public float longTransientForce;
    [HideInInspector]
    public float lateralTransientForce;
    //0.01 Optimal Value. 0.1 - very Soft tires for Lateral
    //x for Long y for Lateral
    public Vector2 relaxationLength;
    [Range(0f, 1f)]
    public float slopeDegree;
    //The speed that will lerp "sticky" model to pacejka
    [Range(0.1f, 2f)]
    public float transientModelSpeed = 1f;
    [Range(0.5f, 5f)]
    public float pacejkaModelSpeed = 2f;

}

[System.Serializable]
public class TargetFrictionMethod
{
    [HideInInspector]
    public float targetAngularVelocity;
    [HideInInspector]
    public float targetFrictionTorque;
    [HideInInspector]
    public float maximumFrictionTorque;
    [HideInInspector]
    public float targetAngularAcceleration;
    [HideInInspector]
    public float fX;

}
[System.Serializable]
public class Torque{
    public float driveTorque;
    [HideInInspector]
    public float totalTorque;
    [HideInInspector]
    public float brakeTorque;
}

[System.Serializable]
public class Resistance{
    [HideInInspector]
    public float rolling;
    public float rollingCoefficient;
    [HideInInspector]
    public float shaft;
    public float shaftCoefficient;
    [HideInInspector]
    public float united;
}

public class WheelController : MonoBehaviour
{
    //Components
    private Rigidbody rb;
    private RaycastHit hit;
    private float deltaTime;
    private float wheelAcceleration;
    //classes
    [SerializeField]
    public Suspension suspension;
    [SerializeField]
    public Wheel wheel;
    [SerializeField]
    private Damper damper;
    [SerializeField]
    public WheelVelocity wheelVelocity;
    [SerializeField]
    private Friction friction;
    [SerializeField]
    private Torque torque;
    [SerializeField]
    private Resistance resistance;
    [SerializeField]
    private TargetFrictionMethod tfm;
    private float angularVelocity;
    public bool wheelRR;
    void Start(){
       rb = transform.root.GetComponent<Rigidbody>();
       wheel.inertia = Mathf.Pow(wheel.radius,2) * wheel.mass;
    }

    public void Steering(float angle){
        wheel.steerAngle = Mathf.Lerp(wheel.steerAngle, angle * MapRangeClamped(Mathf.Abs(friction.slipAngle), 0f, 100f, 1, 0f), deltaTime * wheel.steerTime);
        transform.localRotation = Quaternion.Euler(Vector3.up * wheel.steerAngle);
        wheel.visualMesh.transform.localRotation = Quaternion.Euler(Vector3.up*wheel.steerAngle);
    }

    public void PhysicsUpdate(float dTorque, float bTorque, float deltaT){
        torque.driveTorque = dTorque;
        torque.brakeTorque = bTorque;
        deltaTime = deltaT; //TODO change
        Raycast();
        ApplyVisuals();
        if(wheel.hit){
            GetSuspensionForce();
            ApplySuspensionForce();
            LongtitudinalForce();
            LateralForce();
            AccelerationBrakes();
            ApplySlipForces();
            Debug.Log(friction.longForce);
        }
    }

    private void Raycast(){
        if (Physics.Raycast(transform.position, -transform.up, out hit, (suspension.restLength + wheel.radius))){
            wheel.hit = true;
            suspension.currentLength = (transform.position - (hit.point + (transform.up * wheel.radius))).magnitude;
        }
        else{
            wheel.hit = false;
        }
    }

    private void GetSuspensionForce(){
        suspension.force = (suspension.restLength - suspension.currentLength) * suspension.stiffness;
        damper.force = ((suspension.lastLength - suspension.currentLength) / deltaTime) *damper.stiffness;
        suspension.fZ = Mathf.Max(0, suspension.force + damper.force);
        suspension.lastLength = suspension.currentLength;
    }

    private void ApplySuspensionForce(){
        rb.AddForceAtPosition((suspension.force + damper.force) * transform.up , transform.position - (transform.up * suspension.currentLength));
        wheelVelocity.linear = transform.InverseTransformDirection(rb.GetPointVelocity(hit.point));
    }

    private void AccelerationBrakes(){
        if (friction.usePacejkaMethod) //pacejkaMethod
        {
            resistance.rolling = wheelVelocity.angular == 0 ? 0 : suspension.fZ * resistance.rolling * Mathf.Sign(wheelVelocity.angular);
            resistance.shaft = wheelVelocity.angular == 0 ? 0 : resistance.shaftCoefficient * Mathf.Sign(wheelVelocity.angular);
            friction.torque = friction.longForce * wheel.radius * suspension.fZ * friction.longForceCoefficient;
            resistance.united = (-friction.torque * MapRangeClamped(Mathf.Abs(wheelVelocity.linear.z), 0f, 0.5f, 1f, 0f)) +
                                (-resistance.rolling * MapRangeClamped(Mathf.Abs(wheelVelocity.linear.z), 0f, 0.5f, 1f, 0f));
            wheelAcceleration = (torque.driveTorque - (friction.torque + resistance.rolling + resistance.shaft + resistance.united)) / wheel.inertia;
            wheelVelocity.angular += wheelAcceleration * deltaTime;
        }
        else //Target friction torque method
        {
            
        }
        //Brakes Start
        wheelVelocity.angular -= Mathf.Min(Mathf.Abs(wheelVelocity.angular), torque.brakeTorque * Mathf.Sign(wheelVelocity.angular) / wheel.inertia * deltaTime) ;
    }

    private void LongtitudinalForce(){
        if (friction.usePacejkaMethod)
        {
            friction.slipSpeed = wheelVelocity.linear.z == 0 ? 0 : (wheelVelocity.angular * wheel.radius) - wheelVelocity.linear.z;
            friction.longForceNormalized = friction.longtitudinalCurve.Evaluate(friction.slipSpeed) * friction.longForceCoefficient;
            friction.longForce = friction.longForceNormalized;
        }
        else
        {

            tfm.targetAngularVelocity = wheelVelocity.linear.z / wheel.radius;
            tfm.targetAngularAcceleration = (wheelVelocity.angular - tfm.targetAngularVelocity) / deltaTime;
            tfm.targetFrictionTorque = tfm.targetAngularAcceleration * wheel.inertia;
            tfm.maximumFrictionTorque = suspension.fZ * wheel.radius;
            friction.longForce = suspension.fZ == 0 ? 0 : tfm.targetFrictionTorque / tfm.maximumFrictionTorque;
            var tfmFrictionTorque = tfm.fX * wheel.radius;
            wheelAcceleration = (torque.driveTorque - tfmFrictionTorque) / wheel.inertia;
            wheelVelocity.angular += wheelAcceleration * deltaTime;
        }
    }

    private void LateralForce(){
        friction.slipAngle = wheelVelocity.linear.z == 0 ? 0: (Mathf.Atan(-wheelVelocity.linear.x / Mathf.Abs(wheelVelocity.linear.z)) * Mathf.Rad2Deg);
        //Transient Force Start
            float maxTransientForce = Mathf.Sign(-wheelVelocity.linear.x);
            friction.lateralTransientForce += (Mathf.Sign(-wheelVelocity.linear.x) - friction.lateralTransientForce) * Mathf.Abs(wheelVelocity.linear.x) / friction.relaxationLength.y *deltaTime;
            friction.lateralTransientForce = Mathf.Clamp(friction.lateralTransientForce, -friction.slopeDegree, friction.slopeDegree);
        //Transient Force End
        if(friction.slipAngle > 0){
            friction.sideForceNormalized = friction.lateralCurve.Evaluate(friction.slipAngle);
        }
        else{
            friction.sideForceNormalized = friction.lateralCurve.Evaluate(-friction.slipAngle) *-1; 
        }
        friction.sideForce = Mathf.Lerp(friction.lateralTransientForce, friction.sideForceNormalized, MapRangeClamped(wheelVelocity.linear.magnitude, friction.transientModelSpeed, friction.pacejkaModelSpeed, 0, 1));
    }

    private void ApplySlipForces(){
        if (friction.usePacejkaMethod)
        {
            Vector3 forwardForceVectorNormalized = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;
            Vector3 sideForceVectorNormalized = Vector3.ProjectOnPlane(transform.right, hit.normal).normalized;
            Vector2 combinedForce = new Vector2(friction.longForce, friction.sideForce);
            combinedForce = combinedForce.normalized * Mathf.Min((friction.lateralForceCoefficient + friction.longForceCoefficient) / 2, combinedForce.magnitude);
            Vector3 combinedForceNorm = (forwardForceVectorNormalized * suspension.fZ * friction.additionalCoefficientLong * combinedForce.x + sideForceVectorNormalized * suspension.fZ * combinedForce.y * friction.additionalCoefficientLateral);
            rb.AddForceAtPosition(combinedForceNorm, transform.position - (transform.up * (suspension.currentLength + wheel.radius)));
        }
        else
        {
            Vector3 forwardForceVectorNormalized = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;
            Vector3 sideForceVectorNormalized = Vector3.ProjectOnPlane(transform.right, hit.normal).normalized;
            Vector2 combinedForce = new Vector2(friction.longForce, friction.sideForce);
            //combinedForce = combinedForce.normalized * Mathf.Min((friction.lateralForceCoefficient + friction.longForceCoefficient) / 2, combinedForce.magnitude);
            combinedForce = combinedForce.normalized * combinedForce.magnitude;
            Vector3 combinedForceNorm = (forwardForceVectorNormalized * tfm.fX + sideForceVectorNormalized * suspension.fZ * combinedForce.y * friction.additionalCoefficientLateral);
            tfm.fX = suspension.fZ * friction.longForce;
            rb.AddForceAtPosition(combinedForceNorm, transform.position - (transform.up * (suspension.currentLength + wheel.radius)));
            
        }
    }

    private void ApplyVisuals(){
        var wheelangularVelDeg = (wheelVelocity.angular * Mathf.Rad2Deg) * deltaTime;
        wheelangularVelDeg %= 360f;
        wheel.visualMesh.transform.localRotation = Quaternion.Euler(wheelangularVelDeg, 0, 0);
        //wheel.visualMesh.transform.Rotate(wheelangularVelDeg, 0, 0,Space.Self);
        //wheel.visualMesh.transform.Rotate(0, wheel.steerAngle,0,Space.Self);
        wheel.visualMesh.transform.position = transform.position - suspension.currentLength*transform.up;
    }

    //This should be changed or fixed yep
    public void ApplyAntirollBar(float force){
        rb.AddForceAtPosition(force * transform.up, transform.position);
    }

    private float MapRangeClamped(float value, float inRangeA, float inRangeB, float outRangeA, float outRangeB){
        float result = Mathf.Lerp(outRangeA, outRangeB, Mathf.InverseLerp(inRangeA, inRangeB, value));
        return (result);
    }
}
