using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
namespace Kurisu.UniChat.StateMachine.Editor
{
    [CustomPropertyDrawer(typeof(ChatStateMachineGraph.BehaviorNode))]
    public class BehaviorNodeDrawer : PropertyDrawer
    {
        private const string NullType = "Null";
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight
            + GenericBehaviorWrapperDrawer.CalculatePropertyHeight(property.FindPropertyRelative("container"))
            + EditorGUIUtility.standardVerticalSpacing;
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var totalHeight = position.height;
            position.height = EditorGUIUtility.singleLineHeight;
            var reference = property.FindPropertyRelative("serializedType");
            var container = property.FindPropertyRelative("container");
            var type = SerializedType.FromString(reference.stringValue);
            string id = type != null ? type.Name : NullType;
            if (type != null && container.objectReferenceValue == null)
            {
                container.objectReferenceValue = SerializedBehaviorUtils.Wrap(Activator.CreateInstance(type));
                property.serializedObject.ApplyModifiedProperties();
            }
            if (EditorGUI.DropdownButton(position, new GUIContent(id), FocusType.Keyboard))
            {
                var provider = ScriptableObject.CreateInstance<StateMachineBehaviorSearchWindow>();
                provider.Initialize((selectType) =>
                {
                    reference.stringValue = selectType != null ? SerializedType.ToString(selectType) : NullType;
                    if (selectType != null)
                    {
                        var wrapper = SerializedBehaviorUtils.Wrap(Activator.CreateInstance(selectType));
                        container.objectReferenceValue = wrapper;
                    }
                    else
                    {
                        container.objectReferenceValue = null;
                    }
                    property.serializedObject.ApplyModifiedProperties();
                });
                SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)), provider);
            }
            position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
            position.height = totalHeight - position.height - EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(position, container, true);
            EditorGUI.EndProperty();
        }
    }
    public class StateMachineBehaviorSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private Texture2D _indentationIcon;
        private Action<Type> typeSelectCallBack;
        public void Initialize(Action<Type> typeSelectCallBack)
        {
            this.typeSelectCallBack = typeSelectCallBack;
            _indentationIcon = new Texture2D(1, 1);
            _indentationIcon.SetPixel(0, 0, new Color(0, 0, 0, 0));
            _indentationIcon.Apply();
        }
        List<SearchTreeEntry> ISearchWindowProvider.CreateSearchTree(SearchWindowContext context)
        {
            var entries = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Select StateMachineBehavior"), 0),
                new(new GUIContent("<Null>", _indentationIcon)) { level = 1, userData = null }
            };
            List<Type> nodeTypes = FindSubClasses(typeof(ChatStateMachineBehavior))
                                                .Where(x => x != typeof(InvalidStateMachineBehavior))
                                                .ToList();
            var groups = nodeTypes.GroupBy(t => t.Assembly);
            foreach (var group in groups)
            {
                entries.Add(new SearchTreeGroupEntry(new GUIContent($"Select {group.Key.GetName().Name}"), 1));
                var subGroups = group.GroupBy(x => x.Namespace);
                foreach (var subGroup in subGroups)
                {
                    entries.Add(new SearchTreeGroupEntry(new GUIContent($"Select {subGroup.Key}"), 2));
                    foreach (var type in subGroup)
                    {
                        entries.Add(new SearchTreeEntry(new GUIContent(type.Name, _indentationIcon)) { level = 3, userData = type });
                    }
                }
            }
            return entries;
        }
        bool ISearchWindowProvider.OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            var type = searchTreeEntry.userData as Type;
            typeSelectCallBack?.Invoke(type);
            return true;
        }
        private static IEnumerable<Type> FindSubClasses(Type father)
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).Where(t => t.IsSubclassOf(father) && !t.IsAbstract);
        }
    }
}