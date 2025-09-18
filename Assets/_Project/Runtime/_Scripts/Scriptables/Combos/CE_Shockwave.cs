using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ShockwaveLevelSettings : LevelSettings
{
	public int aftershocks;
}

[CreateAssetMenu(fileName = "Shockwave", menuName = "Combos/New Shockwave", order = 2)]
public class CE_Shockwave : ComboEffect
{
	protected override void Invoke(KeyCode keyCode, Key key, (bool byKey, Key key) trigger)
	{
		var settings = GetLevelSettings<ShockwaveLevelSettings>();
		key.StartCoroutine(Shockwave(keyCode, key, settings.aftershocks));
	}
	
	IEnumerator Shockwave(KeyCode keyCode, Key key, int aftershocks)
	{
		List<Key> surroundingKeys = KeyManager.Instance.GetSurroundingKeys(keyCode);

		foreach (Key k in surroundingKeys)
		{
			KeyManager.SpawnVFX(KeyManager.CommonVFX.Combo, key.transform.position);
			k.Activate(true, 0.5f, key);
			yield return new WaitForSecondsRealtime(0.02f);
		}
		
		yield return new WaitForSecondsRealtime(0.5f); // Wait before triggering aftershocks
		
		if (aftershocks > 0)
		{
			yield return new WaitForSecondsRealtime(0.5f * aftershocks / (aftershocks + 1f));
			key.StartCoroutine(Shockwave(keyCode, key, aftershocks - 1));
		}
	}
}
