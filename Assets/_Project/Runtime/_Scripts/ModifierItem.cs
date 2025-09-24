using System;
using System.Linq;
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
		
		key.ToKeys().SetModifier(Enum.Parse<Key.Modifiers>(modifier.ToString()));
	}
}
