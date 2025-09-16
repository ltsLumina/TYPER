#region
using System;
using System.Collections.Generic;
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

public partial class Key // Effects
{
	[Flags]
	public enum Effects
	{
		[UsedImplicitly] 
		None = 0,
		Combo = 1 << 0,
		Mash = 1 << 1,
		Chained = 1 << 2,
		Loose = 1 << 3,
		Thorned = 1 << 4,
		OffGlobalCooldown = 1 << 5
	}
	
	/// <summary>
	///    Checks if the specified modifier is set for this key.
	/// </summary>
	/// <param name="effect"> The modifier to check. </param>
	/// <returns> True if the modifier is set, false otherwise. </returns>
	public bool GetEffect(Effects effect) => (effects & effect) == effect;
	
	public bool IsCombo => GetEffect(Effects.Combo);
	public bool IsMash => GetEffect(Effects.Mash);
	public bool IsChained => GetEffect(Effects.Chained);
	public bool IsLoose => GetEffect(Effects.Loose);
	public bool IsThorned => GetEffect(Effects.Thorned);
	public bool IsOffGCD => GetEffect(Effects.OffGlobalCooldown);

	/// <summary>
	///     Sets the specified modifier for this key.
	/// </summary>
	/// <param name="effect"> The modifier to set. </param>
	/// <param name="value"> The value to set the modifier to. </param>
	/// <param name="args">
	///     Additional arguments for specific modifiers. For example, OffGlobalCooldown can take a float
	///     argument to set a new cooldown time.
	/// </param>
	public void SetEffect(Effects effect, bool value = true, params object[] args)
	{
		switch (effect)
		{
			case Effects.None:
				effects = Effects.None;
				
				keyEffect = null;
				break;

			case Effects.Combo:
				effects = value ? effects | Effects.Combo : effects & ~Effects.Combo;
				
				comboMarker.SetActive(value);

				if (!LastKeyInCombo) return; // Only the last key in a combo gets a special effect. Prevents issues like the RTY-incident.

				// 50/50 chance to get either adjacent keys or surrounding keys effect
				List<KeyEffect> possibleEffects = new () { KeyEffect.GetEffect<KE_AdjacentKeys>(), KeyEffect.GetEffect<KE_Shockwave>(), KeyEffect.GetEffect<KE_Pulse>(true) };
				keyEffect = possibleEffects[Random.Range(0, possibleEffects.Count)];
				break;

			case Effects.Mash:
				effects = value ? effects | Effects.Mash : effects & ~Effects.Mash;
				
				mashMarker.SetActive(value);

				keyEffect = KeyEffect.GetEffect<KE_Wave>();
				break;

			case Effects.Chained:
				effects = value ? effects | Effects.Chained : effects & ~Effects.Chained;
				
				ChainedMarker.SetActive(value);
				Disable();

				keyEffect = KeyEffect.GetEffect<KE_Chained>();
				break;

			case Effects.Loose:
				effects = value ? effects | Effects.Loose : effects & ~Effects.Loose;
				
				// ReSharper disable once AssignmentInConditionalExpression
				if (value) transform.DOShakeRotation(0.4f, new Vector3(10, 0, 10), 10, 90, false, ShakeRandomnessMode.Harmonic).SetLoops(-1, LoopType.Yoyo).SetDelay(0.5f).SetId("Loose");

				keyEffect = KeyEffect.GetEffect<KE_Loose>();
				break;

			case Effects.Thorned:
				effects = value ? effects | Effects.Thorned : effects & ~Effects.Thorned;
				
				// TODO: add visual indicator for thorned keys. 'thornedMarker' is currently blank
				thornedMarker.SetActive(value);

				keyEffect = KeyEffect.GetEffect<KE_Thorned>(true);
				break;

			case Effects.OffGlobalCooldown:
				effects = value ? effects | Effects.OffGlobalCooldown : effects & ~Effects.OffGlobalCooldown;
				
				oGCDMarker.SetActive(value);
				if (args.Length > 0 && args[0] is float newCooldown and > 0f) cooldown = newCooldown;
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(effect), effect, null);
		}
	}
	
	/// <summary>
	/// Shorthand for SetEffect(effect, true).
	/// </summary>
	public void AddEffect(Effects effect) => SetEffect(effect, true);
	/// <summary>
	/// Shorthand for SetEffect(effect, false).
	/// </summary>
	public void RemoveEffect(Effects effect) => SetEffect(effect, false);

	void OnValidate()
	{
		// set the keyeffect based on the effects flags
		if (effects == Effects.None) keyEffect = null;
		else if (GetEffect(Effects.Combo)) { }
		else if (GetEffect(Effects.Mash)) keyEffect = KeyEffect.GetEffect<KE_Wave>();
		else if (GetEffect(Effects.Chained)) keyEffect = KeyEffect.GetEffect<KE_Chained>();
		else if (GetEffect(Effects.Loose)) keyEffect = KeyEffect.GetEffect<KE_Loose>();
		else if (GetEffect(Effects.Thorned)) keyEffect = KeyEffect.GetEffect<KE_Thorned>();
		else if (GetEffect(Effects.OffGlobalCooldown)) { }
	}
}
