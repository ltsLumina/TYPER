#region
using System;
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
public abstract class KeyEffect : ScriptableObject
{
	[SerializeField] protected string effectName = "Effect Name";
	[Tooltip("Rough equivalent of FName in Unreal. Used for identifying the effect in code.")]
	[SerializeField] protected string effectID = "identifier";
	[TextArea, Tooltip("Description of the combo effect for UI display.")]
	[SerializeField] protected string description = "Effect Description";

	public string EffectName => effectName;
	public string EffectID => effectID;
	public string Description => description;

	protected virtual void Awake() => effectID = effectName.ToLower().Replace(" ", "_");

	/// <summary>
	///   Invoke the effect using a KeyCode and optional Key reference.
	/// </summary>
	/// <param name="keyCode"> The KeyCode that triggered this effect. </param>
	/// <param name="key"> The Key reference that triggered this effect. </param>
	/// <param name="trigger"> (bool triggeredByKey, Key triggerKey) where trigger.byKey is true if triggered by another key (often by an effect), and trigger.key is the Key that triggered this effect (null if not triggered by another key)
	///     <para> triggeredByKey provides an easy shorthand for checking if the effect was triggered by another key.</para>
	/// </param>
	protected abstract void Invoke(KeyCode keyCode, [NotNull] Key key, (bool byKey, Key key) trigger);

	/// <summary>
	///    Invoke the effect using a Key reference.
	/// </summary>
	/// <param name="key"></param>
	/// <param name="triggerKey"> (bool triggeredByKey, Key triggerKey) where trigger.byKey is true if triggered by another key (often by an effect), and trigger.key is the Key that triggered this effect (null if not triggered by another key)
	/// <para> triggeredByKey provides an easy shorthand for checking if the effect was triggered by another key.</para>
	/// </param>
	public void Invoke(Key key, Key triggerKey) => Invoke(key.ToKeyCode(), key, (triggerKey != null, triggerKey));
	
	public static KeyEffect GetEffectByID(string identifier)
	{
		KeyEffect[] effects = Resources.LoadAll<KeyEffect>("Scriptables/Effects");
		foreach (var e in effects)
		{
			if (e.effectID == identifier.ToLower())
				return e;
		}

		Debug.LogWarning($"No KeyEffect found with ID: {identifier}");
		return null;
	}
	
	public static KeyEffect GetEffect<T>(bool instanced = false) where T : KeyEffect
	{
		KeyEffect[] effects = Resources.LoadAll<KeyEffect>("Scriptables/Effects");
		foreach (var e in effects)
		{
			if (e is not T) continue;

			if (instanced)
			{
				var instance = Instantiate(e);
				instance.name = $"{e.name} (Instance #{Guid.NewGuid()})";
				return instance;
			}
			return e;
		}

		Debug.LogWarning($"No KeyEffect found of type: {typeof(T)}");
		return null;
	}
}
