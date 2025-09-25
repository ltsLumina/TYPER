using JetBrains.Annotations;

/// <summary>
///   Base class for key modifiers that are applied to individual keys.
/// </summary>
public abstract class KeyModifier : Effect
{
	public abstract void OnEffectAdded([NotNull] Key key);

	public abstract void OnEffectRemoved([NotNull] Key key);

	// Marker class, so far
}
