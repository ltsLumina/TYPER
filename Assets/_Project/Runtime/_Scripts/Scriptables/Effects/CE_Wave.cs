using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Wave", menuName = "Combos/New Wave", order = 3)]
public class CE_Wave : ComboEffect
{
	protected override void Invoke(KeyCode keyCode, Key key, (bool byKey, Key key) trigger) { Wave(key, 1, 1, 0.5f); }

	void Wave(Key initialKey, int cycles, int maxCycles, float delayBetweenColumns = 0.25f)
	{
		List<List<Key>> wave = KeyManager.Instance.GetWaveKeys();
		initialKey.StartCoroutine(WaveCoroutine(initialKey, wave, cycles, maxCycles, delayBetweenColumns));

		return;

		IEnumerator WaveCoroutine(Key initialKey, List<List<Key>> wave, int cycles, int maxCycles, float delayBetweenColumns)
		{
			for (int i = 0; i < cycles; i++)
			{
				if (i >= maxCycles) break;
				yield return ActivateColumn(initialKey, wave, delayBetweenColumns);
			}
		}

		IEnumerator ActivateColumn(Key initialKey, List<List<Key>> wave, float delayBetweenColumns)
		{
			foreach (List<Key> column in wave)
			{
				foreach (Key key in column)
				{
					KeyManager.SpawnVFX(KeyManager.CommonVFX.Combo, key.transform.position);
					key.Activate(true, 0.5f, initialKey);

					// slight delay between keys in the same column. Helps with combos and sounds.
					// affects the way combos are hit during the wave, however. First row will always be first, so any combos on lower rows may not have their combo triggered.
					yield return new WaitForSeconds(0.02f);
				}

				yield return new WaitForSeconds(delayBetweenColumns);
			}
		}
	}
}
