using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BatchOperation))] // 拡張するクラスを指定
[CanEditMultipleObjects]
public class Customeditor_BatchOperation : Editor
{
    public override void OnInspectorGUI()
    {
        // 元のインスペクター部分を表示
        base.OnInspectorGUI();

        // targetを変換して対象を取得
        BatchOperation batch = target as BatchOperation;

        // publicなメソッドを実行するボタン
        if (GUILayout.Button("All active!"))
        {
            batch.SetActiveScript();
            //batch.DataSet_Public();
        }

        if (GUILayout.Button("All Disactive!"))
        {
            batch.SetDisactiveScript();
        }

        // publicなメソッドを実行するボタン
        if (GUILayout.Button("All Field Set!"))
        {
            batch.SetActiveVariable();
            //batch.DataSet_Public();
        }

        // privateなメソッドを実行するボタン
        //if (GUILayout.Button("PrivateMethod!"))
        //{
        //    batch.SendMessage("DataSet_Private", null, SendMessageOptions.DontRequireReceiver);
        //}
    }
}
