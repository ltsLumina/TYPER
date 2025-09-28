#region
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Lumina.Essentials.Attributes;
using MelenitasDev.SoundsGood;
using UnityEngine;
using VInspector;
using Random = UnityEngine.Random;
#endregion

public class ComboManager : Singleton<ComboManager>
{
	[Tab("Combo")]
	[SerializeField, ReadOnly] string currentCombo;
	[Space(10)]
	[SerializeField, ReadOnly] List<Key> currentComboKeys = new ();
	[Space(5)]
	[Header("Debug")]
	[Tooltip("The length of the combo. 1-based.")]
	[SerializeField, ReadOnly] int comboLength = -1;
	[SerializeField, ReadOnly] Key recentComboKey;
	[SerializeField, ReadOnly] int recentComboIndex;
	[SerializeField, ReadOnly] Key nextComboKey;
	[SerializeField, ReadOnly] int nextComboIndex;
	[SerializeField, ReadOnly] bool loops; // TODO: implement looping combos

	[Tab("Debug")]
	[SerializeField] bool randomizeComboEffects = true;
	[ShowIf(nameof(randomizeComboEffects), true)]
	[SerializeField] List<ComboEffect> excludedComboEffects = new ();
	[EndIf]
	[ShowIf(nameof(randomizeComboEffects), false)]
	[SerializeField] SerializedDictionary<string, string> presetComboEffects = new ();
	[EndIf]
	
	[Tab("Statistics")]
	[SerializeField, ReadOnly] List<string> completedComboStrings = new ();
	[Space(5), UsedImplicitly]
	[SerializeField, ReadOnly] int totalCombosCompleted;
	[SerializeField, UsedImplicitly] SerializedDictionary<string, int> comboFrequency = new ();
	
	readonly List<Dictionary<Key, (int, bool)>> combos = new ();
	public bool DoesComboExist(List<Key> keys) => combos.Any(c => c.Keys.SequenceEqual(keys));
	public bool IsKeyPartOfCombo(Key key) => combos.Any(c => c.ContainsKey(key));

	public bool InProgress => nextComboIndex != -1;
	public int NextComboIndex => nextComboIndex;
	public int ComboLength => comboLength;
	public List<Key> CurrentComboKeys => currentComboKeys;
	public Queue<List<Key>> CompletedCombos { get; } = new ();

	public bool Loops
	{
		get => loops;
		set => loops = value;
	}

	public Key RecentKey => recentComboKey;
	public Key NextKey => nextComboKey;

	#region Debug / Combo Effect Assignment
	IEnumerator Start()
	{
		yield return new WaitUntil(() => KeyManager.Instance.IsInitialized);
		
		#region Modifiers
		if (SceneManagerExtended.ActiveSceneName != "Game") yield break;

		//List<Key> qweCombo = "QWE".ToKeys();
		//CreateCombo(qweCombo);

		// List<Key> asdfCombo = "ASDF".ToKeys();
		// CreateCombo(asdfCombo);
		//
		// List<Key> rtyCombo = "RTY".ToKeys();
		// CreateCombo(rtyCombo);
		//
		// List<Key> cvbCombo = "CVB".ToKeys();
		// CreateCombo(cvbCombo);

		// List<Key> oGCD_Keys = "PLM".ToKeys();
		// oGCD_Keys.SetModifier(Key.Modifiers.OffGlobalCooldown);
		//
		// // set G key to be a mash key
		// var mashKey = KeyCode.G.ToKey();
		// mashKey.SetModifier(Key.Modifiers.Mash);
		//
		// // make H shake
		// var shakeKey = KeyCode.H.ToKey();
		// shakeKey.SetModifier(Key.Modifiers.Loose);
		//
		// // chain J key
		// var chainKey = KeyCode.J.ToKey();
		// chainKey.SetModifier(Key.Modifiers.Chained);
		//
		// // thorn K key
		// var thornKey = KeyCode.K.ToKey();
		// thornKey.SetModifier(Key.Modifiers.Thorned);
		#endregion

		if (randomizeComboEffects) RandomizeComboEffects(KeyManager.Instance.FlatKeys, excludedComboEffects.Select(e => e.GetType()).Distinct().ToArray());
		else SelectPresetComboEffects();
	}
	
	void RandomizeComboEffects(List<Key> keys, params Type[] exclude)
	{
		// remove excluded effects from the list
		ComboEffect[] effects = Resources.LoadAll<ComboEffect>(ResourcePaths.COMBOS);
		effects = effects.Where(e => !exclude.Contains(e.GetType())).ToArray();

		foreach (var key in keys)
		{
			if (!key.LastKeyInCombo) continue; // Only the last key in a combo gets a special effect. Prevents issues like the RTY-incident.

			// randomly select a combo effect, and call GetEffect
			key.ComboEffect = effects.Length > 0 ? Effect.GetEffect<ComboEffect>(effects[Random.Range(0, effects.Length)].GetType()) : key.ComboEffect = Effect.GetEffect<CE_Railgun>();
		}
	}

	void SelectPresetComboEffects()
	{
		if (presetComboEffects.Count == 0) return;
		
		foreach (var kvp in presetComboEffects)
		{
			// split the value by comma to separate effect from desired level (if any)
			string[] parts = kvp.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			// part 0 is the effect name, part 1 is the level (if any)
			string effectName = parts[0].Trim();
			string level = parts.Length > 1 ? parts[1].Trim() : null;

			// add the prefix if it's not already there
			string prefixed = effectName.StartsWith("CE_") ? effectName : "CE_" + effectName;

			Key key = KeyManager.Instance.GetKey((KeyCode) Enum.Parse(typeof(KeyCode), kvp.Key));

			if (!key.LastKeyInCombo)
			{
				Logger.LogWarning($"Key '{kvp.Key}' is not marked as the last key in a combo. Combo effects only apply to the last key in a combo.");
				continue;
			}

			var effectType = Type.GetType(prefixed);

			if (key != null && effectType != null && effectType.IsSubclassOf(typeof(Effect)))
			{
				key.ComboEffect = Effect.GetEffect<ComboEffect>(effectType);
				key.ComboEffect.SetLevel(Enum.TryParse(level, out Level lvl) ? lvl : Level.I, true);

				//Logger.Log($"Assigned preset combo effect '{effectName}' to key '{kvp.Key}' at level {key.ComboEffect.Level}.", this, "KeyManager");
			}
			else if (key == null) Logger.LogWarning($"Key '{kvp.Key}' not found. Check if the key name is valid.", this, "KeyManager");
			else Logger.LogWarning($"Failed to assign preset combo effect '{prefixed}' to key '{kvp.Key}'. Check if the key and effect type are valid.", this, "KeyManager");
		}
	}
	#endregion

	/// <summary>
	/// Creates a new combo from the given list of keys.
	/// </summary>
	/// <param name="comboKeys"></param>
	/// <param name="loops"> Whether the combo should loop back to the start after completion.</param>
	public void CreateCombo(List<Key> comboKeys, bool loops = false)
	{
		// min length of 3 keys
		if (comboKeys.Count < 3)
		{
			Logger.LogError("Combos must be at least 3 keys long." + "\n" + $"Provided combo length: {comboKeys.Count} ({string.Join(" -> ", comboKeys.Select(k => k.KeyCode))})");
			return;
		}

		// if the combo already exists, do not create the combo
		if (DoesComboExist(comboKeys))
		{
			Debug.LogWarning($"Combo already exists: {string.Join(" -> ", comboKeys.Select(k => k.KeyCode))}" 
			                 + "\n" + "The existing combo will be upgraded instead, through a different class/method.");
			return;
		}

		// if any key is already in a combo, do not create the combo
		if (comboKeys.Any(k => k.IsCombo))
		{
			Debug.LogError($"Cannot create combo. One or more keys are already in a combo: {string.Join(" -> ", comboKeys.Where(k => k.IsCombo).Select(k => k.KeyCode))}");
			return;
		}

		Key lastKey = comboKeys.Last();
		lastKey.LastKeyInCombo = true;

		foreach (Key key in comboKeys)
		{
			key.SetModifier(Key.Modifiers.Combo);
			key.ComboIndex = comboKeys.IndexOf(key);
		}

		combos.Add(comboKeys.ToDictionary(k => k, k => (k.ComboIndex, loops)));
		recentComboKey = null;
		nextComboKey = null;
		recentComboIndex = -1;
		nextComboIndex = -1;

		//string comboString = string.Join(" -> ", keys.Select(k => k.KeyboardLetter));
		//Debug.Log($"Created new combo: {comboString} (Loops: {loops})");
	}

	public void RemoveCombo(List<Key> keys)
	{
		var comboToRemove = combos.FirstOrDefault(c => c.Keys.SequenceEqual(keys));

		if (comboToRemove != null)
		{
			combos.Remove(comboToRemove);

			foreach (var key in keys)
			{
				key.RemoveModifier(Key.Modifiers.Combo);
				key.ComboIndex = -1;
				key.ComboHighlight.gameObject.SetActive(false);
			}

			Logger.LogWarning($"Removed combo: {string.Join(" -> ", keys.Select(k => k.KeyCode))}");
		}
	}

	public event Action<Key> OnBeginCombo;
	public event Action<(Key, Key), (int, int)> OnAdvanceCombo;
	public event Action<List<Key>> OnCompleteCombo;
	public event Action<Key> OnComboReset;

	public void BeginCombo(KeyCode key)
	{
		// initialize the combo if not already started
		if (nextComboIndex == -1)
		{
			// Find a combo that starts with the given key
			var matchingCombo = combos.FirstOrDefault(c => c.Keys.First().KeyCode == key);

			if (matchingCombo == null)
			{
				Debug.LogWarning($"No combo starts with key {key}");
				return;
			}

			// Initialize combo state. E.g. if the combo is A, S, D and the player pressed A, set up to expect S next.
			currentCombo = string.Join(" -> ", matchingCombo.Keys.Select(k => k.KeyCode));
			currentComboKeys = matchingCombo.Keys.ToList();
			comboLength = currentComboKeys.Count;
			recentComboKey = currentComboKeys[0];
			recentComboIndex = 0;
			nextComboKey = currentComboKeys[1];
			nextComboIndex = 1; // Set to 1 since we've just matched the first key and now expect the second key

			// Show the indicator for the next key in the combo
			ShowComboHighlight(currentComboKeys[nextComboIndex]);

			OnBeginCombo?.Invoke(recentComboKey);
		}
	}

	static void ShowComboHighlight(Key nextKey)
	{
		nextKey.ComboHighlight.gameObject.SetActive(true);
		var anim = nextKey.ComboHighlight.GetComponent<Animation>();
		anim.Play();
	}

	public void AdvanceCombo(KeyCode keycode)
	{
		// Only advance if the key matches the expected key in the current combo
		if (currentComboKeys[nextComboIndex].KeyCode != keycode)
		{
			//Debug.LogWarning($"Key {keycode} does not match expected combo key {currentComboKeys[nextComboIndex].KeyboardLetter}");
			return;
		}

		// Set to the previous combo index before advancing
		recentComboIndex = nextComboIndex;

		// Increment to the next index in the combo
		nextComboIndex++;

		// If we've reached the end of the combo, loop back to the start
		if (nextComboIndex > comboLength - 1)
		{
			recentComboKey = nextComboKey;
			nextComboIndex = loops ? 0 : -1;
			OnAdvanceCombo?.Invoke((recentComboKey, nextComboKey), (recentComboIndex, nextComboIndex));

			ComboCompleted();
			return;
		}

		// Update current and next combo keys
		recentComboKey = nextComboKey;
		nextComboKey = currentComboKeys[nextComboIndex];

		// Only show the indicator if we're not at the start of the combo
		if (nextComboIndex > 0)
		{
			var nextKey = currentComboKeys[nextComboIndex];
			nextKey.ComboHighlight.gameObject.SetActive(true);
			var anim = nextKey.ComboHighlight.GetComponent<Animation>();
			anim.Play();
		}

		OnAdvanceCombo?.Invoke((recentComboKey, nextComboKey), (recentComboIndex, nextComboIndex));
	}

	void ComboCompleted()
	{
		// string comboString = string.Join(" -> ", currentComboKeys.Select(k => k.KeyboardLetter));
		// Debug.Log($"Combo completed: {comboString} (Loops: {loops})");

		var sfx = new Sound(SFX.powerupSFX);
		sfx.SetOutput(Output.SFX);
		sfx.SetRandomPitch(new (0.9f, 1.05f));
		sfx.SetVolume(0.15f);
		sfx.Play();

		CompletedCombos.Enqueue(currentComboKeys.ToList());
		totalCombosCompleted++;
		if (!comboFrequency.TryAdd(currentCombo, 1))
			comboFrequency[currentCombo]++;

		if (CompletedCombos.Count > 5)
		{
			CompletedCombos.Dequeue();
			completedComboStrings.RemoveAt(completedComboStrings.Count - 1); // remove the oldest entry
		}

		// Insert at the start so the most recent combo is always at the top
		completedComboStrings.Insert(0, $"{Time.time:F2}s: {string.Join(" -> ", currentComboKeys.Select(k => k.KeyCode))}");
		OnCompleteCombo?.Invoke(currentComboKeys.ToList());

		ResetCombo();
	}

	public void ResetCombo()
	{
		//Debug.LogWarning("Combo reset!");

		// Hide all combo indicators before resetting
		currentComboKeys.ForEach(k => k.ComboHighlight.SetActive(false));
		currentComboKeys.Clear();

		// Reset combo state. If loops is enabled, start from the beginning again, otherwise clear the combo.
		//comboLength = -1;
		nextComboIndex = -1;
		recentComboKey = nextComboKey;
		nextComboKey = null;

		OnComboReset?.Invoke(recentComboKey);
	}
}
