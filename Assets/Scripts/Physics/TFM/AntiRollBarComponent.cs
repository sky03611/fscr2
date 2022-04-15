using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntiRollBarComponent : MonoBehaviour
{
    [SerializeField] private bool antirollBarEnabled = true;
    [SerializeField] private float stiffnessFront = 5000f;
    [SerializeField] private float stiffnessRear = 5000f;
    private WheelControllerTFM[] wheelControllers;
    private float[] lengthDifference = new float[2];
    private float[] force = new float[2];


    public void InitializeAntirollBar(WheelControllerTFM[] _wheels)
    {
        wheelControllers = _wheels;
    }
    public void CalculateAntirollBar(){
        if (antirollBarEnabled)
        {

            for (int i = 0; i < wheelControllers.Length; i++)
            {
                //Front axis
                lengthDifference[0] = wheelControllers[i].GetWheelHit() == false ? 0 :
                (wheelControllers[0].GetSuspensionCurrentLength() - wheelControllers[1].GetSuspensionCurrentLength()) / ((wheelControllers[0].GetSuspensionRestLength() + wheelControllers[1].GetSuspensionRestLength()) / 2);
                //Rear axis
                lengthDifference[1] = wheelControllers[i].GetWheelHit() == false ? 0 :
                (wheelControllers[2].GetSuspensionCurrentLength() - wheelControllers[3].GetSuspensionCurrentLength()) / ((wheelControllers[2].GetSuspensionRestLength() + wheelControllers[3].GetSuspensionRestLength()) / 2);
            }
    
            force[0] = lengthDifference[0] * stiffnessFront;
            force[1] = lengthDifference[1] * stiffnessRear;
    
            //Apply Forces
            //Front
            if (wheelControllers[0].GetWheelHit())
            {
                wheelControllers[0].ApplyAntirollBar(-force[0]);
            }
            if (wheelControllers[1].GetWheelHit())
            {
                wheelControllers[1].ApplyAntirollBar(force[0]);
            }
            //Rear
            if (wheelControllers[2].GetWheelHit())
            {
                wheelControllers[2].ApplyAntirollBar(-force[1]);
            }
            if (wheelControllers[3].GetWheelHit())
            {
                wheelControllers[3].ApplyAntirollBar(force[1]);
            }
            
        }
    }
}
