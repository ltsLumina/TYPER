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
	public ComboEffect item;
	public Rarity rarity;

	public abstract void Grant(ComboEffect effect);
	public abstract void Upgrade(ComboEffect effect);
}