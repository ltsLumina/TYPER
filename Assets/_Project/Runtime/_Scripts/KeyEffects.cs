#region
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#endregion

public partial class KeyController
{
	public enum Direction
	{
		Up,
		Down,
		Left,
		Right,
		All
	}

	/// <param name="direction"> Direction to look for an adjacent key. </param>
	/// <param name="adjacentKeys">
	///     If direction is All, this will be populated with all adjacent keys found. Otherwise, it
	///     will be null.
	/// </param>
	/// <returns>
	///     The adjacent key in the specified direction, or null if none exists. If direction is All, returns null and
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
				var directions = new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
				adjacentKeys = directions.Select(dir => GetAdjacentKey(keycode, dir, out _)).Where(adjacent => adjacent != null).ToList();

				// return self to indicate multiple keys found
				return Keys[row][col];

			default:
				throw new ArgumentOutOfRangeException(nameof(direction));
		}
	}

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
				if (r >= 0 && r < Keys.Count && c >= 0 && c < Keys[r].Count && (r != row || c != col)) surroundingKeys.Add(Keys[r][c]);
			}
		}

		if (includeSelf) surroundingKeys.Add(Keys[row][col]);

		return surroundingKeys;
	}

	#region Wave
	public List<List<Key>> GetWaveKeys()
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

	Coroutine waveCoroutine;

	public void Wave(int cycles, int maxCycles, float delayBetweenColumns = 0.25f)
	{
		if (waveCoroutine == null)
		{
			List<List<Key>> wave = GetWaveKeys();
			waveCoroutine = StartCoroutine(WaveCoroutine(wave, cycles, maxCycles, delayBetweenColumns));
		}
	}
	
	IEnumerator WaveCoroutine(List<List<Key>> wave, int cycles, int maxCycles, float delayBetweenColumns)
	{
		for (int i = 0; i < cycles; i++)
		{
			if (i >= maxCycles) break;
			yield return PerformWaveEffect(wave, delayBetweenColumns);
		}
	}

	IEnumerator PerformWaveEffect(List<List<Key>> wave, float delayBetweenColumns)
	{
		foreach (List<Key> column in wave)
		{
			foreach (Key key in column) key.Activate(true, 2.5f, key);
			yield return new WaitForSeconds(delayBetweenColumns);
		}

		waveCoroutine = null;
	}
	#endregion
}
