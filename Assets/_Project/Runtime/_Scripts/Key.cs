#region
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Lumina.Essentials.Attributes;
using MelenitasDev.SoundsGood;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using VInspector;
using Random = UnityEngine.Random;
#endregion

public partial class Key : MonoBehaviour
{
	[Tab("Attributes")]
	[Header("Attributes")]
	[SerializeField, ReadOnly] KeyCode keyboardLetter = KeyCode.Q;
	[SerializeField] int damage;
	[SerializeField] bool chained;
	[SerializeField] bool loose;
	[SerializeField] bool thorned;
	[SerializeField] bool offGlobalCooldown;
	[SerializeField] bool combo;
	[SerializeField] bool mash;
	[SerializeField, ReadOnly] int comboIndex;
	[SerializeField, ReadOnly] int mashCount;
	
	[Header("Cooldown")]
	[SerializeField] float cooldown = 2.5f;
	[SerializeField, ReadOnly] float remainingCooldown;
	[SerializeField, ReadOnly] float currentCooldown;

	[Tab("References")]
	[SerializeField] GameObject chainedMarker;
	[SerializeField] GameObject comboHighlight;
	[SerializeField] SpriteRenderer spriteRenderer;
	[SerializeField] TMP_Text letter;
	[SerializeField] SpriteRenderer cooldownSprite;
	[SerializeField] GameObject homingBar;
	[SerializeField] GameObject oGCDMarker;
	[SerializeField] GameObject comboMarker;
	[SerializeField] GameObject mashMarker;
	[SerializeField] TMP_Text damageText;

	[Tab("Settings")]
	[Header("Settings")]
	[SerializeField] bool isActive = true;
	[Header("Debug Info")]
	[Tooltip("The index of the row this key is in. 1-based.")]
	[SerializeField, ReadOnly] int row;
	[Tooltip("The index of this key within its row. 0-based.")]
	[SerializeField, ReadOnly] int indexInRow;
	[Tooltip("The index of this key in the entire keyboard. 0-based.")]
	[SerializeField, ReadOnly] int indexKey;

	Enemy currentEnemy;
	ComboManager comboManager;

	#region Components/Children
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
	#endregion

	public bool IsActive => isActive;
	public void Disable(bool setColour = true)
	{
		isActive = false;
		if (setColour) SetColour(Color.grey);
	}

	public void Enable(bool setColour = true)
	{
		isActive = true;
		if (setColour) SetColour(Color.white);
	}

	public int ComboIndex
	{
		get => comboIndex;
		set => comboIndex = value;
	}
	
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

	void Awake()
	{
		#region Cooldown Sprite
		// Ensure the cooldown sprite is fully opaque at start. In the prefab view the alpha is 0.5f;
		Color color = CooldownSprite.color;
		color.a = 0.65f;
		CooldownSprite.color = color;

		// makes the cooldown fill animate the other way (looks better)
		CooldownSprite.flipX = true;
		#endregion

		ChainedMarker.SetActive(false);
		ComboHighlight.SetActive(false);
		HomingBar.SetActive(false);
		offGCDMarker.SetActive(false);
		ComboMarker.SetActive(false);
		MashMarker.SetActive(false);
	}


	#region SFX
	Sound sfx;
	float lastSfxTime = -1f;
	const float SfxCooldown = 0.5f;
	
	void InitSFX()
	{
		sfx = new (SFX.beep);
		sfx.SetOutput(Output.SFX);
		sfx.SetVolume(0.85f);
		sfx.SetRandomPitch(new (0.95f, 1.05f));
	}
	#endregion
	
	void Start()
	{
		comboManager = ComboManager.Instance;

		InitSFX();
		
		OnActivated += (hitEnemy, triggeredBy) =>
		{
			if (Time.time - lastSfxTime > SfxCooldown)
			{
				sfx.Play();
				lastSfxTime = Time.time;
			}
		};

		// Calculate damage based on indexInRow (more damage for keys further to the right)
		damage = Mathf.Max(1, Mathf.RoundToInt(indexInRow / 2f));

		offGCDMarker.SetActive(offGlobalCooldown);
		ComboMarker.SetActive(combo);
		MashMarker.SetActive(mash);
		DamageText.text = Mash ? mashCount.ToString() : string.Empty;

		// reset all fills (hide)
		DrawCooldownFill();

		Assert();
	}

	void Assert()
	{
		Debug.Assert(ChainedMarker != null, $"{name} is missing a reference to its ChainedSprite!");
		Debug.Assert(ComboHighlight != null, $"{name} is missing a reference to its ComboHighlight!");
		Debug.Assert(SpriteRenderer != null, $"{name} is missing a reference to its SpriteRenderer!");
		Debug.Assert(Letter != null, $"{name} is missing a reference to its Letter TMP_Text!");
		Debug.Assert(CooldownSprite != null, $"{name} is missing a reference to its CooldownSprite!");
		Debug.Assert(HomingBar != null, $"{name} is missing a reference to its HomingBar!");
		Debug.Assert(offGCDMarker != null, $"{name} is missing a reference to its offGCDMarker!");
		Debug.Assert(ComboMarker != null, $"{name} is missing a reference to its ComboMarker!");
		Debug.Assert(MashMarker != null, $"{name} is missing a reference to its MashMarker!");
		Debug.Assert(DamageText != null, $"{name} is missing a reference to its DamageText!");
	}

	public void InitKey(KeyCode keycode, int row, int indexInRow, int indexKey)
	{
		keyboardLetter = keycode;
		this.row = row;
		this.indexInRow = indexInRow;
		this.indexKey = indexKey;

		Letter.text = keycode.ToString();
		Letter.text = Letter.text.Replace("Alpha", ""); // remove "Alpha" from numeric keys

		HomingBar.SetActive(keycode is KeyCode.F or KeyCode.J);
	}

	void Update()
	{
		if (!isActive || Chained) return;

		// Handle per-key cooldown timer
		if (CooldownTime > 0f)
		{
			CooldownTime -= Time.deltaTime;

			if (CooldownTime <= 0f)
			{
				CooldownTime = 0f;
				SetColour(Color.white);
			}
			else // not finished cooldown yet
				DrawCooldownFill();
		}
	}

	readonly static int Arc2 = Shader.PropertyToID("_Arc2");

	void DrawCooldownFill() => CooldownSprite.material.SetFloat(Arc2, Mathf.Lerp(360f, 0f, CooldownTime / currentCooldown));

	void OnTriggerEnter2D(Collider2D other)
	{
		//Debug.Log($"{name} overlapping with: {other.name}!", other.gameObject);
		if (other.TryGetComponent(out Enemy enemy)) currentEnemy = enemy;
	}

	void OnTriggerExit2D(Collider2D other)
	{
		if (other.TryGetComponent(out Enemy enemy))
			if (currentEnemy == enemy)
				currentEnemy = null;
	}

	Coroutine mashTimerCoroutine;

	void ActivatedWhileLoose()
	{
		DOTween.Kill("Loose"); // Stop the infinite shaking tween.

		isActive = false; // Disables the key while falling but doesn't change the colour.
		Loose = false;
		Vector3 originalPos = transform.position;

		var rb = gameObject.AddComponent<Rigidbody2D>();
		rb.bodyType = RigidbodyType2D.Dynamic;
		const float FORCE = 1.5f;
		rb.AddForce(new Vector3(1f, 3f) * FORCE, ForceMode2D.Impulse);
		rb.AddTorque(5, ForceMode2D.Impulse);

		// Falling off animation
		transform.DOScale(Vector3.zero, 1.5f)
		         .SetDelay(1f)
		         .SetEase(Ease.InBack)
		         .OnComplete
		          (() =>
		          {
			          Disable();
			          SpriteRenderer.gameObject.SetActive(false);
			          transform.position = originalPos;
			          transform.rotation = Quaternion.identity;
			          transform.localScale = Vector3.one;
			          Destroy(rb);
		          });
	}

	int timesActivatedByKey;
	IEnumerator StackOverflowProtection()
	{
		yield return null;
		timesActivatedByKey = 0;
	}

	/// <summary>
	///     Event triggered when the key is activated.
	///     The first parameter indicates whether an enemy was hit (true) or not (false).
	///     The second parameter indicates whether the key was triggered by another key (Key != null) or by player input
	///     (null).
	/// </summary>
	public event Action<bool, Key> OnActivated;

	/// <summary>
	///     Activates the key, dealing damage to the current enemy if one is present.
	/// </summary>
	/// <param name="overrideGlobalCooldown">
	///     If true, the key will not trigger the global cooldown when pressed. (Overrides
	///     the OffGlobalCooldown property)
	/// </param>
	/// <param name="cooldownOverride">
	///     If greater than 0, this value will be used as the cooldown instead of the key's default
	///     cooldown.
	/// </param>
	/// <param name="triggeredBy"> The key that triggered this key, if any. Null if triggered by player input. (false) </param>
	/// <returns> True if an enemy was hit, false otherwise. </returns>
	public void Activate(bool overrideGlobalCooldown = false, float cooldownOverride = -1f, Key triggeredBy = null) // false by default
	{
		bool triggeredByKey = triggeredBy != null;
		#region Infnite Loop Protection - only allow 1 activation per frame per key
		if (triggeredByKey)
		{
			timesActivatedByKey++;

			if (timesActivatedByKey > 1)
			{
#if false
                Debug.LogError($"Potential infinite activation loop detected on key {name}. Activation aborted.");
#endif
				timesActivatedByKey = 0;
				return;
			}

			StartCoroutine(StackOverflowProtection());
		}
		#endregion

		if (Chained)
		{
			if (triggeredByKey)
			{
				Chained = false;
				ChainedMarker.SetActive(false);
				Enable();
			}
			else
			{
				// shake the key left and right quickly to indicate it is chained
				transform.DOPunchPosition(new (0.1f, 0f, 0f), 0.2f, 20);
				return;
			}
		}

		if (!isActive) return;

		// Prevent activation if the key is still on cooldown and global cooldown override is not requested.
		if (CooldownTime > 0f && !overrideGlobalCooldown) return;

		// Reset combo if:
		// (1) combo in progress and this key is not part of the combo, or...
		// (2) combo in progress, not triggered by key, this key is part of the combo but is not the next key
		if (comboManager.InProgress && !triggeredByKey && (!comboManager.CurrentComboKeys.Contains(this) || (comboManager.CurrentComboKeys.Contains(this) && comboIndex != comboManager.NextComboIndex))) comboManager.ResetCombo();

		bool hitEnemy = DealDamage();

		// Combo key logic
		if (Combo)
		{
			int nextKeyIndex = comboManager.NextComboIndex;

			if (comboIndex == 0)
			{
				comboManager.BeginCombo(keyboardLetter);
				StartLocalCooldown(cooldown);
				SetColour(hitEnemy ? Color.green : Color.cyan, 0.25f);
				OnActivated?.Invoke(hitEnemy, triggeredBy);

				ComboHighlight.gameObject.SetActive(false);
				return;
			}

			if (comboIndex == nextKeyIndex)
			{
				comboManager.AdvanceCombo(keyboardLetter);

				// Combo completed
				if (comboIndex == comboManager.ComboLength - 1)
				{
					var vfx = Resources.Load<ParticleSystem>("PREFABS/Combo Effect");
					List<Key> surroundingKeys = KeyManager.Instance.GetSurroundingKeys(keyboardLetter);
					Key self = KeyManager.Instance.GetAdjacentKey(this.ToKeyCode(), KeyManager.Direction.All, out List<Key> adjacentKeys);

					foreach (Key key in surroundingKeys)
					{
						ParticleSystem instantiate = Instantiate(vfx, key.transform.position, Quaternion.identity);
						ParticleSystem.MainModule instantiateMain = instantiate.main;
						instantiateMain.startColor = Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f);
						key.Activate(true, 0.5f, this);
						key.SetColour(hitEnemy ? Color.green : Color.cyan, 0.25f);
						OnActivated?.Invoke(hitEnemy, this);
					}

					StartLocalCooldown(cooldown);
					SetColour(hitEnemy ? Color.green : Color.cyan, 0.25f);
					OnActivated?.Invoke(hitEnemy, triggeredBy);
					return;
				}

				StartLocalCooldown(cooldown);
				SetColour(hitEnemy ? Color.green : Color.cyan, 0.25f);
				OnActivated?.Invoke(hitEnemy, triggeredBy);

				ComboHighlight.gameObject.SetActive(false);
				return;
			}
		}

		if (Mash)
		{
			if (mashTimerCoroutine != null) StopCoroutine(mashTimerCoroutine);
			mashTimerCoroutine = StartCoroutine(MashTimer());

			mashCount++;
			DamageText.text = mashCount > 0 ? mashCount.ToString() : string.Empty;

			// every 5th mash triggers a special effect. Wave effect cycles multiple times based on how many multiples of 5 have been reached.
			if (mashCount % 5 == 0)
			{
				int cycles = mashCount / 5;
				KeyManager.Instance.Wave(cycles, 5); // mashCount of 5 = 1 cycle, 10 = 2 cycles, etc. Max 5 cycles.
				StartLocalCooldown(5f);
				SetColour(hitEnemy ? Color.green : Color.orange, 0.25f);
				OnActivated?.Invoke(hitEnemy, triggeredBy);
				return;
			}

			StartLocalCooldown(0.25f);
			SetColour(hitEnemy ? Color.green : Color.orange, 0.25f);
			OnActivated?.Invoke(hitEnemy, triggeredBy);
			return;
		}

		// If the key is loose, it will fall off the keyboard when pressed by the player (not triggered by another key)
		if (Loose && !triggeredByKey) ActivatedWhileLoose();

		if (OffGlobalCooldown)
		{
			StartLocalCooldown(cooldown + 2.5f);
			SetColour(hitEnemy ? Color.green : Color.crimson, 0.5f);
			OnActivated?.Invoke(hitEnemy, triggeredBy);
			return;
		}

		if (overrideGlobalCooldown)
		{
			StartLocalCooldown(cooldownOverride > 0 ? cooldownOverride : cooldown);
			SetColour(hitEnemy ? Color.green : Color.crimson, 0.5f);
			OnActivated?.Invoke(hitEnemy, triggeredBy);

			//Debug.Log($"{name} activated by {(triggerKey != null ? triggerKey.name : "Player Input")}.");
		}
		else
		{
			KeyManager.Instance.StartGlobalCooldown();
			SetColour(hitEnemy ? Color.green : Color.crimson, 0.5f);
			OnActivated?.Invoke(hitEnemy, triggeredBy);
		}
	}

	bool DealDamage()
	{
		if (currentEnemy)
		{
			currentEnemy.TakeDamage(damage);
			return true;
		}

		return false;
	}

	IEnumerator MashTimer()
	{
		// if 3 seconds pass without a mash, reset the mash count
		yield return new WaitForSeconds(3f);
		mashCount = 0;
		DamageText.text = mashCount.ToString();
	}

	public void StartLocalCooldown(float cooldown)
	{
		// dont start a new cooldown if the current cooldown is longer than the new one
		if (CooldownTime > cooldown) return;

		CooldownTime = cooldown;
		currentCooldown = cooldown;

		SetColour(Color.grey);
	}

	void SetColour(Color color)
	{
		if (SpriteRenderer.color == Color.green || SpriteRenderer.color == Color.crimson) return;
		SpriteRenderer.color = color;
	}

	void SetColour(Color color, float duration) => StartCoroutine(SwitchColour(color, duration));

	IEnumerator SwitchColour(Color colour, float duration)
	{
		SpriteRenderer.color = colour;
		yield return new WaitForSeconds(duration);
		SpriteRenderer.color = CooldownTime > 0f ? Color.grey : Color.white;
	}
}

public static class KeyExtensions
{
	/// <summary>
	///     Sets the specified modifier for all keys in the list.
	/// </summary>
	/// <param name="keys"> The list of keys to modify. </param>
	/// <param name="modifier"> The modifier to set. </param>
	/// <param name="value"> The value to set the modifier to. </param>
	public static void SetModifier(this List<Key> keys, Key.Modifier modifier, bool value = true)
	{
		foreach (Key key in keys) key.SetModifier(modifier, value);
	}

	// to keycode from single key
	public static KeyCode ToKeyCode(this Key key) => !key ? KeyCode.None : key.KeyboardLetter;

	// to Key from single keycode
	public static Key ToKey(this KeyCode keycode) => KeyManager.Instance.GetKey(keycode);

	// convert list of keys to keycodes
	public static List<KeyCode> ToKeyCodes(this List<Key> keys) => keys.Select(k => k.KeyboardLetter).ToList();

	// to keycodes from string
	public static List<KeyCode> ToKeyCodes(this string str)
	{
		str = str.ToUpper();

		List<KeyCode> keycodes = new ();
		try { keycodes = str.Select(c => (KeyCode) Enum.Parse(typeof(KeyCode), c.ToString())).ToList(); } catch (ArgumentException e) { Debug.LogError($"Invalid character in string '{str}': {e.Message}. " + "\nThere may be duplicate or unsupported characters."); }

		return keycodes;
	}

	// get a list of keys from a list of keycodes
	public static List<Key> ToKeys(this List<KeyCode> keycodes) => keycodes.Select(k => KeyManager.Instance.GetKey(k)).Where(k => k != null).ToList();
}
