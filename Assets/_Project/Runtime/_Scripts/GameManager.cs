using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using MelenitasDev.SoundsGood;
using UnityEngine;
using UnityEngine.Experimental.AI;

public class GameManager : MonoBehaviour
{
	[SerializeField] string gameName = "TYPER";
	[SerializeField] string play = "PLAY";
	[SerializeField] string menu = "MENU";
	[SerializeField] string exit = "EXIT";
	[SerializeField] int health = 10;
	[SerializeField] int score;
	
	[Header("Settings")]
	[SerializeField] bool showDamageNumbers;
	
	public static GameManager Instance { get; private set; }

	public bool TyperEntered { get; set; } = false;
	
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
			StartCoroutine(Wave());
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
							
							case CommandType.Play: // TODO just load new scene -- so much easier than this mess
								Debug.Log("Play command entered!");
								
								var playKeys = "play".Select(c => (KeyCode) Enum.Parse(typeof(KeyCode), c.ToString().ToUpper())).Select(tc => KeyController.Instance.FlatKeys.FirstOrDefault(k => k.KeyboardLetter == tc)).Where(k => k != null).ToList();
								ComboController.Instance.RemoveCombo(playKeys);
								
								var canvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
								var canvasGroup = canvas.GetComponent<CanvasGroup>();

								Sequence playSequence = DOTween.Sequence();

								playSequence.Append
								(canvasGroup.DOFade(0, 0.5f)
								            .OnComplete
								             (() =>
								             {
									             canvas.gameObject.SetActive(false);
									             canvasGroup.alpha = 1f;
								             }));

								GameObject keyboard = GameObject.Find("Keyboard");

								playSequence.Append
								             (keyboard.transform.DOMove(new (3.5f, -2), 1.5f)
								                      .SetEase(Ease.InOutSine)
								                      .OnComplete
								                       (() =>
								                       {
									                       KeyController.Instance.ResetToGamePositions();

									                       // Init lanes
									                       for (int r = 0; r < KeyController.Instance.Keys.Count; r++)
									                       {
										                       float lane = KeyController.Instance.Keys[r][0].transform.position.y;
										                       KeyController.Instance.Lanes[r] = KeyController.Instance.Keys[r][0].transform.position.y;
										                       Debug.DrawLine(new (-10f, lane, 0f), new (10f, lane, 0f), Color.green, 300f);
									                       }
								                       }))
								            .OnComplete(() => playSequence.AppendInterval(1f))
								            .OnComplete
								             (() =>
								             {
									             var spawner = FindAnyObjectByType<EnemySpawner>(FindObjectsInactive.Include);
									             spawner.gameObject.SetActive(true);
								             });
								
								// enables all keys
								foreach (Key key in KeyController.Instance.FlatKeys) key.Enable();

								#region Modifiers
								var comboController = ComboController.Instance;
								
								var qweCombo = new List<KeyCode> { KeyCode.Q, KeyCode.W, KeyCode.E };
								comboController.CreateCombo(qweCombo);

								// asdf combo
								var asdfCombo = new List<KeyCode> { KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F };
								comboController.CreateCombo(asdfCombo, true);

								// select three random keys to be oGCD actions
								List<Key> oGCD_Keys = KeyController.Instance.FlatKeys.Where
								                               (x => x.KeyboardLetter != KeyCode.Q && x.KeyboardLetter != KeyCode.W && x.KeyboardLetter != KeyCode.E && x.KeyboardLetter != KeyCode.A && x.KeyboardLetter != KeyCode.S && x.KeyboardLetter != KeyCode.D &&
								                                     x.KeyboardLetter != KeyCode.F && x.KeyboardLetter != KeyCode.G) // exclude combo keys and mash key
								                              .OrderBy(x => Guid.NewGuid())
								                              .Take(3)
								                              .ToList();

								foreach (Key key in oGCD_Keys) key.OffGlobalCooldown = true;

								// set G key to be a mash key
								Key mashKey = KeyController.Instance.GetKey(KeyCode.G);
								if (mashKey != null) mashKey.Mash = true;

								foreach (Key key in KeyController.Instance.FlatKeys)
								{
									key.offGCDMarker.SetActive(key.OffGlobalCooldown);
									key.ComboMarker.SetActive(key.Combo);
									key.MashMarker.SetActive(key.Mash);
								}
								#endregion
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

	public IEnumerator Wave()
	{
		var wave = KeyController.Instance.Wave();

		foreach (List<Key> col in wave)
		{
			foreach (var key in col)
			{
				key.Activate(true, 1f);
			}

			yield return new WaitForSeconds(0.2f);
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
	
	public void AddScore(int points)
	{
		Score += points;
		Debug.Log($"Score increased by {points}. Total score: {Score}");
	}
}
