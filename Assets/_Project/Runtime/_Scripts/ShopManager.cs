using System;
using System.Collections.Generic;
using DG.Tweening;
using JetBrains.Annotations;
using Lumina.Essentials.Attributes;
using Lumina.Essentials.Modules;
using TMPro;
using UnityEngine;
using VInspector;

public partial class ShopManager : MonoBehaviour
{
	[Tab("Inventory")]
	[Tooltip("Represents the items currently available for purchase in the shop.")]
	[SerializeField] List<ShopItem> stock;

	[Tab("References")]
	[SerializeField] TMP_Text enterShopText;
	[SerializeField] TMP_Text shopText;
	[SerializeField] CanvasGroup inventoryItemContainer;
	[SerializeField] GameObject inventoryItemPrefab;
	[SerializeField] CanvasGroup shopItemContainer;
	[SerializeField] GameObject shopItemPrefab;
	
	[Tab("Settings")]
	[Header("Debug")]
#pragma warning disable CS0414 // Field is assigned but its value is never used
	[SerializeField, ReadOnly] bool inShop;
#pragma warning restore CS0414 // Field is assigned but its value is never used
	[SerializeField, ReadOnly] int selectedItemIndex = -1;
	[SerializeField, ReadOnly] ShopItem selectedItem;
	[SerializeField, ReadOnly] int selectedItemCost;
	
	ShardManager shardManager;
    
	void Awake()
	{
		if (Instance != null && Instance != this) Destroy(this);
		else Instance = this;
	}
    
	void Start()
	{
		shardManager = ShardManager.Instance;

		#region Inventory
		foreach (Transform child in inventoryItemContainer.transform) Destroy(child.gameObject);

		#region add a random inventory item to the inventory UI for testing
		var inventoryItemUI = Instantiate(inventoryItemPrefab, inventoryItemContainer.transform).GetComponent<InventoryItem>();
		inventoryItemUI.transform.SetParent(inventoryItemContainer.transform);
		inventoryItemUI.name = "Test Inventory Item (Chomper)";
		inventoryItemUI.Effect = Resources.Load<ComboEffect>(ResourcePaths.Combos + "/Chomper");
		#endregion
		#endregion
		
		#region Shop
		foreach (Transform child in shopItemContainer.transform) Destroy(child.gameObject);

		foreach (ShopItem item in stock)
		{
			var itemUI = Instantiate(shopItemPrefab, shopItemContainer.transform).GetComponent<ShopItemUI>();
			itemUI.transform.SetParent(shopItemContainer.transform);
			itemUI.name = item.itemName;

			//itemUI.ItemNameText.text = item.itemName ?? itemUI.name;
			itemUI.Icon.sprite = item.icon ? Sprite.Create(item.icon, new (0, 0, item.icon.width, item.icon.height), new (0.5f, 0.5f)) : itemUI.Icon.sprite;
			itemUI.CostText.text = item.cost.ToString();

			itemUI.Item = item;
		}
		#endregion
		
		shopText.gameObject.SetActive(false);
		shopItemContainer.alpha = 0;
		inventoryItemContainer.alpha = 0;
	}

	void Update()
	{
		enterShopText.gameObject.SetActive(shardManager.QuotaReached);
		enterShopText.text = shardManager.QuotaReached && !inShop ? "Press 7 to Enter Shop" : "Press 8 to Exit Shop";
		
		if (Input.GetKeyDown(KeyCode.Alpha7)) EnterShop();
		if (Input.GetKeyDown(KeyCode.Alpha8)) ExitShop();
	}
	
	public static event Action OnEnterShop;
	public static event Action OnExitShop;

	(float fov, float keyboardX) preShopState;
	
	public void EnterShop()
	{
		if (!shardManager.QuotaReached)
		{
			Debug.LogWarning("Cannot enter shop until quota is reached.");
			return;
		} 
		
		inShop = true;
		
		preShopState = (Helpers.CameraMain.fieldOfView, KeyManager.Instance.Keyboard.transform.localPosition.x);
		
		Helpers.CameraMain.DOFieldOfView(70, 0.5f);
		var keyboard = KeyManager.Instance.Keyboard;
		keyboard.transform.DOLocalMoveX(-3f, 0.5f);
		
		shopText.gameObject.SetActive(true);
		inventoryItemContainer.DOFade(0.5f, 0.5f);
		shopItemContainer.DOFade(1, 0.5f);
		
		OnEnterShop?.Invoke();
	}

	public void ExitShop()
	{
		inShop = false;

		Helpers.CameraMain.DOFieldOfView(preShopState.fov, 0.5f);
		var keyboard = KeyManager.Instance.Keyboard;
		keyboard.transform.DOLocalMoveX(preShopState.keyboardX, 0.5f);
		
		shopText.gameObject.SetActive(false);
		inventoryItemContainer.DOFade(0, 0.35f);
		shopItemContainer.DOFade(0, 0.35f);
		
		DeselectItem();
		
		OnExitShop?.Invoke();
	}
	
	// Right click
	public void InspectItem(ShopItemUI itemUI)
	{
		var item = stock[itemUI.transform.GetSiblingIndex()];
		Debug.Log($"Inspecting {item.itemName}:\nCost: {item.cost} shards\nEffect: {item.onPurchase?.GetPersistentMethodName(0)}");
	}
	
	// left click -> pops up a little -> then confirm purchase if click again
	public void SelectItem(ShopItemUI itemUI)
	{
		selectedItemIndex = itemUI.transform.GetSiblingIndex();
		selectedItem = stock[selectedItemIndex];
		selectedItemCost = selectedItem.cost;
		Debug.Log($"Selected {selectedItem.itemName} for {selectedItemCost} shards.");
	}
	
	void DeselectItem()
	{
		selectedItem = null;
		selectedItemIndex = -1;
		selectedItemCost = 0;
	}

	public bool PurchaseSelectedItem()
	{
		if (selectedItem == null) return false;
		
		if (shardManager.SpendShards(selectedItemCost))
		{
			selectedItem.onPurchase?.Invoke();
			Debug.Log($"Purchased {selectedItem.itemName} for {selectedItemCost} shards.");
			
			// remove item from stock and UI
			stock.RemoveAt(selectedItemIndex);
			// The item destroys itself in its onPurchase event, so no need to destroy it here.
			
			selectedItem = null;
			selectedItemIndex = -1;
			selectedItemCost = 0;

			return true;
		}

		DeselectItem();
		return false;
	}
}

public partial class ShopManager // Properties
{
	public static ShopManager Instance { get; private set; }
	
	public ShopItemUI SelectedItemUI
	{
		get
		{
			if (selectedItemIndex >= 0 && selectedItemIndex < shopItemContainer.transform.childCount)
				return shopItemContainer.transform.GetChild(selectedItemIndex).GetComponent<ShopItemUI>();
			return null;
		}
	}
}
