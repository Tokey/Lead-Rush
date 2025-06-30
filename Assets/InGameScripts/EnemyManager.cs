using Demo.Scripts.Runtime;
using System.Collections;
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

    // Update is called once per frame
    void Update()
    {
        if (!playerController.isPlayerReady || !playerController.isQoeDisabled || !playerController.isAcceptabilityDisabled)
            return;

        spawnTimer -= Time.deltaTime;
        enemiesInScene = GameObject.FindGameObjectsWithTag("Enemy").Length;
        if (spawnTimer < 0 && enemiesInScene < maxEnemyCount)
        {
            SpawnEnemy();
            spawnTimer = spawnDuration;
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
        /*float minDist = 9999999;
        GameObject closestEnemyGO = null;


        foreach (GameObject enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            if (Vector3.Distance(player.transform.position, enemy.transform.position) < minDist)
            {
                minDist = Vector3.Distance(player.transform.position, enemy.transform.position);
                closestEnemyGO = enemy;
            }
        }*/
        return GameObject.FindGameObjectWithTag("Enemy");
    }

    void SpawnEnemy()
    {
        // LEGACY
        /*int spawnIndex = Random.Range(0, sapwnPoints.Count - 1);
        Instantiate(enemy, sapwnPoints[spawnIndex].position, sapwnPoints[spawnIndex].rotation);*/

        float dist = Random.Range(minSpawnRadius, maxSpawnRadius);
        float angle = Random.Range(0, 360);

        Vector3 spawnPos = CalculateDistantPoint(player.transform.position, dist, angle);

        SpawnNavMeshAgent(enemy, spawnPos);
        if (playerController.isEnemySpawnSpikeEnabled)
        {
            playerController.gameManager.isEventBasedDelay = true;
            playerController.perRoundEnemySpawnSpikeCount++;
        }
    }

    public Vector3 CalculateDistantPoint(Vector3 playerPosition, float distance, float angle)
    {
        float angleRad = Mathf.Deg2Rad * angle;
        float xOffset = distance * Mathf.Cos(angleRad);
        float zOffset = distance * Mathf.Sin(angleRad);

        return new Vector3(playerPosition.x + xOffset, playerPosition.y, playerPosition.z + zOffset);
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
