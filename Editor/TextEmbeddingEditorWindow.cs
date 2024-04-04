using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace Kurisu.UniChat.Editor
{
    public class TextEmbeddingEditorTable : ScriptableObject
    {
        public List<TextEmbeddingEditorEntry> tableEntries = new();
        private TextEmbeddingTable internalTable;
        private string path;
        public void Initialize(TextEmbeddingTable internalTable, string path)
        {
            this.path = path;
            this.internalTable = internalTable;
            tableEntries.Clear();
            tableEntries.AddRange(internalTable.tableEntries.Select(x => new TextEmbeddingEditorEntry(x)));
        }
        public void Update()
        {
            tableEntries.ForEach(x => x.Update());
            internalTable.tableEntries = tableEntries.Select(x => x.internalEntry).ToList();
            internalTable.Save(path);
        }

        public void Remove(uint uintValue)
        {
            tableEntries.RemoveAll(x => x.uniqueId == uintValue);
        }
    }
    public class TextEmbeddingEditorWindow : EditorWindow
    {
        private TextEmbeddingTable dialogueTable;
        private SerializedObject tableObject;
        private TextEmbeddingEditorTable dialogueEditorTable;
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
        [MenuItem("Tools/UniChat/Text Embedding Editor")]
        private static void ShowEditorWindow()
        {
            GetWindow<TextEmbeddingEditorWindow>("Text Embedding Editor");
        }
        private void OnEnable()
        {
            dialogueEditorTable = CreateInstance<TextEmbeddingEditorTable>();
            tableObject = new(dialogueEditorTable);
        }
        private void OnGUI()
        {
            m_ScrollPosition = BeginVerticalScrollView(m_ScrollPosition, false, GUI.skin.verticalScrollbar, "OL Box");
            var enumerator = tableObject.FindProperty("tableEntries").GetEnumerator();
            while (enumerator.MoveNext())
            {
                var property = enumerator.Current as SerializedProperty;
                var canEdit = property.FindPropertyRelative("isEdit");
                EditorGUILayout.BeginHorizontal();
                GUI.enabled = canEdit.boolValue;
                EditorGUILayout.PropertyField(property);
                GUI.enabled = true;
                if (GUILayout.Button(canEdit.boolValue ? "Complete" : "Edit", new GUIStyle(GUI.skin.button) { fixedWidth = 80 }))
                {
                    canEdit.boolValue = !canEdit.boolValue;
                    if (!canEdit.boolValue)
                    {
                        tableObject.ApplyModifiedProperties();
                        tableObject.Update();
                    }
                }
                if (GUILayout.Button("Delate", new GUIStyle(GUI.skin.button) { fixedWidth = 80 }))
                {
                    dialogueEditorTable.Remove(property.FindPropertyRelative("uniqueId").uintValue);
                    tableObject.Update();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select Table"))
            {
                string path = EditorUtility.OpenFilePanel("Choose table file", PathUtil.UserDataPath, "bin");
                if (string.IsNullOrEmpty(path)) return;
                dialogueTable = new(path);
                dialogueEditorTable.Initialize(dialogueTable, path);
                tableObject.Update();
            }
            GUI.enabled = dialogueTable != null;
            if (GUILayout.Button("Update"))
            {
                dialogueEditorTable.Update();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }
    }
}
