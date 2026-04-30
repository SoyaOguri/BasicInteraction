using UnityEngine; //Accelerative

public class CarForwardMoverAccelerative : MonoBehaviour
{
    // --- 公開変数 ---
    public float speed = 7.0f;              // 通常時の移動速度 (7.0f)
    public float startThreshold = 0.3f;
    public GameObject mainBicycle;
    public GameObject frontBicycle;
    public float reducedSpeed = 5.0f;       // 減速時の速度 (5m/s)
    public float evasionSpeed = -1.0f;      // 回避移動速度（Z軸マイナス方向へ1m/s）
    public float TTCCriticalTime = 3.47f;   // TTCのしきい値
    public float TTCCriticalTime2 = 5.0f;   // 減速のTTCのしきい値
    public float EvasionMaxZ = 2.49f;       // Z座標の回避完了しきい値
    public float DecelerationRate = -1.0f;// 減速度
    public float speedResumeTime = 3.0f;    // 速度を戻すまでの待機時間 (2s)

    // ★追加: 音声制御用の設定項目
    [Header("Audio Settings")]
    public float minPitch = 0.8f;   // 低速時の音の高さ
    public float maxPitch = 2.0f;   // 最高速時の音の高さ
    public float maxSpeedForSound = 20.0f; // ピッチが最大になる速度(m/s)

    // --- プライベート変数 ---
    private bool canMove = false;
    private BicycleModelCalculator mainCalc;
    private ForwardMover ForMov;

    private float effectiveSpeed;

    private float resumeSpeedTimer = 0f;
    // ★追加: 回避行動を継続させるためのフラグ
    private bool isCurrentlyEvading = false;
    private bool isEvasionComplete = false;
    private bool decelerationComplete = false;
    public float CurrentVelocity;

    // ★追加: オーディオソース
    private AudioSource engineAudioSource;

    void Start()
    {
        // ★追加: AudioSourceを取得して設定
        engineAudioSource = GetComponent<AudioSource>();
        if (engineAudioSource != null)
        {
            engineAudioSource.loop = true; // ループ再生をON
            engineAudioSource.spatialBlend = 1.0f; // 3Dサウンドにする
            if (!engineAudioSource.isPlaying) engineAudioSource.Play(); // 再生開始
        }

        // 初期化ロジックは省略せずに記述
        if (mainBicycle != null)
        {
            mainCalc = mainBicycle.GetComponent<BicycleModelCalculator>();
            if (mainCalc == null) mainCalc = mainBicycle.GetComponentInChildren<BicycleModelCalculator>();
            if (mainCalc == null) Debug.LogWarning("BicycleModelCalculator が mainBicycle に見つかりません。");
        }

        if (frontBicycle != null)
        {
            ForMov = frontBicycle.GetComponent<ForwardMover>();
            if (ForMov == null) ForMov = frontBicycle.GetComponentInChildren<ForwardMover>();
            if (ForMov == null) Debug.LogWarning("ForwardMover が frontBicycle に見つかりません。");
        }

        effectiveSpeed = speed;
    }

    void Update()
    {
        float mainSpeed = 0f;
        float frontSpeed = 0f;

        if (mainCalc != null) mainSpeed = mainCalc.velocity;
        if (ForMov != null) frontSpeed = ForMov.CurrentVelocity;

        // --- 1. 発進判定 ---
        if (!canMove)
        {
            if (mainSpeed > startThreshold) canMove = true;
            CurrentVelocity = 0f;
            // 止まっているときはアイドリング音（minPitch）にする
            if (engineAudioSource != null) engineAudioSource.pitch = minPitch;
            return;
        }

        // --- 2. 移動ロジック ---

        float TTC1 = float.MaxValue;
        float TTC2 = float.MaxValue;

        if (mainBicycle != null)
        {
            float carX = transform.position.x;
            float bicycleX1 = mainBicycle.transform.position.x;

            float bicycleX2 = (frontBicycle != null) ? frontBicycle.transform.position.x : float.MaxValue;

            CurrentVelocity = effectiveSpeed;

            // --- 2-1. 減速判定 ---
            float signedDistanceX1 = bicycleX1 - carX;
            float signedDistanceX2 = bicycleX2 - carX;

            float SpeedDifference1 = CurrentVelocity - mainSpeed;
            float SpeedDifference2 = CurrentVelocity - frontSpeed;

            TTC1 = signedDistanceX1 / SpeedDifference1;
            TTC2 = signedDistanceX2 / SpeedDifference2;

            bool isCloseToTarget = false;

            // 2-1. 減速判定 (effectiveSpeedがreducedSpeedより大きい場合のみ減速処理)
            if (signedDistanceX1 > 0 && SpeedDifference1 > 0)
            {
                if (TTC1 < TTCCriticalTime2) isCloseToTarget = true;
            }

            // TTC計算（frontBicycle）
            if (signedDistanceX2 > 0 && SpeedDifference2 > 0)
            {
                if (TTC2 < TTCCriticalTime2) isCloseToTarget = true;
            }

            if (isCloseToTarget && !decelerationComplete)
            {
                // 減速処理
                effectiveSpeed += DecelerationRate * Time.deltaTime;
                if (effectiveSpeed < reducedSpeed)
                {
                    effectiveSpeed = reducedSpeed;
                    decelerationComplete = true;
                }
            }

            // X軸移動の実行 (減速/通常速度)
            transform.Translate(Vector3.right * effectiveSpeed * Time.deltaTime);
            CurrentVelocity = effectiveSpeed;

            // --- 2-2. TTC判定と回避行動の開始条件チェック ---

            bool shouldEvadeByTTC = false; // TTCに基づく回避要求

            // TTC計算（mainBicycle）
            if (signedDistanceX1 > 0 && SpeedDifference1 > 0)
            {
                if (TTC1 < TTCCriticalTime) shouldEvadeByTTC = true;
            }

            // TTC計算（frontBicycle）
            if (signedDistanceX2 > 0 && SpeedDifference2 > 0)
            {
                if (TTC2 < TTCCriticalTime) shouldEvadeByTTC = true;
            }

            // ★ 回避開始判定: TTCが閾値未満で、まだ回避中でなければ回避を開始
            if (shouldEvadeByTTC && !isCurrentlyEvading && !isEvasionComplete)
            {
                isCurrentlyEvading = true;
                resumeSpeedTimer = 0f;      // タイマーもリセット
            }

            // ★ 回避実行と完了ロジック
            if (isCurrentlyEvading)
            {
                // 回避移動を継続（Z軸マイナス方向）
                transform.Translate(Vector3.forward * evasionSpeed * Time.deltaTime);

                // TTCが小さい方（より危険な方）をターゲットとする判定
                float carZ = transform.position.z;
                float targetZ = (TTC1 <= TTC2)
                        ? mainBicycle.transform.position.z
                        : frontBicycle.transform.position.z;

                float distanceZ = targetZ - carZ;

                // Z座標が2.49m以上離れたら回避をやめる
                if (Mathf.Abs(distanceZ) > EvasionMaxZ)
                {
                    isCurrentlyEvading = false;  // ★回避移動を停止
                    isEvasionComplete = true;
                    resumeSpeedTimer = speedResumeTime; // 2秒タイマー開始
                }
            }

            // --- 2-3. 速度復帰タイマー処理 ---
            // 回避完了後にタイマーが動作中であれば、徐々に速度を戻す
            if (resumeSpeedTimer > 0f)
            {
                resumeSpeedTimer -= Time.deltaTime;

                float elapsedRatio = 1f - (resumeSpeedTimer / speedResumeTime);
                effectiveSpeed = Mathf.Lerp(reducedSpeed, speed, elapsedRatio);

                if (resumeSpeedTimer <= 0f)
                {
                    effectiveSpeed = speed;
                }
            }
        }

        // ★追加: 速度に応じたエンジン音のピッチ変更処理
        if (engineAudioSource != null)
        {
            // 現在の速度(effectiveSpeed)を基準にピッチを計算
            // 速度0ならminPitch, maxSpeedForSoundならmaxPitchになる
            float pitch = Mathf.Lerp(minPitch, maxPitch, Mathf.Abs(effectiveSpeed) / maxSpeedForSound);
            engineAudioSource.pitch = pitch;
        }
    }
}