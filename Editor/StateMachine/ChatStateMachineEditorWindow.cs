using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace Kurisu.UniChat.StateMachine.Editor
{
    public class ChatStateMachineEditorWindow : EditorWindow
    {
        private SerializedObject targetObject;
        private ChatStateMachineGraphEditorCtrl graphCtrl;
        public delegate Vector2 BeginVerticalScrollViewFunc(Vector2 scrollPosition, bool alwaysShowVertical, GUIStyle verticalScrollbar, GUIStyle background, params GUILayoutOption[] options);
        private static BeginVerticalScrollViewFunc s_func;
        private Vector2 m_ScrollPosition;
        private string path;
        public string fileName = "NewChatFSM";
        private static BeginVerticalScrollViewFunc BeginVerticalScrollView
        {
            get
            {
                if (s_func == null)
                {
                    var methods = typeof(EditorGUILayout).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Where(x => x.Name == "BeginVerticalScrollView").ToArray();
                    var method = methods.First(x => x.GetParameters()[1].ParameterType == typeof(bool));
                    s_func = (BeginVerticalScrollViewFunc)method.CreateDelegate(typeof(BeginVerticalScrollViewFunc));
                }
                return s_func;
            }
        }
        [MenuItem("Tools/UniChat/Chat StateMachine Editor")]
        private static void ShowEditorWindow()
        {
            GetWindow<ChatStateMachineEditorWindow>("Chat StateMachine Editor");
        }
        private void OnEnable()
        {
            graphCtrl = CreateInstance<ChatStateMachineGraphEditorCtrl>();
            targetObject = new(graphCtrl);
        }
        private void OnGUI()
        {
            fileName = EditorGUILayout.TextField(new GUIContent("File Name"), fileName);
            m_ScrollPosition = BeginVerticalScrollView(m_ScrollPosition, false, GUI.skin.verticalScrollbar, "OL Box");
            DrawModel();
            EditorGUILayout.EndScrollView();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load"))
            {
                path = EditorUtility.OpenFilePanel("Select fsm bytes file", Application.dataPath, "bytes");
                if (string.IsNullOrEmpty(path)) return;
                fileName = Path.GetFileNameWithoutExtension(path);
                graphCtrl.Load(path);
                targetObject.Update();
                Debug.Log($"Load fsm from {path}");
            }
            if (GUILayout.Button("New"))
            {
                graphCtrl.Reset();
                targetObject.Update();
            }
            if (GUILayout.Button("Overwrite"))
            {
                graphCtrl.Save(path);
                Debug.Log($"Save to {path}");
            }
            if (GUILayout.Button("Save"))
            {
                var folder = EditorUtility.OpenFolderPanel("Select folder", Application.dataPath, "");
                if (string.IsNullOrEmpty(folder)) return;
                string newPath = Path.Combine(folder, $"{fileName}.bytes");
                graphCtrl.Save(newPath);
                Debug.Log($"Save to {newPath}");
            }
            EditorGUILayout.EndHorizontal();
        }
        private void DrawModel()
        {
            EditorGUI.BeginChangeCheck();
            SerializedProperty prop = targetObject.GetIterator();
            prop.NextVisible(true);
            while (prop.NextVisible(false))
            {
                if (prop.name == "graph") continue;
                EditorGUILayout.PropertyField(prop, true);
            }
            if (EditorGUI.EndChangeCheck())
            {
                targetObject.ApplyModifiedProperties();
            }
        }
    }
}
