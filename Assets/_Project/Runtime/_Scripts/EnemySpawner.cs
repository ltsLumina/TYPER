#region
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#endregion

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy List")]
    [SerializeField] List<Enemy> enemies = new ();
    
    [Header("Enemy Spawner Settings")]
    [SerializeField] Enemy enemyPrefab;
    [SerializeField] float repeatRate = 0.5f;
    [SerializeField] float initialDelay;

    GameObject parent;
    float[] lanes;
    
    void Start()
    {
        lanes = KeyManager.Instance.Lanes;
        
        parent = new ("> Enemies <");
        InvokeRepeating(nameof(SpawnEnemy), initialDelay, repeatRate);
    }

    void SpawnEnemy()
    {
        int laneIndex = Random.Range(0, lanes.Length);
        float lanePos = lanes[laneIndex];
        var spawnPosition = new Vector3(8f, lanePos, 0f);
        Enemy enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, parent.transform);
        enemies.Add(enemy);
        
        enemy.Lane = laneIndex + 1;
    }
}
