using UnityEngine;
using UnityEngine.UI;

public class CountdownTimer : MonoBehaviour
{
    public float timeLeft = 120f;
    public Text timerText;

    [Header("発進検知の設定")]
    public GameObject mainBicycle;        // 検知する自転車オブジェクト
    public float startThreshold = 0.3f;   // タイマーが動き出す速度（0.3m/s）

    private BicycleModelCalculator mainCalc;
    private bool isTimerStarted = false;  // タイマーが動き出したかどうかのフラグ

    public bool isGoalReached = false;

    void Start()
    {
        // 最初に一回だけ「120秒」の表示を作っておく（00:00にならないように）
        UpdateTimerDisplay();

        // 自転車から BicycleModelCalculator スクリプトを取得する
        if (mainBicycle != null)
        {
            mainCalc = mainBicycle.GetComponent<BicycleModelCalculator>();
            if (mainCalc == null) mainCalc = mainBicycle.GetComponentInChildren<BicycleModelCalculator>();
            if (mainCalc == null) Debug.LogWarning("BicycleModelCalculatorが見つかりません");
        }
    }

    void Update()
    {
        // --- 1. 発進（タイマースタート）判定 ---
        if (!isTimerStarted)
        {
            // 自転車の速度がしきい値を超えたらスタート！
            if (mainCalc != null && mainCalc.velocity > startThreshold)
            {
                isTimerStarted = true;
            }
            else
            {
                // まだ速度が出ていなければ、ここで処理を終了（タイマーは減らさない）
                return;
            }
        }

        // --- 2. タイマーのカウントダウン処理 ---
        if (timeLeft > 0 && !isGoalReached)
        {
            timeLeft -= Time.deltaTime;

            if (timeLeft < 0) timeLeft = 0;

            UpdateTimerDisplay();
        }
    }

    void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(timeLeft / 60f);
        int seconds = Mathf.FloorToInt(timeLeft % 60f);

        timerText.text = string.Format("校門通過制限時間まで {0:00}:{1:00}", minutes, seconds);

        // 💡おまけ機能：残り時間が30秒を切ったら文字を赤くして焦らせる
        if (timeLeft <= 30f && timeLeft > 0f)
        {
            timerText.color = Color.red;
        }
        else if (timeLeft == 0f)
        {
            timerText.text = "時間切れ...";
        }
    }
}