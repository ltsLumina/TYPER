#region
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
#endregion

[Serializable] [MovedFrom(true, "Lumina", "Lumina.Essentials", "AdjacentLevelSettings")]
public class AdjacentLevelSettings : LevelSettings
{
	public KeyManager.FDirection direction = KeyManager.FDirection.Right;
}

[CreateAssetMenu(fileName = "Adjacent", menuName = "Combos/Adjacent", order = 1)]
public class CE_Adjacent : ComboEffect
{
	protected override void Invoke(Key key, (bool byKey, Key key) trigger)
	{
		var settings = GetLevelSettings<AdjacentLevelSettings>();
		key.StartCoroutine(Adjacent(settings.direction));

		return;

		IEnumerator Adjacent(KeyManager.FDirection direction)
		{
			List<Key> adjacentKeys = KeyManager.Instance.GetAdjacentKey(key, direction);

			foreach (Key adjacentKey in adjacentKeys)
			{
				KeyManager.SpawnVFX(KeyManager.CommonVFX.Combo, key.transform.position);
				adjacentKey.Activate(true, 0.5f, key);
				yield return new WaitForSecondsRealtime(0.02f);
			}
		}
	}
}
