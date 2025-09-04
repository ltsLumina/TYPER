using MelenitasDev.SoundsGood;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GameManager : MonoBehaviour
{
	[SerializeField] int health = 10;
	[SerializeField] int score;
	
	[Header("Settings")]
	[SerializeField] bool showDamageNumbers;
	
	public static GameManager Instance { get; private set; }

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
		var music = new Music(Track.musicSFX);
		music.SetOutput(Output.Music);
		music.SetVolume(0.1f);
		music.SetLoop(true);
		music.Play();
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


