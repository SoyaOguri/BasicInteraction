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
    [Tooltip("起動して、センサの値が届き始めてから Auto Zero Delay 秒たったら、そのときのハンドルと左右の傾きを 0 にする（Z キーと同じ）。" +
             "起動するときは、誰も乗らずに自転車をまっすぐ立て、ハンドルもまっすぐにしておく")]
    public bool autoZeroSensorsOnStart = true;
    public float autoZeroDelay = 3.0f;
    [Tooltip("センサの 0（ハンドル・左右の傾き）を Setting フォルダに残し、同じ日にもう一度起動したときはそれを使う（起動時の自動 0 合わせはしない）。" +
             "途中から再開するときは人が乗ったまま起動することが多く、そのとき 0 を取り直すと左右に傾いた 0 になってしまうため")]
    public bool rememberSensorZero = true;
    public string sensorZeroFile = @"Setting\bicycle_sensor_zero.txt";
    [SerializeField] string sensorZeroState = "";
    bool sensorsZeroed = false;
    float firstSensorTime = -1f;

    [Header("走り出しはまっすぐ進む")]
    [Tooltip("スタート位置から走り出して、この距離 [m] を進むまでは、向きを変えずにまっすぐ進ませる（0 で無効）。" +
             "傾きやハンドルの値は記録に残るが、向きには使わない")]
    public float straightStartDistance = 10f;
    bool straightLock;
    float lockX, lockY, lockYaw;

    // 走り出しの自動の速さ（Exp2bTrialManager がカウントダウンの終わりに StartAutoSpeed で始める）
    //   決めた秒数のあいだ、速さを「こいだ速さ」と「決めた速さ」の大きいほうにする（止まった状態から走り出さないように）
    float autoSpeed, autoSpeedUntil = -1f;
    public bool AutoSpeedActive { get { return Time.time < autoSpeedUntil; } }

    public void StartAutoSpeed(float speed, float seconds)
    {
        autoSpeed = speed;
        autoSpeedUntil = speed > 0f && seconds > 0f ? Time.time + seconds : -1f;
    }

    [Tooltip("まっすぐ進ませている間（走り出しの straightStartDistance m）の左右の傾きの平均を、まっすぐ区間が終わるときに 0 にする" +
             "（試行ごと。ハンドルの 0 は変えない）。起動したときの状態や、乗った人の座り方で傾きの 0 がずれ、" +
             "まっすぐ区間のあとにずっと左右に曲がってしまうのを防ぐ")]
    public bool zeroRollWhileStraight = true;
    [Tooltip("走り出してから何 m 進んだところから傾きを集めるか（こぎ出しの揺れを外す）")]
    public float rollZeroFromDistance = 3f;
    [Tooltip("同じまっすぐ区間のハンドルの平均を、ハンドルの 0 にする（試行ごと）。起動時に合わせたハンドルの 0 がずれていて、" +
             "まっすぐ区間のあとに左右へ流れていくのを防ぐ（まっすぐ区間ではハンドルを切っても曲がらないので、乗った人は前を向いてまっすぐ持っている）")]
    public bool zeroSteerWhileStraight = true;
    double rollSumR, rollSumL, steerSum;
    int rollSamples;
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

        LoadSensorZero();
        BeginStraightStart();
    }

    // スタート位置にいる今の向きのまま、straightStartDistance だけまっすぐ進ませる
    void BeginStraightStart()
    {
        straightLock = straightStartDistance > 0f;
        lockX = x;
        lockY = y;
        lockYaw = yaw;
        rollSumR = rollSumL = steerSum = 0;
        rollSamples = 0;
    }

    // まっすぐ区間の終わりに、その間の傾きの平均を 0 にする
    void FinishStraightRollZero()
    {
        if (zeroSteerWhileStraight && rollSamples >= 100)
        {
            float st = (float)(steerSum / rollSamples);
            float beforeSteer = (st - steerOfset) * -steerCoefficient;
            steerOfset = st;
            Debug.Log(string.Format("[Bicycle] まっすぐ区間でハンドルの 0 を合わせ直し（ずれ {0:F4} rad）：ハンドル → {1}", beforeSteer, st));
        }
        if (!zeroRollWhileStraight) return;
        if (rollSamples < 100)
        {
            Debug.LogWarning("[Bicycle] まっすぐ区間の傾きが少なすぎるので、傾きの 0 は合わせ直しません");
            return;
        }
        float r = (float)(rollSumR / rollSamples), l = (float)(rollSumL / rollSamples);
        float before = (float)System.Math.Atan2(((r - rightRollBase) - (l - leftRollBase)) / 2 * rollCoefficient, rollLength);
        rightRollBase = r;
        leftRollBase = l;
        rollAngle = (float)System.Math.Atan2(((leanRightData - rightRollBase) - (leanLeftData - leftRollBase)) / 2 * rollCoefficient, rollLength);
        sensorZeroState = string.Format("まっすぐ区間で傾きの 0 を合わせ直し（ずれ {0:F3} rad）", before);
        Debug.Log(string.Format("[Bicycle] {0}：右の傾き → {1}、左の傾き → {2}", sensorZeroState, r, l));
    }

    // 向きを固定する（傾き・ハンドルで曲がらない）。決めた距離を進んだら解除する
    void ApplyStraightStart()
    {
        if (!straightLock) return;
        yaw = lockYaw;
        yawRate = 0;
        slipAngle = 0;
        steerDot = 0;
        float dx = x - lockX, dy = y - lockY;
        float d2 = dx * dx + dy * dy;
        if (d2 >= rollZeroFromDistance * rollZeroFromDistance)
        {
            rollSumR += leanRightData;
            rollSumL += leanLeftData;
            steerSum += steerData;
            rollSamples++;
        }
        if (d2 >= straightStartDistance * straightStartDistance)
        {
            straightLock = false;
            FinishStraightRollZero();
        }
    }

    // 同じ日に残したセンサの 0 があれば使う
    void LoadSensorZero()
    {
        if (!rememberSensorZero || !File.Exists(sensorZeroFile)) return;
        try
        {
            var kv = new Dictionary<string, string>();
            foreach (string line in File.ReadAllLines(sensorZeroFile))
            {
                int i = line.IndexOf('=');
                if (i > 0) kv[line.Substring(0, i).Trim()] = line.Substring(i + 1).Trim();
            }
            string today = System.DateTime.Now.ToString("yyyy-MM-dd");
            if (!kv.ContainsKey("date") || kv["date"] != today)
            {
                Debug.Log("[Bicycle] 残っているセンサの 0 は今日のものではないので使いません（起動時に自動で合わせます）");
                return;
            }
            var f = System.Globalization.CultureInfo.InvariantCulture;
            steerOfset = float.Parse(kv["steer"], f);
            rightRollBase = float.Parse(kv["right"], f);
            leftRollBase = float.Parse(kv["left"], f);
            sensorsZeroed = true;
            sensorZeroState = "今日の " + kv["time"] + " に合わせた 0 を使用";
            Debug.Log(string.Format("[Bicycle] {0}：ハンドル {1}、右の傾き {2}、左の傾き {3}",
                sensorZeroState, steerOfset, rightRollBase, leftRollBase));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Bicycle] センサの 0 のファイルを読めませんでした: " + e.Message);
        }
    }

    void SaveSensorZero()
    {
        if (!rememberSensorZero) return;
        try
        {
            var f = System.Globalization.CultureInfo.InvariantCulture;
            var now = System.DateTime.Now;
            File.WriteAllText(sensorZeroFile,
                "# 自転車のセンサの 0（BicycleModelCalculator が書く。同じ日に起動し直したときに使う。消すと次の起動で自動で合わせ直す）\r\n" +
                "date = " + now.ToString("yyyy-MM-dd") + "\r\n" +
                "time = " + now.ToString("HH:mm:ss") + "\r\n" +
                "steer = " + steerOfset.ToString("R", f) + "\r\n" +
                "right = " + rightRollBase.ToString("R", f) + "\r\n" +
                "left = " + leftRollBase.ToString("R", f) + "\r\n");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Bicycle] センサの 0 を保存できませんでした: " + e.Message);
        }
    }

    const float dt = 0.001f;
    int lastDriveData, lastDriveGearData;

    //ここ調整すること
    const int driveCount = 4096;
    const float tireRadius = 0.35f;
    const float steerCoefficient = 0.0004f;//202610_0.0008から修正
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

    // ハンドルと左右の傾きのセンサの 0 を、今の値に合わせる（自転車をまっすぐ立て、ハンドルをまっすぐにした状態で）
    public void ZeroSensors(string why)
    {
        Debug.Log(string.Format("[Bicycle] センサの 0 合わせ（{0}）：ハンドル {1} → {2}、右の傾き {3} → {4}、左の傾き {5} → {6}",
            why, steerOfset, steerData, rightRollBase, leanRightData, leftRollBase, leanLeftData));
        steerOfset = steerData;
        rightRollBase = leanRightData;
        leftRollBase = leanLeftData;
        sensorsZeroed = true;
        sensorZeroState = why + "で 0 合わせ済み（" + System.DateTime.Now.ToString("HH:mm:ss") + "）";
        SaveSensorZero();
    }

    // 初期位置に戻す（Enter キーと、走行後の評定が終わったとき〈Exp2bTrialManager〉に呼ぶ）
    public void ResetToStart()
    {
        this.transform.position = new Vector3(resetX, resetZ, resetY);
        x = resetX;
        y = resetY;
        this.transform.rotation = Quaternion.Euler(0, resetYaw, 0);
        yaw = resetYaw;

        // 💡念のため速度やスリップ角なども0にリセットしておくと完璧です
        velocity = 0;
        steerAngle = 0;
        rollAngle = 0;
        yawRate = 0;
        slipAngle = 0;
        steerDot = 0;
        autoSpeedUntil = -1f;
        BeginStraightStart();
    }

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
            // 走り出しの自動の速さ（こいだ速さのほうが速ければ、そちら）
            if (AutoSpeedActive && velocity < autoSpeed) velocity = autoSpeed;

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

            // エンターキーで初期位置に戻る（左クリックだと誤爆しやすいので、エンターキーだけに絞るのがオススメです）
            if (Input.GetKeyDown(KeyCode.Return))
            {
                ResetToStart();
            }

            // センサのオフセット値を更新
            if (Input.GetKeyDown(KeyCode.Z))
            {
                ZeroSensors("Z キー");
            }
            // 起動直後に 1 回だけ自動で合わせる（センサの値が届き始めてから autoZeroDelay 秒後、止まっているとき）
            if (autoZeroSensorsOnStart && !sensorsZeroed)
            {
                bool hasData = steerData != 0 || leanRightData != 0 || leanLeftData != 0;
                if (hasData && firstSensorTime < 0f) firstSensorTime = Time.time;
                if (hasData && Time.time - firstSensorTime >= autoZeroDelay && velocity < 0.05f)
                    ZeroSensors("起動時に自動");
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
                ApplyStraightStart();
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
