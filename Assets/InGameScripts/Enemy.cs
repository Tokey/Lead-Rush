using Demo.Scripts.Runtime;
using Michsky.UI.Heat;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Audio;

public class Enemy : MonoBehaviour
{

    GameObject player;
    public FPSController playerController;
    NavMeshAgent enemyAgent;

    public float maxHealth;

    float currentHealth;

    public ParticleSystem deathPE;

    public ParticleSystem explodePE;

    public SphereCollider largeCollider;

    public Transform headTransform;

    public float minAngleToPlayer;

    public GameObject manager;

    public GameObject enemyHead;

    public float angularSizeOnSpawn;
    public SphereCollider headCollider;


    // Start is called before the first frame update
    void Start()
    {
        enemyAgent = gameObject.GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player");
        manager = GameObject.FindGameObjectWithTag("Manager");
        playerController = player.GetComponent<FPSController>();

        maxHealth = playerController.enemyHealthGlobal;
        currentHealth = maxHealth;

        headCollider = headTransform.GetComponent<SphereCollider>();

        var relativePos = this.transform.position - player.transform.position;

        var forward = player.transform.forward;
        minAngleToPlayer = Vector3.Angle(relativePos, forward);
        angularSizeOnSpawn = playerController.CalculateAngularSize(enemyHead, playerController.mainCamera.position);

        enemyAgent.speed = playerController.enemySpeedGlobal;

        player.GetComponent<FPSController>().currentEnemyHead = enemyHead;

        player.GetComponent<FPSController>().roundManager.attributeScalingModule.enemyCollider = enemyHead.GetComponent<SphereCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!playerController.isPlayerReady || !playerController.isQoeDisabled || !playerController.isAcceptabilityDisabled)
            return;
        enemyAgent.destination = player.transform.position;

        if (Vector3.Distance(player.transform.position, headCollider.transform.position) <1.75)
        {
            Instantiate(explodePE, headTransform.position, headTransform.rotation);
            player.GetComponent<FPSController>().PlayDeathSFX();
            player.GetComponent<FPSController>().RespawnPlayer();
        }

        //float angularSize =  playerController.CalculateAngularSize(enemyHead, playerController.mainCamera.position);

        //Debug.Log("Angular size: " + angularSize);

        largeCollider.transform.localScale = new Vector3(2.5F + Mathf.PingPong(Time.time, 1.0f),1,1);

        //Debug.Log("Min: " + minAngleToPlayer);

        
    }

    public void TakeDamage(float damage)
    {
        currentHealth-=damage;
        if(currentHealth < 0)
        {
            FPSController fPSController = player.GetComponent<FPSController>();

            fPSController.degreeToTargetXCumulative += fPSController.degreeToTargetX;
            fPSController.degreeToShootXCumulative += fPSController.degreeToShootX;

            fPSController.timeToTargetEnemyCumulative += fPSController.timeToTargetEnemy;
            fPSController.timeToHitEnemyCumulative += fPSController.timeToHitEnemy;
            fPSController.timeToKillEnemyCumulative += fPSController.timeToKillEnemy;

            fPSController.minAnlgeToEnemyCumulative += minAngleToPlayer;
            fPSController.enemySizeCumulative += angularSizeOnSpawn;

            EnemyLog();
            
            fPSController.killCooldown = .3f;
            fPSController.targetMarked = false;
            fPSController.targetShot = false;
            fPSController.PlayKillSFX();
            Instantiate(deathPE, headTransform.position, headTransform.rotation);
            //Destroy the Instantiated ParticleSystem 

            fPSController.score += fPSController.onKillScore;
            fPSController.roundKills++;
            fPSController.timeTillLastKill = 0f;
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("Enemycol: " + other.gameObject.name);
        /*if (other.gameObject.tag == "Player")
        {
            Instantiate(explodePE, headTransform.position, headTransform.rotation);
            player.GetComponent<FPSController>().PlayDeathSFX();
            player.GetComponent<FPSController>().RespawnPlayer();
        }*/
    }

    public void EnemyLog()
    {

        RoundManager roundManager = manager.GetComponent<RoundManager>();

        FPSController fPSController = player.GetComponent<FPSController>();

        TextWriter textWriter = null;
        string filenameEnemyLog = "Data\\Logs\\EnemyData_" + roundManager.fileNameSuffix + "_" + roundManager.sessionID + "_" + ".csv";

        while (textWriter == null)
            textWriter = File.AppendText(filenameEnemyLog);


        String enemyLogLine =
           roundManager.sessionID.ToString() + "," +
           roundManager.latinRow.ToString() + "," +
           roundManager.currentRoundNumber.ToString() + "," +
           roundManager.sessionStartTime.ToString() + "," +
           System.DateTime.Now.ToString() + "," +
           roundManager.roundConfigs.roundFPS[roundManager.indexArray[roundManager.currentRoundNumber - 1]].ToString() + "," +
               roundManager.roundConfigs.spikeMagnitude[roundManager.indexArray[roundManager.currentRoundNumber - 1]].ToString() + "," +
               roundManager.roundConfigs.onAimSpikeEnabled[roundManager.indexArray[roundManager.currentRoundNumber - 1]].ToString() + "," +
               roundManager.roundConfigs.onEnemySpawnSpikeEnabled[roundManager.indexArray[roundManager.currentRoundNumber - 1]].ToString() + "," +
               roundManager.roundConfigs.onMouseSpikeEnabled[roundManager.indexArray[roundManager.currentRoundNumber - 1]].ToString() + "," +
               roundManager.roundConfigs.onReloadSpikeEnabled[roundManager.indexArray[roundManager.currentRoundNumber - 1]].ToString() + "," +
               roundManager.roundConfigs.attributeScalingEnabled[roundManager.indexArray[roundManager.currentRoundNumber - 1]].ToString() + "," +
               roundManager.indexArray[roundManager.currentRoundNumber - 1].ToString() + "," +
           currentHealth.ToString() + "," +
           minAngleToPlayer.ToString() + "," +
           angularSizeOnSpawn.ToString() + "," +
           fPSController.degreeToTargetX.ToString() + "," +
           fPSController.degreeToTargetY.ToString() + "," +
           fPSController.degreeToShootX.ToString() + "," +
           fPSController.degreeToShootY.ToString() + "," +
           fPSController.timeToTargetEnemy.ToString() + "," +
           fPSController.timeToHitEnemy.ToString() + "," +
           fPSController.timeToKillEnemy.ToString() + "," +
           fPSController.targetMarked.ToString() + "," +
           fPSController.targetShot.ToString()
            ;
        textWriter.WriteLine(enemyLogLine);
        textWriter.Close();

        fPSController.degreeToTargetX = 0;
        fPSController.degreeToTargetY = 0;
        fPSController.degreeToShootX = 0;
        fPSController.degreeToShootY = 0;

        fPSController.timeToKillEnemy = 0;
        fPSController.timeToHitEnemy = 0;
        fPSController.timeToTargetEnemy = 0;
    }
}
