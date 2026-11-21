using UnityEngine;

public class SideCarForward : MonoBehaviour
{
    [Header("移動設定")]
    public float speed = 7.0f;              // 進行速度 (7m/s)
    public float startThreshold = 0.3f;     // 自転車の検知しきい値
    public GameObject mainBicycle;          // 検知する自転車

    [Header("ブレーキ設定")]
    public float safeDistance = 8.0f;      // ブレーキを踏み始める距離
    [Tooltip("ブレーキの強さ。Lerpを使用するため3.0〜5.0前後がリアルです")]
    public float brakeDeceleration = 4.0f;  // ★数値を小さくし、計算式を変えました

    [Header("出現頻度の設定")]
    public int appearInterval = 2;

    [Header("Audio Settings")]
    public float minPitch = 0.8f;
    public float maxPitch = 2.0f;
    public float maxSpeedForSound = 20.0f;
    public AudioClip brakeClip;
    [Tooltip("ブレーキ音の大きさ（1.0が標準。2.0や3.0にするとエンジン音より目立ちます）")]
    public float brakeVolume = 1.5f;        // ★追加：ブレーキ音の音量調整

    // --- プライベート変数 ---
    private BicycleModelCalculator mainCalc;
    private bool canMove = false;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private int trialCount = 1;
    private bool willAppear = true;

    private float currentSpeed;
    private AudioSource engineAudioSource;
    private bool hasPlayedBrakeSound = false;

    void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        currentSpeed = speed;

        engineAudioSource = GetComponent<AudioSource>();
        if (engineAudioSource != null)
        {
            engineAudioSource.loop = true;
            engineAudioSource.spatialBlend = 1.0f;
            if (!engineAudioSource.isPlaying) engineAudioSource.Play();
        }

        if (mainBicycle != null)
        {
            mainCalc = mainBicycle.GetComponent<BicycleModelCalculator>();
            if (mainCalc == null) mainCalc = mainBicycle.GetComponentInChildren<BicycleModelCalculator>();
        }

        SetupTrial();
    }

    void Update()
    {
        // --- 1. エンターキーでリセット処理 ---
        if (Input.GetKeyDown(KeyCode.Return))
        {
            ResetCar();
        }

        // --- 2. 進入しない回（お休み）の処理 ---
        if (!willAppear)
        {
            if (engineAudioSource != null) engineAudioSource.pitch = minPitch;
            return;
        }

        // --- 3. 発進判定 ---
        float mainSpeed = (mainCalc != null) ? mainCalc.velocity : 0f;

        if (!canMove)
        {
            if (mainSpeed > startThreshold) canMove = true;
            if (engineAudioSource != null) engineAudioSource.pitch = minPitch;
            return;
        }

        // --- 4. 衝突回避（急ブレーキ）判定 ---
        if (mainBicycle != null)
        {
            float distance = Vector3.Distance(transform.position, mainBicycle.transform.position);

            if (distance < safeDistance)
            {
                // ★改良：Mathf.Lerp を使うことで、最初は強く減速し、停止間際はジワッと止まる「リアルな車の挙動」になります
                currentSpeed = Mathf.Lerp(currentSpeed, 0f, brakeDeceleration * Time.deltaTime);

                // ブレーキ音を鳴らす（音量を倍率で指定）
                if (!hasPlayedBrakeSound && currentSpeed > 1.0f && brakeClip != null && engineAudioSource != null)
                {
                    engineAudioSource.PlayOneShot(brakeClip, brakeVolume); // ★ここで大音量化
                    hasPlayedBrakeSound = true;
                }
            }
            else
            {
                // 安全な距離なら、元のスピードまでスムーズに加速して戻る
                currentSpeed = Mathf.Lerp(currentSpeed, speed, (brakeDeceleration / 2f) * Time.deltaTime);

                if (currentSpeed >= speed * 0.9f)
                {
                    hasPlayedBrakeSound = false;
                }
            }
        }

        // --- 5. 移動ロジック (Z軸正の向きへ) ---
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime, Space.World);

        // --- 6. エンジン音のピッチ変更 ---
        if (engineAudioSource != null)
        {
            float pitch = Mathf.Lerp(minPitch, maxPitch, currentSpeed / maxSpeedForSound);
            engineAudioSource.pitch = pitch;
        }
    }

    // --- 自動車のリセットと次回の準備 ---
    void ResetCar()
    {
        trialCount++;
        SetupTrial();

        canMove = false;
        currentSpeed = speed;
        hasPlayedBrakeSound = false;

        if (willAppear)
        {
            transform.position = initialPosition;
        }
        transform.rotation = initialRotation;
    }

    // --- 出現するかどうかの判定と地下ワープ ---
    void SetupTrial()
    {
        if (appearInterval <= 1)
        {
            willAppear = true;
        }
        else
        {
            willAppear = (trialCount % appearInterval == 1);
        }

        if (!willAppear)
        {
            transform.position = new Vector3(initialPosition.x, initialPosition.y - 100f, initialPosition.z);
        }
    }
}