using System;
using System.Linq;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "Modifier Item", menuName = "Shop/Modifier Item")]
public class ModifierItem : EffectItem
{
	public enum Modifiers
	{
		OffGlobalCooldown,
	}
	
	public string key;
	public Modifiers modifier;

	public string Key => key;

	public void Grant()
	{
		if (key.Length != 1)
		{
			Logger.LogError($"Key string must be a single character. Got '{key}'" 
			                + "\n" + "Using last character instead.", this, "ModifierItem.Grant");
			key = key.Last().ToString();
		}
		
		key.ToKey().SetModifier(Enum.Parse<Key.Modifiers>(modifier.ToString()));

		KeyManager.SpawnVFX(KeyManager.CommonVFX.Combo, key.ToKey().transform.position);
		key.ToKey().transform.DOPunchScale(Vector3.one * 0.25f, 0.35f, 10, 1).SetEase(Ease.OutBack);
	}
}
