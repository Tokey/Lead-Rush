using Demo.Scripts.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Windows;
using static AttributeScaling;

[System.Serializable]
public struct RoundConfigs
{
    public List<float> roundFPS;
    public List<float> spikeMagnitude;
    public List<bool> onAimSpikeEnabled;
    public List<bool> onReloadSpikeEnabled;
    public List<bool> onMouseSpikeEnabled;
    public List<bool> onEnemySpawnSpikeEnabled;
    public List<bool> attributeScalingEnabled;
    public List<float> attributeScaleRadius;
    public List<float> enemyLateralMoveSpeed;
    public List<bool> predictableEnemyMovement;
}

//ShootingEventLog


[System.Serializable]
public struct ShootingEventLog
{
    public List<string> time;
    public List<float> roundTimer;
    public List<float> mouseX;
    public List<float> mouseY;

    public List<float> playerX;
    public List<float> playerY;
    public List<float> playerZ;

    public List<Quaternion> playerRot;

    public List<Vector3> enemyPos;
    public List<Vector3> muzzlePos;

    public List<int> teleportationCount;

    public List<bool> isEnemyDead;

    public List<float> distanceToPlayer;

    public List<float> targetTimeOnEnemy;

    public List<float> mouseMovedX;
    public List<float> mouseMovedY;

    public List<int> shotsFiredPerEvent;
    public List<int> shotsHitPerEvent;
    public List<int> shotsMissedPerEvent;


    public List<float> durationOfEventNotIncludingSpikes;
    public List<float> durationOfEventIncludingSpikes;
    public List<int> spikeCountPerEvent;
    public List<float> angularDistanceFromEnemyOnStart;
}

[System.Serializable]
public struct PlayerTickLog
{
    public List<string> time;
    public List<float> roundTimer;
    public List<float> mouseX;
    public List<float> mouseY;

    public List<float> playerX;
    public List<float> playerY;
    public List<float> playerZ;

    public List<Quaternion> playerRot;

    public List<Vector3> enemyPos;
    public List<bool> isADS;

    public List<float> muzzleX;
    public List<float> muzzleY;
    public List<float> muzzleZ;

    public List<float> targetAngleToEnemy;
    public List<bool> validMissContinious;

    public List<float> scorePerSec;
    public List<double> frameTimeMS;

    public List<bool> playerOnTarget;
    public List<float> onTargetDuration;
}

[System.Serializable]
public struct PerShotLog
{
    public List<string> time;
    public List<float> roundTimer;
    public List<float> mouseX;
    public List<float> mouseY;

    public List<float> playerX;
    public List<float> playerY;
    public List<float> playerZ;

    public List<Quaternion> playerRot;

    public List<Vector3> enemyPos;
    public List<bool> isADS;
    public List<bool> isHit;

    public List<float> missAngle;
    public List<float> distanceToPlayer;

    public List<float> neededWorldRadius;
    public List<float> extraRadiusWorld;
    public List<float> neededLocalRadius;

    public List<float> currentWorldRadius;
    public List<float> currentLocalRadius;

    public List<float> currentAccuracy;
    public List<long> currentScore;

    public List<float> currentMouseSpeed;
    public List<float> avgMouseSpeed;


    public List<bool> enemyInView;
    public List<float> timeTillLastKill;
    public List<bool> validMiss;


    public List<bool> isFirstShotAfterSpike;
    public List<int> shotCountAfterSpike;
    public List<int> postSpikeFirstShotHits;
    public List<int> postSpikeFirstShotMisses;
    public List<float> postSpikeFirstShotAccuracy;

    public List<float> elapsedTimeFromLastSpike;

    public List<float> horizontalMissAngle;
    public List<float> verticalMissAngle;
    public List<bool> isEnemyInFront;
    public List<float> elapsedTimeSinceLastEventIncludingSpike;
    public List<float> elapsedTimeSinceLastEventExcludingSpike;
}

public class RoundManager : MonoBehaviour
{
    public RoundConfigs roundConfigs;

    public float roundDuration;

    public float roundTimer;

    public int totalRoundNumber;

    public List<int> indexArray = new List<int>();

    public GameManager gameManager;

    public int currentRoundNumber;

    public String sessionStartTime;

    public FPSController playerController;

    public long roundFrameCount = 0;
    public double frametimeCumulativeRound = 0;

    public string fileNameSuffix = "";
    public String filenamePerTick = "Data\\ClientDataPerTick.csv";
    public String filenamePerRound = "Data\\ClientDataPerRound.csv";

    public float qoeValue;

    public bool acceptabilityValue;

    public int sessionID = -1;

    public bool isFTStudy;

    public int latinSquareRowNumber = 0;

    public int latinRow;

    public AttributeScaling attributeScalingModule;

    // Start is called before the first frame update
    void Start()
    {
        latinSquareRowNumber = 0;
        currentRoundNumber = 1;
        fileNameSuffix = GenRandomID(6).ToString();
        sessionStartTime = System.DateTime.Now.ToString("yy:mm:dd:hh:mm:ss");

        gameManager = GetComponent<GameManager>();

        ReadGlobalConfig();

        ReadLatinSquareSize();

        ReadFromLatinSquare();

        totalRoundNumber = roundConfigs.roundFPS.Count;

        for (int i = 0; i < totalRoundNumber; i++)
        {
            indexArray.Add(i);
        }

        // Shuffle the list
        //Shuffle(indexArray);

        //LEGACY CODE
        //Add practice round
        /*int temp = indexArray[totalRoundNumber - 1];
        indexArray[totalRoundNumber - 1] = indexArray[0];
        indexArray[0] = 0;
        indexArray.Add(temp);
        indexArray.Add(0);
        totalRoundNumber++;*/


        playerController.isQoeDisabled = true;
        playerController.isAcceptabilityDisabled = true;
        SetRounConfig();
    }
    void Shuffle<T>(List<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (roundTimer > 0 && playerController.isPlayerReady && playerController.isQoeDisabled && playerController.isAcceptabilityDisabled)
        {
            roundTimer -= Time.deltaTime;
            frametimeCumulativeRound += Time.deltaTime;
            roundFrameCount++;
        }
        else if (roundTimer <= 0 && playerController.isQoeDisabled && playerController.isAcceptabilityDisabled)
        {
            playerController.isQoeDisabled = false;
            playerController.ResetPlayerAndDestroyEnemy();
        }
    }

    void ReadLatinSquareSize()
    {
        if (isFTStudy)
        {
            bool EOF = false;
            string line = null;
            StreamReader strReader = new StreamReader("Data\\Configs\\RoundConfig.csv");
            EOF = false;

            while (!EOF)
            {
                line = strReader.ReadLine();
                if (line == null)
                {
                    EOF = true;
                    break;
                }
                else
                {
                    latinSquareRowNumber++;
                    Debug.Log("LATIN SQUARE ROW COUNT = " + latinSquareRowNumber);
                }
            }

            Debug.Log("LATIN SQUARE final ROW COUNT = " + latinSquareRowNumber);
        }
    }
    void ReadGlobalConfig()
    {
        string line = null;
        StreamReader strReader = new StreamReader("Data\\Configs\\GlobalConfig.csv");
        bool EOF = false;
        while (!EOF)
        {
            line = strReader.ReadLine();

            if (line == null)
            {
                EOF = true;
                break;
            }
            else
            {
                var dataValues = line.Split(',');
                roundDuration = float.Parse(dataValues[0]);
                isFTStudy = bool.Parse(dataValues[1]);
                playerController.aimSpikeDelay = float.Parse(dataValues[2]);
                playerController.mouseSpikeDelay = float.Parse(dataValues[3]);
                playerController.mouseSpikeDegreeOfMovement = float.Parse(dataValues[4]);
                playerController.enemySpeedGlobal = float.Parse(dataValues[5]);
                playerController.enemyHealthGlobal = float.Parse(dataValues[6]);
                playerController.reticleSizeMultiplier = float.Parse(dataValues[7]);

                playerController.onHitScore = int.Parse(dataValues[8]);
                playerController.onMissScore = int.Parse(dataValues[9]);
                playerController.onKillScore = int.Parse(dataValues[10]);
                playerController.onDeathScore = int.Parse(dataValues[11]);
            }
        }
    }

    public void ReadFromLatinSquare()
    {
        string line = null;
        StreamReader strReader = new StreamReader("Data\\Configs\\SessionID.csv");
        bool EOF = false;
        roundConfigs.roundFPS.Clear();

        sessionID = -1;

        while (!EOF)
        {
            line = strReader.ReadLine();

            if (line == null)
            {
                EOF = true;
                break;
            }
            else
            {
                var dataValues = line.Split(',');
                sessionID = int.Parse(dataValues[0]);
            }
        }



        line = null;
        strReader = new StreamReader("Data\\Configs\\LatinSquare.csv");
        EOF = false;
        int index = 1;

        if (!isFTStudy)
        {
            //Practice
            roundConfigs.roundFPS.Add(500);
            roundConfigs.roundFPS.Add(7);

            while (!EOF)
            {
                line = strReader.ReadLine();

                if (line == null)
                {
                    EOF = true;
                    break;
                }
                else
                {
                    var dataValues = line.Split(',');
                    if (index == sessionID)
                    {
                        for (int i = 0; i < dataValues.Length; i++)
                            roundConfigs.roundFPS.Add(float.Parse(dataValues[i]));

                        /*// Round x2
                        for (int i = 0; i < dataValues.Length; i++)
                            roundConfigs.roundFPS.Add(float.Parse(dataValues[i]));*/
                        FrameRateSudySpikeConfigFiller(dataValues.Length);
                        break;
                    }
                }
                index++;
            }
        }
        else
        {
            //Practice round single for FT study
            /*roundConfigs.roundFPS.Add(500);
            roundConfigs.spikeMagnitude.Add(0);
            roundConfigs.onAimSpikeEnabled.Add(false);
            roundConfigs.onReloadSpikeEnabled.Add(false);
            roundConfigs.onMouseSpikeEnabled.Add(false);
            roundConfigs.onEnemySpawnSpikeEnabled.Add(false);*/

            ReadFTStudyCSV();
        }
    }

    public void FrameRateSudySpikeConfigFiller(int length)
    {
        for (int i = 0; i < length * 2 + 2; i++)
        {
            roundConfigs.spikeMagnitude.Add(100);
            roundConfigs.onAimSpikeEnabled.Add(false);
            roundConfigs.onReloadSpikeEnabled.Add(false);
            roundConfigs.onMouseSpikeEnabled.Add(false);
            roundConfigs.onEnemySpawnSpikeEnabled.Add(false);
            roundConfigs.attributeScalingEnabled.Add(false);
            roundConfigs.attributeScaleRadius.Add(1f);
            roundConfigs.enemyLateralMoveSpeed.Add(0f);
            roundConfigs.predictableEnemyMovement.Add(true);

            
        }
    }


    // Primary Config
    public void ReadFTStudyCSV()
    {
        string line = null;
        StreamReader strReader = new StreamReader("Data\\Configs\\LatinMap.csv");
        bool EOF = false;
        List<string> latinMap = new List<string>();

        while (!EOF)
        {
            line = strReader.ReadLine();

            if (line == null)
            {
                EOF = true;
                break;
            }
            else
            {
                latinMap.Add(line);
                //Debug.Log(strReader.ReadLine());
            }
        }

        /* for (int i = 0; i < latinMap.Count; i++)
             Debug.Log("latmap::: " + i +"::: "+latinMap[i]);*/

        line = null;
        strReader = new StreamReader("Data\\Configs\\SessionID.csv");
        EOF = false;

        sessionID = -1;

        while (!EOF)
        {
            line = strReader.ReadLine();

            if (line == null)
            {
                EOF = true;
                break;
            }
            else
            {
                var dataValues = line.Split(',');
                sessionID = int.Parse(dataValues[0]);
            }
        }

        line = null;
        strReader = new StreamReader("Data\\Configs\\RoundConfig.csv");
        EOF = false;
        roundConfigs.roundFPS.Clear();
        latinRow = ((sessionID - 1) % latinSquareRowNumber) + 1;

        Debug.Log("LATIN ROW NUMBER: " + latinRow);

        int index = 1;

        while (!EOF)
        {
            line = strReader.ReadLine();

            if (line == null)
            {
                EOF = true;
                break;
            }
            else
            {
                var configVals = line.Split(',');
                Debug.Log("CONF:  " + line);
                if (index == latinRow)

                {
                    for (int i = 0; i < configVals.Length; i++)
                    {
                        string config = latinMap[int.Parse(configVals[i]) - 1];
                        Debug.Log("AC CONFIG:: " + config + "index ::: " + int.Parse(configVals[i]));
                        var dataValues = config.Split(',');



                        Debug.Log(dataValues[1]);
                        roundConfigs.roundFPS.Add(float.Parse(dataValues[0]));
                        roundConfigs.spikeMagnitude.Add(float.Parse(dataValues[1]));
                        roundConfigs.onAimSpikeEnabled.Add(bool.Parse(dataValues[2]));
                        roundConfigs.onReloadSpikeEnabled.Add(bool.Parse(dataValues[3]));
                        roundConfigs.onMouseSpikeEnabled.Add(bool.Parse(dataValues[4]));
                        roundConfigs.onEnemySpawnSpikeEnabled.Add(bool.Parse(dataValues[5]));
                        roundConfigs.attributeScalingEnabled.Add(bool.Parse(dataValues[6]));
                        roundConfigs.attributeScaleRadius.Add(float.Parse(dataValues[7]));
                        roundConfigs.enemyLateralMoveSpeed.Add(float.Parse(dataValues[8]));
                        roundConfigs.predictableEnemyMovement.Add(bool.Parse(dataValues[9]));
                    }
                }
            }
            index++;
        }
    }

    String GenRandomID(int len)
    {
        String alphanum = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        String tmp_s = "";

        for (int i = 0; i < len; ++i)
        {
            tmp_s += alphanum[UnityEngine.Random.Range(0, 1000) % (alphanum.Length - 1)];
        }

        return tmp_s;
    }

    public void SetRounConfig()
    {
        roundTimer = roundDuration;
        gameManager.isFixedFT = false;


        Application.targetFrameRate = (int)roundConfigs.roundFPS[indexArray[currentRoundNumber - 1]];


        playerController.isAimSpikeEnabled = roundConfigs.onAimSpikeEnabled[indexArray[currentRoundNumber - 1]];
        playerController.isReloadSpikeEnabled = roundConfigs.onReloadSpikeEnabled[indexArray[currentRoundNumber - 1]];
        playerController.isMouseMovementSpikeEnabled = roundConfigs.onMouseSpikeEnabled[indexArray[currentRoundNumber - 1]];
        playerController.isEnemySpawnSpikeEnabled = roundConfigs.onEnemySpawnSpikeEnabled[indexArray[currentRoundNumber - 1]];

        gameManager.delayDuration = roundConfigs.spikeMagnitude[indexArray[currentRoundNumber - 1]];
        attributeScalingModule.spikeMagnitude = roundConfigs.spikeMagnitude[indexArray[currentRoundNumber - 1]];
        attributeScalingModule.useAttributeScaling = roundConfigs.attributeScalingEnabled[indexArray[currentRoundNumber - 1]];
        attributeScalingModule.attributeScaleRadius = roundConfigs.attributeScaleRadius[indexArray[currentRoundNumber - 1]];
        attributeScalingModule.attributeScalingDuration = roundConfigs.spikeMagnitude[indexArray[currentRoundNumber - 1]] / 1000.0f;
        roundFrameCount = 0;
        frametimeCumulativeRound = 0;

    }
    public void LogRoundData()
    {
        PlayerStats stats = playerController.gameObject.GetComponent<PlayerStats>();

        string filenamePerRound = "Data\\Logs\\RoundData_" + fileNameSuffix + "_" + sessionID + "_" + ".csv";
        bool fileExists = System.IO.File.Exists(filenamePerRound);

        using (var textWriter = new StreamWriter(filenamePerRound, append: true))
        {
            if (!fileExists)
            {
                string header =
                    "sessionID,latinRow,currentRoundNumber,sessionStartTime,now,roundFPS,spikeMagnitude," +
                    "onAimSpikeEnabled,onEnemySpawnSpikeEnabled,onMouseSpikeEnabled,onReloadSpikeEnabled,attributeScalingEnabled," + "attributeScaleRadius,"+ "enemyMoveSpeed," + "predictableEnemyMovement," +
                    "indexArray,score,shotsFiredPerRound,shotsHitPerRound,headshotsHitPerRound,realoadCountPerRound," +
                    "tacticalReloadCountPerRound,accuracy,roundKills,roundDeaths,distanceTravelledPerRound,delXCumilative,delYCumilative," +
                    "totalDelXY,frametimeCumulativeRound,roundFrameCount,avgFT,avgFPS,perRoundAimSpikeCount,perRoundReloadSpikeCount," +
                    "perRoundMouseMovementSpikeCount,spikeDurationCumulative,avgspikeDurationCumulative,perRoundEnemySpawnSpikeCount," +
                    "degreeToShootXCumulative,degreeToTargetXCumulative,minAnlgeToEnemyCumulative,enemySizeCumulative,timeToTargetEnemyCumulative," +
                    "timeToHitEnemyCumulative,timeToKillEnemyCumulative,degXShootAvg,degXTargetAvg,enemySizeOnSpawnAvg,aimDurationPerRound," +
                    "isFiringDurationPerRound,qoeValue,acceptabilityValue";
                textWriter.WriteLine(header);
            }

            float accuracy = 0;
            if (playerController.shotsFiredPerRound > 0)
            {
                accuracy = (float)playerController.shotsHitPerRound / (float)playerController.shotsFiredPerRound;
            }

            float degXTargetAvg = (float)playerController.degreeToTargetXCumulative / (float)playerController.roundKills;
            float degXShootAvg = (float)playerController.degreeToShootXCumulative / (float)playerController.roundKills;
            float enemySizeOnSpawnAvg = (float)playerController.enemySizeCumulative / (float)playerController.roundKills;

            float timeToTargetAvg = (float)playerController.timeToTargetEnemyCumulative / (float)playerController.roundKills;
            float timeToHitAvg = (float)playerController.timeToHitEnemyCumulative / (float)playerController.roundKills;
            float timeToKillAvg = (float)playerController.timeToKillEnemyCumulative / (float)playerController.roundKills;
            double avgspikeDurationCumulative = 0;
            if (playerController.perRoundAimSpikeCount + playerController.perRoundReloadSpikeCount + playerController.perRoundMouseMovementSpikeCount > 0)
                avgspikeDurationCumulative = (float)playerController.spikeDurationCumulative / (float)(playerController.perRoundAimSpikeCount + playerController.perRoundReloadSpikeCount + playerController.perRoundMouseMovementSpikeCount);
            double avgFT = frametimeCumulativeRound / roundFrameCount;
            double avgFPS = 1 / avgFT;

            String roundLogLine =
                sessionID.ToString() + "," +
                latinRow.ToString() + "," +
                currentRoundNumber.ToString() + "," +
                sessionStartTime.ToString() + "," +
                System.DateTime.Now.ToString() + "," +
                roundConfigs.roundFPS[indexArray[currentRoundNumber - 1]].ToString() + "," +
                roundConfigs.spikeMagnitude[indexArray[currentRoundNumber - 1]].ToString() + "," +
                roundConfigs.onAimSpikeEnabled[indexArray[currentRoundNumber - 1]].ToString() + "," +
                roundConfigs.onEnemySpawnSpikeEnabled[indexArray[currentRoundNumber - 1]].ToString() + "," +
                roundConfigs.onMouseSpikeEnabled[indexArray[currentRoundNumber - 1]].ToString() + "," +
                roundConfigs.onReloadSpikeEnabled[indexArray[currentRoundNumber - 1]].ToString() + "," +
                roundConfigs.attributeScalingEnabled[indexArray[currentRoundNumber - 1]].ToString() + "," +
                roundConfigs.attributeScaleRadius[indexArray[currentRoundNumber - 1]].ToString() + "," +
                roundConfigs.enemyLateralMoveSpeed[indexArray[currentRoundNumber - 1]].ToString() + "," +
                roundConfigs.predictableEnemyMovement[indexArray[currentRoundNumber - 1]].ToString() + "," +
                indexArray[currentRoundNumber - 1].ToString() + "," +
                playerController.score + "," +
                playerController.shotsFiredPerRound + "," +
                playerController.shotsHitPerRound + "," +
                playerController.headshotsHitPerRound + "," +
                playerController.reloadCountPerRound + "," +
                playerController.tacticalReloadCountPerRound + "," +
                accuracy.ToString() + "," +
                playerController.roundKills + "," +
                playerController.roundDeaths + "," +
                playerController.distanceTravelledPerRound + "," +
                playerController.delXCumilative.ToString() + "," +
                playerController.delYCumilative.ToString() + "," +
                (playerController.delXCumilative + playerController.delYCumilative).ToString() + "," +
                frametimeCumulativeRound.ToString() + "," +
                roundFrameCount.ToString() + "," +
                avgFT.ToString() + "," +
                avgFPS.ToString() + "," +
                playerController.perRoundAimSpikeCount.ToString() + "," +
                playerController.perRoundReloadSpikeCount.ToString() + "," +
                playerController.perRoundMouseMovementSpikeCount.ToString() + "," +
                playerController.spikeDurationCumulative.ToString() + "," +
                avgspikeDurationCumulative.ToString() + "," +
                playerController.perRoundEnemySpawnSpikeCount.ToString() + "," +
                playerController.degreeToShootXCumulative.ToString() + "," +
                playerController.degreeToTargetXCumulative.ToString() + "," +
                playerController.minAnlgeToEnemyCumulative.ToString() + "," +
                playerController.enemySizeCumulative.ToString() + "," +
                playerController.timeToTargetEnemyCumulative.ToString() + "," +
                playerController.timeToHitEnemyCumulative.ToString() + "," +
                playerController.timeToKillEnemyCumulative.ToString() + "," +
                degXShootAvg.ToString() + "," +
                degXTargetAvg.ToString() + "," +
                enemySizeOnSpawnAvg.ToString() + "," +
                playerController.aimDurationPerRound.ToString() + "," +
                playerController.isFiringDurationPerRound.ToString() + "," +
                qoeValue.ToString() + "," +
                acceptabilityValue.ToString();
            textWriter.WriteLine(roundLogLine);
        }
    }


    public void LogPlayerData()
    {
        PlayerStats stats = playerController.gameObject.GetComponent<PlayerStats>();

        string filenamePerRound = "Data\\Logs\\PlayerData_" + fileNameSuffix + "_" + sessionID + "_" + ".csv";
        bool fileExists = System.IO.File.Exists(filenamePerRound);

        using (var textWriter = new StreamWriter(filenamePerRound, append: true))
        {
            if (!fileExists)
            {
                string header =
                    "sessionID,latinRow,currentRoundNumber,roundFPS,spikeMagnitude," +
                    "onAimSpikeEnabled,onEnemySpawnSpikeEnabled,onMouseSpikeEnabled,onReloadSpikeEnabled,attributeScalingEnabled," + "attributeScaleRadius," + "enemyMoveSpeed," + "predictableEnemyMovement," +
                    "indexArray,roundTimer,time,mouseX,mouseY,playerX,playerY,playerZ," +
                    "scorePerSec,playerRot_w,playerRot_x,playerRot_y,playerRot_z," +
                    "enemyPos_x,enemyPos_y,enemyPos_z," +
                    "isADS,frameTimeMS,muzzleX,muzzleY,muzzleZ,playerOnTarget,onTargetDuration,targetAngleToEnemy,validMissContinious";
                textWriter.WriteLine(header);
            }

            var log = playerController.playerTickLog;
            int tickCount = log.mouseX.Count;

            for (int i = 0; i < tickCount; i++)
            {
                string tickLogLine =
                    sessionID.ToString() + "," +
                    latinRow.ToString() + "," +
                    currentRoundNumber.ToString() + "," +
                    roundConfigs.roundFPS[indexArray[currentRoundNumber - 1]].ToString() + "," +
                    roundConfigs.spikeMagnitude[indexArray[currentRoundNumber - 1]].ToString() + "," +
                    roundConfigs.onAimSpikeEnabled[indexArray[currentRoundNumber - 1]].ToString() + "," +
                    roundConfigs.onEnemySpawnSpikeEnabled[indexArray[currentRoundNumber - 1]].ToString() + "," +
                    roundConfigs.onMouseSpikeEnabled[indexArray[currentRoundNumber - 1]].ToString() + "," +
                    roundConfigs.onReloadSpikeEnabled[indexArray[currentRoundNumber - 1]].ToString() + "," +
                    roundConfigs.attributeScalingEnabled[indexArray[currentRoundNumber - 1]].ToString() + "," +
                    roundConfigs.attributeScaleRadius[indexArray[currentRoundNumber - 1]].ToString() + "," +
                    roundConfigs.enemyLateralMoveSpeed[indexArray[currentRoundNumber - 1]].ToString() + "," +
                    roundConfigs.predictableEnemyMovement[indexArray[currentRoundNumber - 1]].ToString() + "," +
                    indexArray[currentRoundNumber - 1].ToString() + "," +
                    log.roundTimer[i].ToString() + "," +
                    log.time[i].ToString() + "," +
                    log.mouseX[i].ToString() + "," +
                    log.mouseY[i].ToString() + "," +
                    log.playerX[i].ToString() + "," +
                    log.playerY[i].ToString() + "," +
                    log.playerZ[i].ToString() + "," +
                    log.scorePerSec[i].ToString() + "," +
                    log.playerRot[i].w.ToString() + "," +
                    log.playerRot[i].x.ToString() + "," +
                    log.playerRot[i].y.ToString() + "," +
                    log.playerRot[i].z.ToString() + "," +
                    log.enemyPos[i].x.ToString() + "," +
                    log.enemyPos[i].y.ToString() + "," +
                    log.enemyPos[i].z.ToString() + "," +
                    log.isADS[i].ToString() + "," +
                    log.frameTimeMS[i].ToString() + "," +
                    log.muzzleX[i].ToString() + "," +
                    log.muzzleY[i].ToString() + "," +
                    log.muzzleZ[i].ToString() + "," +
                    log.playerOnTarget[i].ToString() + "," +
                    log.onTargetDuration[i].ToString() + "," +
                    log.targetAngleToEnemy[i].ToString() + "," +
                    log.validMissContinious[i].ToString();

                textWriter.WriteLine(tickLogLine);
            }
        }
    }

    public void LogPerShotData()
    {
        PlayerStats stats = playerController.gameObject.GetComponent<PlayerStats>();

        string filenamePerRound = "Data\\Logs\\ShotData_" + fileNameSuffix + "_" + sessionID + "_" + ".csv";
        bool fileExists = System.IO.File.Exists(filenamePerRound);

        using (var textWriter = new StreamWriter(filenamePerRound, append: true))
        {
            if (!fileExists)
            {
                // Write header first, only once
                string header =
                "sessionID,latinRow,currentRoundNumber,roundFPS,spikeMagnitude," +
                "onAimSpikeEnabled,onEnemySpawnSpikeEnabled,onMouseSpikeEnabled,onReloadSpikeEnabled,attributeScalingEnabled," + "attributeScaleRadius," + "enemyMoveSpeed," + "predictableEnemyMovement," +
                "indexArray,roundTimer,time,mouseX,mouseY,playerX,playerY,playerZ," +
                "playerRot_w,playerRot_x,playerRot_y,playerRot_z," +
                "enemyPos_x,enemyPos_y,enemyPos_z," +
                "isHit,currentAccuracy,currentScore,missAngle,distanceToPlayer," +
                "currentLocalRadius,currentWorldRadius,neededLocalRadius,neededWorldRadius,extraRadiusWorld," +
                "currentMouseSpeed,avgMouseSpeed,isADS,enemyInView,timeTillLastKill," +
                "validMiss,isFirstShotAfterSpike,shotCountAfterSpike,postSpikeFirstShotHits,postSpikeFirstShotMisses," +
                "postSpikeFirstShotAccuracy,elapsedTimeFromLastSpike, elapsedTimeSinceLastEventIncludingSpike, elapsedTimeSinceLastEventExcludingSpike,horizontalMissAngle,verticalMissAngle,isEnemyInFront";
                textWriter.WriteLine(header);
            }

            var log = playerController.perShotLog;
            int shotCount = log.mouseX.Count;

            for (int i = 0; i < shotCount; i++)
            {
                String shotLogLine =
                   sessionID.ToString() + "," +
                   latinRow.ToString() + "," +
                   currentRoundNumber.ToString() + "," +
                   roundConfigs.roundFPS[indexArray[currentRoundNumber - 1]].ToString() + "," +
                   roundConfigs.spikeMagnitude[indexArray[currentRoundNumber - 1]].ToString() + "," +
                   roundConfigs.onAimSpikeEnabled[indexArray[currentRoundNumber - 1]].ToString() + "," +
                   roundConfigs.onEnemySpawnSpikeEnabled[indexArray[currentRoundNumber - 1]].ToString() + "," +
                   roundConfigs.onMouseSpikeEnabled[indexArray[currentRoundNumber - 1]].ToString() + "," +
                   roundConfigs.onReloadSpikeEnabled[indexArray[currentRoundNumber - 1]].ToString() + "," +
                   roundConfigs.attributeScalingEnabled[indexArray[currentRoundNumber - 1]].ToString() + "," +
                   roundConfigs.attributeScaleRadius[indexArray[currentRoundNumber - 1]].ToString() + "," +
                   roundConfigs.enemyLateralMoveSpeed[indexArray[currentRoundNumber - 1]].ToString() + "," +
                   roundConfigs.predictableEnemyMovement[indexArray[currentRoundNumber - 1]].ToString() + "," +
                   indexArray[currentRoundNumber - 1].ToString() + "," +
                   log.roundTimer[i].ToString() + "," +
                   log.time[i].ToString() + "," +
                   log.mouseX[i].ToString() + "," +
                   log.mouseY[i].ToString() + "," +
                   log.playerX[i].ToString() + "," +
                   log.playerY[i].ToString() + "," +
                   log.playerZ[i].ToString() + "," +
                   log.playerRot[i].w.ToString() + "," +
                   log.playerRot[i].x.ToString() + "," +
                   log.playerRot[i].y.ToString() + "," +
                   log.playerRot[i].z.ToString() + "," +
                   log.enemyPos[i].x.ToString() + "," +
                   log.enemyPos[i].y.ToString() + "," +
                   log.enemyPos[i].z.ToString() + "," +
                   log.isHit[i].ToString() + "," +
                   log.currentAccuracy[i].ToString() + "," +
                   log.currentScore[i].ToString() + "," +
                   log.missAngle[i].ToString() + "," +
                   log.distanceToPlayer[i].ToString() + "," +
                   log.currentLocalRadius[i].ToString() + "," +
                   log.currentWorldRadius[i].ToString() + "," +
                   log.neededLocalRadius[i].ToString() + "," +
                   log.neededWorldRadius[i].ToString() + "," +
                   log.extraRadiusWorld[i].ToString() + "," +
                   log.currentMouseSpeed[i].ToString() + "," +
                   log.avgMouseSpeed[i].ToString() + "," +
                   log.isADS[i].ToString() + "," +
                   log.enemyInView[i].ToString() + "," +
                   log.timeTillLastKill[i].ToString() + "," +
                   log.validMiss[i].ToString() + "," +
                   log.isFirstShotAfterSpike[i].ToString() + "," +
                   log.shotCountAfterSpike[i].ToString() + "," +
                   log.postSpikeFirstShotHits[i].ToString() + "," +
                   log.postSpikeFirstShotMisses[i].ToString() + "," +
                   log.postSpikeFirstShotAccuracy[i].ToString() + "," +
                   log.elapsedTimeFromLastSpike[i].ToString() + "," +
                   log.elapsedTimeSinceLastEventIncludingSpike[i].ToString() + "," +
                   log.elapsedTimeSinceLastEventExcludingSpike[i].ToString() + "," +
                   log.horizontalMissAngle[i].ToString() + "," +
                   log.verticalMissAngle[i].ToString() + "," +
                   log.isEnemyInFront[i].ToString();
                textWriter.WriteLine(shotLogLine);
            }
        }
    }


    public void LogPerShootingEventData()
    {
        PlayerStats stats = playerController.gameObject.GetComponent<PlayerStats>();

        string filenamePerEvent = "Data\\Logs\\ShootingEventData_" + fileNameSuffix + "_" + sessionID + "_" + ".csv";
        bool fileExists = System.IO.File.Exists(filenamePerEvent);

        using (var textWriter = new StreamWriter(filenamePerEvent, append: true))
        {
            // Write header only if file doesn't exist
            if (!fileExists)
            {
                string header =
                 "sessionID,latinRow,currentRoundNumber,roundFPS,spikeMagnitude," +
                 "onAimSpikeEnabled,onEnemySpawnSpikeEnabled,onMouseSpikeEnabled,onReloadSpikeEnabled,attributeScalingEnabled," + "attributeScaleRadius," + "enemyMoveSpeed," + "predictableEnemyMovement," +
                 "indexArray,time,roundTimer,mouseX,mouseY,playerX,playerY,playerZ," +
                 "playerRot_w,playerRot_x,playerRot_y,playerRot_z," +
                 "enemyPos_x,enemyPos_y,enemyPos_z," +
                 "muzzlePos_x,muzzlePos_y,muzzlePos_z,isEnemyDead," +  
                 "distanceToPlayer,mouseMovedX,mouseMovedY," +
                 "shotsFiredPerEvent,shotsHitPerEvent,shotsMissedPerEvent," +
                 "durationOfEvent,durationOfEventIncludingSpikes,spikeCountPerEvent,angularDistanceFromEnemyOnStart,targetTimeOnEnemy";

                textWriter.WriteLine(header);
            }

            var log = playerController.shootingEventLog;
            int eventCount = log.time.Count;

            for (int i = 0; i < eventCount; i++)
            {
                string eventLogLine =
                    sessionID.ToString() + "," +
                    latinRow.ToString() + "," +
                    currentRoundNumber.ToString() + "," +
                    roundConfigs.roundFPS[indexArray[currentRoundNumber - 1]].ToString() + "," +
                    roundConfigs.spikeMagnitude[indexArray[currentRoundNumber - 1]].ToString() + "," +
                    roundConfigs.onAimSpikeEnabled[indexArray[currentRoundNumber - 1]].ToString() + "," +
                    roundConfigs.onEnemySpawnSpikeEnabled[indexArray[currentRoundNumber - 1]].ToString() + "," +
                    roundConfigs.onMouseSpikeEnabled[indexArray[currentRoundNumber - 1]].ToString() + "," +
                    roundConfigs.onReloadSpikeEnabled[indexArray[currentRoundNumber - 1]].ToString() + "," +
                    roundConfigs.attributeScalingEnabled[indexArray[currentRoundNumber - 1]].ToString() + "," +
                    roundConfigs.attributeScaleRadius[indexArray[currentRoundNumber - 1]].ToString() + "," +
                    roundConfigs.enemyLateralMoveSpeed[indexArray[currentRoundNumber - 1]].ToString() + "," +
                    roundConfigs.predictableEnemyMovement[indexArray[currentRoundNumber - 1]].ToString() + "," +
                    indexArray[currentRoundNumber - 1].ToString() + "," +
                    log.time[i].ToString() + "," +
                    log.roundTimer[i].ToString() + "," +
                    log.mouseX[i].ToString() + "," +
                    log.mouseY[i].ToString() + "," +
                    log.playerX[i].ToString() + "," +
                    log.playerY[i].ToString() + "," +
                    log.playerZ[i].ToString() + "," +
                    log.playerRot[i].w.ToString() + "," +
                    log.playerRot[i].x.ToString() + "," +
                    log.playerRot[i].y.ToString() + "," +
                    log.playerRot[i].z.ToString() + "," +
                    log.enemyPos[i].x.ToString() + "," +
                    log.enemyPos[i].y.ToString() + "," +
                    log.enemyPos[i].z.ToString() + "," +
                    log.muzzlePos[i].x.ToString() + "," +
                    log.muzzlePos[i].y.ToString() + "," +
                    log.muzzlePos[i].z.ToString() + "," +
                    log.isEnemyDead[i].ToString() + "," +
                    log.distanceToPlayer[i].ToString() + "," +
                    log.mouseMovedX[i].ToString() + "," +
                    log.mouseMovedY[i].ToString() + "," +
                    log.shotsFiredPerEvent[i].ToString() + "," +
                    log.shotsHitPerEvent[i].ToString() + "," +
                    log.shotsMissedPerEvent[i].ToString() + "," +
                    log.durationOfEventNotIncludingSpikes[i].ToString() + "," +
                    log.durationOfEventIncludingSpikes[i].ToString() + "," +                                                            
                    log.spikeCountPerEvent[i].ToString() + "," +
                    log.angularDistanceFromEnemyOnStart[i].ToString() + "," +
                    log.targetTimeOnEnemy[i].ToString();

                textWriter.WriteLine(eventLogLine);
            }
        }
    }


}
