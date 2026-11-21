using UnityEngine;
using System.Collections.Generic;

public class TrialManager : MonoBehaviour
{
    [Header("連携するオブジェクトの登録")]
    public CarControllManagement rightCar;
    public CarControllManagement leftCar;
    public GoalLine goalLine;
    public DataRecorder dataRecorder;

    public enum SpawnType { None, Right, Left }
    private List<SpawnType> spawnPatternList = new List<SpawnType>();
    private int currentTrialIndex = 0;

    void Start()
    {
        GenerateRandomPattern();
        ApplyCurrentCondition();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            NextTrial();
        }
    }

    void GenerateRandomPattern()
    {
        spawnPatternList.Clear();

        for (int i = 0; i < 6; i++) spawnPatternList.Add(SpawnType.None);
        for (int i = 0; i < 1; i++) spawnPatternList.Add(SpawnType.Right);
        for (int i = 0; i < 1; i++) spawnPatternList.Add(SpawnType.Left);

        for (int i = 0; i < spawnPatternList.Count; i++)
        {
            SpawnType temp = spawnPatternList[i];
            int randomIndex = Random.Range(i, spawnPatternList.Count);
            spawnPatternList[i] = spawnPatternList[randomIndex];
            spawnPatternList[randomIndex] = temp;
        }

        currentTrialIndex = 0;
        Debug.Log("新しい8回のパターンをシャッフルしました！");
    }

    void ApplyCurrentCondition()
    {
        SpawnType currentType = spawnPatternList[currentTrialIndex];
        Debug.Log($"現在の試行: {currentTrialIndex + 1}回目 / 条件: {currentType}");

        // ★修正：条件に合わせて、車を「地上（初期位置）」か「地下」へ振り分ける
        switch (currentType)
        {
            case SpawnType.None:
                if (rightCar != null) rightCar.SendToUnderground();
                if (leftCar != null) leftCar.SendToUnderground();
                break;
            case SpawnType.Right:
                if (rightCar != null) rightCar.ResetToSurface();
                if (leftCar != null) leftCar.SendToUnderground();
                break;
            case SpawnType.Left:
                if (rightCar != null) rightCar.SendToUnderground();
                if (leftCar != null) leftCar.ResetToSurface();
                break;
        }
    }

    void NextTrial()
    {
        currentTrialIndex++;

        if (currentTrialIndex >= spawnPatternList.Count)
        {
            Debug.Log("1条件（8回）が終了しました！");
            GenerateRandomPattern();
        }

        ApplyCurrentCondition();

        if (goalLine != null)
        {
            goalLine.ResetGoal();
        }

        if (dataRecorder != null)
        {
            dataRecorder.StartNewTrialLog();
        }
    }
}