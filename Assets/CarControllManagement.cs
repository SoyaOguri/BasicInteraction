using UnityEngine;

public class CarControllManagement : MonoBehaviour
{
    [Header("移動設定")]
    public float speed = 7.0f;
    public float startThreshold = 0.3f;
    public GameObject mainBicycle;

    [Header("ブレーキ設定")]
    public float safeDistance = 8.0f;
    public float brakeDeceleration = 4.0f;

    [Header("交差点の中で止まらない（実験2-b。Exp2bTrialManager が SetCrossingZone で範囲を渡す）")]
    [Tooltip("自転車の走る道路（交差点）の範囲 [ワールド z]。車の前の端がこの手前で止まれないときは、ブレーキをかけずに通り抜ける")]
    public bool useCrossingZone = false;
    public float crossingMinZ, crossingMaxZ;
    [Tooltip("確認用：今の状態")]
    [SerializeField] string crossingState = "";

    [Header("Audio Settings")]
    public float minPitch = 0.8f;
    public float maxPitch = 2.0f;
    public float maxSpeedForSound = 20.0f;
    public AudioClip brakeClip;
    public float brakeVolume = 1.5f;

    // --- プライベート変数 ---
    private BicycleModelCalculator mainCalc;
    private bool canMove = false;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private float currentSpeed;
    private AudioSource engineAudioSource;
    private bool hasPlayedBrakeSound = false;

    private bool isUnderground = false;
    private bool committed = false;   // 交差点に入った（または手前で止まれない）→ もうブレーキをかけない
    private bool braking = false;
    private float frontOffset = 2.3f; // 車の中心から前の端までの長さ [m]（Awake で見た目の大きさから決める）

    // ★追加：進む方向を決める変数（Rightなら1、Leftなら-1）
    private float directionMultiplier = 1f;

    void Awake()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        currentSpeed = speed;

        // ★追加：自分の名前に "Left" が含まれていたらZ方向負（-1）、それ以外はZ方向正（1）にする
        if (gameObject.name.Contains("Left") || gameObject.name.Contains("left"))
        {
            directionMultiplier = -1f;
        }
        else
        {
            directionMultiplier = 1f;
        }

        // 車の前の端までの長さ（見た目の大きさから）
        var rs = GetComponentsInChildren<Renderer>();
        if (rs.Length > 0)
        {
            Bounds bb = rs[0].bounds;
            foreach (var r in rs) bb.Encapsulate(r.bounds);
            float f = directionMultiplier > 0 ? bb.max.z - transform.position.z : transform.position.z - bb.min.z;
            if (f > 0.5f && f < 6f) frontOffset = f;
        }

        engineAudioSource = GetComponent<AudioSource>();
        if (engineAudioSource != null)
        {
            engineAudioSource.loop = true;
            engineAudioSource.spatialBlend = 1.0f;
        }

        if (mainBicycle != null)
        {
            mainCalc = mainBicycle.GetComponent<BicycleModelCalculator>();
            if (mainCalc == null) mainCalc = mainBicycle.GetComponentInChildren<BicycleModelCalculator>();
        }
    }

    void Update()
    {
        if (isUnderground) return;

        float mainSpeed = (mainCalc != null) ? mainCalc.velocity : 0f;

        if (!canMove)
        {
            if (mainSpeed > startThreshold) canMove = true;
            if (engineAudioSource != null) engineAudioSource.pitch = minPitch;
            return;
        }

        float toLine = float.PositiveInfinity;   // 車の前の端から交差点の入り口までの距離（入ったら 0 以下）
        if (mainBicycle != null)
        {
            float distance = Vector3.Distance(transform.position, mainBicycle.transform.position);
            bool wantBrake = distance < safeDistance;

            if (useCrossingZone)
            {
                float front = transform.position.z + directionMultiplier * frontOffset;
                float entry = directionMultiplier > 0 ? crossingMinZ : crossingMaxZ;
                toLine = (entry - front) * directionMultiplier;
                if (toLine <= 0f) committed = true;
                // ブレーキをかけ始める時点で、入り口の手前で止まれない（止まるまでの距離 ≒ 速さ ÷ 減速の強さ）なら、そのまま通り抜ける
                if (wantBrake && !braking && !committed && currentSpeed / Mathf.Max(brakeDeceleration, 0.01f) > toLine) committed = true;
                if (committed) wantBrake = false;
                crossingState = committed ? "交差点に入った（通り抜ける）" : wantBrake ? "入り口の手前で止まる" : "";
            }
            braking = wantBrake;

            if (wantBrake)
            {
                currentSpeed = Mathf.Lerp(currentSpeed, 0f, brakeDeceleration * Time.deltaTime);

                if (!hasPlayedBrakeSound && currentSpeed > 1.0f && brakeClip != null && engineAudioSource != null)
                {
                    engineAudioSource.PlayOneShot(brakeClip, brakeVolume);
                    hasPlayedBrakeSound = true;
                }
            }
            else
            {
                currentSpeed = Mathf.Lerp(currentSpeed, speed, (brakeDeceleration / 2f) * Time.deltaTime);
                if (currentSpeed >= speed * 0.9f) hasPlayedBrakeSound = false;
            }
        }

        // ★修正：directionMultiplier を掛け合わせて、LeftCarの場合は逆方向に進ませる
        // Space.World を指定することで、確実ワールド座標のZ軸（正・負）に沿って動きます
        float step = currentSpeed * Time.deltaTime;
        // 手前で止まるときは、前の端が交差点の入り口を越えないようにする
        if (useCrossingZone && braking && step > toLine) { step = Mathf.Max(0f, toLine); currentSpeed = 0f; }
        transform.Translate(0, 0, step * directionMultiplier, Space.World);

        if (engineAudioSource != null)
        {
            float pitch = Mathf.Lerp(minPitch, maxPitch, currentSpeed / maxSpeedForSound);
            engineAudioSource.pitch = pitch;
        }
    }

    public void ResetToSurface()
    {
        isUnderground = false;
        canMove = false;
        committed = false;
        braking = false;
        currentSpeed = speed;
        hasPlayedBrakeSound = false;
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        if (engineAudioSource != null) engineAudioSource.pitch = minPitch;
    }

    public void SetCrossingZone(float minZ, float maxZ)
    {
        useCrossingZone = true;
        crossingMinZ = minZ;
        crossingMaxZ = maxZ;
    }

    public void SendToUnderground()
    {
        isUnderground = true;
        canMove = false;

        transform.position = new Vector3(initialPosition.x, initialPosition.y - 100f, initialPosition.z);
        transform.rotation = initialRotation;

        if (engineAudioSource != null) engineAudioSource.pitch = minPitch;
    }
}