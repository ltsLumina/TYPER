using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MelenitasDev.SoundsGood.Editor
{
    [CustomPropertyDrawer(typeof(Track))]
    public class TrackDrawer : PropertyDrawer
    {
        private string[] names;
        private Track[] values;
        private bool cached;

        private void Cache ()
        {
            if (cached) return;

            var fields = typeof(Track)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(Track));

            names = fields.Select(f => f.Name).ToArray();
            values = fields.Select(f => (Track)f.GetValue(null)).ToArray();

            if (names.Length == 0)
            {
                names = new[] { "-----" };
                values = new[] { new Track(string.Empty) };
            }

            cached = true;
        }

        public override VisualElement CreatePropertyGUI (SerializedProperty property)
        {
            Cache();

            SerializedProperty stringProp = property.FindPropertyRelative("value");
            string current = stringProp.stringValue;
            int index = Array.IndexOf(values, new Track(current));
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
            int index = Array.IndexOf(values, new Track(current));
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