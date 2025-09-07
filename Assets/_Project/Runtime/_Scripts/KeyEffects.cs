using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
				return row > 0 ? keys[row - 1][Mathf.Min(col, keys[row - 1].Count - 1)] : null;

			case Direction.Down:
				adjacentKeys = null;
				return row < keys.Count - 1 ? keys[row + 1][Mathf.Min(col, keys[row + 1].Count - 1)] : null;

			case Direction.Left:
				adjacentKeys = null;
				return col > 0 ? keys[row][col - 1] : null;

			case Direction.Right:
				adjacentKeys = null;
				return col < keys[row].Count - 1 ? keys[row][col + 1] : null;

			case Direction.All: // return the first adjacent key found in every direction
				var directions = new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
				adjacentKeys = directions.Select(dir => GetAdjacentKey(keycode, dir, out _)).Where(adjacent => adjacent != null).ToList();
				
				// return self to indicate multiple keys found
				return keys[row][col];

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
				if (r >= 0 && r < keys.Count && c >= 0 && c < keys[r].Count && (r != row || c != col)) { surroundingKeys.Add(keys[r][c]); }
			}
		}

		if (includeSelf) surroundingKeys.Add(keys[row][col]);

		return surroundingKeys;
	}

	#region Wave
	public List<List<Key>> GetWaveKeys()
	{
		List<List<Key>> wave = new ();
		int maxCols = keys.Max(row => row.Count);

		for (int col = 0; col < maxCols; col++)
		{
			List<Key> waveRow = (from t in keys where col < t.Count select t[col]).ToList();
			wave.Add(waveRow);
		}

		return wave;
	}
	
	IEnumerator PerformWaveEffect(List<List<Key>> wave, float delayBetweenColumns)
	{
		foreach (List<Key> column in wave)
		{
			foreach (var key in column)
			{
				key.Activate(true, 2.5f, key);
			}
			yield return new WaitForSeconds(delayBetweenColumns);
		}
	}
	
	public void Wave(float delayBetweenColumns = 0.1f)
	{
		var wave = GetWaveKeys();
		StartCoroutine(PerformWaveEffect(wave, delayBetweenColumns));
	}
	#endregion
}
