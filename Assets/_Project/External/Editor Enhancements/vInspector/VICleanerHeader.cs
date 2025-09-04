#if UNITY_EDITOR
#region
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static VInspector.Libs.VUtils;
using static VInspector.Libs.VGUI;
#endregion

namespace VInspector
{
public class VICleanerHeader
{
	void OnGUI()
	{
		Color bgNorm = EditorGUIUtility.isProSkin ? Greyscale(.248f) : Greyscale(.8f);
		Color bgHovered = EditorGUIUtility.isProSkin ? Greyscale(.28f) : Greyscale(.84f);
		string name = target.GetType().Name.Decamelcase();
		Rect nameRect = headerElement.contentRect.MoveX(60).SetWidth(name.GetLabelWidth(isBold: true));

		void headerClick()
		{
			if (curEvent.isMouseDown) mousePressedOnHeader = true;

			if (curEvent.isMouseUp) mousePressedOnHeader = false;
		}

		void scriptNameClick()
		{
			if (mousePressedOnScriptName && curEvent.isMouseUp) window.Repaint();

			if (curEvent.isMouseUp) mousePressedOnScriptName = false;

			if (!nameRect.IsHovered()) return;
			if (!curEvent.isMouseDown) return;

			curEvent.Use();

			mousePressedOnScriptName = true;

			MonoScript script = MonoScript.FromMonoBehaviour(target);

			if (curEvent.clickCount == 2) AssetDatabase.OpenAsset(script);

			if (curEvent.holdingAlt) PingObject(script);
		}

		void hideScriptText()
		{
			Rect rect = headerElement.contentRect.SetWidth(60).MoveX(name.GetLabelWidth(isBold: true) + 60).SetHeightFromMid(15);

			// #if UNITY_2022_3_OR_NEWER
			//                 rect.x *= .94f;
			//                 rect.x += 2;
			// #endif

			rect.xMax = rect.xMax.Min(headerElement.contentRect.width - 60).Max(rect.xMin);

			rect.Draw(headerElement.contentRect.IsHovered() && (!mousePressedOnHeader || mousePressedOnScriptName) ? bgHovered : bgNorm);
		}

		void greyoutScriptName()
		{
			if (!mousePressedOnScriptName) return;

			nameRect.Resize(1).Draw(Greyscale(bgHovered.r, EditorGUIUtility.isProSkin ? .3f : .45f));
		}

		headerClick();
		scriptNameClick();

		defaultHeaderGUI();

		hideScriptText();
		greyoutScriptName();
	}

	bool mousePressedOnScriptName;
	bool mousePressedOnHeader;

	public void Update()
	{
		if (headerElement is VisualElement v && v.panel == null)
		{
			headerElement.onGUIHandler = defaultHeaderGUI;
			headerElement = null;
		}

		if (headerElement != null && headerElement.onGUIHandler == OnGUI) return;
		if (typeof(ScriptableObject).IsAssignableFrom(target.GetType())) return;
		if (!(editor.GetPropertyValue("propertyViewer") is EditorWindow window)) return;

		this.window = window;

		void findHeader(VisualElement element)
		{
			if (element == null) return;

			if (element.GetType().Name == "EditorElement")
			{
				IMGUIContainer curHeader = null;

				foreach (VisualElement child in element.Children())
				{
					curHeader = curHeader ?? new[]
					{ child as IMGUIContainer }.FirstOrDefault(r => r != null && r.name.EndsWith("Header"));

					if (curHeader is null) continue;
					if (!(child is InspectorElement)) continue;

					if (child.GetFieldValue<Editor>("m_Editor").target == target)
					{
						headerElement = curHeader;
						return;
					}
				}
			}

			foreach (VisualElement r in element.Children())
			{
				if (headerElement == null) findHeader(r);
			}
		}

		void setupGUICallbacks()
		{
			defaultHeaderGUI = headerElement.onGUIHandler;
			headerElement.onGUIHandler = OnGUI;
		}

		findHeader(window.rootVisualElement);

		if (headerElement != null) setupGUICallbacks();
	}

	IMGUIContainer headerElement;
	Action defaultHeaderGUI;

	EditorWindow window;

	public VICleanerHeader(MonoBehaviour script, Editor editor)
	{
		target = script;
		this.editor = editor;
	}

	MonoBehaviour target;
	Editor editor;

	static void UpdateAllHeaders(Editor editor) // finishedDefaultHeaderGUI
	{
		if (!(editor.target is GameObject gameObject)) return;
		if (!curEvent.isLayout) return;

		foreach (MonoBehaviour script in gameObject.GetComponents<MonoBehaviour>())
		{
			if (!cleanerHeaders.ContainsKey(script)) cleanerHeaders[script] = new (script, editor);

			cleanerHeaders[script].Update();
		}
	}

	static Dictionary<MonoBehaviour, VICleanerHeader> cleanerHeaders = new ();

#if !DISABLED
	[InitializeOnLoadMethod]
#endif
	static void Init()
	{
		if (!VIMenuItems.cleanerHeaderEnabled) return;

		Editor.finishedDefaultHeaderGUI -= UpdateAllHeaders;
		Editor.finishedDefaultHeaderGUI += UpdateAllHeaders;
	}
}
}
#endif
