using System;
using System.Collections.Generic;
using System.Linq;
using Lumina.Essentials.Attributes;
using UnityEngine;

public class ComboController : MonoBehaviour
{
	public static ComboController Instance { get; private set; }
	
	void Awake()
	{
		if (Instance != null && Instance != this) Destroy(this);
		else Instance = this;
	}

	[Header("Combos")]
	[SerializeField] List<Dictionary<Key, (int, bool)>> combos = new ();

	[Header("Combo Settings")]
	[Tooltip("The length of the combo. 1-based.")]
	[SerializeField, ReadOnly] int comboLength = 3;
	// ReSharper disable once NotAccessedField.Local
	[SerializeField, ReadOnly] Key currentComboKey;
	// ReSharper disable once NotAccessedField.Local
	[SerializeField, ReadOnly] int recentComboIndex;
	[SerializeField, ReadOnly] Key nextComboKey;
	[SerializeField, ReadOnly] int nextComboIndex;
	[SerializeField] bool loops;
	[Space(10)]
	[SerializeField, ReadOnly] List<Key> currentComboKeys = new ();
	
	public int NextComboIndex => nextComboIndex;
	public int ComboLength => comboLength;
	public List<Key> CurrentComboKeys => currentComboKeys;
	public bool Loops
	{
		get => loops;
		set => loops = value;
	}
	
	/// <summary>
	/// Creates a new combo from the given list of keys.
	/// </summary>
	/// <param name="keys"></param>
	/// <param name="loops"> Whether the combo should loop back to the start after completion.</param>
	public void CreateCombo(List<Key> keys, bool loops = false)
	{
		foreach (var key in keys)
		{
			key.Combo = true;
			key.ComboIndex = keys.IndexOf(key);
		}
		
		combos.Add(keys.ToDictionary(k => k, k => (k.ComboIndex, loops)));
		currentComboKey = null;
		nextComboKey = null;
		recentComboIndex = -1;
		nextComboIndex = -1;
		
		// log the combo
		string comboString = string.Join(" -> ", keys.Select(k => k.KeyboardLetter));
		//Debug.Log($"Created new combo: {comboString} (Loops: {loops})");
	}

	public void CreateCombo(List<KeyCode> keys, bool loops = false)
	{
		List<Key> comboKeys = keys.Select(keyCode => KeyController.Instance.GetKey(keyCode)).Where(key => key != null).ToList();
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
				key.ComboIndicator.gameObject.SetActive(false);
			}
			Debug.Log($"Removed combo: {string.Join(" -> ", keys.Select(k => k.KeyboardLetter))}");
		}
	}

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
			nextComboIndex = 1; // Set to 1 since we've just matched the first key and now expect the second key
			currentComboKey = currentComboKeys[nextComboIndex];
			nextComboKey = currentComboKey;
			recentComboIndex = -1;

			// Show the indicator for the next key in the combo
			ComboIndicator(currentComboKeys[nextComboIndex]);
		}
	}

	static void ComboIndicator(Key nextKey)
	{
		nextKey.ComboIndicator.gameObject.SetActive(true);
		var anim = nextKey.ComboIndicator.GetComponent<Animation>();
		anim.Play();
	}
	
	public void AdvanceCombo(KeyCode key)
	{
		// Only advance if the key matches the expected key in the current combo
		if (currentComboKeys[nextComboIndex].KeyboardLetter != key)
		{
			Debug.LogWarning($"Key {key} does not match expected combo key {currentComboKeys[nextComboIndex].KeyboardLetter}");
			return;
		}
		
		// Set to the previous combo index before advancing
		recentComboIndex = nextComboIndex;
		// Increment to the next index in the combo
		nextComboIndex++;

		// If we've reached the end of the combo, loop back to the start
		if (nextComboIndex > comboLength - 1)
		{
			nextComboIndex = loops ? 0 : -1;
			ComboCompleted();
			return;
		}

		// Update current and next combo keys
		nextComboKey = currentComboKeys[nextComboIndex];
		currentComboKey = nextComboKey;

		// Only show the indicator if we're not at the start of the combo
		if (nextComboIndex > 0)
		{
			var nextKey = currentComboKeys[nextComboIndex];
			nextKey.ComboIndicator.gameObject.SetActive(true);
			var anim = nextKey.ComboIndicator.GetComponent<Animation>();
			anim.Play();
		}
	}
	
	void ComboCompleted()
	{
		Debug.Log("Combo completed!");
		ResetCombo();
	}
	
	public void ResetCombo()
	{
		// Hide all combo indicators before resetting
		currentComboKeys.ForEach(k => k.ComboIndicator.SetActive(false));
		currentComboKeys.Clear();
		
		// Reset combo state. If loops is enabled, start from the beginning again, otherwise clear the combo.
		nextComboIndex = loops ? 0 : -1;
		currentComboKey = null;
		nextComboKey = null;
	}
}
