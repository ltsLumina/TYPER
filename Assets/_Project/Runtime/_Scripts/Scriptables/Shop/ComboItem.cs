using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;
using VInspector;
using Random = UnityEngine.Random;

[CreateAssetMenu(fileName = "Combo Item", menuName = "Shop/Combo Item")]
public class ComboItem : EffectItem
{
	[SerializeField] string keys = "???";
	[SerializeField] ComboEffect combo;

	public string Keys => keys;

	void OnEnable()
	{
		keys = keys.ToUpper();

		itemName = combo ? combo.EffectName : "Undefined Combo Item";
		description = $"Grants the combo effect '{combo.EffectName}' to the last key in the combo '{keys}'. " + "\n" + "If the last key already has this effect, it upgrades the effect instead.";
	}

	protected override void OnValidate()
	{
		base.OnValidate();

		keys = keys.ToUpper();

		Debug.Assert(combo != null, $"ComboItem '{itemName}' has no ComboEffect assigned!");
		Debug.Assert(!string.IsNullOrEmpty(keys) && keys != "???", $"ComboItem '{itemName}' has no keys assigned!");
	}

	public override void Grant()
	{
		Key lastKey = keys.ToKeys()[^1];
		List<Key> comboKeys = keys.ToKeys();

		ComboManager.Instance.CreateCombo(comboKeys);

		if (lastKey.ComboEffect == combo) Upgrade(combo);
		else lastKey.ComboEffect = combo;

		foreach (Key key in comboKeys)
		{
			KeyManager.SpawnVFX(KeyManager.CommonVFX.Combo, key.transform.position);
			key.transform.DOPunchScale(Vector3.one * 0.25f, 0.35f, 10, 1).SetEase(Ease.OutBack);
		}
	}

	void Upgrade(ComboEffect effect) => effect.GainLevel();

	[Button, ButtonSize(20), UsedImplicitly]
	void RandomizeKeys()
	{
		var possibleKeys = new List<KeyCode>(KeyboardData.Layouts.QWERTY.Alphabetic);

		keys = string.Concat
		(Enumerable.Range(0, 3)
		           .Select
		            (_ =>
		            {
			            int idx = Random.Range(0, possibleKeys.Count);
			            KeyCode key = possibleKeys[idx];
			            possibleKeys.RemoveAt(idx);
			            return key;
		            }));
	}
}
