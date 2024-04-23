using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
namespace Kurisu.UniChat.Editor.ChatModel
{
    public class ChatModelEditorWindow : EditorWindow
    {
        private TextEmbeddingTable sourceTable;
        private ChatEditorGraph sourceGraph;
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
        private uint? selectId;
        private static GUIStyle FixedWidthButtonStyle => new(GUI.skin.button) { fixedWidth = 80 };
        [MenuItem("Tools/UniChat/Chat Model Editor")]
        private static void ShowEditorWindow()
        {
            GetWindow<ChatModelEditorWindow>("Chat Model Editor");
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
            DrawTable();
            GUILayout.FlexibleSpace();
            DrawToolBar();
        }
        private void DrawTable()
        {
            m_ScrollPosition = BeginVerticalScrollView(m_ScrollPosition, false, GUI.skin.verticalScrollbar, "OL Box");
            var enumerator = tableObject.FindProperty("tableEntries").GetEnumerator();
            while (enumerator.MoveNext())
            {
                var property = enumerator.Current as SerializedProperty;
                var canEdit = property.FindPropertyRelative("isEdit");
                var audioInfos = property.FindPropertyRelative("audioInfos");
                uint id = property.FindPropertyRelative("uniqueId").uintValue;
                if (selectId.HasValue && selectId.Value != id)
                {
                    continue;
                }
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
        }
        private void DrawToolBar()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select Chat Model"))
            {
                string path = EditorUtility.OpenFilePanel("Choose chat model", PathUtil.UserDataPath, "cfg");
                if (string.IsNullOrEmpty(path)) return;
                ChatModelFile file = JsonConvert.DeserializeObject<ChatModelFile>(File.ReadAllText(path));
                sourceTable = new(file.TablePath);
                sourceGraph = new(file.GraphPath);
                editorTable.Initialize(sourceTable, file.TablePath);
                tableObject.Update();
                ChatGraphViewer.CreateWindow(file.GraphPath).OnSelectEdge = OnSelectEdge;
            }
            GUI.enabled = sourceTable != null;
            if (GUILayout.Button(new GUIContent("Update Table", "This will overwrite the text table")))
            {
                editorTable.Update();
            }
            if (GUILayout.Button(new GUIContent("Intersect Graph", "Remove ports without linking in the text table")))
            {
                sourceGraph.Intersect(sourceTable);
                ChatGraphViewer.CreateWindow(sourceGraph.graphPath).OnSelectEdge = OnSelectEdge;
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }
        private void OnSelectEdge(ChatGraph.Edge? edge)
        {
            selectId = edge.HasValue ? edge.Value.output.uniqueId : null;
            Repaint();
        }
    }
    public class ChatEditorGraph
    {
        public readonly string graphPath;
        public ChatEditorGraph(string graphPath)
        {
            this.graphPath = graphPath;
        }
        public void Intersect(TextEmbeddingTable table)
        {
            using var db = new ChatDataBase(graphPath);
            for (int i = db.Count - 1; i >= 0; i--)
            {
                if (!table.tableEntries.Any(x => x.uniqueId == db.GetOutput(i)))
                {
                    db.RemoveEdge(i);
                }
            }
            File.Move(graphPath, Path.Combine(Path.GetDirectoryName(graphPath), $"backup_{Path.GetFileName(graphPath)}"));
            db.Save(graphPath);
        }
    }
}
