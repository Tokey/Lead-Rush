using Demo.Scripts.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Windows;

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

    public List<float> scorePerSec;
    public List<double> frameTimeMS;
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

        attributeScalingModule.useAttributeScaling = roundConfigs.attributeScalingEnabled[indexArray[currentRoundNumber - 1]];
        attributeScalingModule.attributeScalingDuration = roundConfigs.spikeMagnitude[indexArray[currentRoundNumber - 1]] / 1000.0f;
        roundFrameCount = 0;
        frametimeCumulativeRound = 0;

    }

    public void LogRoundData()
    {
        PlayerStats stats = playerController.gameObject.GetComponent<PlayerStats>();

        TextWriter textWriter = null;
        filenamePerRound = "Data\\Logs\\RoundData_" + fileNameSuffix + "_" + sessionID + "_" + ".csv";

        while (textWriter == null)
            textWriter = System.IO.File.AppendText(filenamePerRound);

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
           indexArray[currentRoundNumber - 1].ToString() + "," +
           playerController.score + "," +
           playerController.shotsFiredPerRound + "," +
           playerController.shotsHitPerRound + "," +
           playerController.headshotsHitPerRound + "," +
           playerController.realoadCountPerRound + "," +
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
           acceptabilityValue.ToString()
            ;
        textWriter.WriteLine(roundLogLine);
        textWriter.Close();
    }

    public void LogPlayerData()
    {
        PlayerStats stats = playerController.gameObject.GetComponent<PlayerStats>();

        TextWriter textWriter = null;
        filenamePerRound = "Data\\Logs\\PlayerData_" + fileNameSuffix + "_" + sessionID + "_" + ".csv";
        while (textWriter == null)
            textWriter = System.IO.File.AppendText(filenamePerRound);

        for (int i = 0; i < playerController.playerTickLog.mouseX.Count; i++)
        {
            String tickLogLine =
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
               indexArray[currentRoundNumber - 1].ToString() + "," +
               playerController.playerTickLog.roundTimer[i].ToString() + "," +
               playerController.playerTickLog.time[i].ToString() + "," +
               playerController.playerTickLog.mouseX[i].ToString() + "," +
               playerController.playerTickLog.mouseY[i].ToString() + "," +
               playerController.playerTickLog.playerX[i].ToString() + "," +
               playerController.playerTickLog.playerY[i].ToString() + "," +
               playerController.playerTickLog.playerZ[i].ToString() + "," +
               playerController.playerTickLog.scorePerSec[i].ToString() + "," +
               playerController.playerTickLog.playerRot[i].ToString() + "," +
               playerController.playerTickLog.enemyPos[i].ToString() + "," +
               playerController.playerTickLog.isADS[i].ToString() + "," +
               playerController.playerTickLog.frameTimeMS[i].ToString();

            textWriter.WriteLine(tickLogLine);
        }
        textWriter.Close();
    }

    public void LogPerShotData()
    {
        PlayerStats stats = playerController.gameObject.GetComponent<PlayerStats>();

        TextWriter textWriter = null;
        filenamePerRound = "Data\\Logs\\ShotData_" + fileNameSuffix + "_" + sessionID + "_" + ".csv";
        while (textWriter == null)
            textWriter = System.IO.File.AppendText(filenamePerRound);

        for (int i = 0; i < playerController.perShotLog.mouseX.Count; i++)
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
               indexArray[currentRoundNumber - 1].ToString() + "," +
               playerController.perShotLog.roundTimer[i].ToString() + "," +
               playerController.perShotLog.time[i].ToString() + "," +
               playerController.perShotLog.mouseX[i].ToString() + "," +
               playerController.perShotLog.mouseY[i].ToString() + "," +
               playerController.perShotLog.playerX[i].ToString() + "," +
               playerController.perShotLog.playerY[i].ToString() + "," +
               playerController.perShotLog.playerZ[i].ToString() + "," +
               playerController.perShotLog.playerRot[i].ToString() + "," +
               playerController.perShotLog.enemyPos[i].ToString() + "," +
               playerController.perShotLog.isHit[i].ToString() + "," +
               playerController.perShotLog.currentAccuracy[i].ToString() + "," +
               playerController.perShotLog.currentScore[i].ToString() + "," +
               playerController.perShotLog.missAngle[i].ToString() + "," +
               playerController.perShotLog.distanceToPlayer[i].ToString() + "," +
               playerController.perShotLog.currentLocalRadius[i].ToString() + "," +
               playerController.perShotLog.currentWorldRadius[i].ToString() + "," +
               playerController.perShotLog.neededLocalRadius[i].ToString() + "," +
               playerController.perShotLog.neededWorldRadius[i].ToString() + "," +
               playerController.perShotLog.extraRadiusWorld[i].ToString() + "," +
               playerController.perShotLog.currentMouseSpeed[i].ToString() + "," +
               playerController.perShotLog.avgMouseSpeed[i].ToString() + "," +
               playerController.perShotLog.isADS[i].ToString() + "," +
               playerController.perShotLog.enemyInView[i].ToString() + "," +
               playerController.perShotLog.timeTillLastKill[i].ToString() + "," +
               playerController.perShotLog.validMiss[i].ToString() + "," +
               playerController.perShotLog.isFirstShotAfterSpike[i].ToString() + "," +
               playerController.perShotLog.shotCountAfterSpike[i].ToString() + "," +
               playerController.perShotLog.postSpikeFirstShotHits[i].ToString() + "," +
               playerController.perShotLog.postSpikeFirstShotMisses[i].ToString() + "," +
               playerController.perShotLog.postSpikeFirstShotAccuracy[i].ToString() + "," +
               playerController.perShotLog.elapsedTimeFromLastSpike[i].ToString();
            textWriter.WriteLine(shotLogLine);
        }
        textWriter.Close();

    }
}
