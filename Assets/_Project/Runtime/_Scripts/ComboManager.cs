#region
using System;
using System.Collections.Generic;
using System.Linq;
using Lumina.Essentials.Attributes;
using MelenitasDev.SoundsGood;
using UnityEngine;
#endregion

public class ComboManager : MonoBehaviour
{
	public static ComboManager Instance { get; private set; }

	void Awake()
	{
		if (Instance != null && Instance != this) Destroy(this);
		else Instance = this;
	}

	[Header("Combos")]
	[SerializeField] List<Dictionary<Key, (int, bool)>> combos = new ();

	[Header("Combo Settings")]
	[Tooltip("The length of the combo. 1-based.")]
	[SerializeField, ReadOnly] int comboLength = -1;

	// ReSharper disable once NotAccessedField.Local
	[SerializeField, ReadOnly] Key recentComboKey;

	// ReSharper disable once NotAccessedField.Local
	[SerializeField, ReadOnly] int recentComboIndex;
	[SerializeField, ReadOnly] Key nextComboKey;
	[SerializeField, ReadOnly] int nextComboIndex;
	[SerializeField] bool loops; // TODO: implement looping combos
	[Space(10)]
	[SerializeField, ReadOnly] List<Key> currentComboKeys = new ();

	public List<Dictionary<Key, (int, bool)>> Combos => combos;

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

	/// <summary>
	/// Creates a new combo from the given list of keys.
	/// </summary>
	/// <param name="keys"></param>
	/// <param name="loops"> Whether the combo should loop back to the start after completion.</param>
	public void CreateCombo(List<Key> keys, bool loops = false)
	{
		foreach (Key key in keys)
		{
			key.Combo = true;
			key.ComboIndex = keys.IndexOf(key);
		}

		combos.Add(keys.ToDictionary(k => k, k => (k.ComboIndex, loops)));
		recentComboKey = null;
		nextComboKey = null;
		recentComboIndex = -1;
		nextComboIndex = -1;

		//string comboString = string.Join(" -> ", keys.Select(k => k.KeyboardLetter));
		//Debug.Log($"Created new combo: {comboString} (Loops: {loops})");
	}

	public void CreateCombo(List<KeyCode> keycodes, bool loops = false)
	{
		// min length of 3 keys
		if (keycodes.Count < 3)
		{
			Debug.LogError("Combo must be at least 3 keys long.");
			return;
		}

		List<Key> comboKeys = keycodes.ToKeys();

		// if the combo already exists, do not create the combo
		if (combos.Any(c => c.Keys.SequenceEqual(comboKeys)))
		{
			Debug.LogError($"Combo already exists: {string.Join(" -> ", comboKeys.Select(k => k.KeyboardLetter))}");
			return;
		}

		// if any key is already in a combo, do not create the combo
		if (comboKeys.Any(k => k.Combo))
		{
			Debug.LogError($"Cannot create combo. One or more keys are already in a combo: {string.Join(" -> ", comboKeys.Where(k => k.Combo).Select(k => k.KeyboardLetter))}");
			return;
		}

		CreateCombo(comboKeys, loops);
	}

	public void RemoveCombo(List<Key> keys)
	{
		var comboToRemove = combos.FirstOrDefault(c => c.Keys.SequenceEqual(keys));

		if (comboToRemove != null)
		{
			combos.Remove(comboToRemove);

			foreach (var key in keys)
			{
				key.Combo = false;
				key.ComboIndex = -1;
				key.ComboHighlight.gameObject.SetActive(false);
			}

			Debug.Log($"Removed combo: {string.Join(" -> ", keys.Select(k => k.KeyboardLetter))}");
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
			var matchingCombo = combos.FirstOrDefault(c => c.Keys.First().KeyboardLetter == key);

			if (matchingCombo == null)
			{
				Debug.LogWarning($"No combo starts with key {key}");
				return;
			}

			// Initialize combo state. E.g. if the combo is A, S, D and the player pressed A, set up to expect S next.
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
		if (currentComboKeys[nextComboIndex].KeyboardLetter != keycode)
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
		string comboString = string.Join(" -> ", currentComboKeys.Select(k => k.KeyboardLetter));
		Debug.Log($"Combo completed: {comboString} (Loops: {loops})");

		var sfx = new Sound(SFX.powerupSFX);
		sfx.SetOutput(Output.SFX);
		sfx.SetRandomPitch(new (0.95f, 1.05f));
		sfx.SetVolume(0.15f);
		sfx.Play();
		
		CompletedCombos.Enqueue(currentComboKeys.ToList());
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
		nextComboIndex = loops ? 0 : -1;
		recentComboKey = nextComboKey;
		nextComboKey = null;

		OnComboReset?.Invoke(recentComboKey);
	}
}
