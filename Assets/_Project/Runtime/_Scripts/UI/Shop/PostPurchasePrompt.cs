using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class PostPurchasePrompt : MonoBehaviour
{
	[SerializeField] CanvasGroup screenOverlay;
	[SerializeField] TMP_InputField inputField;
	[SerializeField] TMP_Text purchasedItemText;
	[SerializeField] TMP_Text minimumCharsText;
	[SerializeField] TMP_Text infoWarningText;

	void Awake()
	{
		screenOverlay.gameObject.SetActive(false);
		gameObject.SetActive(true);
		
		transform.localPosition = new Vector3(0, 1000, 0); // Start off-screen
	}

	int minChars;

	public void Show(string itemName, int minChars)
	{
		gameObject.SetActive(true);
		EventSystem.current.SetSelectedGameObject(inputField.gameObject.gameObject);
		
		this.minChars = minChars;

		ActivationSequence = string.Empty;
		inputField.text = string.Empty;
		infoWarningText.text = string.Empty;
		
		screenOverlay.gameObject.SetActive(true);
		screenOverlay.alpha = 0;
		screenOverlay.DOFade(1, 0.5f).SetEase(Ease.OutCirc).SetLink(gameObject);
		
		purchasedItemText.text = $"Purchased {itemName}!";
		minimumCharsText.text = $"{itemName} requires a minimum of {minChars} keys.";
		infoWarningText.gameObject.SetActive(false);
		
		transform.localPosition = new Vector3(0, 1000, 0); // Move off-screen
		transform.DOLocalMoveY(0, 0.5f).SetEase(Ease.OutCirc).SetLink(gameObject);
	}
	
	public event Action<List<Key>> OnHide;

	public void TryHide()
	{
		if (inputState.hasErrors) return; // warnings are allowed
		
		screenOverlay.DOFade(0, 0.5f).SetEase(Ease.OutCirc).SetLink(gameObject).OnComplete(() => screenOverlay.gameObject.SetActive(false));
		
		transform.DOLocalMoveY(1000, 0.5f).SetEase(Ease.InCirc).SetLink(gameObject);
		OnHide?.Invoke(ActivationSequence.ToKeys());
	}

	string ActivationSequence { get; set; }
	
	(bool hasErrors, bool hasWarnings) inputState;

	public void ValidateInputString(string input)
	{
		// Set error color (pinkish red)
		infoWarningText.color = new (0.98f, 0.39f, 0.35f);

		#region ERRORS
		bool isEmpty = string.IsNullOrWhiteSpace(input);
		bool isNotUnique = input.Length != new HashSet<char>(input).Count;
		bool isTooShort = input.Length < minChars;
		#endregion

		#region WARNINGS
		bool isLongerThanMin = input.Length > minChars;
		#endregion

		string message = string.Empty;

		if (isEmpty) message = "Input cannot be empty.";
		else if (isNotUnique) message = "Input must not contain duplicate characters.";
		else if (isTooShort) message = $"Input must be at least {minChars} characters long.";
		else if (isLongerThanMin)
		{
			message = "Input is longer than the minimum required length.";
			infoWarningText.color = Color.yellow;
		}

		infoWarningText.text = message;

		inputState.hasErrors = isEmpty || isNotUnique || isTooShort;
		inputState.hasWarnings = isLongerThanMin;

		infoWarningText.gameObject.SetActive(inputState.hasErrors || inputState.hasWarnings);
		ActivationSequence = input;
	}

	public void Print(string input) => Debug.Log($"Input: {input}");
}
