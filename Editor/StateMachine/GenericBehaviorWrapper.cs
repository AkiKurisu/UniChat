using System;
using UnityEngine;
using static Kurisu.UniChat.StateMachine.ChatStateMachineGraph;
namespace Kurisu.UniChat.StateMachine.Editor
{
    [Serializable]
    public class GenericBehaviorWrapper<T> : SerializedBehaviorWrapper
    {
        [SerializeField]
        T m_Value;

        public override object Value
        {
            get { return m_Value; }
            set { m_Value = (T)value; }
        }
    }
    public class SerializedBehaviorUtils
    {
        public static SerializedBehaviorWrapper Wrap(object value = null)
        {
            Type type = value.GetType();
            Type genericType = typeof(GenericBehaviorWrapper<>).MakeGenericType(type);
            Type dynamicType = DynamicTypeBuilder.MakeDerivedType(genericType, type);

            var dynamicTypeInstance = ScriptableObject.CreateInstance(dynamicType);
            if (dynamicTypeInstance is not SerializedBehaviorWrapper wrapper)
            {
                return null;
            }
            wrapper.Value = value ?? default;
            return (SerializedBehaviorWrapper)dynamicTypeInstance;
        }
    }
}
