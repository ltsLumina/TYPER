using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public abstract partial class ShopItem : ScriptableObject
{
	public enum Rarity
	{
		Common,   // 60%
		Uncommon, // 25%
		Rare,     // 10%
		Epic,     // 4%
		Legendary // 1%
	}

	[SerializeField] protected string itemName;
	[Multiline(2)]
	[SerializeField] protected string description;
	[SerializeField] protected int cost;
	[SerializeField] protected Texture2D icon;
	[SerializeField] protected Rarity rarity;
	[SerializeField] protected int minActivationKeys = 3;
	[Space(10)]
	[SerializeField] protected UnityEvent onPurchase;

	protected virtual void OnValidate()
	{
		// validate that name is suffixed with the parent type of the item (e.g., "Potion" for PotionItem)
		if (this is PotionItem && !name.EndsWith("Potion")) Debug.LogWarning($"PotionItem '{name}' should have a name ending with 'Potion'.", this);
		if (this is ComboItem && !name.EndsWith("Item")) Debug.LogWarning($"ComboItem '{name}' should have a name ending with 'Item'.", this);
		if (this is ModifierItem && !name.EndsWith("Modifier")) Debug.LogWarning($"ModifierItem '{name}' should have a name ending with 'Modifier'.", this);
	}

	public float GetRarityChance() => rarity switch
	{ Rarity.Common    => 0.50f,
	  Rarity.Uncommon  => 0.30f,
	  Rarity.Rare      => 0.16f,
	  Rarity.Epic      => 0.08f,
	  Rarity.Legendary => 0.02f,
	  _                => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, null) };
}

public abstract partial class ShopItem // Properties
{
	public string ItemName => itemName;
	public string Description => description;
	public int Cost => cost;
	public Texture2D Icon => icon;
	public Rarity ItemRarity => rarity;
	public int MinActivationKeys => minActivationKeys;
	public UnityEvent OnPurchase => onPurchase;
}

public abstract class PotionItem : ShopItem
{
	public abstract void UsePotion();
}

public abstract class EffectItem : ShopItem
{
	public abstract void Grant(List<Key> keys, Key key);
}
