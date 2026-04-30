using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class BicycleModelCalculator : MonoBehaviour
{

    public GameObject steer, driveGear, leanRight, learnLeft, subLeanRight, subLearnLeft, brake, judgeSwitch, strainGage, judgeSwitch2;//追加2025_11_13
    private SerialCommunication steerSerial, driveGearSerial, leanRightSerial, learnLeftSerial, subLeanRightSerial, subLearnLeftSerial, brakeSerial, judgeSwtchSerial, strainGageSerial, judgeSwitch2Serial;//追加2025_11_13
    [Header("----------------------")]
    public float steerData;
    public float driveGearData;
    public float leanRightData;
    public float leanLeftData;
    public float subLeanRightData;
    public float subLearnLeftData;
    public float brakeData;
    public float judgeSwitchData;
    public float judgeSwitch2Data;//追加2025_11_13
    public float strainGageBendingData;
    public float strainGageTwistData;
    [Header("----------------------")]
    public float steerOfset = 1394;
    public float rightRollBase = 29.0f;
    public float leftRollBase = 1238.0f;
    [Header("----------------------")]
    public float SelfSteerTerm = 0.0f;
    public float rnd1 = 0.0f;
    public float rnd2 = 0.0f;
    [Header("----------------------")]
    public double casterAngle = 73.0;
    public float FrontMass = 150.0f;
    public float inertiaMomentSteer = 2.0f;
    public float l1 = 0.15f;
    public float l2 = 0.10f;
    public float steerDot;
    public float steerDdot;
    public float TireMoment;
    public float FrameMoment;
    [Header("----------------------")]
    public float velocity;
    //public float accelaration;
    public float steerAngle;
    public float steerAngleCorr;
    public float steerCorrectTerm;
    public float rollAngle;
    public float subRollAngle;
    public float SelfSteer = 0;
    public float Trail = 0;

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
    public float SelfSteerGain; // = 0.50f;
    public float TrailGain; // = 0.01f;
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
    [Header("----------------------")]
    public float resetX;
    public float resetY;
    public float resetZ;
    public float resetYaw;

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

    private int steerCorrectMode = 0;

    string[] param_files;
    int param_num = 0;
    string[] gain_files;
    int gain_num = 0;

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
        if (judgeSwitch != null) judgeSwtchSerial = judgeSwitch.GetComponent<SerialCommunication>();
        if (strainGage != null) strainGageSerial = strainGage.GetComponent<SerialCommunication>();
        if (judgeSwitch2 != null) judgeSwitch2Serial = judgeSwitch2.GetComponent<SerialCommunication>();//追加2025_11_13

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

        // 
        yaw = (-90 - this.transform.rotation.eulerAngles.y / 180) * Mathf.PI;

        param_files = Directory.GetFiles(@"Setting\TuningParam", "*.txt");
        gain_files = Directory.GetFiles(@"Setting\SelfSteerGain", "*.txt");

        lastTime = Time.time;
    }

    const float dt = 0.001f;
    int lastDriveData, lastDriveGearData;

    //ここ調整すること
    const int driveCount = 4096;
    const float tireRadius = 0.35f;
    const float steerCoefficient = 0.0008f;
    //const int steerOfset = 1420;
    //const float rightRollBase = 50.0f;
    //const float leftRollBase = 0.1f;
    //const int steerOfset = 1394; // 2023.05.29 ypdate
    //const float rightRollBase = 29.0f;
    //const float leftRollBase = 1238.0f;
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
            if (learnLeftSerial != null) leanLeftData = learnLeftSerial.data;
            if (subLeanRightSerial != null) subLeanRightData = subLeanRightSerial.data;
            if (subLearnLeftSerial != null) subLearnLeftData = subLearnLeftSerial.data;
            if (brakeSerial != null) brakeData = brakeSerial.data;
            if (judgeSwtchSerial != null) judgeSwitchData = judgeSwtchSerial.data;
            if (judgeSwitch2Serial != null) judgeSwitch2Data = judgeSwitch2Serial.data;//追加2025_11_13
            if (strainGageSerial != null)
            {
                if (strainGageSerial.rawData != null)
                {
                    string[] strainGageData = strainGageSerial.rawData.Split(',');
                    if (strainGageData.Length == 2)
                    {
                        strainGageBendingData = float.Parse(strainGageData[0]);
                        strainGageTwistData = float.Parse(strainGageData[1]);
                    }
                    else
                    {
                        strainGageBendingData = 0;
                        strainGageTwistData = 0;
                    }
                }
                else
                {
                    strainGageBendingData = 0;
                    strainGageTwistData = 0;
                }

            }
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

            rollAngle = (float)System.Math.Atan2(((leanRightData - rightRollBase) - (leanLeftData - leftRollBase)) / 2 * rollCoefficient, rollLength);

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

            //この箇所は新たにコメントアウトしました。2025/12/06
            /*
            // 画面を左クリックで初期位置に戻る
            if (Input.GetMouseButtonDown(0))
            {
                // this.transform.position = new Vector3(12, 0, 3);
                // x = 12;
                // y = 3;
                // this.transform.rotation = Quaternion.Euler(0, 90, 0);
                // yaw = 90;
                // this.transform.position = new Vector3(301, 0, 3);
                // x = 301;
                // y = 3;
                // this.transform.rotation = Quaternion.Euler(0, 0, 0);
                // yaw = 0;
                this.transform.position = new Vector3(resetX, resetZ, resetY);
                x = resetX;
                y = resetY;
                this.transform.rotation = Quaternion.Euler(0, resetYaw, 0);
                yaw = resetYaw;
            }
            // エンターキーで初期位置(x軸方向10[m]前)に戻る
            if (Input.GetKeyDown(KeyCode.Return))
            {
                this.transform.position = new Vector3(resetX - 10, resetZ, resetY);
                x = resetX - 10;
                y = resetY;
                this.transform.rotation = Quaternion.Euler(0, resetYaw, 0);
                yaw = resetYaw;
            }
            */

            // センサのオフセット値を更新
            if (Input.GetKeyDown(KeyCode.Z))
            {
                steerOfset = steerData;
                rightRollBase = leanRightData;
                leftRollBase = leanLeftData;
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
        switch (steerCorrectMode)
        {
            case 1: // セルフステア補正有り(定数ゲイン)
                //ロールと速度と定数ゲインでステアを上乗せ
                //SelfSteerTerm = -rollAngle * SelfSteerGain * (float)System.Math.Exp(-velocity);
                //SelfSteerTerm = -rollAngle * SelfSteerGain - TrailGain * System.Math.Sign(steerAngleCorr) * velocity;
                SelfSteerTerm = -rollAngle * SelfSteerGain - TrailGain * steerAngle * velocity;
                SelfSteer = -rollAngle * SelfSteerGain;
                Trail = -TrailGain * steerAngle * velocity;
                steerAngleCorr = steerAngle + SelfSteerTerm;
                steerCorrectTerm = SelfSteerTerm;
                break;

            case 2: // セルフステア補正有り(運動方程式)
                //ハンドル軸回りの動力学を考えてステアを上乗せ
                TireMoment = l2 * 100.0f * rollAngle + l2 * 200.0f * (slipAngle + frontCenter * yawRate / (velocity + 0.2f) - steerAngleCorr);
                FrameMoment = FrontMass * gravitationalAcceleration * l1 * (float)System.Math.Sin(rollAngle);
                steerDdot = -FrameMoment + TireMoment;
                steerDdot /= inertiaMomentSteer;
                steerDot += steerDdot * dt;
                steerAngleCorr = steerAngle + (steerDot * dt);
                steerCorrectTerm = steerDot * dt;
                //yawRateDot = frontCorneringStiffness * steerAngle - (gravitationalAcceleration * mass * mass * centerHight * centerHight / inertiaMomentRoll + frontCamberThrust + rearCamberThrust) * rollAngle
                //slipAngleDot = -(A1 * velocity + A2 / velocity) * slipAngle - (A3 - mass * acceleration) * yawRate + frontCorneringStiffness * steerAngle + A5 * rollAngle;
                break;

            case 3: // 定数ゲイン(速度の二乗で除算)
                if (velocity >= 1)
                {
                    SelfSteerTerm = (-rollAngle * SelfSteerGain - TrailGain * steerAngle) / (velocity * velocity);
                }
                else
                {
                    SelfSteerTerm = (-rollAngle * SelfSteerGain - TrailGain * steerAngle);
                }
                steerAngleCorr = steerAngle + SelfSteerTerm;

                break;

            default: // セルフステア補正無し
                steerAngleCorr = steerAngle;
                steerCorrectTerm = 0;
                break;
        }

        //速度によって振幅を変えるホワイトノイズ(標準正規分布に乗っ取った乱数)をステアに印加
        //rnd1 = Random.value;
        //rnd2 = Random.value;
        //const float SelfSteerGain2 = 0.5f;
        //SelfSteerTerm = SelfSteerGain2 * (float)System.Math.Sqrt(-2.0 * System.Math.Log(rnd1)) * (float)System.Math.Cos(2.0 * System.Math.PI * rnd2) * (float)System.Math.Exp(-velocity);
        //steerAngle += SelfSteerTerm;


        //slipAngleDot = -(A1 * velocity + A2 / velocity) * yawRate- (A3) * slipAngle + frontCorneringStiffness * steerAngle - A5 * rollAngle;
        //slipAngleDot /= A1* velocity;

        //slipAngleDot = -(C1 * velocity + A2 / (velocity + 0.2f)) * yawRate - (A3) * slipAngle + frontCorneringStiffness * steerAngle - C5 * rollAngle;  //k
        slipAngleDot = -(C1 * velocity + A2 / (velocity + 0.2f)) * yawRate - (A3) * slipAngle + frontCorneringStiffness * steerAngleCorr - C5 * rollAngle;  //k
        slipAngleDot /= (C1 * (velocity + 0.2f)); //k
        //yawRateDot = -B2 / velocity * yawRate - A2 * slipAngle + B4 * steerAngle + B5 * rollAngle;
        //yawRateDot = -B2 / (velocity + 0.2f) * yawRate - A2 * slipAngle + B4 * steerAngle - B5 * rollAngle; //k
        yawRateDot = -B2 / (velocity + 0.2f) * yawRate - A2 * slipAngle + B4 * steerAngleCorr - B5 * rollAngle; //k
        yawRateDot /= inertiaMomentYaw;

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

    public void LoadNextParams()
    {
        if (param_files != null)
        {
            param_num = param_num + 1;
            if (param_num >= param_files.Length)
            {
                param_num = 0;
            }
            LoadParams(param_files[param_num]);
        }
    }

    public void LoadNextGains()
    {
        if (gain_files != null)
        {
            gain_num = gain_num + 1;
            if (gain_num >= gain_files.Length)
            {
                gain_num = 0;
            }
            LoadParams(gain_files[gain_num]);
        }
    }

    public void ReloadParams()
    {
        if (param_files != null)
        {
            LoadParams(param_files[param_num]);
        }

        if (gain_files != null)
        {
            LoadParams(gain_files[gain_num]);
        }
    }

    private void LoadParams(string file)
    {
        if (param_files != null)
        {
            if (File.Exists(file))
            {
                StreamReader sr = new StreamReader(file);

                while (sr.Peek() != -1)
                {
                    string line = sr.ReadLine();
                    string[] str_split = line.Split(':');

                    switch (str_split[0])
                    {
                        case "Mass":
                            mass = float.Parse(str_split[1]);
                            break;
                        case "Ix":
                            inertiaMomentRoll = float.Parse(str_split[1]);
                            break;
                        case "Iz":
                            inertiaMomentYaw = float.Parse(str_split[1]);
                            break;
                        case "Kf":
                            frontCorneringStiffness = float.Parse(str_split[1]);
                            break;
                        case "Kr":
                            rearCorneringStiffness = float.Parse(str_split[1]);
                            break;
                        case "Kcf":
                            frontCamberThrust = float.Parse(str_split[1]);
                            break;
                        case "Kcr":
                            rearCamberThrust = float.Parse(str_split[1]);
                            break;
                        case "SelfSteerGain":
                            SelfSteerGain = float.Parse(str_split[1]);
                            break;
                        case "TrailGain":
                            TrailGain = float.Parse(str_split[1]);
                            break;
                        default:
                            break;
                    }
                }
                sr.Close();

                ReCalcConstant();
            }
        }
    }

    /*
     * パラメータをファイルから読み込んだ際に定数を再計算
     */
    private void ReCalcConstant()
    {
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
    }

    public int getSteerCorrectMode()
    {
        return steerCorrectMode;
    }
    public void setSteerCorrectMode(int mode)
    {
        steerCorrectMode = mode;
    }

    public string getCurrentParam()
    {
        string current_param = "";
        if (param_files != null)
        {
            current_param = param_files[param_num];
        }

        return current_param;
    }

    public string getCurrentGain()
    {
        string current_gain = "";
        if (gain_files != null)
        {
            current_gain = gain_files[gain_num];
        }

        return current_gain;
    }
}
