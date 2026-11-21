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

        if (mainBicycle != null)
        {
            float distance = Vector3.Distance(transform.position, mainBicycle.transform.position);

            if (distance < safeDistance)
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
        transform.Translate(0, 0, currentSpeed * directionMultiplier * Time.deltaTime, Space.World);

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
        currentSpeed = speed;
        hasPlayedBrakeSound = false;
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        if (engineAudioSource != null) engineAudioSource.pitch = minPitch;
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