#region
using System;
using System.Collections;
using JetBrains.Annotations;
using MelenitasDev.SoundsGood;
using TransitionsPlus;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endregion

public partial class GameManager : SingletonPersistent<GameManager>
{
	public enum State
	{
		Menu,
		Playing,
		Paused,
		Shop,
		GameOver
	}

	[UsedImplicitly]
	[SerializeField] State currentState;
	[SerializeField] int health = 10;

	[Header("Transitions")]
	[SerializeField] TransitionAnimator enterTransition;
	[SerializeField] TransitionAnimator exitTransition;
	
	void Start() // Only runs once when the game starts because it's a SingletonPersistent
	{
		Music = new (Track.musicSFX);
		Music.SetOutput(Output.Music);
		Music.SetVolume(0.5f);
		Music.SetLoop(true);
		Music.Play();
		
		currentState = State.Menu;
	}

	void Update()
	{
		#region assignment
		var volume = FindAnyObjectByType<Volume>();

		// Note: ONLY FOR ASSIGNMENT HAND-IN
		if (Input.GetKeyDown(KeyCode.Alpha0))
		{
			if (volume.profile.TryGet(out ColorAdjustments colorAdjustments)) { colorAdjustments.saturation.value = -100; }
		}
		else if (Input.GetKeyDown(KeyCode.Alpha9))
		{
			if (volume.profile.TryGet(out ColorAdjustments colorAdjustments)) { colorAdjustments.saturation.value = 0; }
		}
		#endregion
	}

	public void StartGame() => StartCoroutine(StartGameRoutine());
	IEnumerator StartGameRoutine()
	{
		ExitTransition.gameObject.SetActive(true);

		yield return new WaitForSeconds(1f);

		Music.Play();
	}

	public void SetState(State newState)
	{
		currentState = newState;
		
		switch (newState)
		{
			case State.Menu:
				break;

			case State.Playing:
				break;

			case State.Paused:
				break;

			case State.Shop:
				break;

			case State.GameOver:
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
		}
	}

	public void Heal(int amount)
	{
		Health += amount;
		Debug.LogWarning($"Player healed {amount} health.\n" + $"Current health: {Health}");
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
		else { Debug.LogWarning($"Player took {damage} damage.\n" + $"Remaining health: {Health}"); }
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

	IEnumerator HitStop(float duration, float slowdownFactor)
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
}

public partial class GameManager // Properties
{
	public static string TYPER => "TYPER";
	public Music Music { get; private set; }
	public int Health
	{
		get => health;
		private set => health = Mathf.Clamp(health, 0, value);
	}
	public TransitionAnimator EnterTransition => enterTransition;
	public TransitionAnimator ExitTransition => exitTransition;
}
