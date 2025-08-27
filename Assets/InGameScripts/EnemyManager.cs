using Demo.Scripts.Runtime;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyManager : MonoBehaviour
{
    public GameObject enemy;
    public GameObject player;
    public FPSController playerController;
    public List<Transform> sapwnPoints;
    public List<Enemy> enemies;

    public float spawnDuration;
    public float spawnTimer;
    int enemiesInScene;
    public int maxEnemyCount;

    public float minSpawnRadius;
    public float maxSpawnRadius;
    public float walkwableAreaRadius;
    float spawnAngle;

    public float invincibilityTimer;
    public float remainingTeleportTimeFromLastEnemy = 0f;

    public enum SpawnMode { DonutSpawn, VisibleSpawn }
    public SpawnMode spawnMode = SpawnMode.DonutSpawn;

    [Range(10f, 180f)]
    public float visibleSpawnAngle = 60f;

    void Start()
    {
        TimerReset();
        SpawnEnemy();
        playerController = player.GetComponent<FPSController>();
    }

    void TimerReset()
    {
        spawnTimer = spawnDuration;
    }

    void Update()
    {
        if (!playerController.isPlayerReady || !playerController.isQoeDisabled || !playerController.isAcceptabilityDisabled)
            return;

        if (remainingTeleportTimeFromLastEnemy > 0f)
        {
            spawnTimer = remainingTeleportTimeFromLastEnemy;
            remainingTeleportTimeFromLastEnemy = 0f;
        }

        spawnTimer -= Time.deltaTime;
        enemiesInScene = GameObject.FindGameObjectsWithTag("Enemy").Length;
        if (spawnTimer < 0 && enemiesInScene < maxEnemyCount)
        {
            SpawnEnemy();
            //spawnTimer = spawnDuration;
            spawnTimer = remainingTeleportTimeFromLastEnemy;
        }
    }

    public void DestroyAllEnemy()
    {
        Enemy enemy = GameObject.FindGameObjectWithTag("Enemy").GetComponent<Enemy>();
        if (enemy != null)
            enemy.EnemyLog();
        Destroy(enemy.gameObject);
    }

    public GameObject GetClosestEnemy()
    {
        return GameObject.FindGameObjectWithTag("Enemy");
    }

    void SpawnEnemy()
    {
        Vector3 spawnPos = Vector3.zero;

        if (spawnMode == SpawnMode.DonutSpawn)
        {
            float dist = Random.Range(minSpawnRadius, maxSpawnRadius);
            float angle = Random.Range(0, 360);
            spawnPos = CalculateDistantPoint(player.transform.position, dist, angle);
        }
        else if (spawnMode == SpawnMode.VisibleSpawn)
        {
            spawnPos = CalculateVisibleSpawnPoint();
        }

        SpawnNavMeshAgent(enemy, spawnPos);

        if (playerController.isEnemySpawnSpikeEnabled)
        {
            playerController.gameManager.isEventBasedDelay = true;
            playerController.perRoundEnemySpawnSpikeCount++;
        }
    }

    // --- Donut spawn helper ---
    public Vector3 CalculateDistantPoint(Vector3 playerPosition, float distance, float angle)
    {
        float angleRad = Mathf.Deg2Rad * angle;
        float xOffset = distance * Mathf.Cos(angleRad);
        float zOffset = distance * Mathf.Sin(angleRad);

        return new Vector3(playerPosition.x + xOffset, playerPosition.y, playerPosition.z + zOffset);
    }

    // --- Visible spawn helper ---
    public Vector3 CalculateVisibleSpawnPoint()
    {
        // Get player forward
        Vector3 playerForward = player.transform.forward;

        // Pick a random angle within half of visibleSpawnAngle on either side
        float halfFov = visibleSpawnAngle / 2f;
        float angleOffset = Random.Range(-halfFov, halfFov);

        // Calculate rotation around Y
        Quaternion rotation = Quaternion.Euler(0, angleOffset, 0);
        Vector3 spawnDirection = rotation * playerForward;

        // Pick a random distance in front of the player using min/maxSpawnRadius
        float dist = Random.Range(minSpawnRadius, maxSpawnRadius);

        // Calculate spawn point
        Vector3 spawnPos = player.transform.position + spawnDirection.normalized * dist;
        spawnPos.y = player.transform.position.y;
        return spawnPos;
    }

    public Vector3 GetRandomWalkablePositionNear(Vector3 desiredPosition, float sampleRadius)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(desiredPosition, out hit, sampleRadius, NavMesh.AllAreas))
        {
            return hit.position;
        }
        // If no valid position found, return desired position (for debugging)
        return desiredPosition;
    }

    public void SpawnNavMeshAgent(GameObject agentPrefab, Vector3 desiredPosition)
    {
        Vector3 spawnPosition = GetRandomWalkablePositionNear(desiredPosition, walkwableAreaRadius); // Adjust sampleRadius as needed
        Instantiate(agentPrefab, spawnPosition, Quaternion.identity);
    }
}
