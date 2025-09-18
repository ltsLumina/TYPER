#region
using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;
#endregion

public partial class Key // Properties
{
	/// <summary>
	///   The KeyCode this key represents.
	/// </summary>
	public KeyCode KeyCode => keyCode;

	/// <summary>
	/// Represents the index of this key in the current combo, or -1 if not part of a combo.
	/// </summary>
	public int ComboIndex
	{
		get => comboIndex;
		set => comboIndex = value;
	}

	/// <summary>
	///    Indicates whether this key is the last key in a combo.
	/// </summary>
	public bool LastKeyInCombo { get; set; }
}

public partial class Key // Components
{
	public GameObject ChainedMarker => chainedMarker;
	public GameObject ThornedMarker => thornedMarker;
	public GameObject ComboHighlight => comboHighlight;
	public SpriteRenderer SpriteRenderer => spriteRenderer;
	public TMP_Text Letter => letter;
}

public partial class Key // Modifiers
{
	[Flags]
	public enum Modifiers
	{
		[UsedImplicitly]
		None = 0,
		Combo = 1 << 0,
		Mash = 1 << 1,
		Frozen = 1 << 2,
		Chained = 1 << 3,
		Loose = 1 << 4,
		Thorned = 1 << 5,
		OffGlobalCooldown = 1 << 6
	}

	/// <summary>
	///    Checks if the specified modifier is set for this key.
	/// </summary>
	/// <param name="modifier"> The modifier to check. </param>
	/// <returns> True if the modifier is set, false otherwise. </returns>
	public bool HasModifier(Modifiers modifier) => (modifiers & modifier) == modifier;

	public bool HasAnyModifier() => modifiers != Modifiers.None;

	public bool IsCombo => HasModifier(Modifiers.Combo);
	public bool IsMash => HasModifier(Modifiers.Mash);
	public bool IsFrozen => HasModifier(Modifiers.Frozen);
	public bool IsChained => HasModifier(Modifiers.Chained);
	public bool IsLoose => HasModifier(Modifiers.Loose);
	public bool IsThorned => HasModifier(Modifiers.Thorned);
	public bool IsOffGCD => HasModifier(Modifiers.OffGlobalCooldown);

	/// <summary>
	///     Sets the specified modifier for this key.
	/// </summary>
	/// <param name="modifier"> The modifier to set. </param>
	/// <param name="value"> The value to set the modifier to. </param>
	/// <param name="args">
	///     Additional arguments for specific modifiers. For example, OffGlobalCooldown can take a float
	///     argument to set a new cooldown time.
	/// </param>
	public void SetModifier(Modifiers modifier, bool value = true, params object[] args)
	{
		switch (modifier)
		{
			case Modifiers.None:
				modifiers = Modifiers.None;
				
				KeyModifier = null;
				break;

			case Modifiers.Combo:
				modifiers = value ? modifiers | Modifiers.Combo : modifiers & ~Modifiers.Combo;

				comboMarker.SetActive(value);

				if (!LastKeyInCombo) return; // Only the last key in a combo gets a special effect. Prevents issues like the RTY-incident.

				// 50/50 chance to get either adjacent keys or surrounding keys effect
				//List<ComboEffect> possibleEffects = new () { /*Effect.GetEffect<CE_Adjacent>(), */ Effect.GetEffect<CE_Shockwave>(), /*Effect.GetEffect<CE_Pulse>() */};
				//comboEffect = possibleEffects[Random.Range(0, possibleEffects.Count)];
				ComboEffect[] effects = Resources.LoadAll<ComboEffect>(ResourcePaths.Combos);
				ComboEffect = effects.Where(e => e is not CE_Wave).OrderBy(_ => Random.value).FirstOrDefault();
				break;

			case Modifiers.Mash:
				modifiers = value ? modifiers | Modifiers.Mash : modifiers & ~Modifiers.Mash;

				mashMarker.SetActive(value);

				ComboEffect = Effect.GetEffect<CE_Wave>();
				break;
			
			case Modifiers.Frozen:
				modifiers = value ? modifiers | Modifiers.Frozen : modifiers & ~Modifiers.Frozen;
				
				//frozenMarker.SetActive(value);
				
				KeyModifier = Effect.GetModifier<KE_Frozen>();
				if (!value) KeyModifier = null;
				break;

			case Modifiers.Chained:
				modifiers = value ? modifiers | Modifiers.Chained : modifiers & ~Modifiers.Chained;

				ChainedMarker.SetActive(value);
				Disable();

				KeyModifier = Effect.GetModifier<KE_Chained>();
				break;

			case Modifiers.Loose:
				modifiers = value ? modifiers | Modifiers.Loose : modifiers & ~Modifiers.Loose;

				// ReSharper disable once AssignmentInConditionalExpression
				if (value) transform.DOShakeRotation(0.4f, new Vector3(10, 0, 10), 10, 90, false, ShakeRandomnessMode.Harmonic).SetLoops(-1, LoopType.Yoyo).SetDelay(0.5f).SetId("Loose");

				KeyModifier = Effect.GetModifier<KE_Loose>();
				break;

			case Modifiers.Thorned:
				modifiers = value ? modifiers | Modifiers.Thorned : modifiers & ~Modifiers.Thorned;

				// TODO: add visual indicator for thorned keys. 'thornedMarker' is currently blank
				thornedMarker.SetActive(value);

				KeyModifier = Effect.GetModifier<KE_Thorned>(true);
				break;

			case Modifiers.OffGlobalCooldown:
				modifiers = value ? modifiers | Modifiers.OffGlobalCooldown : modifiers & ~Modifiers.OffGlobalCooldown;

				oGCDMarker.SetActive(value);
				if (args.Length > 0 && args[0] is float newCooldown and > 0f) cooldown = newCooldown;
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(modifier), modifier, null);
		}
	}

	/// <summary>
	/// Shorthand for SetModifier(effect, true).
	/// </summary>
	public void AddModifier(Modifiers modifier) => SetModifier(modifier, true);
	/// <summary>
	/// Shorthand for SetModifier(effect, false).
	/// </summary>
	public void RemoveModifier(Modifiers modifier) => SetModifier(modifier, false);
}
