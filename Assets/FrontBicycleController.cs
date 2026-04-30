using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrontBicycleController : MonoBehaviour
{
    public GameObject mainBicycle;  // メイン自転車（親オブジェクト）をInspectorで指定
    public float moveSpeed = 3.0f;  // 前方自転車の移動速度
    public float velocityThreshold = 0.5f; // メイン自転車が動き出したとみなす閾値

    private bool isMoving = false;
    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        if (mainBicycle == null)
        {
            Debug.LogWarning("mainBicycle が指定されていません。");
            return;
        }

        // 子オブジェクト "BicycleModel" の NetworkCharacterVelocity を取得
        NetworkCharacterVelocity velocityScript = mainBicycle
            .transform.Find("BicycleModel")
            .GetComponent<NetworkCharacterVelocity>();

        if (velocityScript == null)
        {
            Debug.LogWarning("mainBicycle 内に NetworkCharacterVelocity が見つかりません。");
            return;
        }

        // メイン自転車の速度の大きさを取得
        float mainSpeed = velocityScript.velocity.magnitude;

        // 一定以上の速度になったら前方自転車を動かす
        if (!isMoving && mainSpeed > velocityThreshold)
        {
            isMoving = true;
            Debug.Log("前方自転車が発進しました！");
        }

        // 動き出したら前方向に移動
        if (isMoving)
        {
            transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
        }
    }
}
