using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class RailgunLevelSettings : LevelSettings
{
	public KeyManager.Direction direction = KeyManager.Direction.Right;
	public int rows = 1;
}

[CreateAssetMenu(fileName = "Railgun", menuName = "Combos/New Railgun", order = 8)]
public class CE_Railgun : ComboEffect
{
	protected override void Invoke(Key key, (bool byKey, Key key) trigger)
	{
		var settings = GetLevelSettings<RailgunLevelSettings>();
		Railgun(settings.direction, settings.rows);

		return;

		void Railgun(KeyManager.Direction direction, int rows)
		{
			// get the front three keys in a wall (top right, middle right, bottom right)
			var railgunKeys = KeyManager.Instance.GetRailgunKeys(key, direction, rows);

			if (railgunKeys.upperLane != null && railgunKeys.upperLane.Any()) key.StartCoroutine(ActivateLane(railgunKeys.upperLane));
			if (railgunKeys.centerLane != null && railgunKeys.centerLane.Any()) key.StartCoroutine(ActivateLane(railgunKeys.centerLane));
			if (railgunKeys.lowerLane != null && railgunKeys.lowerLane.Any()) key.StartCoroutine(ActivateLane(railgunKeys.lowerLane));

			return;

			IEnumerator ActivateLane(IEnumerable<Key> lane)
			{
				foreach (var k in lane)
				{
					KeyManager.SpawnVFX(KeyManager.CommonVFX.Combo, key.transform.position);
					k.Activate(true, 0.5f, key);
					yield return new WaitForSecondsRealtime(0.02f);
				}
			}
		}
	}
}
