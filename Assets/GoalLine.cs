using UnityEngine;
using UnityEngine.UI;

public class GoalLine : MonoBehaviour
{
    [Header("タイマー設定")]
    public Text timerText;
    public float timeLimit = 15.0f;

    [Header("発進検知の設定")]
    public GameObject mainBicycle;
    public float startThreshold = 0.3f;

    private BicycleModelCalculator mainCalc;
    private bool isTimerStarted = false;
    private bool isGoalReached = false;
    public bool isSuccess = false;

    private float initialTimeLimit;

    // 実験2-b：true のときは、自転車が動き出してもタイマーを始めない（StartTimerNow で始める。カウントダウン用）
    [HideInInspector] public bool waitForStartSignal = false;

    void Awake()
    {
        initialTimeLimit = timeLimit;
    }

    void Start()
    {

        if (mainBicycle != null)
        {
            mainCalc = mainBicycle.GetComponent<BicycleModelCalculator>();
            if (mainCalc == null) mainCalc = mainBicycle.GetComponentInChildren<BicycleModelCalculator>();
        }

        UpdateTimerDisplay();
    }

    void Update()
    {
        // ★修正：エンターキーの処理は TrialManager に任せるのでここから削除しました！

        if (isGoalReached) return;

        // --- 1. 発進判定 ---
        if (!isTimerStarted)
        {
            if (!waitForStartSignal && mainCalc != null && mainCalc.velocity > startThreshold)
            {
                isTimerStarted = true;
            }
            else
            {
                return;
            }
        }

        // --- 2. タイマー減少処理 ---
        if (timeLimit > 0)
        {
            timeLimit -= Time.deltaTime;

            if (timeLimit <= 0)
            {
                timeLimit = 0;
                isGoalReached = true;
                timerText.text = "遅刻... (ペナルティ)";
                timerText.color = Color.red;
            }
            else
            {
                UpdateTimerDisplay();
            }
        }
    }

    void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(timeLimit / 60F);
        int seconds = Mathf.FloorToInt(timeLimit % 60F);
        timerText.text = string.Format("校門通過制限時間まで {0:00}:{1:00}", minutes, seconds);
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("GoalZoneに何かが触れました！ 名前: " + other.gameObject.name);

        if (isGoalReached || timeLimit <= 0) return;

        if (other.gameObject.name == "Bicycle" || other.transform.root.name == "Bicycle")
        {
            isGoalReached = true;
            isSuccess = true;
            timerText.text = "間に合った！\n(アメ獲得)";
            timerText.color = Color.yellow;

            Debug.Log("自転車がゴールラインを通過しました！");
        }
    }

    // 実験2-b：制限時間を外（Exp2bTrialManager が Setting/exp2b.txt から読んだ値）から変える
    public void SetTimeLimit(float seconds)
    {
        initialTimeLimit = seconds;
        timeLimit = seconds;
        if (timerText != null) UpdateTimerDisplay();
    }

    // 実験2-b：カウントダウンが 0 になった瞬間に、制限時間のタイマーを始める
    public void StartTimerNow()
    {
        waitForStartSignal = false;
        if (!isGoalReached) isTimerStarted = true;
    }

    // ★追加：外部（TrialManager）からタイマーをリセットするための関数
    public void ResetGoal()
    {
        isTimerStarted = false;
        isGoalReached = false;
        isSuccess = false;
        timeLimit = initialTimeLimit;
        timerText.color = Color.white;
        UpdateTimerDisplay();
    }
}