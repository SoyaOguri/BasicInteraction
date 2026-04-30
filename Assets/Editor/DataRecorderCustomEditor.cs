using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

[CustomEditor(typeof(DataRecorder))]
public class AnyClassNameInspector : Editor
{
    DataRecorder m_Target = null;
    //GameObject m_PreviousObject = null;
    List<string[]> m_Methods = new List<string[]>();
    List<string> m_Objects = new List<string>();
    List<DataRecorder.EditorDataList> listEvacuation = new List<DataRecorder.EditorDataList>();
    FoldoutManager saveDataFoldout = new FoldoutManager("Save Data Array");
    List<FoldoutManager> dataListFoldout = new List<FoldoutManager>();

    string temp;

    class FoldoutManager
    {
        public bool isOpen;
        bool m_isOpen;
        public string str;


        public FoldoutManager()
        {
            isOpen = false;
            m_isOpen = false;
        }

        public FoldoutManager(string name) : base()
        {
            str = name;
        }

        public void Display()
        {
            isOpen = EditorGUILayout.Foldout(m_isOpen, str);
            if (m_isOpen != isOpen)
            {
                m_isOpen = isOpen;
            }
        }
    }

    void OnEnable()
    {
        // AnyClassNameコンポーネントを取得
        m_Target = target as DataRecorder;

        ResizeTargetList();
    }

    void ResizeTargetList()
    {
        if (m_Methods.Count != m_Target.trackingTarget.Count)
        {
            if (m_Methods.Count < m_Target.trackingTarget.Count)
            {
                for (int i = m_Methods.Count; i < m_Target.trackingTarget.Count; i++)
                {
                    m_Methods.Add(new string[] { });
                    m_Objects.Add("");
                }
            }
            else
            {
                for (int i = m_Target.trackingTarget.Count; i < m_Methods.Count; i++)
                {
                    m_Methods.RemoveAt(m_Methods.Count-1);
                    m_Objects.RemoveAt(m_Objects.Count-1);
                }
            }
        }
        for (int i = 0; i < m_Target.trackingTarget.Count; i++)
        {
            m_Objects[i] = "";
            if (m_Objects.Contains(m_Target.trackingTarget[i].obj.name))
                m_Objects[i] = m_Target.trackingTarget[i].obj.name + " (Element " + i + ")";
            else
                m_Objects[i] = m_Target.trackingTarget[i].obj.name;
        }
        for (int i = 0; i < m_Target.trackingTarget.Count; i++)
        {
            CollectMethods(i);
        }
    }

    void ResizeSaveDataList()
    {

        if (m_Target.list.Count != m_Target.saveDataSize)
        {
            if (m_Target.list.Count < m_Target.saveDataSize)
            {
                for (int i = m_Target.list.Count; i < m_Target.saveDataSize; i++)
                {
                    if (listEvacuation.Count == 0)
                        m_Target.list.Add(new DataRecorder.EditorDataList());
                    else
                    {
                        m_Target.list.Add(listEvacuation[listEvacuation.Count - 1]);
                        listEvacuation.RemoveAt(listEvacuation.Count - 1);
                    }
                }
            }
            else
            {
                DataRecorder.EditorDataList last;
                for (int i = m_Target.saveDataSize; i < m_Target.list.Count; i++)
                {
                    last = m_Target.list[m_Target.list.Count - 1];
                    if (last.name != "None")
                        listEvacuation.Add(last);
                    m_Target.list.RemoveAt(m_Target.list.Count - 1);
                }
            }

        }

        if (dataListFoldout.Count != m_Target.saveDataSize)
        {
            if (dataListFoldout.Count < m_Target.saveDataSize)
            {
                for (int i = dataListFoldout.Count; i < m_Target.saveDataSize; i++)
                {
                    dataListFoldout.Add(new FoldoutManager("Element " + i));
                }
            }
            else
            {
                for (int i = m_Target.saveDataSize; i < dataListFoldout.Count; i++)
                {
                    dataListFoldout.RemoveAt(dataListFoldout.Count - 1);
                }
            }

        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        saveDataFoldout.Display();
        using (new EditorGUI.IndentLevelScope(1))
        {

            if (saveDataFoldout.isOpen)
            {

                if (m_Target == null)
                {
                    return;
                }

                ResizeTargetList();
                ResizeSaveDataList();


                for (int i = 0; i < m_Target.list.Count; i++)
                {

                    int index;
                    try
                    {
                        index = m_Methods[m_Target.list[i].number]
                                        .Select((Name, Index) => new { Name, Index })
                                        .First(x => x.Name == m_Target.list[i].name)
                                        .Index;
                    }
                    catch
                    {
                        index = 0;
                    }

                    dataListFoldout[i].Display();
                    if (dataListFoldout[i].isOpen)
                    {
                        using (new EditorGUI.IndentLevelScope(1))
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.LabelField("Target Variable " + (i));
                                m_Target.list[i].number = EditorGUILayout.Popup(m_Target.list[i].number, m_Objects.ToArray());

                            }

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                temp = m_Target.list[i].name;
                                EditorGUILayout.LabelField(m_Objects[m_Target.list[i].number]);
                                m_Target.list[i].name = m_Methods[m_Target.list[i].number][EditorGUILayout.Popup(index, m_Methods[m_Target.list[i].number])];
                                if(temp != m_Target.list[i].name)
                                {
                                    m_Target.list[i].label = m_Target.list[i].name;
                                }

                            }

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                if (Selection.activeGameObject)
                                {
                                    m_Target.list[i].label = EditorGUILayout.TextField("Field name: ", m_Target.list[i].label); ;
                                }

                            }
                        }
                    }

                }
            }
        }
        


    }

    void ClearVariable()
    {
        m_Methods = new List<string[]>();
    }



    /// <summary>
    /// 登録されたオブジェクトのPublic Fieldを収集
    /// </summary>
    void CollectMethods(int num)
    {
        if (m_Target == null)
        {
            ClearVariable();
            return;
        }

        if(m_Target.trackingTarget == null)
        {
            ClearVariable();
            return;
        }

        MonoBehaviour[] components = m_Target.trackingTarget[num].obj.GetComponents<MonoBehaviour>();

        ArrayList result = new ArrayList();
        result.Add("None");
        foreach (var component in components)
        {
            string[] methodsName = component.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public)
                                    .Where(x => x.DeclaringType == component.GetType())
                                    //.Where(x => x.GetParameters().Length == 0)
                                    .Select(x => x.Name)
                                    .ToArray();
            result.AddRange(methodsName);
        }
        
        m_Methods[num] = (string[])result.ToArray(typeof(string));
    }
}