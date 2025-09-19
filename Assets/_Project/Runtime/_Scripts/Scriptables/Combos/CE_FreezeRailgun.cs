using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class FreezeRailgunLevelSettings : LevelSettings
{
	public float duration = 5f;
	public KeyManager.Direction direction = KeyManager.Direction.Right;
	public int rows = 1;
}

[CreateAssetMenu(fileName = "(WIP) Freeze-Railgun", menuName = "Combos/(WIP) Freeze-Railgun", order = 7)]
public class CE_FreezeRailgun : ComboEffect
{
	protected override void Invoke(Key key, (bool byKey, Key key) trigger)
	{
		var settings = GetLevelSettings<FreezeRailgunLevelSettings>();
		var railgunKeys = KeyManager.Instance.GetRailgunKeys(key, settings.direction, settings.rows);

		if (railgunKeys.upperLane != null && railgunKeys.upperLane.Any()) key.StartCoroutine(FreezeLane(railgunKeys.upperLane, settings.duration));
		if (railgunKeys.centerLane != null && railgunKeys.centerLane.Any()) key.StartCoroutine(FreezeLane(railgunKeys.centerLane, settings.duration));
		if (railgunKeys.lowerLane != null && railgunKeys.lowerLane.Any()) key.StartCoroutine(FreezeLane(railgunKeys.lowerLane, settings.duration));

		return;

		IEnumerator FreezeLane(IEnumerable<Key> lane, float duration)
		{
			IEnumerable<Key> keys = lane.ToList();

			foreach (var k in keys)
			{
				//KeyManager.SpawnVFX(KeyManager.CommonVFX.Combo, key.transform.position);
				k.AddModifier(Key.Modifiers.Frozen);
				k.KeyModifier?.Invoke(k, null);
				yield return new WaitForSecondsRealtime(0.05f);
			}

			yield return new WaitForSecondsRealtime(duration);

			foreach (var k in keys)
			{
				//KeyManager.SpawnVFX(KeyManager.CommonVFX.Combo, key.transform.position);
				k.RemoveModifier(Key.Modifiers.Frozen);
				k.KeyModifier?.Invoke(k, null);
				yield return new WaitForSecondsRealtime(0.05f);
			}
		}
	}
}
