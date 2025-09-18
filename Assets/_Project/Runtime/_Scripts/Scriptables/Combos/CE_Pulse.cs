using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
//[MovedFrom(true, "Lumina", "Lumina.Essentials", "PulseLevelSettings")]
public class PulseLevelSettings : LevelSettings
{
	public int maxLayers;
	public float delayBetweenLayers;
}

[CreateAssetMenu(fileName = "Pulse", menuName = "Combos/New Pulse", order = 7)]
public class CE_Pulse : ComboEffect
{
	Coroutine pulseCoroutine;

	protected override void Invoke(Key key, (bool byKey, Key key) trigger)
	{
		var settings = GetLevelSettings<PulseLevelSettings>();
		Pulse(key, settings.maxLayers, settings.delayBetweenLayers);
	}

	/// <summary>
	/// Pulse effect that activates keys in expanding layers from a central key.
	/// </summary>
	/// <param name="centerKey"> The key to start the pulse from. </param>
	/// <param name="maxLayers"> Maximum number of layers to pulse outwards. This limits how far the pulse spreads. To cover the whole keyboard, set this to a high number like 10. </param>
	/// <param name="delayBetweenLayers"> Delay in seconds between activating each layer of keys. </param>
	void Pulse(Key centerKey, int maxLayers, float delayBetweenLayers)
	{
		if (pulseCoroutine != null) return;

		pulseCoroutine = centerKey.StartCoroutine(PulseCoroutine(centerKey, maxLayers, delayBetweenLayers));
	}

	IEnumerator PulseCoroutine(Key centerKey, int maxLayers, float delayBetweenLayers)
	{
		(bool found, int _, int _) = KeyManager.Instance.FindKey(centerKey.ToKeyCode());
		if (!found)
		{
			pulseCoroutine = null;
			yield break;
		}

		int layers = 0;
		List<Key> currentLayerKeys = new () { centerKey };
		HashSet<Key> activatedKeys = new () { centerKey };

		while (currentLayerKeys.Count > 0 && layers < maxLayers)
		{
			List<Key> nextLayerKeys = new ();

			foreach (Key key in currentLayerKeys)
			{
				KeyManager.SpawnVFX(KeyManager.CommonVFX.Combo, key.transform.position);
				key.Activate(true, 0.5f, centerKey);

				List<Key> adjacentKeys = KeyManager.Instance.GetSurroundingKeys(key.ToKeyCode());

				foreach (Key adjacent in adjacentKeys.Where(adjacent => !activatedKeys.Contains(adjacent)))
				{
					nextLayerKeys.Add(adjacent);
					activatedKeys.Add(adjacent);
				}
			}

			currentLayerKeys = nextLayerKeys;
			layers++;
			yield return new WaitForSeconds(delayBetweenLayers);
		}

		pulseCoroutine = null;
	}
}
