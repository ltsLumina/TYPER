using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Lumina.Essentials.Attributes;
using UnityEngine;

[Serializable]
public class LevelSettings
{
	[HideInInspector, UsedImplicitly]
	public string level;

	public void SetName(string name) => level = name;
}

public enum Level
{
	I,
	II,
	III,
	IV,
	V,
	VI,
	VII,
	VIII,
	IX,
	X,
}

/// <summary>
///     Base class for combo effects that involve multiple keys or complex interactions.
/// </summary>
/// <remarks> ComboEffects are always instanced to allow for unique state per key. </remarks>
public abstract class ComboEffect : Effect
{
	[Space(10)]
	[Header("Level"), UsedImplicitly, Tooltip("Only for setting the default level in the inspector.")]
	[SerializeField] protected Level level = Level.I;
	[UsedImplicitly, Tooltip("The maximum level available based on the number of levels defined.")]
	[SerializeField, ReadOnly] protected Level maxLevel = Level.X;

	[SubclassSelector]
	[SerializeReference] protected List<LevelSettings> levels = new ();

	public Level Level => level;

	ComboEffect cachedAsset;
	
	/// <summary>
	/// A reference to 
	/// </summary>
	ComboEffect Asset
	{
		get
		{
			if (!cachedAsset)
			{
				cachedAsset = Resources.Load<ComboEffect>($"{ResourcePaths.Combos}/{name.Replace("(Clone)", string.Empty)}");
				if (!cachedAsset) Logger.LogError($"ComboEffect asset not found in \"{ResourcePaths.Combos}/{name}\"!", this);
			}

			return cachedAsset;
		}
	}

	#region Utility / Setup
	void OnEnable()
	{
		// Reset level to default in case it was changed at runtime
		Asset.level = Level.I;
		level = Level.I;
	}

	void OnDestroy()
	{
		// Reset level to default in case it was changed at runtime
		Asset.level = Level.I;
		level = Level.I;
	}

	void OnValidate()
	{
		// ensure Asset is not null
		Debug.Assert(Asset != null, "ComboEffect asset is null!", this);

		for (int i = 0; i < levels.Count; i++)
		{
			var settings = levels[i];
			if (settings == null) return;
			settings.SetName($"Level {(Level) i}");
			levels[i] = settings;
		}

		// Set maxLevel based on levels count
		maxLevel = (Level) Mathf.Clamp(levels.Count - 1, 0, Enum.GetValues(typeof(Level)).Length - 1);
	}

	/// <summary>
	///     Retrieves the settings for the current level.
	/// </summary>
	/// <typeparam name="T"> The type of LevelSettings to retrieve. </typeparam>
	/// <returns> The LevelSettings for the current level. </returns>
	protected T GetLevelSettings<T>() where T : LevelSettings
	{
		int levelIndex = Mathf.Clamp((int) Asset.level, 0, Asset.levels.Count - 1);
		var settings = (T) Asset.levels[levelIndex];
		return settings;
	}

	/// <summary>
	///     Retrieves the settings for a specific level.
	/// </summary>
	/// <param name="overrideLevel"> The level index to retrieve settings for. </param>
	/// <typeparam name="T"> The type of LevelSettings to retrieve. </typeparam>
	/// <remarks>
	///     This should only be used in special cases where you need to get settings for a level other than the current
	///     one.
	/// </remarks>
	/// <returns> The LevelSettings for the specified level. </returns>
	protected T GetLevelSettings<T>(int overrideLevel) where T : LevelSettings
	{
		int levelIndex = Mathf.Clamp(overrideLevel, 0, Asset.levels.Count - 1);
		var settings = (T) Asset.levels[levelIndex];
		return settings;
	}
	#endregion

	/// <summary>
	///    Sets the level of the combo effect, clamping to the valid range if out of bounds.
	/// </summary>
	/// <param name="newLevel"> The new level to set.
	/// <para> If out of range, it will be clamped to the nearest valid level. </para> </param>
	/// <param name="silent"> (optional) Suppresses warning logs for out-of-range levels. </param>
	public void SetLevel(Level newLevel, bool silent = false)
	{
		int maxIndex = levels.Count - 1;
		int clampedIndex = Mathf.Clamp((int) newLevel, 0, maxIndex);

		if (!silent)
		{
			if ((int) newLevel != clampedIndex)
			{
				if ((int) newLevel == 10)
				{
					Logger.LogWarning($"Level \"XI\" is out of range. Clamped to Level {(Level) clampedIndex}.", this, $"{name}");
					return;
				}
			
				Logger.LogWarning($"Level \"{newLevel}\" is out of range. Clamped to Level {(Level) clampedIndex}.", this, $"{name}");
			}
		}

		// Set the level for this instance
		level = (Level) clampedIndex;

		// Update the level for the asset so it persists
		Asset.level = (Level) clampedIndex;
	}
}
