using UnityEngine;

[CreateAssetMenu(fileName = "Pulse Item", menuName = "Shop/Pulse Item")]
public class PulseItem : ComboItem
{
	public void Grant()
	{
		var lastKey = keys.ToKeys()[^1];
		var comboKeys = keys.ToKeys();
		
		ComboManager.Instance.CreateCombo(comboKeys);
		
		if (lastKey.ComboEffect == item) // already has it, upgrade instead
		{
			Upgrade(item);
		}
		else // assign new effect
		{
			lastKey.ComboEffect = item;
		}
	}

	void Upgrade(ComboEffect effect)
	{
		effect.GainLevel();
	}
}