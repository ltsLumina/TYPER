#region
using System.Collections;
using System.Collections.Generic;
using Lumina.Essentials.Modules;
using MelenitasDev.SoundsGood;
using TransitionsPlus;
using UnityEngine;
#endregion

public class GameManager : MonoBehaviour
{
	[SerializeField] string gameName = "TYPER";
	[SerializeField] int health = 10;
	[SerializeField] int score;

	[Header("Transitions")]
	[SerializeField] TransitionAnimator enterTransition;
	[SerializeField] TransitionAnimator exitTransition;

	public static GameManager Instance { get; private set; }

	public bool TyperEntered { get; private set; }

	public string GameName => gameName;

	public int Health
	{
		get => health;
		private set => health = Mathf.Max(0, value);
	}

	public int Score
	{
		get => score;
		private set => score = Mathf.Max(0, value);
	}

	public TransitionAnimator EnterTransition => enterTransition;
	public TransitionAnimator ExitTransition => exitTransition;

	void Awake()
	{
		if (Instance != null && Instance != this) Destroy(this);
		else Instance = this;
	}

	void Start()
	{
		//DOTween.SetTweensCapacity(1250, 50);

		// ensure gameName is within 9 characters and has no duplicate letters
		if (gameName.Length > 9 || new HashSet<char>(gameName).Count != gameName.Length)
		{
			Debug.LogError("Game name exceeds 9 characters. Truncating to first 9 characters.");
			gameName = gameName[..9];
			Debug.Break();
		}

		var music = new Music(Track.musicSFX);
		music.SetOutput(Output.Music);
		music.SetVolume(0.1f);
		music.SetLoop(true);
		music.Play();
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			// for testing
		}
	}

	public void TakeDamage(int damage)
	{
		Health -= damage;

		if (Health <= 0)
		{
			Debug.LogWarning("Game Over!");
			Debug.Break();

			// draw a cross with debug lines in red
			Debug.DrawLine(new (-5, -5, 0), new (5, 5, 0), Color.red, 10f);
			Debug.DrawLine(new (-5, 5, 0), new (5, -5, 0), Color.red, 10f);
		}
		else { Debug.Log($"Player took {damage} damage. Remaining health: {Health}"); }
	}

	Coroutine hitStopCoroutine;

	public void TriggerHitStop(float duration = 0.1f, float slowdownFactor = 0.05f)
	{
		// if already active, restart hit stop
		if (hitStopCoroutine != null)
		{
			StopCoroutine(hitStopCoroutine);
			hitStopCoroutine = null;
		}

		hitStopCoroutine = StartCoroutine(HitStop(duration, slowdownFactor));
	}

	IEnumerator HitStop(float duration, float slowdownFactor = 0f)
	{
		// Clamp the values to avoid extreme cases
		duration = Mathf.Max(0.01f, duration);
		slowdownFactor = Mathf.Clamp01(slowdownFactor);

		Time.timeScale = slowdownFactor;
		Time.fixedDeltaTime = 0.02f * Time.timeScale;

		yield return new WaitForSecondsRealtime(duration);

		Time.timeScale = 1f;
		Time.fixedDeltaTime = 0.02f;

		hitStopCoroutine = null;
	}

	public void AddScore(int points)
	{
		var frenzyManager = FrenzyManager.Instance;

		int pointsWithMult = Mathf.CeilToInt(points * frenzyManager.FrenzyMultiplier);
		int scoreToAdd = frenzyManager.Frenzied ? pointsWithMult : points;

		Score += frenzyManager.Frenzied ? pointsWithMult : points;

		string withMult = frenzyManager.Frenzied ? $"({frenzyManager.FrenzyMultiplier}x frenzy multiplier)" : string.Empty;
		Debug.Log($"Score increased by {scoreToAdd}! {withMult}");
		
		//TODO: very temporary way of doing this
		var scoreText = GameObject.FindWithTag("Canvas").transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();
	string text = $"{Score} pts"
			+ (frenzyManager.Frenzied ? $" ({frenzyManager.FrenzyMultiplier}x)" : string.Empty)
			+ (Time.timeScale > 1.1f ? $" ({Time.timeScale:F1}x speed)" : string.Empty);
		scoreText.text = text;
	}
}
