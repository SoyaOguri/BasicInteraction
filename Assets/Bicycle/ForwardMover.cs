using UnityEngine;

public class ForwardMover : MonoBehaviour
{
    public float speed = 2.0f;
    public float startThreshold = 0.3f;
    public GameObject mainBicycle; // parent Bicycle（BicycleModelCalculatorがついているオブジェクト）

    bool canMove = false;
    public float CurrentVelocity { get; private set; }

    BicycleModelCalculator mainCalc;

    void Start()
    {
        if (mainBicycle == null)
        {
            Debug.LogWarning("ForwardMover: mainBicycle が未設定です。");
            return;
        }

        // まず親オブジェクト直下にあるかを試す
        mainCalc = mainBicycle.GetComponent<BicycleModelCalculator>();

        // 見つからなければ子から探す（安全対策）
        if (mainCalc == null)
            mainCalc = mainBicycle.GetComponentInChildren<BicycleModelCalculator>();

        if (mainCalc == null)
            Debug.LogWarning("ForwardMover: BicycleModelCalculator が見つかりません。");
    }

    void Update()
    {
        float mainSpeed = 0f;

        if (mainCalc != null)
        {
            mainSpeed = mainCalc.velocity; // ここがキモ：BicycleModelCalculatorの速度
        }
        else
        {
            // フォールバック：位置差で推定（万一）
            // mainSpeed = (mainBicycle.transform.position - prevPos).magnitude / Time.deltaTime;
        }

        // しきい値判定
        if (!canMove)
        {
            if (mainSpeed > startThreshold)
                canMove = true;

            CurrentVelocity = 0f;
        }
        else
        {
            transform.Translate(Vector3.right * speed * Time.deltaTime);
            CurrentVelocity = speed;
        }
    }
}
