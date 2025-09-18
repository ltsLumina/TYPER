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
	public KeyManager.Direction direction = KeyManager.Direction.Right;
}

[CreateAssetMenu(fileName = "Adjacent", menuName = "Combos/New Adjacent", order = 1)]
public class CE_Adjacent : ComboEffect
{
	protected override void Invoke(KeyCode keyCode, Key key, (bool byKey, Key key) trigger)
	{
		var settings = GetLevelSettings<AdjacentLevelSettings>();
		key.StartCoroutine(Adjacent(keyCode, key, settings.direction));
	}

	IEnumerator Adjacent(KeyCode keyCode, Key key, KeyManager.Direction direction)
	{
		List<Key> adjacentKeys = KeyManager.Instance.GetAdjacentKey(keyCode, direction);

		foreach (Key adjacentKey in adjacentKeys)
		{
			KeyManager.SpawnVFX(KeyManager.CommonVFX.Combo, key.transform.position);
			adjacentKey.Activate(true, 0.5f, key);
			yield return new WaitForSecondsRealtime(0.02f);
		}
	}
}
