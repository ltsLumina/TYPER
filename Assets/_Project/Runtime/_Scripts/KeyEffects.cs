#region
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
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
	/// <returns> A list of lists representing the keyboard in columns, for wave effects. </returns>
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
	#endregion
	
	#region VFX
	public enum CommonVFX
	{
		Combo,
		Hit,
		Death,
	}
	
	static ObjectPool GetCommonVFXPool(CommonVFX type)
	{
		string path = type switch
		{
			CommonVFX.Combo => "PREFABS/VFX/Combo VFX",
			CommonVFX.Hit   => "PREFABS/VFX/Enemy Hit VFX",
			CommonVFX.Death => "PREFABS/VFX/Enemy Death VFX",
			_               => throw new ArgumentOutOfRangeException(nameof(type), type, null)
		};
		
		var prefab = Resources.Load<ParticleSystem>(path);
		if (!prefab)
		{
			Debug.LogWarning($"No ParticleSystem prefab found at path: {path}");
			return null;
		}

		return ObjectPoolManager.FindObjectPool(prefab.gameObject);
	}

	/// <summary>
	///    Spawns a common VFX at the specified position with the given color, or a random color if none is provided.
	/// </summary>
	/// <param name="prefab"> The ParticleSystem prefab to spawn. Use Resources.Load&lt;ParticleSystem&gt;("path/to/prefab") to load it. </param>
	/// <param name="position"> The world position to spawn the VFX at. </param>
	/// <param name="colour"> The color to apply to the ParticleSystem. Refer to remarks for default behavior. </param>
	/// <remarks> If colour is default, the colour will instead be randomized instead using Random.ColorHSV(). </remarks>
	/// <returns> The spawned ParticleSystem, or null if the pool or prefab was not found. </returns>
	public static ParticleSystem SpawnVFX(ParticleSystem prefab, Vector3 position, Color colour = default)
	{
		var pool = ObjectPoolManager.FindObjectPool(prefab.gameObject);
		if (pool == null) return null;

		var vfx = pool.GetPooledObject<ParticleSystem>(true, position);
		ParticleSystem.MainModule main = vfx.main;
		main.startColor = colour == default ? Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f) : colour;
		return vfx;
	}

	/// <summary>
	///    Spawns a common VFX at the specified position with the given color, or a random color if none is provided.
	/// </summary>
	/// <param name="type"> The type of common VFX to spawn. </param>
	/// <param name="position"> The world position to spawn the VFX at. </param>
	/// <param name="colour"> The color to apply to the ParticleSystem. Refer to remarks for default behavior. </param>
	/// <remarks> If colour is default, the colour will instead be randomized instead using Random.ColorHSV(). </remarks>
	/// <returns> The spawned ParticleSystem, or null if the pool or prefab was not found. </returns>
	public static ParticleSystem SpawnVFX(CommonVFX type, Vector3 position, Color colour = default)
	{
		var pool = GetCommonVFXPool(type);
		if (pool == null) return null;
		
		var vfx = pool.GetPooledObject<ParticleSystem>(true, position);
		ParticleSystem.MainModule main = vfx.main;
		main.startColor = colour == default ? Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f) : colour;
		return vfx;
	}
	#endregion
}
