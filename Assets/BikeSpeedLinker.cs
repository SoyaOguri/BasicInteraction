using UnityEngine;

// このスクリプトは、ForwardMover と BicycleAnimaterChanger の両方が
// アタッチされているオブジェクトにアタッチしてください。
public class BikeSpeedLinker : MonoBehaviour
{
    // ペダル回転速度を調整するための係数 (Inspectorで調整可能)
    public float pedalRate;

    private ForwardMover forwardMover;
    private BicycleAnimaterChanger animChanger;

    void Start()
    {
        // プロパティの初期化 (エラーCS0182対策)
        pedalRate = 0.5f;

        // 必要なコンポーネントを同じGameObjectから取得
        forwardMover = GetComponent<ForwardMover>();
        animChanger = GetComponent<BicycleAnimaterChanger>();

        // コンポーネントの有無チェック
        if (forwardMover == null || animChanger == null)
        {
            Debug.LogError("BikeSpeedLinker: 必要なコンポーネント (ForwardMover または BicycleAnimaterChanger) が見つかりません。");
            enabled = false;
        }
    }

    void Update()
    {
        // 1. ForwardMoverの現在の速度を取得
        float currentMoveSpeed = forwardMover.CurrentVelocity;

        // 2. 速度に調整係数をかけて、アニメーションの再生速度を計算
        float animationSpeed = currentMoveSpeed * pedalRate;

        // 3. BicycleAnimaterChanger の既存の 'velocity' 変数に値を渡す
        animChanger.velocity = animationSpeed;
    }
}