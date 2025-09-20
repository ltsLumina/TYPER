#region
using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;
#endregion

/// <summary>
///     Base class for combo effects as scriptable objects assigned to keys and triggered on combo completion.
///     <para>Extend to implement custom effects, animations, or gameplay mechanics.</para>
///     <para>Combo effects can be invoked with a KeyCode or a Key reference.</para>
///     <para>Assign combo effects as scriptable objects to keys in the inspector.</para>
/// </summary>
public abstract class Effect : ScriptableObject
{
	[SerializeField] protected string effectName = "Effect Name";
	[TextArea(3, 3), Tooltip("Description of the combo effect for UI display.")]
	[SerializeField] protected string description = "Effect Description";

	public string EffectName => effectName;
	public string Description => description;

	/// <summary>
	///   Invoke the effect using a KeyCode and optional Key reference.
	/// </summary>
	/// <param name="key"> The Key reference that triggered this effect. </param>
	/// <param name="trigger"> (bool triggeredByKey, Key triggerKey) where trigger.byKey is true if triggered by another key (often by an effect), and trigger.key is the Key that triggered this effect (null if not triggered by another key)
	///     <para> triggeredByKey provides an easy shorthand for checking if the effect was triggered by another key.</para>
	/// </param>
	protected abstract void Invoke([NotNull] Key key, (bool byKey, Key key) trigger);

	/// <summary>
	///    Invoke the effect using a Key reference.
	/// </summary>
	/// <param name="key"></param>
	/// <param name="triggerKey"> (bool triggeredByKey, Key triggerKey) where trigger.byKey is true if triggered by another key (often by an effect), and trigger.key is the Key that triggered this effect (null if not triggered by another key)
	/// <para> triggeredByKey provides an easy shorthand for checking if the effect was triggered by another key.</para>
	/// </param>
	public void Invoke(Key key, Key triggerKey) => Invoke(key, (triggerKey != null, triggerKey));

	static Effect[] _cachedEffects;

	void OnValidate()
	{
		effectName = name;
		if (description == "Effect Description" || string.IsNullOrWhiteSpace(description)) Logger.LogWarning($"Effect '{name}' has no description. Please provide a description for better clarity.");
	}

	// Must remember to clear cache on domain reloads
	void OnDestroy() => _cachedEffects = null;

	/// <summary>
	///     Retrieves an effect of the specified type from the Resources folder.
	///     If 'instanced' is true, returns a new instance of the effect; otherwise, returns the original asset.
	/// </summary>
	/// <typeparam name="T">
	///     The type of Effect to retrieve.
	///     <para> <see cref="ComboEffect" /> or <see cref="KeyModifier" /></para>
	/// </typeparam>
	/// <returns> An instance of the requested effect type, or null if not found. </returns>
	/// <remarks> <see cref="ComboEffect" />s are always instanced to allow for unique state per key. </remarks>
	public static T GetEffect<T>(bool instanced = false)
			where T : Effect
	{
		string resourcePath = typeof(T) switch
		{ { } t when t == typeof(ComboEffect) || t.IsSubclassOf(typeof(ComboEffect)) => ResourcePaths.Combos,
		  _                                                                          => ResourcePaths.Modifiers };

		// If _cachedEffects is null, which it will be on the first access, load all effects from the specified resource path and cache them.
		Effect[] effects = _cachedEffects ??= Resources.LoadAll<Effect>(ResourcePaths.Combos).Concat(Resources.LoadAll<Effect>(ResourcePaths.Modifiers)).ToArray();

		foreach (Effect e in effects)
		{
			if (e is not T target) continue;

			// Always instance ComboEffects to allow for unique state per key
			if (instanced || typeof(T) == typeof(ComboEffect) || typeof(T).IsSubclassOf(typeof(ComboEffect)))
			{
				Effect instance = Instantiate(e);
				instance.name = $"{target.name} (Instance #{Random.Range(1000, 9999)})";
				return instance as T;
			}

			return target;
		}

		Debug.LogWarning($"'{typeof(T)}' NOT found in: \"{resourcePath}\"" + "\n" + $"Make sure the modifier/effect is created as a ScriptableObject and placed in the \"Resources/{resourcePath}\" folder.");
		return null;
	}

	public static T GetEffect<T>(Type type, bool instanced = false)
			where T : Effect
	{
		string resourcePath = type == typeof(ComboEffect) || type.IsSubclassOf(typeof(ComboEffect)) ? ResourcePaths.Combos : ResourcePaths.Modifiers;

		Effect[] effects = _cachedEffects ??= Resources.LoadAll<Effect>(ResourcePaths.Combos).Concat(Resources.LoadAll<Effect>(ResourcePaths.Modifiers)).ToArray();

		foreach (Effect e in effects)
		{
			if (e.GetType() != type) continue;

			// Always instance ComboEffects to allow for unique state per key
			if (instanced || type == typeof(ComboEffect) || type.IsSubclassOf(typeof(ComboEffect)))
			{
				Effect instance = Instantiate(e);
				instance.name = $"{e.name} (Instance #{Random.Range(1000, 9999)})";
				return instance as T;
			}

			return e as T;
		}

		Debug.LogWarning($"'{type}' NOT found in: \"{resourcePath}\"" + "\n" + $"Make sure the modifier/effect is created as a ScriptableObject and placed in the \"Resources/{resourcePath}\" folder.");
		return null;
	}
}

public struct ResourcePaths
{
	/// <remarks>  Does not include a trailing slash. </remarks>
	public const string Modifiers = "Scriptables/Modifiers";
	/// <remarks>  Does not include a trailing slash. </remarks>
	public const string Combos = "Scriptables/Combos";
}
