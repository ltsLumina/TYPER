using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WaveLevelSettings : LevelSettings
{
	public int cycles = 1;
	public float delayBetweenColumns = 0.25f;
}

[CreateAssetMenu(fileName = "Wave", menuName = "Combos/Wave", order = 3)]
public class CE_Wave : ComboEffect
{
	protected override void Invoke(Key key, (bool byKey, Key key) trigger)
	{
		var settings = GetLevelSettings<WaveLevelSettings>();
		Wave(key, settings.cycles, settings.delayBetweenColumns);
	}

	void Wave(Key initialKey, int cycles, float delayBetweenColumns = 0.25f)
	{
		List<List<Key>> wave = KeyManager.Instance.GetWaveKeys();
		initialKey.StartCoroutine(WaveCoroutine());

		return;
		IEnumerator WaveCoroutine()
		{
			for (int i = 0; i < cycles; i++)
			{
				yield return ActivateColumn();
			}
		}

		IEnumerator ActivateColumn()
		{
			foreach (List<Key> column in wave)
			{
				foreach (Key key in column)
				{
					KeyManager.SpawnVFX(KeyManager.CommonVFX.Combo, key.transform.position);
					key.Activate(0.5f, initialKey);

					// slight delay between keys in the same column. Helps with combos and sounds.
					// affects the way combos are hit during the wave, however. First row will always be first, so any combos on lower rows may not have their combo triggered.
					yield return new WaitForSeconds(0.02f);
				}

				yield return new WaitForSeconds(delayBetweenColumns);
			}
		}
	}
}
