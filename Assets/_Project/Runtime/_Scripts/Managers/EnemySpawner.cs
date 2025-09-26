#region
using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Lumina.Essentials.Attributes;
using UnityEngine;
using VInspector;
using Random = UnityEngine.Random;
#endregion

public class EnemySpawner : MonoBehaviour
{
	[Header("Enemy List")]
	[SerializeField] ObjectPool enemyPool;
	[SerializeField] public List<Enemy> enemies = new ();

	[Header("Enemy Spawner Settings")]
	[SerializeField] Enemy enemyPrefab;
	[SerializeField] float initialDelay;
	[UsedImplicitly, ReadOnly]
	[SerializeField] string currentWave;
	[Tooltip("Waves defined as <elapsed time, repeat rate>")]
	[SerializeField] SerializedDictionary<int, float> waves = new () { { 0, 0.5f }, { 60, 0.3f }, { 120, 0.2f } };

	GameObject parent;
	float[] lanes;

	public static event Action<Enemy> OnEnemySpawned;

	public bool IsPaused { get; private set; }

	public void PauseSpawner() => IsPaused = true;

	public void PlaySpawner() => IsPaused = false;

	void Start()
	{
		lanes = KeyManager.Instance.Lanes;
		StartCoroutine(SpawnRoutine());
	}

	IEnumerator SpawnRoutine()
	{
		yield return new WaitForSeconds(initialDelay);
		float elapsed = 0f;

		while (true)
		{
			while (IsPaused) yield return null;
			float repeatRate = GetRepeatRate(elapsed);
			SpawnEnemy();
			yield return new WaitForSeconds(repeatRate);
			elapsed += repeatRate;
		}
	}

	/// <summary>
	///     Gets the repeat rate based on the elapsed time and the defined waves.
	/// </summary>
	/// <param name="elapsed"> The elapsed time since the start of the game.</param>
	/// <returns> The repeat rate in seconds.</returns>
	float GetRepeatRate(float elapsed)
	{
		float rate = 1f;

		foreach (KeyValuePair<int, float> wave in waves)
		{
			if (elapsed >= wave.Key)
			{
				rate = wave.Value;
				currentWave = $"Wave starting at {wave.Key}s ({elapsed:F1}s) | ({rate} spawns/sec) | {enemies.Count} enemies alive";
			}
			else break;
		}

		return rate;
	}

	void SpawnEnemy() // TODO: slowly span tougher and tougher enemies?
	{
		int laneIndex = Random.Range(0, lanes.Length);
		float lanePos = lanes[laneIndex];
		var spawnPosition = new Vector3(10f, lanePos, 0f);
		var enemy = enemyPool.GetPooledObject<Enemy>(true, spawnPosition, Quaternion.identity, enemyPool.transform);
		enemy.OnDeath += () => enemies.Remove(enemy);
		enemies.Add(enemy);
		enemy.Lane = laneIndex + 1;

		OnEnemySpawned?.Invoke(enemy);
	}
}
