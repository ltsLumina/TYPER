#region
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using MelenitasDev.SoundsGood;
using UnityEngine;
#endregion

public class MenuManager : MonoBehaviour
{
	RectTransform buttonGroup;

	KeyManager keyManager;
	ComboManager comboManager;
	Canvas canvas;

	public static MenuManager Instance { get; private set; }

	void Awake()
	{
		if (Instance != null && Instance != this) Destroy(this);
		else Instance = this;

		canvas = GetComponent<Canvas>();
		buttonGroup = (RectTransform) transform.GetChild(0);
	}

	public bool IntroSequenceCompleted { get; private set; }

	void Start()
	{
		keyManager = KeyManager.Instance;
		comboManager = ComboManager.Instance;

		// Disable the canvas at start. Its shown at the end of the intro sequence
		buttonGroup.gameObject.SetActive(false);

		// Ensure the object is active for the intro sequence so the coroutine can run
		gameObject.SetActive(true);

		// Start intro animation sequence after all setup
		StartCoroutine(IntroSequence());

		return;

		IEnumerator IntroSequence()
		{
			// disable all keys at start
			foreach (Key key in keyManager.FlatKeys) key.Disable(false);

			GameManager.Instance.EnterTransition.gameObject.SetActive(true);

			// Wait to ensure all keys are initialized
			yield return new WaitForSeconds(1f);

			var wooshSFX = new Sound(SFX.introWoosh);
			wooshSFX.SetOutput(Output.SFX);
			wooshSFX.SetVolume(0.5f);
			wooshSFX.Play();

			yield return new WaitForSeconds(0.1f);

			var introTween = keyManager.Keyboard.transform.DOMove(new (3.5f, -2f), 1.5f).SetEase(Ease.OutCubic);

			yield return introTween.WaitForCompletion();
			yield return new WaitForSeconds(1f);

			#region Swap positions of title keys with random keys
			// Center the title "TYPER" in the middle of the middle row
			string title = GameManager.Instance.GameName;
			comboManager.CreateCombo(title.ToKeyCodes());

			// Doesn't use the 'interactable' parameter since we want to animate the markers separately
			HighlightKeys(title, true, false);
			#endregion

			yield return new WaitForSeconds(0.75f);

			#region Wait for Player to Start
			// Animate combo markers on title keys
			foreach (Key titleKey in title.ToKeyCodes().ToKeys())
			{
				titleKey.Enable();
				titleKey.SetEffect(Key.Effects.Combo);

				var sfx = new Sound(SFX.beep);
				sfx.SetOutput(Output.SFX);
				sfx.SetRandomPitch(new (0.95f, 1.05f));
				sfx.SetVolume(0.5f);
				sfx.Play();

				yield return new WaitForSeconds(0.1f);
			}

			yield return new WaitUntil(() => comboManager.CompletedCombos.Count > 0);
			comboManager.CompletedCombos.Dequeue();
			#endregion

			#region Return to Original Positions
			// Removes combo for title keys as well
			HighlightKeys(title, false, false);

			yield return new WaitForSeconds(1f);

			foreach (Key key in keyManager.FlatKeys) key.RemoveEffect(Key.Effects.Combo);
			#endregion

			keyManager.Keyboard.transform.DOMove(new (0.85f, -5f), 1.5f)
			          .SetEase(Ease.InOutCubic)
			          .OnComplete
			           (() =>
			           {
				           // Enable the menu (canvas)
				           canvas.enabled = true;
				           buttonGroup.gameObject.SetActive(true);

				           buttonGroup.anchoredPosition = new (500, 0);
				           buttonGroup.DOAnchorPosX(0, 1f).SetEase(Ease.OutCubic);

				           menuKeyPositions = keyManager.FlatKeys.ToDictionary(k => k, k => k.transform.position);
			           });

			yield return new WaitForSeconds(1f);

			IntroSequenceCompleted = true;
		}
	}

	public void Highlight(string str) => HighlightKeys(str, true, true);

	public void EndHighlight(string str) => HighlightKeys(str, false, false);

	Dictionary<Key, Vector3> menuKeyPositions = new ();
	Vector3 menuKeyboardParentPosition; // stores menu position of parent

	/// <summary>
	///     Moves the specified word to the center of the middle row, swapping with existing keys.
	/// </summary>
	/// <param name="word">The word to move (e.g., "PLAY", "MENU", "EXIT").</param>
	void MoveWordToCenterRow(string word)
	{
		const int middleRow = 1;
		List<Key> middleRowKeys = keyManager.Keys[middleRow];
		int startIdx = (middleRowKeys.Count - word.Length) / 2;
		List<KeyCode> wordKeyCodes = word.Select(c => (KeyCode) Enum.Parse(typeof(KeyCode), c.ToString().ToUpper())).ToList();

		// The keys corresponding to the letters in the word
		List<Key> wordKeys = wordKeyCodes.Select(tc => keyManager.FlatKeys.FirstOrDefault(k => k.KeyCode == tc)).Where(k => k != null).ToList();

		if (wordKeys.Count != word.Length)
		{
			Debug.LogWarning($"Not all letters in the word '{word}' are present on the keyboard.");
			return;
		}

		// save original positions of all keys
		menuKeyPositions = keyManager.FlatKeys.ToDictionary(k => k, k => k.transform.position);

		for (int i = 0; i < wordKeys.Count; i++)
		{
			Key targetKey = middleRowKeys[startIdx + i];
			Key wordKey = wordKeys[i];

			if (wordKey == targetKey) continue; // skip if the key is already in the correct position

			Vector3 targetPosition = targetKey.transform.position;
			Vector3 wordPosition = wordKey.transform.position;

			// Swap positions
			wordKey.transform.DOMove(targetPosition, 0.5f).SetEase(Ease.InOutCubic);
			targetKey.transform.DOMove(wordPosition, 0.5f).SetEase(Ease.InOutCubic);
		}
	}

	public void HighlightKeys(string word, bool enable, bool interactable)
	{
		foreach (Key key in keyManager.FlatKeys)
		{
			if (!key) continue;
			if (menuKeyPositions.TryGetValue(key, out Vector3 position)) key.transform.position = position;
		}

		List<Key> keysToHighlight = word.Select(c => (KeyCode) Enum.Parse(typeof(KeyCode), c.ToString().ToUpper())).Select(tc => keyManager.FlatKeys.FirstOrDefault(k => k.KeyCode == tc)).Where(k => k != null).ToList();

		// List of keys to disable (all keys except the ones to highlight)
		List<Key> keysToDisable = keyManager.FlatKeys.Except(keysToHighlight).ToList();

		foreach (Key key in keysToDisable)
		{
			if (enable) key.Disable();
			else key.Enable();
		}

		if (enable)
		{
			MoveWordToCenterRow(word);

			if (interactable)
			{
				comboManager.CreateCombo(keysToHighlight);

				// combo markers on highlighted keys
				foreach (Key highlightKey in keysToHighlight) highlightKey.SetEffect(Key.Effects.Combo);
			}
		}
		else
		{
			ResetKeyPositions();

			if (!interactable)
			{
				comboManager.RemoveCombo(keysToHighlight);

				foreach (Key highlightKey in keysToHighlight) highlightKey.RemoveEffect(Key.Effects.Combo);
			}
		}
	}

	public void ResetKeyPositions()
	{
		foreach (Key key in keyManager.FlatKeys)
		{
			if (menuKeyPositions.TryGetValue(key, out Vector3 position)) key.transform.DOMove(position, 1f);
			else Debug.LogWarning($"No stored position for key {key.KeyCode}");
		}
	}
}
