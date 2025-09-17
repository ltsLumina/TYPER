using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public partial class Key : IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler // IPointer events
{
	Key currentKey;

	static Tooltip tooltip
	{
		get => KeyManager.Instance.Tooltip;
		set => KeyManager.Instance.Tooltip = value;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		//Logger.Log($"Key {this} hovered.");

		currentKey = GetPointerKey(eventData);

		if (DOTween.IsTweening("KeyHover")) return;
		Sequence sequence = DOTween.Sequence();

		// Highlight if it has a key or combo effect (key effect takes precedence)
		if (currentKey.keyModifier || currentKey.comboEffect)
		{
			ShowTooltip();

			sequence.AppendCallback
			(() =>
			{
				currentKey.comboHighlight.SetActive(true);
				var anim = currentKey.ComboHighlight.GetComponent<Animation>();
				anim.Play();
			});
		}

		// Otherwise just do the hover pop tween
		sequence.Append(currentKey.transform.DOScale(Vector3.one * 1.1f, 0.25f).SetEase(Ease.OutBack));

		sequence.OnKill
		(() =>
		{
			if (currentKey.comboEffect || currentKey.keyModifier)
			{
				// Only disable if it's not the next key in the combo, otherwise it will flicker off when hovering over it
				if (comboManager.NextKey != currentKey) currentKey.comboHighlight.SetActive(false);
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
				currentKey?.Activate();
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
	}

	void ShowTooltip()
	{
		if (tooltip)
		{
			tooltip.gameObject.SetActive(true);
			tooltip.transform.position = Input.mousePosition;

			if (currentKey?.keyModifier)
			{
				(string title, string description) = (currentKey.keyModifier.EffectName, currentKey.keyModifier.Description);
				tooltip.SetText(title, description);
				tooltip.SetOpacity(0.85f);
				return;
			}

			if (currentKey?.comboEffect)
			{
				(string title, string description) = (currentKey.comboEffect.EffectName, currentKey.comboEffect.Description);
				tooltip.SetText(title, description);
				tooltip.SetOpacity(0.85f);
				return;
			}

			tooltip.SetText("Empty Key", "This key has no effect assigned.");
			tooltip.SetOpacity(0.85f); // TODO: adjust opacity based on if the tooltip is hovering over other UI elements or keys
		}
		else { CreateTooltip(); }
	}

	void HideTooltip()
	{
		if (tooltip != null) tooltip.gameObject.SetActive(false);
	}

	void CreateTooltip()
	{
		GameObject canvas = GameObject.FindWithTag("Canvas");
		var prefab = Resources.Load<Tooltip>("PREFABS/Tooltip");
		tooltip ??= Instantiate(prefab, Input.mousePosition, Quaternion.identity, canvas.transform);

		(string title, string description) = (currentKey.keyModifier.EffectName, currentKey.keyModifier.Description);
		tooltip.SetText(title, description);
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
