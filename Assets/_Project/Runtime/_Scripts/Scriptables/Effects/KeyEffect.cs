#region
using UnityEngine;
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

	protected abstract void Invoke(KeyCode keyCode, Key key = null);

	public void Invoke(Key key) => Invoke(key.ToKeyCode(), key);
}
