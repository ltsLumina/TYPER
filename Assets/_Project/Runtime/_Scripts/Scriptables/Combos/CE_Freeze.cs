using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class FreezeLevelSettings : LevelSettings
{
	public float duration = 5f;
	[InspectorName("Flagged Direction")]
	public KeyManager.FDirection direction = KeyManager.FDirection.Right;
	public int range = 1;
}

[CreateAssetMenu(fileName = "Freeze", menuName = "Combos/Freeze", order = 7)]
public class CE_Freeze : ComboEffect
{
	protected override void Invoke(Key key, (bool byKey, Key key) trigger)
	{
		var settings = GetLevelSettings<FreezeLevelSettings>();
		key.StartCoroutine(Freeze());

		return;
		IEnumerator Freeze()
		{
			// get the front three keys in a wall (top right, middle right, bottom right)
			var wallKeys = KeyManager.Instance.GetWallKeys(key, settings.direction, settings.range);

			foreach (Key k in wallKeys)
			{
				//KeyManager.SpawnVFX(KeyManager.CommonVFX.Combo, key.transform.position);
				k.SetModifier(Key.Modifiers.Frozen);
				k.KeyModifier?.Invoke(k, null);
				yield return new WaitForSecondsRealtime(0.02f);
			}
			
			yield return new WaitForSecondsRealtime(settings.duration);
			
			foreach (Key k in wallKeys)
			{
				//KeyManager.SpawnVFX(KeyManager.CommonVFX.Combo, key.transform.position);
				k.RemoveModifier(Key.Modifiers.Frozen);
				yield return new WaitForSecondsRealtime(0.02f);
			}
		}
	}
}