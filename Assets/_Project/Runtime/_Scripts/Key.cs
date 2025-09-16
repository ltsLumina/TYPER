#region
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Lumina.Essentials.Attributes;
using Lumina.Essentials.Modules;
using MelenitasDev.SoundsGood;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using VInspector;
using Random = UnityEngine.Random;
#endregion

[SelectionBase]
public partial class Key : MonoBehaviour
{
	[Tab("Attributes")]
	[Header("Attributes")]
	[SerializeField, ReadOnly] KeyCode keyCode = KeyCode.Q;
	[SerializeField] int damage;
	[SerializeField] bool chained;
	[SerializeField] bool loose;
	[SerializeField] bool thorned;
	[SerializeField] bool offGlobalCooldown;
	[SerializeField] bool combo;
	[SerializeField] bool mash;
	[SerializeField] KeyEffect keyEffect;
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
	[SerializeField] TMP_Text mashText;

	[Tab("Settings")]
	[Header("Settings")]
	[SerializeField] bool isActive = true;
	[Header("Debug Info")]
	[Tooltip("The index of the row this key is in. 1-based.")]
	[SerializeField, ReadOnly] int row;
	[Tooltip("The index of this key within its row. 0-based.")]
	[SerializeField, ReadOnly] int indexInRow;
	[Tooltip("The index of this key in the entire keyboard. 0-based.")]
	[SerializeField, ReadOnly] int indexKeyboard;

	Enemy currentEnemy;
	ComboManager comboManager;

	public bool IsActive => isActive;
	public void Disable(bool setColour = true)
	{
		isActive = false;
		SetColour(setColour ? Color.grey : SpriteRenderer.color);
	}

	public void Enable(bool setColour = true)
	{
		isActive = true;
		SetColour(setColour ? Color.white : SpriteRenderer.color);
	}

	void Awake()
	{
		#region Cooldown Sprite
		// Ensure the cooldown sprite is fully opaque at start. In the prefab view the alpha is 0.5f;
		Color color = cooldownSprite.color;
		color.a = 0.65f;
		cooldownSprite.color = color;

		// makes the cooldown fill animate the other way (looks better)
		cooldownSprite.flipX = true;
		#endregion

		chainedMarker.SetActive(false);
		comboHighlight.SetActive(false);
		homingBar.SetActive(false);
		oGCDMarker.SetActive(false);
		comboMarker.SetActive(false);
		mashMarker.SetActive(false);
	}

	#region SFX
	Sound sfx;
	float lastSfxTime = -1f;
	const float SfxCooldown = 0.25f;

	void InitSFX()
	{
		sfx = new (SFX.beep);
		sfx.SetOutput(Output.SFX);
		sfx.SetVolume(0.85f);
		sfx.SetRandomPitch();
	}
	#endregion

	void Start()
	{
		comboManager = ComboManager.Instance;

		InitSFX();

		OnActivated += (_, _) =>
		{
			if (Time.time - lastSfxTime > SfxCooldown)
			{
				sfx.SetRandomPitch();
				sfx.Play();
				lastSfxTime = Time.time;
			}
		};

		// Calculate damage based on indexInRow (more damage for keys further to the right)
		// Minimum damage is 2, maximum is roughly half the number of keys in the row, e.g. for 10 keys in a row, max damage is 5
		int min = 2;
		damage = Mathf.Max(min, Mathf.RoundToInt(indexInRow / 2f) + min);
		
		MashText.text = Mash ? mashCount.ToString() : string.Empty;

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
		Debug.Assert(MashText != null, $"{name} is missing a reference to its DamageText!");
		
		Debug.Assert(Helpers.CameraMain.GetComponent<Physics2DRaycaster>(), "Main Camera is missing a \"Physics 2D Raycaster\" component, which is required for UI interaction with keys.");
	}

	public void InitKey(KeyCode keycode, int row, int indexInRow, int indexKeyboard)
	{
		keyCode = keycode;
		this.row = row;
		this.indexInRow = indexInRow;
		this.indexKeyboard = indexKeyboard;

		Letter.text = keycode.ToString();
		Letter.text = Letter.text.Replace("Alpha", string.Empty); // remove "Alpha" from numeric keys

		HomingBar.SetActive(keycode is KeyCode.F or KeyCode.J);
	}

	void Update()
	{
		if (!isActive || Chained) return;

		// Handle per-key cooldown timer
		if (RemainingCooldown > 0f)
		{
			RemainingCooldown -= Time.deltaTime;

			if (RemainingCooldown <= 0f)
			{
				RemainingCooldown = 0f;
				SetColour(Color.white);
			}
			else // not finished cooldown yet
				DrawCooldownFill();
		}
	}

	readonly static int Arc2 = Shader.PropertyToID("_Arc2");

	void DrawCooldownFill() => CooldownSprite.material.SetFloat(Arc2, Mathf.Lerp(360f, 0f, RemainingCooldown / currentCooldown));

	readonly List<Enemy> overlappingEnemies = new ();
	
	void OnTriggerEnter2D(Collider2D other)
	{
	    if (other.TryGetComponent(out Enemy enemy) && !overlappingEnemies.Contains(enemy))
	        overlappingEnemies.Add(enemy);
	}
	
	void OnTriggerExit2D(Collider2D other)
	{
	    if (other.TryGetComponent(out Enemy enemy))
	        overlappingEnemies.Remove(enemy);
	}

	Coroutine mashTimerCoroutine;

	int timesActivatedByKey;
	const int MAX_ACTIVATIONS_PER_FRAME = 5; // arbitrary limit to prevent infinite loops

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
	/// <param name="triggerKey"> The key that triggered this key, if any. Null if triggered by player input. (false) </param>
	/// <returns> True if an enemy was hit, false otherwise. </returns>
	public void Activate(bool overrideGlobalCooldown = false, float cooldownOverride = -1f, Key triggerKey = null) // false by default
	{
		bool triggeredByKey = triggerKey != null;
		#region Infnite Loop Protection - only allow 1 activation per frame per key
		if (triggeredByKey)
		{
			timesActivatedByKey++;

			if (timesActivatedByKey > MAX_ACTIVATIONS_PER_FRAME)
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
			keyEffect?.Invoke(this, triggerKey);
		}

		if (!isActive) return;

		// Prevent activation if the key is still on cooldown and global cooldown override is not requested.
		if (RemainingCooldown > 0f && !overrideGlobalCooldown) return;

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
				comboManager.BeginCombo(keyCode);
				StartLocalCooldown(cooldown);
				SetColour(hitEnemy ? Color.green : Color.cyan, 0.25f);
				OnActivated?.Invoke(hitEnemy, triggerKey);

				ComboHighlight.gameObject.SetActive(false);
				return;
			}

			if (comboIndex == nextKeyIndex)
			{
				comboManager.AdvanceCombo(keyCode);

				// Combo completed
				if (comboIndex == comboManager.ComboLength - 1)
				{
					var comboVFX = Resources.Load<ParticleSystem>("PREFABS/Combo VFX");
					ObjectPool comboPool = ObjectPoolManager.FindObjectPool(comboVFX.gameObject);

					if (SceneManagerExtended.ActiveSceneName == "Game")
					{
						// Condition that fixes the infamous "RTY-bug". idk why this works, probably a race condition?
						if (comboIndex == comboManager.ComboLength - 1 && comboManager.RecentKey == this) 
							keyEffect?.Invoke(this, triggerKey);
					}
					else
					{
						foreach (Key key in "TYPER".ToKeyCodes().ToKeys())
						{
							var vfx = comboPool.GetPooledObject<ParticleSystem>(true, key.transform.position);
							ParticleSystem.MainModule main = vfx.main;
							main.startColor = Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f);
						}

						foreach (Key key in "PLAY".ToKeyCodes().ToKeys())
						{
							var vfx = comboPool.GetPooledObject<ParticleSystem>(true, key.transform.position);
							ParticleSystem.MainModule main = vfx.main;
							main.startColor = Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f);

							GameManager.Instance.ExitTransition.gameObject.SetActive(true);
						}
					}

					StartLocalCooldown(cooldown);
					SetColour(hitEnemy ? Color.green : Color.cyan, 0.25f);
					OnActivated?.Invoke(hitEnemy, triggerKey);
					return;
				}

				StartLocalCooldown(cooldown);
				SetColour(hitEnemy ? Color.green : Color.cyan, 0.25f);
				OnActivated?.Invoke(hitEnemy, triggerKey);

				// Hide the highlight when the combo is complete or reset
				ComboHighlight.gameObject.SetActive(false);
				return;
			}
		}

		if (Mash)
		{
			// Don't increment mash count if the key is triggered by itself (e.g., through its own effect)
			if (triggerKey != this) mashCount++;
			else return;

			MashText.text = mashCount.ToString();

			if (mashTimerCoroutine != null) StopCoroutine(mashTimerCoroutine);
			mashTimerCoroutine = StartCoroutine(MashTimer());

			if (mashCount % 5 == 0) // every 5th mash
			{
				keyEffect?.Invoke(this, triggerKey);
				StartLocalCooldown(5f);
				SetColour(hitEnemy ? Color.green : Color.orange, 0.25f);
				OnActivated?.Invoke(hitEnemy, triggerKey);
				return;
			}

			StartLocalCooldown(0.25f);
			SetColour(hitEnemy ? Color.green : Color.orange, 0.25f);
			OnActivated?.Invoke(hitEnemy, triggerKey);
			return;
		}

		// If the key is loose, it will fall off the keyboard when pressed by the player (not triggered by another key)
		if (Loose) keyEffect?.Invoke(this, triggerKey);
		
		if (Thorned) keyEffect?.Invoke(this, triggerKey);

		if (OffGlobalCooldown)
		{
			StartLocalCooldown(cooldown + 2.5f); // note: temporary
			SetColour(hitEnemy ? Color.green : Color.crimson, 0.5f);
			OnActivated?.Invoke(hitEnemy, triggerKey);
			return;
		}

		if (overrideGlobalCooldown)
		{
			StartLocalCooldown(cooldownOverride > -1f ? cooldownOverride : cooldown);
			SetColour(hitEnemy ? Color.green : Color.crimson, 0.5f);
			OnActivated?.Invoke(hitEnemy, triggerKey);
		}
		else
		{
			KeyManager.Instance.StartGlobalCooldown();
			SetColour(hitEnemy ? Color.green : Color.crimson, 0.5f);
			OnActivated?.Invoke(hitEnemy, triggerKey);
		}
	}

	bool DealDamage()
	{
		foreach (Enemy enemy in overlappingEnemies)
		{
			if (enemy != null)
			{
				enemy.TakeDamage(damage);
				return true;
			}
		}

		return false;
	}

	IEnumerator MashTimer()
	{
		// if 3 seconds pass without a mash, reset the mash count
		yield return new WaitForSeconds(3f);
		mashCount = 0;
		MashText.text = mashCount.ToString();
	}

	public void StartLocalCooldown(float cooldown)
	{
		// dont start a new cooldown if the current cooldown is longer than the new one
		if (RemainingCooldown > cooldown) return;

		RemainingCooldown = cooldown;
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
		SpriteRenderer.color = RemainingCooldown > 0f ? Color.grey : Color.white;
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
	public static KeyCode ToKeyCode(this Key key) => !key ? KeyCode.None : key.KeyCode;

	// to Key from single keycode
	public static Key ToKey(this KeyCode keycode) => KeyManager.Instance.GetKey(keycode);

	// convert list of keys to keycodes
	public static List<KeyCode> ToKeyCodes(this List<Key> keys) => keys.Select(k => k.KeyCode).ToList();

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
