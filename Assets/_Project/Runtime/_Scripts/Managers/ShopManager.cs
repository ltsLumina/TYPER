using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Lumina.Essentials.Attributes;
using Lumina.Essentials.Modules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VInspector;
using Random = System.Random;

public partial class ShopManager : MonoBehaviour
{
	[Tab("Shop")]
	[Tooltip("Represents the items currently available for purchase in the shop.")]
	[SerializeField] List<ShopItem> stock;

	[Tab("References")]
	[SerializeField] TMP_Text enterShopText;
	[SerializeField] TMP_Text shopText;
	[SerializeField] Image shopFrame;
	[SerializeField] CanvasGroup inventoryItemContainer;
	[SerializeField] GameObject inventoryItemPrefab;
	[SerializeField] CanvasGroup shopItemContainer;
	[SerializeField] GameObject shopItemPrefab;
	[SerializeField] PostPurchasePrompt postPurchasePrompt;
	[SerializeField] Image shopOverlay;

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
		
		foreach (Transform child in inventoryItemContainer.transform) Destroy(child.gameObject);

		ShuffleStock();

		shopText.gameObject.SetActive(false);
		shopItemContainer.alpha = 0;
		//inventoryItemContainer.alpha = 0;
	}

	void Update()
	{
		if (inShop) enterShopText.text = "Press 8 to exit shop";
		else if (shardManager.QuotaReached) enterShopText.text = "Press 7 to enter shop";
		else enterShopText.text = "Reach shard quota to enter shop";

		if (Input.GetKeyDown(KeyCode.Alpha7)) EnterShop();
		if (Input.GetKeyDown(KeyCode.Alpha8)) ExitShop();
	}

	public static event Action OnEnterShop;
	public static event Action OnExitShop;

	(float fov, Vector2 keyboardPos) preShopState;

	public void EnterShop()
	{
		if (!shardManager.QuotaReached)
		{
			Debug.LogWarning("Cannot enter shop until quota is reached.");
			return;
		}

		if (inShop) return;

		inShop = true;

		ShuffleStock();

		KeyManager.Instance.SetInputMode(KeyManager.InputMode.Disabled, KeyManager.InputModeReason.Shop);

		var enemySpawner = FindAnyObjectByType<EnemySpawner>(FindObjectsInactive.Include);
		enemySpawner.PauseSpawner();

		foreach (Enemy enemySpawnerEnemy in enemySpawner.enemies)
		{
			enemySpawnerEnemy.Reset();
			ObjectPoolManager.ReturnToPool(enemySpawnerEnemy.gameObject);
		}

		preShopState = (Helpers.CameraMain.fieldOfView, KeyManager.Instance.Keyboard.transform.localPosition);

		Helpers.CameraMain.DOFieldOfView(70, 0.5f);
		var keyboard = KeyManager.Instance.Keyboard;
		keyboard.transform.DOLocalMove(new Vector2(3.75f, 1.5f), 0.5f);

		shopText.gameObject.SetActive(true);
		shopFrame.DOFade(1, 0.5f);
		shopItemContainer.DOFade(1, 0.5f);
		shopItemContainer.blocksRaycasts = true;
		
		inventoryItemContainer.DOFade(1, 0.5f);
		inventoryItemContainer.blocksRaycasts = true;

		OnEnterShop?.Invoke();
	}

	public void ExitShop()
	{
		if (!inShop) return; // can't exit if not in shop

		inShop = false;

		KeyManager.Instance.SetInputMode(KeyManager.InputMode.Enabled);

		var enemySpawner = FindAnyObjectByType<EnemySpawner>(FindObjectsInactive.Include);
		enemySpawner.PlaySpawner();

		Helpers.CameraMain.DOFieldOfView(preShopState.fov, 0.5f);
		var keyboard = KeyManager.Instance.Keyboard;
		keyboard.transform.DOLocalMove(preShopState.keyboardPos, 0.5f);

		shopText.gameObject.SetActive(false);
		inventoryItemContainer.DOFade(0, 0.35f);
		shopItemContainer.DOFade(0, 0.35f);
		
		shopFrame.DOFade(0, 0.5f);
		shopItemContainer.blocksRaycasts = false;
		inventoryItemContainer.blocksRaycasts = false;
		
		DeselectItem();

		OnExitShop?.Invoke();
	}

		/// <summary>
	/// Randomizes and reshuffles the stock of items available in the shop.
	/// </summary>
	void ShuffleStock()
	{
		// -- Stock randomization --

		Random rng = new ();

		const int comboItems = 3;
		const int modifierItems = 1;
		const int potions = 2;
		const int totalItems = comboItems + modifierItems + potions;

		stock = new (totalItems);

		List<ShopItem> inventory = Resources.LoadAll<ShopItem>(ResourcePaths.SHOP).ToList();
		List<ShopItem> comboItemsList = inventory.Where(i => i is ComboItem).ToList();
		List<ShopItem> modifierItemsList = inventory.Where(i => i is ModifierItem).ToList();
		List<ShopItem> potionsList = inventory.Where(i => i is PotionItem).ToList();

		stock.Clear();

		for (int i = 0; i < comboItems; i++)
		{
			var item = GetRandomByRarity(comboItemsList, rng);
			stock.Add(item);
		}

		var usedModifierIndices = new HashSet<int>();

		for (int i = 0; i < modifierItems; i++)
		{
			int index;

			do index = rng.Next(modifierItemsList.Count);
			while (!usedModifierIndices.Add(index) && usedModifierIndices.Count < modifierItemsList.Count);

			stock.Add(modifierItemsList[index]);
		}

		// TODO: vvv
		// potions are always included last in the stock, and there's always at least one of each (shard and health potions)
		//stock.Add(potionsList.First(i => i is ShardPotion));
		//stock.Add(potionsList.First(i => i is HealthPotion));

		// ensure there are 4 shop items and 2 potions
		//Debug.Assert(stock.Count == comboItems + modifierItems + potions, $"Stock count mismatch: {stock.Count} != {comboItems + modifierItems + potions}");

		// -- UI --

		foreach (Transform child in shopItemContainer.transform) Destroy(child.gameObject);

		foreach (ShopItem item in stock)
		{
			var itemUI = Instantiate(shopItemPrefab, shopItemContainer.transform).GetComponent<ShopItemUI>();
			itemUI.transform.SetParent(shopItemContainer.transform);
			itemUI.name = item.ItemName;
			itemUI.Icon.sprite = item.Icon ? Sprite.Create(item.Icon, new (0, 0, item.Icon.width, item.Icon.height), new (0.5f, 0.5f)) : itemUI.Icon.sprite;
			itemUI.CostText.text = item.Cost.ToString();
			itemUI.Item = item;
		}

		return;
		static ShopItem GetRandomByRarity(List<ShopItem> items, Random rng)
		{
			float roll = (float) rng.NextDouble();
			float cumulative = 0f;

			foreach (ShopItem item in items)
			{
				cumulative += item.GetRarityChance();
				if (roll <= cumulative) return item;
			}

			return items.Last(); // fallback
		}
	}
	
	// Right click
	public void InspectItem(ShopItemUI itemUI)
	{
		var item = stock[itemUI.transform.GetSiblingIndex()];
		Debug.Log($"Inspecting {item.ItemName}:\nCost: {item.Cost} shards\nEffect: {item.OnPurchase?.GetPersistentMethodName(0)}");
	}

	// left click -> pops up a little -> then confirm purchase if click again
	public void SelectItem(ShopItemUI itemUI)
	{
		if (SelectedItemUI != itemUI) SelectedItemUI?.Deselect();

		selectedItemIndex = itemUI.transform.GetSiblingIndex();
		selectedItem = stock[selectedItemIndex];
		selectedItemCost = selectedItem.Cost;
		Debug.Log($"Selected {selectedItem.ItemName} for {selectedItemCost} shards.");
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
			// switch (selectedItem)
			// {
			// 	case EffectItem effectItem:
			// 		effectItem.Grant();
			// 		break;
			//
			// 	case PotionItem potionItem:
			// 		potionItem.UsePotion();
			// 		break;
			// }
			//
			// selectedItem.OnPurchase?.Invoke();
			
			Debug.Log($"Purchased {selectedItem.ItemName} for {selectedItemCost} shards.");
			postPurchasePrompt.Show(selectedItem.ItemName, selectedItem.MinActivationKeys);
			var item = SelectedItemUI;
			postPurchasePrompt.OnHide += keys => item.SetPayload(keys);

			// The item itself "removes" itself from the shop by disabling its UI element.

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
