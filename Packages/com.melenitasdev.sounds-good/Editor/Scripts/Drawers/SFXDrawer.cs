using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MelenitasDev.SoundsGood.Editor
{
    [CustomPropertyDrawer(typeof(SFX))]
    public class SFXDrawer : PropertyDrawer
    {
        private string[] names;
        private SFX[] values;
        private bool cached;

        private void Cache ()
        {
            if (cached) return;

            var fields = typeof(SFX)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(SFX));

            names = fields.Select(f => f.Name).ToArray();
            values = fields.Select(f => (SFX)f.GetValue(null)).ToArray();

            if (names.Length == 0)
            {
                names = new[] { "-----" };
                values = new[] { new SFX(string.Empty) };
            }

            cached = true;
        }

        public override VisualElement CreatePropertyGUI (SerializedProperty property)
        {
            Cache();

            SerializedProperty stringProp = property.FindPropertyRelative("value");
            string current = stringProp.stringValue;
            int index = Array.IndexOf(values, new SFX(current));
            if (index < 0) index = 0;

            var popup = new PopupField<string>(names.ToList(), index)
            {
                label = property.displayName
            };

            popup.RegisterValueChangedCallback(evt =>
            {
                int newIndex = Array.IndexOf(names, evt.newValue);
                if (newIndex >= 0)
                {
                    stringProp.stringValue = values[newIndex].ToString();
                    property.serializedObject.ApplyModifiedProperties();
                }
            });

            return popup;
        }

        public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
        {
            Cache();

            SerializedProperty stringProp = property.FindPropertyRelative("value");
            string current = stringProp.stringValue;
            int index = Array.IndexOf(values, new SFX(current));
            if (index < 0) index = 0;

            int selected = EditorGUI.Popup(position, label.text, index, names);
            if (selected != index)
            {
                stringProp.stringValue = values[selected].ToString();
                property.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}