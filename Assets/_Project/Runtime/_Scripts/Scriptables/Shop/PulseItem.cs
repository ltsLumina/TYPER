using DG.Tweening;
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

		foreach (Key key in comboKeys)
		{
			KeyManager.SpawnVFX(KeyManager.CommonVFX.Combo, key.transform.position);
			key.transform.DOPunchScale(Vector3.one * 0.25f, 0.35f, 10, 1).SetEase(Ease.OutBack);
		}
	}

	void Upgrade(ComboEffect effect)
	{
		effect.GainLevel();
	}
}