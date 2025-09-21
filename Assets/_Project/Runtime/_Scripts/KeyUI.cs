using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

public class KeyUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler // IPointer events
{
	Key currentKey;

	static Tooltip tooltip
	{
		get => KeyManager.Instance.Tooltip;
		set => KeyManager.Instance.Tooltip = value;
	}
	
	public event Action<Key> OnCursorEnter;
	public event Action<Key> OnCursorExit;
	public event Action<Key, PointerEventData.InputButton> OnClick;

	public void OnPointerEnter(PointerEventData eventData)
	{
		//Logger.Log($"Key {this} hovered.");

		currentKey = GetPointerKey(eventData);
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
				//Logger.Log($"Key {this} clicked.");
				currentKey.Activate();
				currentKey.ComboHighlight.SetActive(false); // mostly for 'Loose' key modifier.
				break;

			case PointerEventData.InputButton.Right:
				//Logger.Log($"Key {this} right-clicked.");

				// Toggle off if already active
				if (tooltip && tooltip.gameObject.activeInHierarchy)
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
		if (tooltip)
		{
			tooltip.gameObject.SetActive(true);
			tooltip.transform.position = Input.mousePosition;

			if (currentKey?.KeyModifier)
			{
				(string title, string description) = (currentKey.KeyModifier.EffectName, currentKey.KeyModifier.Description);
				tooltip.SetText(title, description);
				tooltip.SetOpacity(0.85f);
				return;
			}

			if (currentKey?.ComboEffect)
			{
				(string title, string description) = (currentKey.ComboEffect.EffectName, currentKey.ComboEffect.Description);
				tooltip.SetText(title, description);
				tooltip.SetOpacity(0.85f);
				return;
			}

			tooltip.SetText("Empty Key", "This key has no effect assigned.");
			tooltip.SetOpacity(0.85f); // TODO: adjust opacity based on if the tooltip is hovering over other UI elements or keys
		}
		else CreateTooltip();
	}

	void HideTooltip()
	{
		if (tooltip != null) tooltip.gameObject.SetActive(false);
	}

	void CreateTooltip()
	{
		GameObject canvas = GameObject.FindWithTag("Canvas");
		var prefab = Resources.Load<Tooltip>("PREFABS/Tooltip");
		tooltip ??= Object.Instantiate(prefab, Input.mousePosition, Quaternion.identity, canvas.transform);

		if (currentKey.KeyModifier)
		{
			(string title, string description) = (currentKey.KeyModifier.EffectName, currentKey.KeyModifier.Description);
			tooltip.SetText(title, description);
		}
		else if (currentKey.ComboEffect)
		{
			(string title, string description) = (currentKey.ComboEffect.EffectName, currentKey.ComboEffect.Description);
			tooltip.SetText(title, description);
		}
	}

	GameObject GetPointerGameObject(PointerEventData pointerEventData)
	{
		var currentObject = pointerEventData.pointerCurrentRaycast.gameObject;
		return currentObject;
	}

	Key GetPointerKey(PointerEventData pointerEventData)
	{
		var currentObject = pointerEventData.pointerCurrentRaycast.gameObject;
		return currentObject?.GetComponent<Key>();
	}
}
