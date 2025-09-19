using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class FreezeRailgunLevelSettings : LevelSettings
{
	public float duration = 5f;
	public KeyManager.Direction direction = KeyManager.Direction.Right;
}

[CreateAssetMenu(fileName = "(WIP) Freeze-Railgun", menuName = "Combos/(WIP) Freeze-Railgun", order = 7)]
public class CE_FreezeRailgun : ComboEffect
{
	protected override void Invoke(Key key, (bool byKey, Key key) trigger)
	{
		var settings = GetLevelSettings<FreezeRailgunLevelSettings>();
		key.StartCoroutine(Freeze());

		return;

		IEnumerator Freeze()
		{
			// get the front three keys in a wall (top right, middle right, bottom right)
			var railgunKeys = KeyManager.Instance.GetRailgunKeys(key, settings.direction);

			foreach (Key k in railgunKeys)
			{
				//KeyManager.SpawnVFX(KeyManager.CommonVFX.Combo, key.transform.position);
				k.SetModifier(Key.Modifiers.Frozen);
				k.KeyModifier?.Invoke(k, null);
				yield return new WaitForSecondsRealtime(0.05f);
			}

			yield return new WaitForSecondsRealtime(settings.duration);

			foreach (Key k in railgunKeys)
			{
				//KeyManager.SpawnVFX(KeyManager.CommonVFX.Combo, key.transform.position);
				k.RemoveModifier(Key.Modifiers.Frozen);
				yield return new WaitForSecondsRealtime(0.02f);
			}
		}
	}
}
