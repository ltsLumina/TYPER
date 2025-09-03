#region
using System.Collections.Generic;
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
    
    void Start()
    {
        parent = new ("> Enemies <");
        
        InvokeRepeating(nameof(SpawnEnemy), initialDelay, repeatRate);
    }

    void SpawnEnemy()
    {
        float[] keyPositionsY = { 1.5f, 0.5f, -0.5f };
        
        float yPosition = keyPositionsY[Random.Range(0, keyPositionsY.Length)];
        Vector3 spawnPosition = new Vector3(8f, yPosition, 0f);
        var enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, parent.transform);
        enemies.Add(enemy);
    }
}
