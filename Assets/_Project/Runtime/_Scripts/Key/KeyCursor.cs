using System.Collections;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;

public class KeyCursor : MonoBehaviour
{
	[Tooltip("The default cursor texture. Set by default in the project settings.")]
	[SerializeField] Texture2D cursor;
	[Tooltip("Shown when hovering a key that can be pressed.")]
	[SerializeField] Texture2D help;
	[Tooltip("Shown when a key is on cooldown.")]
	[SerializeField] Texture2D loading;
	[Tooltip("Shown when a key has a combo effect or modifier applied.")]
	[SerializeField] Texture2D link;
	[Tooltip("Shown when moving a key (drag and drop).")]
	[SerializeField] Texture2D move;
	[Tooltip("Shown when a key is inactive.")]
	[SerializeField] Texture2D unavailable;
	[Tooltip("Not currently used.")]
	[SerializeField] Texture2D arrow; // 'cursor_up.png'

	KeyUI keyUI;
	[CanBeNull]
	Key currentKey;

	void Awake()
	{
		keyUI = GetComponent<KeyUI>();
		currentKey = GetComponent<Key>();
	}

	void Start()
	{
		SetCursor(cursor);

		keyUI.OnCursorEnter += SelectCursor;

		keyUI.OnCursorExit += ResetCursor;

		keyUI.OnClick += (_, buttonType) =>
		{
			if (!currentKey) return;
			if (buttonType == PointerEventData.InputButton.Right) SetCursor(help);
		};

		currentKey!.OnActivated += (_, _) => SelectCursor(currentKey);

		currentKey = null;
	}

	void SelectCursor(Key key)
	{
		if (!key) return;

		bool hasModifier = key.KeyModifier || key.ComboEffect;

		if (key.IsChained || (!key.IsActive && hasModifier)) SetCursor(help);
		else if (!key.IsActive) SetCursor(unavailable);
		else if (key.OnCooldown) SetCursor(loading);
		else if (key.KeyModifier || key.ComboEffect) SetCursor(help);
		else if (key.IsActive) SetCursor(link);

		currentKey = key;
	}

	void ResetCursor(Key _)
	{
		SetCursor(cursor);
		currentKey = null;
	}

	public static void SetCursor(Texture2D texture)
	{
		if (texture == null) texture = null; // Uses default (set in project settings) if null
		Cursor.SetCursor(texture, Vector2.zero, CursorMode.Auto);
	}

	public void SetCursor(Texture2D texture, float duration)
	{
		StartCoroutine(SetCursorCoroutine());

		return;

		IEnumerator SetCursorCoroutine()
		{
			SetCursor(texture);
			yield return new WaitForSecondsRealtime(duration);
			SelectCursor(currentKey);
		}
	}
}
