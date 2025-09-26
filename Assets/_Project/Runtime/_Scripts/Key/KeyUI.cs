using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class KeyUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler /*, IDropHandler*/ // IPointer events
{
	Key currentKey;

	static KeyTooltip KeyTooltip { get; set; }

	public event Action<Key> OnCursorEnter;
	public event Action<Key> OnCursorExit;
	public event Action<Key, PointerEventData.InputButton> OnClick;

	void Awake() => KeyTooltip = null;

	public void OnPointerEnter(PointerEventData eventData)
	{
		//Logger.Log($"Key {this} hovered.");

		currentKey = GetPointerKey(eventData);
		if (currentKey.IsRemoved) return; // don't interact with removed keys, but keep interacting with disabled keys

		OnCursorEnter?.Invoke(currentKey);

		if (DOTween.IsTweening("KeyHover")) return;
		Sequence sequence = DOTween.Sequence();

		// Highlight if it has a key or combo effect (key effect takes precedence)
		if (currentKey.KeyModifier || currentKey.ComboEffect)
		{
			ShowTooltip();

			sequence.AppendCallback
			(() =>
			{
				currentKey.ComboHighlight.SetActive(true);
				var anim = currentKey.ComboHighlight.GetComponent<Animation>();
				anim.Play();
			});
		}

		// Otherwise just do the hover pop tween
		sequence.Append(currentKey.transform.DOScale(Vector3.one * 1.1f, 0.25f).SetEase(Ease.OutBack));

		sequence.OnKill
		(() =>
		{
			if (currentKey.ComboEffect || currentKey.KeyModifier)
			{
				// Only disable if it's not the next key in the combo, otherwise it will flicker off when hovering over it
				if (ComboManager.Instance.NextKey != currentKey) currentKey.ComboHighlight.SetActive(false);
			}

			currentKey.transform.DOScale(Vector3.one, 0.1f).SetEase(Ease.OutBack);
		});

		sequence.SetId("KeyHover");
		sequence.SetAutoKill(false);
		sequence.Play();
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		//Logger.Log($"Key {this} unhovered.");

		DOTween.Kill("KeyHover");
		OnCursorExit?.Invoke(currentKey);
		currentKey = null;

		HideTooltip();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		currentKey = GetPointerKey(eventData);

		switch (eventData.button)
		{
			case PointerEventData.InputButton.Left:
				if (!currentKey.IsActive) return;

				currentKey.Activate();
				currentKey.ComboHighlight.SetActive(false); // mostly for 'Loose' key modifier.
				break;

			case PointerEventData.InputButton.Right:
				// Toggle off if already active
				if (KeyTooltip && KeyTooltip.gameObject.activeInHierarchy)
				{
					HideTooltip();
					return;
				}

				ShowTooltip();
				break;
		}

		OnClick?.Invoke(currentKey, eventData.button);
	}

	void ShowTooltip()
	{
		if (KeyTooltip)
		{
			KeyTooltip.gameObject.SetActive(true);
			KeyTooltip.transform.position = Input.mousePosition;

			if (currentKey?.KeyModifier)
			{
				(string title, string description) = (currentKey.KeyModifier.EffectName, currentKey.KeyModifier.Description);
				KeyTooltip.SetText(title, description);
				KeyTooltip.SetOpacity(0.85f);
				return;
			}

			if (currentKey?.ComboEffect)
			{
				(string title, string description) = ($"{currentKey.ComboEffect.EffectName} {currentKey.ComboEffect.Level}", currentKey.ComboEffect.Description);
				KeyTooltip.SetText(title, description);
				KeyTooltip.SetOpacity(0.85f);
				return;
			}

			KeyTooltip.SetText("Empty Key", "This key has no effect assigned.");
			KeyTooltip.SetOpacity(0.85f); // TODO: adjust opacity based on if the tooltip is hovering over other UI elements or keys
		}
		else CreateTooltip();
	}

	void HideTooltip()
	{
		if (KeyTooltip != null) KeyTooltip.gameObject.SetActive(false);
	}

	void CreateTooltip()
	{
		GameObject canvas = GameObject.FindWithTag("Canvas");
		var prefab = Resources.Load<KeyTooltip>("PREFABS/Key Tooltip");
		KeyTooltip ??= Instantiate(prefab, Input.mousePosition, Quaternion.identity, canvas.transform);

		if (currentKey.KeyModifier)
		{
			(string title, string description) = (currentKey.KeyModifier.EffectName, currentKey.KeyModifier.Description);
			KeyTooltip.SetText(title, description);
		}
		else if (currentKey.ComboEffect)
		{
			(string title, string description) = ($"{currentKey.ComboEffect.EffectName} {currentKey.ComboEffect.Level}", currentKey.ComboEffect.Description);
			KeyTooltip.SetText(title, description);
		}
	}

	Key GetPointerKey(PointerEventData pointerEventData)
	{
		var currentObject = pointerEventData.pointerCurrentRaycast.gameObject;
		return currentObject?.GetComponent<Key>();
	}

	// public void OnDrop(PointerEventData data)
	// {
	// 	// if the dragged object is an InventoryItem, log it
	// 	var draggedObject = data.pointerDrag;
	//
	// 	if (draggedObject != null && draggedObject.TryGetComponent<InventoryItem>(out var item))
	// 	{
	// 		Debug.Log($"Dropped {draggedObject.name} onto {gameObject.name}");
	// 		
	// 		GetComponent<Key>().ComboEffect = item.Effect;
	// 	}
	// }
}
