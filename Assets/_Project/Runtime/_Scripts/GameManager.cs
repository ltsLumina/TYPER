using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using MelenitasDev.SoundsGood;
using TransitionsPlus;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Experimental.AI;

public class GameManager : MonoBehaviour
{
	[SerializeField] string gameName = "TYPER";
	[SerializeField] string play = "PLAY";
	[SerializeField] string menu = "MENU";
	[SerializeField] string exit = "EXIT";
	[SerializeField] int health = 10;
	[SerializeField] int score;

	[Header("Transitions")]
	[SerializeField] TransitionAnimator enterTransition;
	[SerializeField] TransitionAnimator exitTransition;
	
	[Header("Settings")]
	[SerializeField] bool showDamageNumbers;
	
	public static GameManager Instance { get; private set; }

	public bool TyperEntered { get; private set; }
	
	public string GameName => gameName;
	public bool ShowDamageNumbers => showDamageNumbers;

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
		DOTween.SetTweensCapacity(1250, 50);
		
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

	int currentCommandIndex;
	enum CommandType { None, TYPER, Play, Menu, Exit }
	CommandType currentCommand = CommandType.None;

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			// for testing
		}


		List<KeyCode> typerKeycodes = gameName.Select(c => (KeyCode)Enum.Parse(typeof(KeyCode), c.ToString())).ToList();
		List<KeyCode> playKeycodes = play.Select(c => (KeyCode)Enum.Parse(typeof(KeyCode), c.ToString())).ToList();
		List<KeyCode> menuKeycodes = menu.Select(c => (KeyCode)Enum.Parse(typeof(KeyCode), c.ToString())).ToList();
		List<KeyCode> exitKeycodes = exit.Select(c => (KeyCode)Enum.Parse(typeof(KeyCode), c.ToString())).ToList();

		if (Input.anyKeyDown)
		{
			// Helper to check if a key was pressed
			bool KeyPressed(KeyCode key) => Input.GetKeyDown(key);

			// If not currently entering a command, check if any command is starting
			if (currentCommand == CommandType.None)
			{
				if (KeyPressed(typerKeycodes[0]))
				{
					currentCommand = CommandType.TYPER;
					currentCommandIndex = 1;
				}
				else if (KeyPressed(playKeycodes[0]))
				{
					currentCommand = CommandType.Play;
					currentCommandIndex = 1;
				}
				else if (KeyPressed(menuKeycodes[0]))
				{
					currentCommand = CommandType.Menu;
					currentCommandIndex = 1;
				}
				else if (KeyPressed(exitKeycodes[0]))
				{
					currentCommand = CommandType.Exit;
					currentCommandIndex = 1;
				}
				else
				{
					currentCommand = CommandType.None;
					currentCommandIndex = 0;
				}
			}
			else
			{
				List<KeyCode> keycodes = currentCommand switch
				{ CommandType.TYPER  => typerKeycodes,
				  CommandType.Play   => playKeycodes,
				  CommandType.Menu => menuKeycodes,
				  _                  => exitKeycodes };

				if (currentCommandIndex < keycodes.Count && KeyPressed(keycodes[currentCommandIndex]))
				{
					currentCommandIndex++;

					if (currentCommandIndex == keycodes.Count)
					{
						switch (currentCommand)
						{
							case CommandType.TYPER:
								TyperEntered = true;
								break;
							
							case CommandType.Play:
								Debug.Log("Play command entered!");
								
								exitTransition.gameObject.SetActive(true);
								break;

							case CommandType.Menu:
								Debug.Log("Menu command entered!");
								break;

							case CommandType.Exit:
								Debug.Log("Exit command entered!");

								var sequence = new Lumina.Essentials.Sequencer.Sequence(this);
								sequence.WaitThenExecute
								(1.5f, () =>
								{
									Application.Quit();
									Debug.Break();
								});
								
								break;
						}

						currentCommand = CommandType.None;
						currentCommandIndex = 0;
					}
				}
				else
				{
					// Wrong key, reset
					currentCommand = CommandType.None;
					currentCommandIndex = 0;
				}
			}
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
		else
		{
			Debug.Log($"Player took {damage} damage. Remaining health: {Health}");
		}
	}

	Coroutine hitStopCoroutine;

	public void TriggerHitStop(float duration, float slowdownFactor = 0f)
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
		Score += points;
		Debug.Log($"Score increased by {points}. Total score: {Score}");
	}
}
