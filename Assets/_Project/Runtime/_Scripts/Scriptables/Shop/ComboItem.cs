using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "Combo Item", menuName = "Shop/Combo Item")]
public class ComboItem : EffectItem
{
	[SerializeField] ComboEffect effect;

	void OnEnable() => itemName = effect ? effect.EffectName : "Undefined Combo Item";

	protected override void OnValidate()
	{
		base.OnValidate();

		Debug.Assert(effect != null, $"ComboItem '{itemName}' has no ComboEffect assigned!");
	}

	public override void Grant(List<Key> keys, Key key)
	{
		ComboManager.Instance.CreateCombo(keys);

		if (key.ComboEffect == effect) Upgrade(effect);
		else key.ComboEffect = effect;

		foreach (Key k in keys)
		{
			KeyManager.SpawnVFX(KeyManager.CommonVFX.Combo, k.transform.position);
			k.transform.DOPunchScale(Vector3.one * 0.25f, 0.35f, 10, 1).SetEase(Ease.OutBack);
		}
	}

	void Upgrade(ComboEffect effect) => effect.GainLevel();
}
