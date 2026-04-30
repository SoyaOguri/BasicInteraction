using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BicycleModelYawrate : MonoBehaviour
{

    public GameObject steer, driveGear, leanRight, learnLeft, subLeanRight, subLearnLeft, brake;
    private SerialCommunication steerSerial, driveGearSerial, leanRightSerial, learnLeftSerial, subLeanRightSerial, subLearnLeftSerial, brakeSerial;
    [Header("----------------------")]
    public float steerData;
    public float driveGearData;
    public float leanRightData;
    public float learnLeftData;
    public float subLeanRightData;
    public float subLearnLeftData;
    public float brakeData;
    [Header("----------------------")]
    public float velocity;
    //public float accelaration;
    public float steerAngle;
    public float rollAngle;
    public float subRollAngle;

    [Header("----------------------")]
    public float yaw;
    public float yawRate;
    public float slipAngle;
    public float yawRateDot;
    public float slipAngleDot;
    public float x;
    public float y;
    public float Roll;
    [Header("----------------------")]
    public float mass;//= 70;
    public float frontCenter;// = 0.68f;
    public float rearCenter;// = 0.32f;
    public float centerHight;// = 0.586f;
    public float inertiaMomentYaw;// = 3;
    public float inertiaMomentRoll;// = 3;
    public float frontCorneringStiffness;// = 1500;
    public float rearCorneringStiffness;// = 2100;
    public float frontCamberThrust;// = 333;
    public float rearCamberThrust;// = 466;
    [Header("----------------------")]
    [SerializeField] float A1;
    [SerializeField] float C1;
    [SerializeField] float A5;
    [SerializeField] float C5;
    [SerializeField] float A2;
    [SerializeField] float A3;
    [SerializeField] float B2;
    [SerializeField] float B4;
    [SerializeField] float B5;
    [Header("----------------------")]
    public bool useExternalData;
    public bool dontInputUpdate;

    const float gravitationalAcceleration = 9.798f;

    //float A1, A2, A3, A5;
    //float B2, B4, B5;
    //float C1, C5; 

    float lastTime = 0;

    float[] rawVels;
    float[] rawRols;
    int countVels;
    int countRols;

    float[] rightSensers;
    float[] leftSensers;
    int countRight, countLeft;
    int rightFilCount = 0;
    int leftFilCount = 0;
    float rightRoll, leftRoll;

    // Start is called before the first frame update
    void Start()
    {
        //Register serial ports
        if (steer != null) steerSerial = steer.GetComponent<SerialCommunication>();
        if (driveGear != null) driveGearSerial = driveGear.GetComponent<SerialCommunication>();
        if (leanRight != null) leanRightSerial = leanRight.GetComponent<SerialCommunication>();
        if (learnLeft != null) learnLeftSerial = learnLeft.GetComponent<SerialCommunication>();
        if (subLeanRight != null) subLeanRightSerial = subLeanRight.GetComponent<SerialCommunication>();
        if (subLearnLeft != null) subLearnLeftSerial = subLearnLeft.GetComponent<SerialCommunication>();
        if (brake != null) brakeSerial = brake.GetComponent<SerialCommunication>();

        //A1*V,(A1*v+A2/V),A3-mass*a,frontCorneringStiffness,A5

        A1 = mass * (1 + mass * centerHight * centerHight / inertiaMomentRoll);
        A2 = (frontCorneringStiffness * frontCenter - rearCorneringStiffness * rearCenter);
        A3 = frontCorneringStiffness + rearCorneringStiffness;
        A5 = (gravitationalAcceleration * mass * mass * centerHight * centerHight / inertiaMomentRoll + frontCamberThrust + rearCamberThrust);

        //inertiaMomentYaw,B2/V,A2,B4,B5

        //B2 = (frontCorneringStiffness * frontCenter * frontCenter - rearCorneringStiffness * rearCenter * rearCenter);
        B2 = (frontCorneringStiffness * frontCenter * frontCenter + rearCorneringStiffness * rearCenter * rearCenter); //k
        B4 = frontCorneringStiffness * frontCenter;
        B5 = frontCamberThrust * frontCenter - rearCamberThrust * rearCenter;

        C1 = mass * (1 - mass * centerHight * centerHight / (inertiaMomentRoll + mass * centerHight * centerHight)); //k
        C5 = (-gravitationalAcceleration * mass * mass * centerHight * centerHight / (inertiaMomentRoll + mass * centerHight * centerHight) + frontCamberThrust + rearCamberThrust); //k

        x = this.transform.localPosition.x;
        y = this.transform.localPosition.z;


        rawVels = new float[10];
        countVels = rawVels.Length;
        for (int i = 0; i < countVels; i++)
        {
            rawVels[i] = 0;
        }

        rawRols = new float[20];
        countRols = rawRols.Length;
        for (int i = 0; i < countRols; i++)
        {
            rawRols[i] = 0;
        }

        rightSensers = new float[20];
        countRight = rightSensers.Length;
        for (int i = 0; i < countRight; i++)
        {
            rightSensers[i] = 0;
        }

        leftSensers = new float[20];
        countLeft = leftSensers.Length;
        for (int i = 0; i < countLeft; i++)
        {
            leftSensers[i] = 0;
        }


        yaw = (-90 - this.transform.rotation.eulerAngles.y / 180) * Mathf.PI;
    }

    const float dt = 0.001f;
    int lastDriveData, lastDriveGearData;

    //ここ調整すること
    const int driveCount = 4096;
    const float tireRadius = 0.35f;
    const float steerCoefficient = 0.0008f;
    const int steerOfset = 1420;
    const float rightRollBase = 50.0f;
    const float leftRollBase = 0.1f;
    const float subRightRollBase = 121.0f;
    const float subLeftRollBase = 105.1f;
    const double rollLength = 1;
    const float rollCoefficient = -0.00009f;
    const float subRollCoefficient = -0.01f;

    float rawVelocity;
    int velCount = 0;

    float rawRoll;
    int rolCount = 0;
    float stopTime = 0;



    // Update is called once per frame
    void Update()
    {
        if (!dontInputUpdate)
        {
            //Get Data
            if (steerSerial != null) steerData = steerSerial.data;
            if (driveGearSerial != null) driveGearData = driveGearSerial.data;
            if (leanRightSerial != null) leanRightData = leanRightSerial.data;
            if (learnLeftSerial != null) learnLeftData = learnLeftSerial.data;
            if (subLeanRightSerial != null) subLeanRightData = subLeanRightSerial.data;
            if (subLearnLeftSerial != null) subLearnLeftData = subLearnLeftSerial.data;
            if (brakeSerial != null) brakeData = brakeSerial.data;
        }

        if (!useExternalData)
        {

            steerAngle = (steerData - steerOfset) * -steerCoefficient;

            ////Drive
            //if (driveGearData >= lastDriveGearData)
            //{
            //    driveData += driveGearData - lastDriveGearData;
            //}else
            //{
            //    driveData += driveGearData - lastDriveGearData + driveCount;
            //}

            rawVelocity = (driveGearData / (7.125f)) / driveCount * 2 * (float)System.Math.PI * tireRadius;
            rawVels[velCount] = rawVelocity;
            velocity = 0;
            for (int i = 0; i < countVels; i++)
            {
                velocity += rawVels[i];
                ;
            }
            velocity /= countVels;
            velCount++;
            if (countVels <= velCount)
            {
                velCount = 0;
            }

            //velocity = (driveData - lastDriveData) / Time.deltaTime * 2 * (float)System.Math.PI * tireRadius;
            //lastDriveData = driveData;
            //lastDriveGearData = driveGearData;

            rollAngle = (float)System.Math.Atan2(((leanRightData - rightRollBase) - (learnLeftData - leftRollBase)) / 2 * rollCoefficient, rollLength);

            rightSensers[rightFilCount] = subLeanRightData;
            rightRoll = 0;
            for (int i = 0; i < countRight; i++)
            {
                rightRoll += rightSensers[i];
            }
            rightRoll /= countRight;
            rightFilCount++;
            if (countRight <= rightFilCount)
            {
                rightFilCount = 0;
            }


            leftSensers[leftFilCount] = subLearnLeftData;
            leftRoll = 0;
            for (int i = 0; i < countLeft; i++)
            {
                leftRoll += leftSensers[i];
            }
            leftRoll /= countLeft;
            leftFilCount++;
            if (countLeft <= leftFilCount)
            {
                leftFilCount = 0;
            }

            //Roll
            rawRoll = (float)System.Math.Atan2(((rightRoll - subRightRollBase) - (leftRoll - subLeftRollBase)) / 2 * subRollCoefficient, rollLength);
            rawRols[rolCount] = rawRoll;
            subRollAngle = 0;
            for (int i = 0; i < countRols; i++)
            {
                subRollAngle += rawRols[i];
            }
            subRollAngle /= countRols;
            rolCount++;
            if (countRols <= rolCount)
            {
                rolCount = 0;
            }

            if (Input.GetMouseButtonDown(0))
            {
                this.transform.position = new Vector3(12, 0, 3);
                x = 12;
                y = 3;
                //this.transform.position = new Vector3(301, 0, 3);
                //x = 301;
                //y = 3;
                this.transform.rotation = Quaternion.Euler(0, 90, 0);
                yaw = 90;
            }

            if (velocity <= 0.08)
            {
                stopTime += Time.deltaTime;
                return;
            }

            lastTime += stopTime;
            stopTime = 0;

            while (lastTime < Time.time)
            {
                UpdateState(dt);
                lastTime += dt;
            }


        }

        this.transform.localPosition = new Vector3(x, 0, y);
        this.transform.localRotation = Quaternion.Euler(0, -(float)(yaw * 180 / System.Math.PI), 0);
        //float pitch = this.transform.rotation.z;
        //this.transform.localRotation = Quaternion.Euler((float)(  rollAngle * 180 / System.Math.PI), -(float)(yaw * 180 / System.Math.PI), (float)(pitch * 180 / System.Math.PI));



    }

    const float slipAngleMax = 0.05f;

    void UpdateState(float dt)
    {
        //yawRateDot = frontCorneringStiffness * steerAngle - (gravitationalAcceleration * mass * mass * centerHight * centerHight / inertiaMomentRoll + frontCamberThrust + rearCamberThrust) * rollAngle
        //slipAngleDot = -(A1 * velocity + A2 / velocity) * slipAngle - (A3 - mass * acceleration) * yawRate + frontCorneringStiffness * steerAngle + A5 * rollAngle;

        //slipAngleDot = -(A1 * velocity + A2 / velocity) * yawRate- (A3) * slipAngle + frontCorneringStiffness * steerAngle - A5 * rollAngle;
        //slipAngleDot /= A1* velocity;

        slipAngleDot = -(C1 * velocity + A2 / (velocity + 0.2f)) * yawRate - (A3) * slipAngle + frontCorneringStiffness * steerAngle - C5 * rollAngle;  //k
        slipAngleDot /= (C1 * (velocity + 0.2f)); //k
        //yawRateDot = -B2 / velocity * yawRate - A2 * slipAngle + B4 * steerAngle + B5 * rollAngle;
        yawRateDot = -B2 / (velocity + 0.2f) * yawRate - A2 * slipAngle + B4 * steerAngle - B5 * rollAngle; //k
        yawRateDot /= inertiaMomentYaw;

        //yawratedotのふるまいを変更する(ゆっくり変化)




        //yawratedotのふるまいを変更する(急激変化)

        float YawRateScale = 3.0f;
        yawRateDot = yawRateDot * YawRateScale;


        slipAngle += slipAngleDot * dt;

        if (slipAngle < -slipAngleMax)
            slipAngle = -slipAngleMax;
        else if (slipAngle > slipAngleMax)
            slipAngle = slipAngleMax;

        yawRate += yawRateDot * dt;
        yaw += yawRate * dt;

        x += (float)System.Math.Cos(yaw) * velocity * dt;
        y += (float)System.Math.Sin(yaw) * velocity * dt;


    }
}
