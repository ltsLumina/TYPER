using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The instance of a shop item in the UI.
/// Handles user interactions such as hover and click events.
/// </summary>
public partial class ShopItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
	//[SerializeField] TMP_Text itemNameText;
	[SerializeField] Image icon;
	[SerializeField] TMP_Text costText;
	
	Sequence hoverSequence;
	Sequence selectedSequence;
	Sequence purchaseSequence;
	Sequence deselectSequence;

	void Awake() => tooltip = null;

	public void OnPointerEnter(PointerEventData eventData)
	{
		ShowTooltip();
		
		if (IsPlaying(purchaseSequence)) return;
		
		hoverSequence = DOTween.Sequence();
		hoverSequence.Append(transform.DOScale(Vector3.one * 1.1f, 0.25f).SetEase(Ease.OutBack));
		hoverSequence.SetId("enter");
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		HideTooltip();
		
		DOTween.Kill("enter");
		
		if (IsPlaying(purchaseSequence)) return;
		transform.DOScale(Vector3.one, 0.1f).SetEase(Ease.OutBack).SetLink(gameObject);
	}

	int confirmations;

	public void OnPointerClick(PointerEventData eventData)
	{
		if (IsPlaying(purchaseSequence)) return;
		
		switch (eventData.button)
		{
			case PointerEventData.InputButton.Left:
				ShopManager.Instance.SelectItem(this);
				confirmations++;

				switch (confirmations)
				{
					case 1:
						selectedSequence = DOTween.Sequence();
						selectedSequence.Append(transform.DOMoveY(transform.position.y + 10f, 0.25f).SetEase(Ease.OutBack));
						selectedSequence.OnComplete(() => transform.DOMoveY(transform.position.y, 0.1f).SetEase(Ease.OutQuad));
						selectedSequence.SetLink(gameObject);
						break;

					case >= 2:
						if (ShopManager.Instance.PurchaseSelectedItem())
						{
							purchaseSequence = DOTween.Sequence();
							purchaseSequence.Append(transform.DORotate(new Vector3(0, 360f, 45), 0.75f, RotateMode.FastBeyond360).SetEase(Ease.InBack));
							purchaseSequence.AppendInterval(0.25f);
							purchaseSequence.Join(transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack));
							purchaseSequence.SetLink(gameObject);
							purchaseSequence.OnComplete(() => Destroy(gameObject));
						}
						else
						{
							DOTween.Kill(transform);

							deselectSequence = DOTween.Sequence();
							deselectSequence.Append(transform.DOPunchPosition(new (-10f, 0, 0), 0.2f, 10, 1f).SetLoops(1, LoopType.Yoyo).SetEase(Ease.OutQuad));
							deselectSequence.AppendInterval(0.1f);
							deselectSequence.Append(transform.DOMoveY(transform.position.y - 10f, 0.1f).SetEase(Ease.OutQuad));
							deselectSequence.SetLink(gameObject);
						}

						confirmations = 0;
						break;
				}
				break;

			case PointerEventData.InputButton.Right:
				ShopManager.Instance.InspectItem(this);
				break;

			case PointerEventData.InputButton.Middle:
				break;

			default:
				throw new ArgumentOutOfRangeException();
		}
	}
	
	public ShopItem Item { get; set; }

	bool IsPlaying(Sequence sequence) => sequence != null && sequence.IsActive() && sequence.IsPlaying();

	#region Tooltip
	static ShopItemTooltip tooltip; // static because there should only ever be one tooltip (of this type) visible at a time

	void ShowTooltip()
	{
		if (tooltip)
		{
			tooltip.gameObject.SetActive(true);
			tooltip.transform.position = Input.mousePosition;

			switch (Item)
			{
				case ComboItem combo: {
					(string title, string description) = (name, $"Cost: {costText.text} shards" + "\n" + 
					                                            $"Keys: {combo.Keys}" + "\n" + 
					                                            "Click to select. " + "\n" +
					                                            "Click again to confirm purchase.");
					tooltip.SetText(title, description);
					break;
				}

				case ModifierItem modifier: {
					(string title, string description) = (name, $"Cost: {costText.text} shards" + "\n" + 
					                                            $"Key: {modifier.Key}" + "\n" +
					                                            "Click to select. " + "\n" +
					                                            "Click again to confirm purchase.");
					tooltip.SetText(title, description);
					break;
				}
			}
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
		var prefab = Resources.Load<ShopItemTooltip>("PREFABS/Shop Item Tooltip");
		tooltip ??= Instantiate(prefab, Input.mousePosition, Quaternion.identity, canvas.transform);

		ShowTooltip();
	}
	#endregion
}

public partial class ShopItemUI // properties
{
	public Image Icon
	{
		get => icon;
		set => icon = value;
	}
	// public TMP_Text ItemNameText
	// {
	// 	get => itemNameText;
	// 	set => itemNameText = value;
	// }
	public TMP_Text CostText
	{
		get => costText;
		set => costText = value;
	}
}
