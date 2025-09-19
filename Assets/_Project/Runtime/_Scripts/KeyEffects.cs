#region
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
#endregion

public partial class KeyManager
{
	/// <summary>
	///     Simple enum representing a single direction for effects like railgun.
	///     Doesn't use flags, only one direction at a time.
	/// </summary>
	public enum Direction
	{
		Up,
		Down,
		Left,
		Right,
	}

	/// <summary>
	///     Flags enum representing multiple directions for adjacent key lookup.
	///     Can combine multiple directions using bitwise.
	/// </summary>
	[Flags]
	public enum FDirection
	{
		Up = 1 << 0,
		Down = 1 << 1,
		Left = 1 << 2,
		Right = 1 << 3,
	}

	/// <summary>
	///     Simple enum representing a single direction including diagonals.
	///     Doesn't use flags, only one direction at a time.
	/// </summary>
	public enum DDirection
	{
		Top,
		Bottom,
		Left,
		Right,
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight
	}

	#region Adjacent/Surrounding Keys
	/// <param name="keycode"> The key to look from. </param>
	/// <param name="direction"> Direction to look for an adjacent key. </param>
	/// <returns>
	///     The adjacent key in the specified direction, or null if none exists. If direction is All, returns 'self' (the provided keycode) and
	///     populates adjacentKeys with all found adjacent keys.
	/// </returns>
	public List<Key> GetAdjacentKey(KeyCode keycode, FDirection direction)
	{
		(bool found, int row, int col) = FindKey(keycode);

		if (!found) return null;

		if (direction == (FDirection.Up | FDirection.Down | FDirection.Left | FDirection.Right)) { return AllAdjacentKeys(keycode); }

		var foundKeys = new List<Key>();

		if ((direction & FDirection.Up) == FDirection.Up)
		{
			var upKey = row > 0 ? Keys[row - 1][Mathf.Min(col, Keys[row - 1].Count - 1)] : null;
			if (upKey != null) foundKeys.Add(upKey);
		}

		if ((direction & FDirection.Down) == FDirection.Down)
		{
			var downKey = row < Keys.Count - 1 ? Keys[row + 1][Mathf.Min(col, Keys[row + 1].Count - 1)] : null;
			if (downKey != null) foundKeys.Add(downKey);
		}

		if ((direction & FDirection.Left) == FDirection.Left)
		{
			var leftKey = col > 0 ? Keys[row][col - 1] : null;
			if (leftKey != null) foundKeys.Add(leftKey);
		}

		if ((direction & FDirection.Right) == FDirection.Right)
		{
			var rightKey = col < Keys[row].Count - 1 ? Keys[row][col + 1] : null;
			if (rightKey != null) foundKeys.Add(rightKey);
		}

		return foundKeys.Count > 0 ? foundKeys : null;

		List<Key> AllAdjacentKeys(KeyCode keyCode)
		{
			var directions = new[] { FDirection.Up, FDirection.Down, FDirection.Left, FDirection.Right };
			return directions.SelectMany(dir => GetAdjacentKey(keyCode, dir) ?? new List<Key>()).ToList();
		}
	}

	public List<Key> GetAdjacentKey(Key key, FDirection direction) => GetAdjacentKey(key.ToKeyCode(), direction);

	/// <summary>
	/// Returns the adjacent key in the specified direction, including diagonals.
	/// </summary>
	/// <param name="keycode">The key to look from.</param>
	/// <param name="direction">The direction to look for an adjacent key (supports diagonals).</param>
	/// <returns>The adjacent key in the specified direction, or null if none exists.</returns>
	public Key GetAdjacentKey(KeyCode keycode, DDirection direction)
	{
		(bool found, int row, int col) = FindKey(keycode);
		if (!found) return null;

		var offsets = new Dictionary<DDirection, (int dr, int dc)>
		{ { DDirection.Top, (-1, 0) },
		  { DDirection.Bottom, (1, 0) },
		  { DDirection.Left, (0, -1) },
		  { DDirection.Right, (0, 1) },
		  { DDirection.TopLeft, (-1, -1) },
		  { DDirection.TopRight, (-1, 1) },
		  { DDirection.BottomLeft, (1, -1) },
		  { DDirection.BottomRight, (1, 1) } };

		if (!offsets.TryGetValue(direction, out (int dr, int dc) offset)) return null;

		int targetRow = row + offset.dr;
		int targetCol = col + offset.dc;

		if (targetRow >= 0 && targetRow < Keys.Count && targetCol >= 0 && targetCol < Keys[targetRow].Count) return Keys[targetRow][targetCol];

		return null;
	}

	public Key GetSingleAdjacentKey(Key key, DDirection direction) => GetAdjacentKey(key.ToKeyCode(), direction);

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
				if (r >= 0 && r < Keys.Count && c >= 0 && c < Keys[r].Count && (r != row || c != col)) surroundingKeys.Add(Keys[r][c]);
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
		{ CommonVFX.Combo => "PREFABS/VFX/Combo VFX",
		  CommonVFX.Hit   => "PREFABS/VFX/Enemy Hit VFX",
		  CommonVFX.Death => "PREFABS/VFX/Enemy Death VFX",
		  _               => throw new ArgumentOutOfRangeException(nameof(type), type, null) };

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

	public List<Key> GetWallKeys(Key centerKey, FDirection direction, int range)
	{
		(bool found, int row, int col) = FindKey(centerKey.ToKeyCode());
		if (!found) return null;

		List<Key> wallKeys = new ();

		switch (direction)
		{
			case FDirection.Right: {
				for (int r = -1; r <= 1; r++)
				{
					int targetRow = row + r;
					if (targetRow < 0 || targetRow >= Keys.Count) continue;

					for (int offset = 1; offset <= range; offset++)
					{
						int targetCol = col + offset;
						if (targetCol < Keys[targetRow].Count) wallKeys.Add(Keys[targetRow][targetCol]);
					}
				}

				break;
			}

			case FDirection.Left: {
				for (int r = -1; r <= 1; r++)
				{
					int targetRow = row + r;
					if (targetRow < 0 || targetRow >= Keys.Count) continue;

					for (int offset = 1; offset <= range; offset++)
					{
						int targetCol = col - offset;
						if (targetCol >= 0) wallKeys.Add(Keys[targetRow][targetCol]);
					}
				}

				break;
			}

			default:
				Debug.LogWarning("Invalid wall direction specified. Use Left or Right.");
				return null;
		}

		return wallKeys.Count > 0 ? wallKeys : null;
	}

	public (List<Key> centerLane, List<Key> upperLane, List<Key> lowerLane) GetRailgunKeys(Key centerKey, Direction direction, int lanes)
	{
		(bool found, int row, int col) = FindKey(centerKey.ToKeyCode());
		if (!found) return (null, null, null);

		List<Key> centerLane = new ();
		List<Key> upperLane = new ();
		List<Key> lowerLane = new ();

		switch (direction)
		{
			case Direction.Left:
				for (int c = 0; c < col; c++) centerLane.Add(Keys[row][c]);

				if (lanes >= 2 && row > 0)
					for (int c = 0; c < col; c++)
						upperLane.Add(Keys[row - 1][c]);

				if (lanes >= 3 && row < Keys.Count - 1)
					for (int c = 0; c < col; c++)
						lowerLane.Add(Keys[row + 1][c]);

				break;

			case Direction.Right:
				for (int c = col + 1; c < Keys[row].Count; c++) centerLane.Add(Keys[row][c]);

				if (lanes >= 2 && row > 0)
					for (int c = col + 1; c < Keys[row - 1].Count; c++)
						upperLane.Add(Keys[row - 1][c]);

				if (lanes >= 3 && row < Keys.Count - 1)
					for (int c = col + 1; c < Keys[row + 1].Count; c++)
						lowerLane.Add(Keys[row + 1][c]);

				break;

			default:
				Debug.LogWarning("Invalid wall direction specified. Use \"Left\", or \"Right\".");
				return (null, null, null);
		}

		return (centerLane.Count > 0 ? centerLane : null, upperLane.Count > 0 ? upperLane : null, lowerLane.Count > 0 ? lowerLane : null);
	}
}
