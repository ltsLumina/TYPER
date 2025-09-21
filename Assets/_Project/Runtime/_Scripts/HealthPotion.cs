using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Health Potion", menuName = "Shop/Health Potion")]
public class HealthPotion : PotionItem
{
	[Tooltip("Amount of Healths the potion gives when purchased.")]
	public int healAmount;

	public override void UsePotion() => /*GameManager.Instance.Health += healAmount*/ throw new NotImplementedException("would heal player but not implemented yet");
}
