using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class GlobalParams{
    public float deltaTime;
    public float rpmToRads;
    public float radsToRpm;
    public bool usePacejkaMethod;
    [HideInInspector]
    public float downForce;
}
[System.Serializable]
public class Steering{
    public WheelController[] wheelControllers;
    public WheelControllerTFM[] wheelControllersTFM;
    [HideInInspector]
    public float wheelBase;
    [Range(0.5f, 50f)]
    public float steerForce = 2f;
    public float turnRadius;
    [HideInInspector]
    public float rearTrack;
    [HideInInspector]
    public float steerInput;
    [HideInInspector]
    public float ackermannAngleL;
    [HideInInspector]
    public float ackermannAngleR;
    [HideInInspector]
    public float ackermannAngle;
}

[System.Serializable]
public class Engine{
    public AnimationCurve torqueCurve;
    [HideInInspector]
    public float throttle;
    [HideInInspector]
    public float throttleInput;
    [HideInInspector]
    public float torque;
    [Range(1f,200f)]
    public float startFriction;
    [Range(0.01f, 2f)]
    public float frictionCoefficient;
    [Range(0.05f, 2f)]
    public float inertia;
    [HideInInspector]
    public float angularVelocity;
    [HideInInspector]
    public float rpm;
    [Range(10f,2000f)]
    public float idleRpm;
    [Range(1000,10000)]
    public float maxRpm;
    [HideInInspector]
    public bool cutOff;
    [HideInInspector]
    public float loadTorque;
    [Range(10, 5000)]
    public float cutoffValue;
    [HideInInspector]
    public float brakeInput;
}

[System.Serializable]
public class Brakes{
    [HideInInspector]
    public float input;
    [SerializeField]
    public float[] torque;
    [SerializeField]
    public float maxTorque;
    [SerializeField][Tooltip("Front wheels have more brakes than rear [0] - 1, [1] - 0.7f ")]
    public float[] bias = new float[2];
    [SerializeField][Tooltip("Depending on wheel Angular Velocity, brakes will have more force or less. 1 - full force")]
    public AnimationCurve torqueCurve;
}

[System.Serializable]
public class Clutch{
    public float angularVelocity;
    public float slip;
    public float cLock;
    public float stiffness;
    public float capacity;
    [Range(0f, 0.99f)]
    public float damping;
    public float torque;
    public float lastTorque;
}

[System.Serializable]
public class Transmission{
    [HideInInspector]
    public bool inGear;
    [HideInInspector]
    public float currentGearRatio;
    [HideInInspector]
    public float inputShaftVelocity;
    public float[] gearRatio;
    public float shiftTime;
    
    public int currentGear;
    [HideInInspector]
    public int nextGear;
    [HideInInspector]
    public float inputTorque;
    [HideInInspector]
    public float outputTorque;
}

[System.Serializable]
public class Differential{
    public bool diffLocked;
    public float mainGear;
    public float shaftVelocity;
    public float outputTorqueL;
    public float outputTorqueR;
    public float angularVelocityL;
    public float angularVelocityR;
}

[System.Serializable]
public class AntirollBar{
    public bool enabled;
    [HideInInspector]
    public float[] lengthDifference = new float[2];
    public float stiffnessFront;
    public float stiffnessRear;
    [HideInInspector]
    public float[] force = new float[2];
}
[System.Serializable]
public class DashboardC{
    public bool enabled;
    public Transform needle;
    public Text speed;
    public Text gear;
    [HideInInspector]
    public float tachoAngle;
    public float zeroTachoAngle = 36f;
    public float maxTachoAngle = -130f;
    [HideInInspector]
    public float tachoAngleSize;

    
}

public class CarController : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField]
    private Vector3 centerOfMass;
    [SerializeField]
    private GlobalParams globalParams;
    [SerializeField]
    private Steering steering;
    [SerializeField]
    private Engine engine;
    [SerializeField]
    private Brakes brake;
    [SerializeField]
    private Transmission transmission;
    [SerializeField]
    private Clutch clutch;
    [SerializeField]
    private Differential differential;
    [SerializeField]
    private AntirollBar antirollBar;
    [SerializeField]
    private DashboardC dashboard;
    
    private float MapRangeClamped(float value, float inRangeA, float inRangeB, float outRangeA, float outRangeB){
        float result = Mathf.Lerp(outRangeA, outRangeB, Mathf.InverseLerp(inRangeA, inRangeB, value));
        return (result);
    }

    void Awake()
    {
        Initializator();
    }

    private void Initializator()
    {
        rb = transform.root.GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMass;
        InitializeAckermannParams();
        engine.angularVelocity = 100f; //just to be sure;
        globalParams.rpmToRads = Mathf.PI * 2 / 60;
        globalParams.radsToRpm = 1 / globalParams.rpmToRads;
        transmission.inGear = true;
        transmission.nextGear = 1;
        transmission.currentGear = 1;
        dashboard.tachoAngleSize = dashboard.zeroTachoAngle - dashboard.maxTachoAngle;
    }

    //Update is used only to get inputs. Otherwise it won't read all inputs.;
    void Update()
    {
        UpdateInputs();
        TransmissionShfiter();
    }

    void FixedUpdate()
    {
        globalParams.deltaTime = Time.fixedDeltaTime;
        CalculateDownForce();
        CalculateAckermann();
        CalculateTransmissionOutputTorque(clutch.torque);
        CalculateDifferentialOutputTorque(transmission.outputTorque);
        CalculateWheels(differential.outputTorqueL, differential.outputTorqueR);
        if (globalParams.usePacejkaMethod)
        {
            CalculateDifferentialInputShfatVelocity(steering.wheelControllers[2].wheelVelocity.angular, steering.wheelControllers[3].wheelVelocity.angular);
        }
        else
        {
            CalculateDifferentialInputShfatVelocity(steering.wheelControllersTFM[2].GetWheelAngularVelocity(), steering.wheelControllersTFM[3].GetWheelAngularVelocity());
        }
        CalculateTransmissionInputShaftVelocity(differential.shaftVelocity);
        CalculateClutch(transmission.inputShaftVelocity, engine.angularVelocity);
        CalculateEngine(clutch.torque);
        CalculateBrakes();
        //CalculateAntirollBar();
        DashboardUpdate();
    }

    private void CalculateDownForce()
    {
        //Credits BlinkAChu
        Vector3 linearVelocity = transform.InverseTransformDirection(rb.GetPointVelocity(transform.position));
        globalParams.downForce = 0.5f * 1.22f * Mathf.Pow((Mathf.Max(0, linearVelocity.z)),2) * (5f * 2f);
        rb.AddForceAtPosition(-transform.up * globalParams.downForce, transform.position);
    }

    private void UpdateInputs(){
        steering.steerInput = Input.GetAxis("Horizontal"); //TODO
        engine.throttleInput = Input.GetAxis("Vertical") <= 0 ? 0: Input.GetAxis("Vertical");; //TODO
        brake.input = Input.GetAxis("Vertical") >= 0 ? 0: Input.GetAxis("Vertical");
    }

    private void DashboardUpdate(){
        if(dashboard.enabled){
            float rpmNormalized = engine.rpm / engine.maxRpm;
            dashboard.tachoAngle = dashboard.zeroTachoAngle - rpmNormalized * dashboard.tachoAngleSize;
            dashboard.needle.eulerAngles = new Vector3(0, 0, dashboard.tachoAngle);
            dashboard.speed.text = Mathf.Round(rb.velocity.magnitude*3.6f).ToString();
        }
    }

    private void InitializeAckermannParams(){
        if (globalParams.usePacejkaMethod)
        {
            steering.wheelBase = Vector3.Distance(steering.wheelControllers[0].transform.position, steering.wheelControllers[2].transform.position);
            steering.rearTrack = Vector3.Distance(steering.wheelControllers[2].transform.position, steering.wheelControllers[3].transform.position);
        }
        else
        {
            steering.wheelBase = Vector3.Distance(steering.wheelControllersTFM[0].transform.position, steering.wheelControllersTFM[2].transform.position);
            steering.rearTrack = Vector3.Distance(steering.wheelControllersTFM[2].transform.position, steering.wheelControllersTFM[3].transform.position);
        }
    }

    public void CalculateAckermann(){
        if(steering.steerInput > 0 ){
            steering.ackermannAngleL = Mathf.Rad2Deg * Mathf.Atan(steering.wheelBase / (steering.turnRadius + steering.rearTrack /2)) * steering.steerInput * steering.steerForce;
            steering.ackermannAngleR = Mathf.Rad2Deg * Mathf.Atan(steering.wheelBase / (steering.turnRadius - steering.rearTrack /2)) * steering.steerInput * steering.steerForce;
        }
        else if (steering.steerInput < 0){
            steering.ackermannAngleL = Mathf.Rad2Deg * Mathf.Atan(steering.wheelBase / (steering.turnRadius - steering.rearTrack /2)) * steering.steerInput * steering.steerForce;
            steering.ackermannAngleR = Mathf.Rad2Deg * Mathf.Atan(steering.wheelBase / (steering.turnRadius + steering.rearTrack /2)) * steering.steerInput * steering.steerForce;
        }
        else{
            steering.ackermannAngleL = 0f;
            steering.ackermannAngleR = 0f;
        }
        if (globalParams.usePacejkaMethod)
        {
            steering.wheelControllers[0].Steering(steering.ackermannAngleL);
            steering.wheelControllers[1].Steering(steering.ackermannAngleR);
        }
        else
        {
            steering.wheelControllersTFM[0].Steering(steering.ackermannAngleL);
            steering.wheelControllersTFM[1].Steering(steering.ackermannAngleR);
        }
    }

    public void CalculateEngine(float loadTorque){
        //Idle rpm uses throttle 
        
        if(engine.angularVelocity < engine.idleRpm * globalParams.rpmToRads && engine.throttleInput == 0f){
            engine.throttle += 0.1f;
        }
        else if (!engine.cutOff){
            engine.throttle = engine.throttleInput;
        }
        else{
            engine.throttle = Mathf.Epsilon;
        }
        //CutOff. My implementation of CutOff is based on new cars that use throttle to cut engine's power. Though older cars relied on ignition
        if (engine.rpm > engine.maxRpm){
            engine.cutOff = true;
        }
        if (engine.rpm<= engine.maxRpm - engine.cutoffValue){
            engine.cutOff = false;
        }
        //engine.throttle = engine.throttleInput;
        engine.torque = engine.torqueCurve.Evaluate(engine.rpm);
        //EngineFriction start
        float friction = engine.rpm * engine.frictionCoefficient + engine.startFriction;
        float engineInitialTorque = engine.torque + friction * engine.throttle;
        float currentEffectiveTorque = engineInitialTorque - friction;
        //Engine Acceleration
        float engineAngularAcceleration = (currentEffectiveTorque - loadTorque) / engine.inertia;
        engine.angularVelocity += engineAngularAcceleration * globalParams.deltaTime;
        //second value can be set to engine.idleRpm to disable throttle idling
        engine.rpm = Mathf.Max(engine.angularVelocity * globalParams.radsToRpm, 0); 
        //Debug.Log("EngineRpm= " + engine.rpm + "EngineCutoff= " + engine.cutOff + "effectiveTorque = " + currentEffectiveTorque + "EngineACC= " + engineAngularAcceleration + "EngineAngularVel = " + engine.angularVelocity);
    }

    public void CalculateClutch(float inputShaftVelocity, float engineAngularVel){
        //clutch.angularVelocity = inputShaftVelocity;
        clutch.slip = transmission.gearRatio[transmission.currentGear] == 0 ? 0: (engineAngularVel - inputShaftVelocity) * Mathf.Sign(Mathf.Abs(transmission.gearRatio[transmission.currentGear]));
        //clutch.cLock = 1;
        // Clutch lock works like this - engine RPM - 600 - clutch lock - 0, engine rpm 900 - clutch lock 1
        clutch.cLock = transmission.gearRatio[transmission.currentGear] == 0 ? 0: MapRangeClamped(engineAngularVel * globalParams.radsToRpm, 750, 1100, 0, 1);
        float clutchLastTorque = clutch.slip * clutch.cLock * clutch.stiffness;
        clutchLastTorque = Mathf.Clamp(clutchLastTorque, - clutch.capacity, clutch.capacity);
        clutch.torque = clutchLastTorque + ((clutch.torque - clutchLastTorque) * clutch.damping); // *clutch.damping
    }
    private void TransmissionShfiter()
    {
        //ShiftUp
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (transmission.inGear && transmission.currentGear < transmission.gearRatio.Length - 1)
            {
                transmission.nextGear++;
                StartCoroutine(GearChange(transmission.nextGear, transmission.shiftTime));
            }
            else
            {
                transmission.currentGear = transmission.nextGear;
            }
        }
        //ShiftDown
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (transmission.inGear && transmission.currentGear != 0)
            {
                if (transmission.currentGear != 0)
                {
                    transmission.nextGear--;
                    StartCoroutine(GearChange(transmission.nextGear, transmission.shiftTime));
                }
                else
                {
                    transmission.currentGear = transmission.nextGear;
                }
            }
        }
    }
    //Transmission Start
    public void CalculateTransmissionOutputTorque(float clutchInputTorque){
        transmission.outputTorque = clutchInputTorque * transmission.gearRatio[transmission.currentGear];
    }

    public void CalculateTransmissionInputShaftVelocity(float inputVelocity){
        transmission.inputShaftVelocity = inputVelocity * transmission.gearRatio[transmission.currentGear];
    }

    IEnumerator GearChange(int nextGear, float shiftTime){
        transmission.inGear = false;
        transmission.currentGear = 1;
        yield return new WaitForSeconds(shiftTime);
        transmission.currentGear = nextGear;
        transmission.inGear = true;    
    }
    //Transmission End

    //Diff Start
    public void CalculateDifferentialOutputTorque(float transmissionTorque)
    {
        if (differential.diffLocked)
        {
            differential.angularVelocityL = steering.wheelControllersTFM[2].GetWheelAngularVelocity();
            differential.angularVelocityR = steering.wheelControllersTFM[3].GetWheelAngularVelocity();
            var vel = (differential.angularVelocityL - differential.angularVelocityR) * 0.5f / Time.fixedDeltaTime * steering.wheelControllersTFM[0].GetWheelInertia();
            differential.outputTorqueL = (transmissionTorque * 0.5f * differential.mainGear) - vel;
            differential.outputTorqueR = (transmissionTorque * 0.5f * differential.mainGear) + vel;
            return;
        }
        differential.outputTorqueL =  transmissionTorque * 0.5f * differential.mainGear;
        differential.outputTorqueR = transmissionTorque * 0.5f * differential.mainGear;
    }

    public void CalculateDifferentialInputShfatVelocity(float leftWheelVelocity, float rightWheelVelocity){
        differential.shaftVelocity = (leftWheelVelocity + rightWheelVelocity) * 0.5f * differential.mainGear;
    }
    //Diff end

    public void CalculateBrakes(){
        if (globalParams.usePacejkaMethod)
        {
            brake.torque[0] = -brake.input * brake.bias[0] * brake.maxTorque * brake.torqueCurve.Evaluate(Mathf.Abs((steering.wheelControllers[0].wheelVelocity.angular + steering.wheelControllers[1].wheelVelocity.angular) * 0.5f));
            brake.torque[1] = -brake.input * brake.bias[1] * brake.maxTorque * brake.torqueCurve.Evaluate(Mathf.Abs((steering.wheelControllers[2].wheelVelocity.angular + steering.wheelControllers[3].wheelVelocity.angular) * 0.5f));
        }
        else
        {
            brake.torque[0] = -brake.input * brake.bias[0] * brake.maxTorque * brake.torqueCurve.Evaluate(Mathf.Abs((steering.wheelControllersTFM[0].GetWheelAngularVelocity() + steering.wheelControllersTFM[0].GetWheelAngularVelocity()) * 0.5f));
            brake.torque[1] = -brake.input * brake.bias[1] * brake.maxTorque * brake.torqueCurve.Evaluate(Mathf.Abs((steering.wheelControllersTFM[2].GetWheelAngularVelocity() + steering.wheelControllersTFM[3].GetWheelAngularVelocity()) * 0.5f));

        }
    }

    public void CalculateWheels(float left, float right){
        if (globalParams.usePacejkaMethod)
        {
            steering.wheelControllers[0].PhysicsUpdate(0, brake.torque[0], globalParams.deltaTime);
            steering.wheelControllers[1].PhysicsUpdate(0, brake.torque[0], globalParams.deltaTime);
            steering.wheelControllers[2].PhysicsUpdate(left, brake.torque[1], globalParams.deltaTime);
            steering.wheelControllers[3].PhysicsUpdate(right, brake.torque[1], globalParams.deltaTime);
        }
        else
        {
            steering.wheelControllersTFM[0].PhysicsUpdate(0, brake.torque[0], globalParams.deltaTime);
            steering.wheelControllersTFM[1].PhysicsUpdate(0, brake.torque[0], globalParams.deltaTime);
            steering.wheelControllersTFM[2].PhysicsUpdate(left, brake.torque[1], globalParams.deltaTime);
            steering.wheelControllersTFM[3].PhysicsUpdate(right, brake.torque[1], globalParams.deltaTime);
        }
    }

    //public void CalculateAntirollBar(){
    //    if (antirollBar.enabled)
    //    {
    //        if (globalParams.usePacejkaMethod)
    //        {
    //            for (int i = 0; i < steering.wheelControllers.Length; i++)
    //            {
    //                //Front axis
    //                antirollBar.lengthDifference[0] = steering.wheelControllers[i].wheel.hit == false ? 0 :
    //                (steering.wheelControllers[0].suspension.currentLength - steering.wheelControllers[1].suspension.currentLength) / ((steering.wheelControllers[0].suspension.restLength + steering.wheelControllers[1].suspension.restLength) / 2);
    //                //Rear axis
    //                antirollBar.lengthDifference[1] = steering.wheelControllers[i].wheel.hit == false ? 0 :
    //                (steering.wheelControllers[2].suspension.currentLength - steering.wheelControllers[3].suspension.currentLength) / ((steering.wheelControllers[2].suspension.restLength + steering.wheelControllers[3].suspension.restLength) / 2);
    //            }
    //
    //            antirollBar.force[0] = antirollBar.lengthDifference[0] * antirollBar.stiffnessFront;
    //            antirollBar.force[1] = antirollBar.lengthDifference[1] * antirollBar.stiffnessRear;
    //
    //            //Apply Forces
    //            //Front
    //            if (steering.wheelControllers[0].wheel.hit)
    //            {
    //                steering.wheelControllers[0].ApplyAntirollBar(-antirollBar.force[0]);
    //            }
    //            if (steering.wheelControllers[1].wheel.hit)
    //            {
    //                steering.wheelControllers[1].ApplyAntirollBar(antirollBar.force[0]);
    //            }
    //            //Rear
    //            if (steering.wheelControllers[2].wheel.hit)
    //            {
    //                steering.wheelControllers[2].ApplyAntirollBar(-antirollBar.force[1]);
    //            }
    //            if (steering.wheelControllers[3].wheel.hit)
    //            {
    //                steering.wheelControllers[3].ApplyAntirollBar(antirollBar.force[1]);
    //            }
    //        }
    //    }
    //    else
    //    {
    //        for (int i = 0; i < steering.wheelControllers.Length; i++)
    //        {
    //            //Front axis
    //            antirollBar.lengthDifference[0] = steering.wheelControllersTFM[i].GetWheelHit() == false ? 0 :
    //            (steering.wheelControllersTFM[0].GetSuspensionCurrentLength() - steering.wheelControllersTFM[1].GetSuspensionCurrentLength()) / ((steering.wheelControllersTFM[0].GetSuspensionRestLength() + steering.wheelControllersTFM[1].GetSuspensionRestLength()) / 2);
    //            //Rear axis
    //            antirollBar.lengthDifference[1] = steering.wheelControllersTFM[i].GetWheelHit() == false ? 0 :
    //            (steering.wheelControllersTFM[2].GetSuspensionCurrentLength() - steering.wheelControllersTFM[3].GetSuspensionCurrentLength()) / ((steering.wheelControllersTFM[2].GetSuspensionRestLength() + steering.wheelControllersTFM[3].GetSuspensionRestLength()) / 2);
    //        }
    //
    //        antirollBar.force[0] = antirollBar.lengthDifference[0] * antirollBar.stiffnessFront;
    //        antirollBar.force[1] = antirollBar.lengthDifference[1] * antirollBar.stiffnessRear;
    //
    //        //Apply Forces
    //        //Front
    //        if (steering.wheelControllersTFM[0].GetWheelHit())
    //        {                            
    //            steering.wheelControllersTFM[0].ApplyAntirollBar(-antirollBar.force[0]);
    //        }                           
    //        if (steering.wheelControllersTFM[1].GetWheelHit())
    //        {                            
    //            steering.wheelControllersTFM[1].ApplyAntirollBar(antirollBar.force[0]);
    //        }                            
    //        //Rear                       
    //        if (steering.wheelControllersTFM[2].GetWheelHit())
    //        {                            
    //            steering.wheelControllersTFM[2].ApplyAntirollBar(-antirollBar.force[1]);
    //        }                            
    //        if (steering.wheelControllersTFM[3].GetWheelHit())
    //        {                            
    //            steering.wheelControllersTFM[3].ApplyAntirollBar(antirollBar.force[1]);
    //        }
    //    }
    //}


}
