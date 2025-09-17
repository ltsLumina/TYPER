#region
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Lumina.Essentials.Attributes;
using UnityEngine;
using VInspector;
#endregion

public class EnemySpawner : MonoBehaviour
{
	[Header("Enemy List")]
	[SerializeField] ObjectPool enemyPool;
	[SerializeField] List<Enemy> enemies = new ();

	[Header("Enemy Spawner Settings")]
	[SerializeField] Enemy enemyPrefab;
	[SerializeField] float initialDelay;
	[UsedImplicitly, ReadOnly]
	[SerializeField] string currentWave;
	[Tooltip("Waves defined as <elapsed time, repeat rate>")]
	[SerializeField] SerializedDictionary<int, float> waves = new ()
	{ { 0, 0.5f },
	  { 60, 0.3f },
	  { 120, 0.2f } };

	GameObject parent;
	float[] lanes;

	void Start()
	{
		lanes = KeyManager.Instance.Lanes;
		parent = new ("--- Enemies ---");
		StartCoroutine(SpawnRoutine());
	}

	IEnumerator SpawnRoutine()
	{
		yield return new WaitForSeconds(initialDelay);
		float elapsed = 0f;

		while (true)
		{
			float repeatRate = GetRepeatRate(elapsed);
			SpawnEnemy();
			yield return new WaitForSeconds(repeatRate);
			elapsed += repeatRate;
		}
	}

	/// <summary>
	/// Gets the repeat rate based on the elapsed time and the defined waves.
	/// </summary>
	/// <param name="elapsed"> The elapsed time since the start of the game.</param>
	/// <returns> The repeat rate in seconds.</returns>
	float GetRepeatRate(float elapsed)
	{
		float rate = 1f;
		foreach (var wave in waves)
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

	void SpawnEnemy()
	{
		int laneIndex = Random.Range(0, lanes.Length);
		float lanePos = lanes[laneIndex];
		var spawnPosition = new Vector3(8f, lanePos, 0f);
		var enemy = enemyPool.GetPooledObject<Enemy>(true, spawnPosition, Quaternion.identity, parent.transform);
		enemy.OnDeath += () => enemies.Remove(enemy);
		enemies.Add(enemy);
		enemy.Lane = laneIndex + 1;
	}
}
