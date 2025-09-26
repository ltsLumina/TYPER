using System;
using UnityEngine;
using UnityEngine.Events;

public abstract class ShopItem : ScriptableObject
{
	public enum Rarity
	{
		Common,
		Uncommon,
		Rare,
		Epic,
		Legendary
	}
	
	public string itemName;
	public string description;
	public int cost;
	public Texture2D icon;
	[Space(10)]
	public UnityEvent onPurchase;

	void OnValidate()
	{
		// validate that name is suffixed with the parent type of the item (e.g., "Potion" for PotionItem)
		if (this is PotionItem && !name.EndsWith("Potion"))
			Debug.LogWarning($"PotionItem '{name}' should have a name ending with 'Potion'.", this);
		if (this is ComboItem && !name.EndsWith("Item"))
			Debug.LogWarning($"EffectItem '{name}' should have a name ending with 'Item'.", this);
		if (this is ModifierItem && !name.EndsWith("Modifier"))
			Debug.LogWarning($"ModifierItem '{name}' should have a name ending with 'Modifier'.", this);
	}
}

public abstract class PotionItem : ShopItem
{
	public abstract void UsePotion();
}

public abstract class EffectItem : ShopItem
{
	public Rarity rarity;
}

public class ComboItem : EffectItem
{
	public string keys = "???";
	public ComboEffect item;

	void OnEnable()
	{
		itemName = item ? item.EffectName : "Undefined Combo Item";
		Debug.Assert(item != null, $"ComboItem '{itemName}' has no ComboEffect assigned!");
	}
}