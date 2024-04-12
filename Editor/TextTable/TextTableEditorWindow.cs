using System.Collections;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
namespace Kurisu.UniChat.Editor.TextTable
{
    public class TextTableEditorWindow : EditorWindow
    {
        private TextEmbeddingTable sourceTable;
        private SerializedObject tableObject;
        private TextEditorTable editorTable;
        public delegate Vector2 BeginVerticalScrollViewFunc(Vector2 scrollPosition, bool alwaysShowVertical, GUIStyle verticalScrollbar, GUIStyle background, params GUILayoutOption[] options);
        private static BeginVerticalScrollViewFunc s_func;
        private Vector2 m_ScrollPosition;
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
        private static GUIStyle FixedWidthButtonStyle => new(GUI.skin.button) { fixedWidth = 80 };
        [MenuItem("Tools/UniChat/Text Table Editor")]
        private static void ShowEditorWindow()
        {
            GetWindow<TextTableEditorWindow>("Text Table Editor");
        }
        private void OnEnable()
        {
            editorTable = CreateInstance<TextEditorTable>();
            tableObject = new(editorTable);
        }
        private void DrawAudioInfos(uint id, int size, IEnumerator enumerator)
        {
            if (size == 0) return;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(EditorGUIUtility.currentViewWidth - 100));
            while (enumerator.MoveNext())
            {
                var property = enumerator.Current as SerializedProperty;
                EditorGUILayout.PropertyField(property);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical();
            for (int i = 0; i < size; ++i)
            {
                if (GUILayout.Button("Play", FixedWidthButtonStyle))
                {
                    editorTable.PlayAudio(id, i).Forget();
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

        }
        private void OnGUI()
        {
            m_ScrollPosition = BeginVerticalScrollView(m_ScrollPosition, false, GUI.skin.verticalScrollbar, "OL Box");
            var enumerator = tableObject.FindProperty("tableEntries").GetEnumerator();
            while (enumerator.MoveNext())
            {
                var property = enumerator.Current as SerializedProperty;
                var canEdit = property.FindPropertyRelative("isEdit");
                var audioInfos = property.FindPropertyRelative("audioInfos");
                uint id = property.FindPropertyRelative("uniqueId").uintValue;
                EditorGUILayout.BeginHorizontal();
                GUI.enabled = canEdit.boolValue;
                EditorGUILayout.PropertyField(property);
                GUI.enabled = true;
                if (GUILayout.Button(canEdit.boolValue ? "Complete" : "Edit", FixedWidthButtonStyle))
                {
                    canEdit.boolValue = !canEdit.boolValue;
                    if (!canEdit.boolValue)
                    {
                        tableObject.ApplyModifiedProperties();
                        tableObject.Update();
                    }
                }
                if (GUILayout.Button("Delate", FixedWidthButtonStyle))
                {
                    editorTable.Remove(id);
                    tableObject.Update();
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.EndHorizontal();
                DrawAudioInfos(id, audioInfos.arraySize, audioInfos.GetEnumerator());
                GUILayout.Space(20);
            }
            EditorGUILayout.EndScrollView();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select Table"))
            {
                string path = EditorUtility.OpenFilePanel("Choose table file", PathUtil.UserDataPath, "bin");
                if (string.IsNullOrEmpty(path)) return;
                sourceTable = new(path);
                editorTable.Initialize(sourceTable, path);
                tableObject.Update();
            }
            GUI.enabled = sourceTable != null;
            if (GUILayout.Button("Update"))
            {
                editorTable.Update();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }
    }
}
