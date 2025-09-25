using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Health Potion", menuName = "Shop/Health Potion")]
public class HealthPotion : PotionItem
{
	[Tooltip("Amount of Healths the potion gives when purchased.")]
	public int healAmount;

	public override void UsePotion() => GameManager.Instance.Heal(healAmount);
}
