using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BuildSwitcher))] // 拡張するクラスを指定
[CanEditMultipleObjects]
public class CustomeEditor_BuildSwitcher : Editor
{
    int selectedIndex = 0;
    string[] strs;

    public override void OnInspectorGUI()
    {


        // targetを変換して対象を取得
        BuildSwitcher switcher = target as BuildSwitcher;

        strs = new string[switcher.targets.Count];
        for (int i = 0; i < switcher.targets.Count; i++)
        {
            strs[i] = switcher.targets[i].targetName;
        }

        selectedIndex = EditorGUILayout.Popup("Build Target", selectedIndex, strs);

        if (GUILayout.Button("Switch Target!"))
        {
            switcher.SwitchCamera(selectedIndex);
            switcher.SwitchMultiScreenScript(selectedIndex);
            switcher.SwitchCommunicationScript(selectedIndex);
        }


        // 元のインスペクター部分を表示
        base.OnInspectorGUI();
    }
}
