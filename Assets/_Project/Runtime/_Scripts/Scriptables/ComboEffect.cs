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
	X
}

/// <summary>
///    Base class for combo effects that involve multiple keys or complex interactions.
/// </summary>
public abstract class ComboEffect : Effect
{
	[Space(10)]
	[Header("Level"), UsedImplicitly, Tooltip("Only for setting the default level in the inspector.")]
	[SerializeField] protected Level level = Level.I;
	[UsedImplicitly, Tooltip("The maximum level available based on the number of levels defined.")]
	[SerializeField, ReadOnly] protected Level maxLevel = Level.III;

	[SubclassSelector]
	[SerializeReference] protected List<LevelSettings> levels = new ();

	public Level Level => level;

	ComboEffect cachedAsset;
	protected ComboEffect Asset
	{
		get
		{
			if (cachedAsset == null)
			{
				cachedAsset = Resources.Load<ComboEffect>($"Scriptables/Combos/{name.Replace("(Clone)", string.Empty)}");
				if (cachedAsset == null) Logger.LogError($"ComboEffect asset not found in \"{ResourcePaths.Combos}/{name}\"!", this);
			}

			return cachedAsset;
		}
	}

	#region Utility / Setup
	void Awake()
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
			settings.SetName($"Level {i + 1}");
			levels[i] = settings;
		}

		// Set maxLevel based on levels count
		maxLevel = (Level) Mathf.Clamp(levels.Count - 1, 0, Enum.GetValues(typeof(Level)).Length - 1);
	}

	public T GetLevelSettings<T>()
			where T : LevelSettings
	{
		int levelIndex = Mathf.Clamp((int) Asset.level, 0, Asset.levels.Count - 1);
		var settings = (T) Asset.levels[levelIndex];
		return settings;
	}
	#endregion

	public void SetLevel(Level newLevel)
	{
		int maxIndex = levels.Count - 1;
		int clampedIndex = Mathf.Clamp((int) newLevel, 0, maxIndex);

		if ((int) newLevel != clampedIndex) Logger.LogWarning($"Level {newLevel} is out of range. Clamped to Level {(Level) clampedIndex}.", this, $"{name}");

		// Set the level for this instance
		level = (Level) clampedIndex;

		// Update the level for the asset so it persists
		Asset.level = (Level) clampedIndex;
	}
}
