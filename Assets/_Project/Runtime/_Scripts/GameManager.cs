#region
using System.Collections;
using MelenitasDev.SoundsGood;
using TransitionsPlus;
using UnityEngine;
#endregion

public class GameManager : MonoBehaviour
{
	public enum State
	{
		Menu,
		Playing,
		Paused,
		Shop,
		GameOver
	}
	
	[SerializeField] int health = 10;

	[Header("Transitions")]
	[SerializeField] TransitionAnimator enterTransition;
	[SerializeField] TransitionAnimator exitTransition;

	Music music;
	
	public static GameManager Instance { get; private set; }

	public static string TYPER => "TYPER";

	public int Health
	{
		get => health;
		private set => health = Mathf.Clamp(health, 0, value);
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
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;
		
		music = new (Track.musicSFX);
		music.SetOutput(Output.Music);
		music.SetVolume(0.65f);
		music.SetLoop(true);
		music.Play();
	}
	
	public void Heal(int amount)
	{
		Health += amount;
		Debug.LogWarning($"Player healed {amount} health.\n" +
		                 $"Current health: {Health}");
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
		else { Debug.LogWarning($"Player took {damage} damage.\n" +
		                        $"Remaining health: {Health}"); }
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
