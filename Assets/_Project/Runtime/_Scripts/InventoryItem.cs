using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
	[SerializeField] string keys;
	[SerializeField] ComboEffect effect;
	[SerializeField] Image icon;
	[SerializeField] TMP_Text title;
	[SerializeField] CanvasGroup canvasGroup;
	
	Vector2 originalPosition;

	public ComboEffect Effect
	{
		get => effect;
		set => effect = value;
	}

	void Start()
	{
		originalPosition = transform.localPosition;
		
		title.text = effect.EffectName;
	}

	public void OnDrag(PointerEventData eventData)
	{
		transform.DOMove(eventData.position, 0.1f).SetId("drag").SetLink(gameObject);
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		canvasGroup.blocksRaycasts = false;

		var lastKeysInCombo = KeyManager.Instance.FlatKeys.Where(k => k.LastKeyInCombo);
		foreach (var key in lastKeysInCombo) key.ComboHighlight.SetActive(true);
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		var lastKeysInCombo = KeyManager.Instance.FlatKeys.Where(k => k.LastKeyInCombo);
		foreach (var key in lastKeysInCombo) key.ComboHighlight.SetActive(false);
		
		DOTween.Kill("drag");
		
		canvasGroup.blocksRaycasts = true;

		var parent = transform.parent;
		transform.SetParent(null, false);
		transform.SetParent(parent, false);
		
		transform.position = originalPosition;
		
		// log the drop result
		var keyUI = eventData.pointerCurrentRaycast.gameObject?.GetComponent<KeyUI>();
		if (keyUI != null)
		{
			Destroy(gameObject);
		}
	}
}


