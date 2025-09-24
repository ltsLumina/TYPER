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
	public int cost;
	public Texture2D icon;
	[Space(10)]
	public UnityEvent onPurchase;
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
	protected string keys = "???";
	protected ComboEffect item = null;

	public string Keys => keys;
	public ComboEffect Item => item;

	void OnEnable()
	{
		itemName = item ? item.EffectName : "Undefined Combo Item";
		Debug.Assert(item != null, $"ComboItem '{itemName}' has no ComboEffect assigned!");
	}
}