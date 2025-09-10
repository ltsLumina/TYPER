#region
using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
#endregion

public partial class Key // Properties
{
	public KeyCode KeyboardLetter
	{
		get => keyboardLetter;
		set => keyboardLetter = value;
	}
	public int Row
	{
		get => row;
		set => row = value;
	}
	public int IndexInRow
	{
		get => indexInRow;
		set => indexInRow = value;
	}
	public int IndexGlobal
	{
		get => indexKeyboard;
		set => indexKeyboard = value;
	}
	public int ComboIndex
	{
		get => comboIndex;
		set => comboIndex = value;
	}
	public bool OffGlobalCooldown
	{
		get => offGlobalCooldown;
		set => offGlobalCooldown = value;
	}
	public bool Combo
	{
		get => combo;
		set => combo = value;
	}
	public bool Mash
	{
		get => mash;
		set => mash = value;
	}
	public bool Chained
	{
		get => chained;
		set => chained = value;
	}
	public bool Loose
	{
		get => loose;
		set => loose = value;
	}
	public bool Thorned
	{
		get => thorned;
		set => thorned = value;
	}
	public float CooldownTime
	{
		get => remainingCooldown;
		set => remainingCooldown = value;
	}
}

public partial class Key // Components
{
	public GameObject ChainedMarker => chainedMarker;
	public GameObject ComboHighlight => comboHighlight;
	public SpriteRenderer SpriteRenderer => spriteRenderer;
	public TMP_Text Letter => letter;
	public SpriteRenderer CooldownSprite => cooldownSprite;
	public GameObject HomingBar => homingBar;
	public GameObject offGCDMarker => oGCDMarker;
	public GameObject ComboMarker => comboMarker;
	public GameObject MashMarker => mashMarker;
	public TMP_Text DamageText => damageText;
}

public partial class Key // Modifiers
{
	public enum Modifier
	{
		OffGlobalCooldown, // key can be pressed without triggering the global cooldown. Typically, has a longer cooldown.
		Combo,             // key is part of a combo sequence, which must be pressed in order. If pressed out of order, the combo resets
		Mash,              // key can be pressed rapidly to build up a counter, which triggers an effect when it reaches a certain threshold
		Chained,           // key is locked and cannot be pressed manually. Can be unlocked by having a different key activate it (e.g., through a combo that activates an adjacent key)
		Loose,             // key is loose and will fall off the keyboard when pressed
		Thorned,           // "rooted"? - take self-damage when key is pressed
	}

	/// <summary>
	///     Sets the specified modifier for this key.
	/// </summary>
	/// <param name="modifier"> The modifier to set. </param>
	/// <param name="value"> The value to set the modifier to. </param>
	/// <param name="args">
	///     Additional arguments for specific modifiers. For example, OffGlobalCooldown can take a float
	///     argument to set a new cooldown time.
	/// </param>
	public void SetModifier(Modifier modifier, bool value = true, params object[] args)
	{
		switch (modifier)
		{
			case Modifier.OffGlobalCooldown:
				OffGlobalCooldown = value;
				offGCDMarker.SetActive(OffGlobalCooldown);
				if (args.Length > 0 && args[0] is float newCooldown and > 0f) cooldown = newCooldown;
				break;

			case Modifier.Combo:
				Combo = value;
				ComboMarker.SetActive(Combo);
				break;

			case Modifier.Mash:
				Mash = value;
				MashMarker.SetActive(Mash);
				break;

			case Modifier.Chained:
				Chained = value;
				ChainedMarker.SetActive(Chained);
				Disable();
				break;

			case Modifier.Loose:
				Loose = value;
				transform.DOShakeRotation(0.4f, new Vector3(10, 0, 10), 10, 90, false, ShakeRandomnessMode.Harmonic).SetLoops(-1, LoopType.Yoyo).SetDelay(0.5f).SetId("Loose");
				break;

			case Modifier.Thorned:
				Thorned = value;
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(modifier), modifier, null);
		}
	}
}
