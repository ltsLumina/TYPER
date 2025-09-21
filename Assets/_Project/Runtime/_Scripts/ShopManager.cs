using System.Collections.Generic;
using Lumina.Essentials.Attributes;
using UnityEngine;
using VInspector;

public partial class ShopManager : MonoBehaviour
{
	[Tab("Inventory")]
	[Tooltip("Represents the items currently available for purchase in the shop.")]
	[SerializeField] List<ShopItem> stock;
	
	[Tab("References")]
	[SerializeField] Transform shopItemContainer;
	[SerializeField] GameObject shopItemPrefab;
	
	[Tab("Settings")]
	[Header("Debug")]
	[SerializeField, ReadOnly] bool inShop;
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
		
		// replace the shopItemContainer children with shop items from stock
		foreach (Transform child in shopItemContainer)
			Destroy(child.gameObject);

		foreach (var item in stock)
		{
			var itemUI = Instantiate(shopItemPrefab, shopItemContainer).GetComponent<ShopItemUI>();
			itemUI.transform.SetParent(shopItemContainer);
			itemUI.name = item.itemName;
			//itemUI.Icon = TO-DO
			//itemUI.ItemNameText.text = item.itemName ?? itemUI.name;
			itemUI.CostText.text = item.cost.ToString();
		}
	}

	public void EnterShop() { }
	public void ExitShop() { }
	
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
			//Destroy(shopItemContainer.GetChild(selectedItemIndex).gameObject);
			
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
			if (selectedItemIndex >= 0 && selectedItemIndex < shopItemContainer.childCount)
				return shopItemContainer.GetChild(selectedItemIndex).GetComponent<ShopItemUI>();
			return null;
		}
	}
}
