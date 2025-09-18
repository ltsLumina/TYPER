using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class RailgunLevelSettings : LevelSettings
{
	public KeyManager.Direction direction = KeyManager.Direction.Right;
}

[CreateAssetMenu(fileName = "Railgun", menuName = "Combos/New Railgun", order = 8)]
public class CE_Railgun : ComboEffect
{
	protected override void Invoke(Key key, (bool byKey, Key key) trigger)
	{
		var settings = GetLevelSettings<RailgunLevelSettings>();
		key.StartCoroutine(Freeze(key, settings.direction));
	}

	IEnumerator Freeze(Key key, KeyManager.Direction direction)
	{
		// get the front three keys in a wall (top right, middle right, bottom right)
		var railgunKeys = KeyManager.Instance.GetRailgunKeys(key, direction);

		foreach (Key k in railgunKeys)
		{
			KeyManager.SpawnVFX(KeyManager.CommonVFX.Combo, key.transform.position);
			k.Activate(true, 0.5f, key);
			yield return new WaitForSecondsRealtime(0.02f);
		}
	}
}
