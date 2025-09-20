using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Flags]
public enum Rows
{
	Top = 1 << 0,
	Middle = 1 << 1,
	Bottom = 1 << 2,
}

[Serializable]
public class RailgunLevelSettings : LevelSettings
{
	[Space(5)]
	public KeyManager.Direction direction = KeyManager.Direction.Right;
	public Rows rows = Rows.Middle;
}

[CreateAssetMenu(fileName = "Railgun", menuName = "Combos/Railgun", order = 8)]
public class CE_Railgun : ComboEffect
{
	protected override void Invoke(Key key, (bool byKey, Key key) trigger)
	{
		var settings = GetLevelSettings<RailgunLevelSettings>();
		Railgun(settings.direction, settings.rows);

		return;
		void Railgun(KeyManager.Direction direction, Rows rows)
		{
			// get the front three keys in a wall (top right, middle right, bottom right)
			var railgunKeys = KeyManager.Instance.GetRailgunKeys(key, direction);

			if (railgunKeys.upperLane != null && railgunKeys.upperLane.Any() && rows.HasFlag(Rows.Top)) key.StartCoroutine(ActivateLane(railgunKeys.upperLane));
			if (railgunKeys.centerLane != null && railgunKeys.centerLane.Any() && rows.HasFlag(Rows.Middle)) key.StartCoroutine(ActivateLane(railgunKeys.centerLane));
			if (railgunKeys.lowerLane != null && railgunKeys.lowerLane.Any() && rows.HasFlag(Rows.Bottom)) key.StartCoroutine(ActivateLane(railgunKeys.lowerLane));
			
			return;
			IEnumerator ActivateLane(IEnumerable<Key> lane)
			{
				foreach (var k in lane)
				{
					KeyManager.SpawnVFX(KeyManager.CommonVFX.Combo, key.transform.position);
					k.Activate(0.5f, key);
					yield return new WaitForSecondsRealtime(0.02f);
				}
			}
		}
	}
}
