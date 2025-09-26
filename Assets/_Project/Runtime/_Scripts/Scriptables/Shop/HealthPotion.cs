using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Health Potion", menuName = "Shop/Health Potion")]
public class HealthPotion : PotionItem
{
	[Tooltip("Amount of Healths the potion gives when purchased.")]
	[Range(1, 10)]
	public int healAmount;

	void OnEnable()
	{
		description = $"Restores {healAmount} Health.";
		itemName = "Health Potion";
	}

	public override void UsePotion() => GameManager.Instance.Heal(healAmount);
}
