#region
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#endregion

public partial class KeyManager
{
	public enum Direction
	{
		Up,
		Down,
		Left,
		Right,
		All
	}

	#region Adjacent/Surrounding Keys
	/// <param name="keycode"> The key to look from. </param>
	/// <param name="direction"> Direction to look for an adjacent key. </param>
	/// <param name="adjacentKeys">
	///     If direction is All, this will be populated with all adjacent keys found. Otherwise, it
	///     will be null.
	/// </param>
	/// <returns>
	///     The adjacent key in the specified direction, or null if none exists. If direction is All, returns 'self' (the provided keycode) and
	///     populates adjacentKeys with all found adjacent keys.
	/// </returns>
	public Key GetAdjacentKey(KeyCode keycode, Direction direction, out List<Key> adjacentKeys)
	{
		(bool found, int row, int col) = FindKey(keycode);

		if (!found)
		{
			adjacentKeys = null;
			return null;
		}

		switch (direction) // super fancy math or something
		{
			case Direction.Up:
				adjacentKeys = null;
				return row > 0 ? Keys[row - 1][Mathf.Min(col, Keys[row - 1].Count - 1)] : null;

			case Direction.Down:
				adjacentKeys = null;
				return row < Keys.Count - 1 ? Keys[row + 1][Mathf.Min(col, Keys[row + 1].Count - 1)] : null;

			case Direction.Left:
				adjacentKeys = null;
				return col > 0 ? Keys[row][col - 1] : null;

			case Direction.Right:
				adjacentKeys = null;
				return col < Keys[row].Count - 1 ? Keys[row][col + 1] : null;

			case Direction.All: // return the first adjacent key found in every direction
				adjacentKeys = AllAdjacentKeys(keycode);
				return GetKey(keycode);

			default:
				throw new ArgumentOutOfRangeException(nameof(direction));
		}

		List<Key> AllAdjacentKeys(KeyCode keyCode)
		{
			var directions = new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
			return directions.Select(dir => GetAdjacentKey(keyCode, dir, out _)).Where(adjacent => adjacent != null).ToList();
		}
	}

	/// <param name="keycode"></param>
	/// <param name="includeSelf"> Whether to include the specified key in the returned list. </param>
	/// <returns> Returns a list of all keys surrounding the specified key (up to 8 keys). </returns>
	public List<Key> GetSurroundingKeys(KeyCode keycode, bool includeSelf = false)
	{
		(bool found, int row, int col) = FindKey(keycode);
		if (!found) return null;

		List<Key> surroundingKeys = new ();

		for (int r = row - 1; r <= row + 1; r++)
		{
			for (int c = col - 1; c <= col + 1; c++)
			{
				if (r >= 0 && r < Keys.Count && c >= 0 && c < Keys[r].Count && (r != row || c != col)) 
					surroundingKeys.Add(Keys[r][c]);
			}
		}

		if (includeSelf) surroundingKeys.Add(Keys[row][col]);

		return surroundingKeys;
	}
	#endregion

	#region Wave
	List<List<Key>> GetWaveKeys()
	{
		List<List<Key>> wave = new ();
		int maxCols = Keys.Max(row => row.Count);

		for (int col = 0; col < maxCols; col++)
		{
			List<Key> waveRow = (from t in Keys where col < t.Count select t[col]).ToList();
			wave.Add(waveRow);
		}

		return wave;
	}
	
	public void Wave(int cycles, int maxCycles, float delayBetweenColumns = 0.25f)
	{
		List<List<Key>> wave = GetWaveKeys();
		StartCoroutine(WaveCoroutine(wave, cycles, maxCycles, delayBetweenColumns));
	}

	IEnumerator WaveCoroutine(List<List<Key>> wave, int cycles, int maxCycles, float delayBetweenColumns)
	{
		for (int i = 0; i < cycles; i++)
		{
			if (i >= maxCycles) break;
			yield return ActivateColumn(wave, delayBetweenColumns);
		}
	}

	IEnumerator ActivateColumn(List<List<Key>> wave, float delayBetweenColumns)
	{
		foreach (List<Key> column in wave)
		{
			foreach (Key key in column)
			{
				key.Activate(true, 0.5f, key);
				// slight delay between keys in the same column. Helps with combos and sounds.
				// affects the way combos are hit during the wave, however. First row will always be first, so any combos on lower rows may not have their combo triggered.
				yield return new WaitForSeconds(0.02f);
			}

			yield return new WaitForSeconds(delayBetweenColumns);
		}

		//waveCooldown ??= StartCoroutine(WaveCooldown(cooldown));
	}

	float cooldownRemaining;
	public float CooldownRemaining => cooldownRemaining;

	IEnumerator WaveCooldown(float cooldown)
	{
		cooldownRemaining = cooldown;

		while (cooldownRemaining > 0f)
		{
			cooldownRemaining -= Time.deltaTime;
			yield return null;
		}

		//waveCooldown = null;
	}
	#endregion

	#region Pulse
	// pulse outwards from a central key, activating adjacent keys in a wave-like manner
	Coroutine pulseCoroutine;
	
	/// <summary>
	/// Pulse effect that activates keys in expanding layers from a central key.
	/// </summary>
	/// <param name="centerKey"> The key to start the pulse from. </param>
	/// <param name="maxLayers"> Maximum number of layers to pulse outwards. This limits how far the pulse spreads. To cover the whole keyboard, set this to a high number like 10. </param>
	/// <param name="delayBetweenLayers"> Delay in seconds between activating each layer of keys. </param>
	public void Pulse(Key centerKey, int maxLayers = 3, float delayBetweenLayers = 0.1f)
	{
		if (pulseCoroutine != null)
		{
			Debug.Log("Pulse is already active.");
			return;
		}

		pulseCoroutine = StartCoroutine(PulseCoroutine(centerKey, maxLayers, delayBetweenLayers));
	}
	
	IEnumerator PulseCoroutine(Key centerKey, int maxLayers, float delayBetweenLayers)
	{
		(bool found, int row, int col) = FindKey(centerKey.ToKeyCode());
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
				key.Activate(true, 0.5f, centerKey);

				List<Key> adjacentKeys = GetSurroundingKeys(key.ToKeyCode());
				foreach (Key adjacent in adjacentKeys)
				{
					if (!activatedKeys.Contains(adjacent))
					{
						nextLayerKeys.Add(adjacent);
						activatedKeys.Add(adjacent);
					}
				}
			}

			currentLayerKeys = nextLayerKeys;
			layers++;
			yield return new WaitForSeconds(layers == 1 ? 0.1f : delayBetweenLayers);
		}

		pulseCoroutine = null;
	}
	#endregion
}
