using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "Modifier Item", menuName = "Shop/Modifier Item")]
public class ModifierItem : EffectItem
{
	public enum Modifiers
	{
		OffGlobalCooldown,
		Mash,
	}

	[SerializeField] string key;
	[SerializeField] Modifiers modifier;

	public string Key => key;
	public Modifiers Modifier => modifier;

	public override void Grant(List<Key> keys, Key key)
	{
		key.SetModifier(Enum.Parse<Key.Modifiers>(modifier.ToString()));

		KeyManager.SpawnVFX(KeyManager.CommonVFX.Combo, key.transform.position);
		key.transform.DOPunchScale(Vector3.one * 0.25f, 0.35f, 10, 1).SetEase(Ease.OutBack);
	}
}
