using Demo.Scripts.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{

    public Image highAlertTop;
    public Image highAlertBottom;
    public Image highAlertLeft;
    public Image highAlertRight;

    public Image hitMarkerImage;

    public TMPro.TMP_Text ammoText;
    public TMPro.TMP_Text healthText;
    public TMPro.TMP_Text scoreText;
    public TMPro.TMP_Text durationText;

    public TMPro.TMP_Text killsText;
    public TMPro.TMP_Text deathsText;

    public TMPro.TMP_Text roundText;

    public GameObject enemyRespawningText;

    public GameObject player;

    public RoundManager roundManager;

    public EnemyManager enemyManager;

    public bool highAlertBlinkOn;

    float highAlertBlinkerValue;

    public AudioClip beepSFX;

    bool beeperFuse;

    public GameObject readyText;

    public GameObject qoeSliderGO;
    public Slider qoeSlider;
    public TMPro.TMP_Text sliderText;
    public GameObject qoeSubmitGO;

    public GameManager gameManager;
    public TMPro.TMP_Text lagText;

    float sliderHandleRed;
    float sliderHandleGreen;
    public Image SliderHandle;

    public GameObject acceptabilityGO;

    public DynamicCrosshair crosshairScript; // Assign in Inspector
    // Start is called before the first frame update
    void Start()
    {
        beeperFuse = true;
        crosshairScript = GetComponent<DynamicCrosshair>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateHitMarker();

        UpdateUITexts();

        UpdateHighAlert();
    }

    void UpdateHighAlert()
    {

        GameObject enemy = enemyManager.GetClosestEnemy();

        if (player.GetComponent<FPSController>().deathTimeOut > 0)
        {
            highAlertTop.gameObject.SetActive(true);
            highAlertBottom.gameObject.SetActive(true);
            highAlertLeft.gameObject.SetActive(true);
            highAlertRight.gameObject.SetActive(true);

            float distance = player.GetComponent<FPSController>().deathTimeOut;

            highAlertTop.rectTransform.localScale = new Vector3(distance, 1, 1f);
            highAlertBottom.rectTransform.localScale = new Vector3(distance, 1, 1);
            highAlertLeft.rectTransform.localScale = new Vector3(distance, 1, 1);
            highAlertRight.rectTransform.localScale = new Vector3(distance, 1, 1);

            highAlertTop.color = new Color(1, 0, 0, 1);
            highAlertBottom.color = new Color(1, 0, 0, 1);
            highAlertLeft.color = new Color(1, 0, 0, 1);
            highAlertRight.color = new Color(1, 0, 0, 1);
        }


        else if (enemy != null)
        {

            /*var relativePoint = player.transform.InverseTransformPoint(enemy.transform.position);

            float distance = Vector3.Distance(player.transform.position, enemy.transform.position);

            SetHighAlertAlpha(distance / 2, 0, enemy.GetComponent<AudioSource>());


            if (relativePoint.x < 0.0 && relativePoint.z > 0.0) // Front left
            {
                if (Math.Abs(relativePoint.x) > Math.Abs(relativePoint.z))
                {
                    highAlertLeft.gameObject.SetActive(true);
                    highAlertTop.gameObject.SetActive(false);
                }
                else
                {
                    highAlertLeft.gameObject.SetActive(false);
                    highAlertTop.gameObject.SetActive(true);
                }

                highAlertBottom.gameObject.SetActive(false);
                highAlertRight.gameObject.SetActive(false);
            }
            else if (relativePoint.x > 0.0 && relativePoint.z > 0.0) // front right
            {
                if (Math.Abs(relativePoint.x) > Math.Abs(relativePoint.z))
                {
                    highAlertRight.gameObject.SetActive(true);
                    highAlertTop.gameObject.SetActive(false);
                }
                else
                {
                    highAlertRight.gameObject.SetActive(false);
                    highAlertTop.gameObject.SetActive(true);
                }

                highAlertLeft.gameObject.SetActive(false);
                highAlertBottom.gameObject.SetActive(false);
            }

            else if (relativePoint.x < 0.0 && relativePoint.z < 0.0) // Back left
            {

                if (Math.Abs(relativePoint.x) > Math.Abs(relativePoint.z))
                {
                    highAlertLeft.gameObject.SetActive(true);
                    highAlertBottom.gameObject.SetActive(false);
                }
                else
                {
                    highAlertLeft.gameObject.SetActive(false);
                    highAlertBottom.gameObject.SetActive(true);
                }

                highAlertTop.gameObject.SetActive(false);
                highAlertRight.gameObject.SetActive(false);
            }
            else if (relativePoint.x > 0.0 && relativePoint.z < 0.0) // Back right
            {
                if (Math.Abs(relativePoint.x) > Math.Abs(relativePoint.z))
                {
                    highAlertRight.gameObject.SetActive(true);
                    highAlertBottom.gameObject.SetActive(false);
                }
                else
                {
                    highAlertRight.gameObject.SetActive(false);
                    highAlertBottom.gameObject.SetActive(true);
                }

                highAlertLeft.gameObject.SetActive(false);
                highAlertTop.gameObject.SetActive(false);

            }*/

            enemyRespawningText.SetActive(false);
        }
        else
        {
            highAlertTop.gameObject.SetActive(false);
            highAlertBottom.gameObject.SetActive(false);
            highAlertLeft.gameObject.SetActive(false);
            highAlertRight.gameObject.SetActive(false);

            enemyRespawningText.SetActive(true);
        }
    }
    void UpdateUITexts()
    {
        var fps = player.GetComponent<FPSController>();
        ammoText.text = "" + fps.GetGun().currentAmmoCount;
        healthText.text = "      " + (fps.liveAccuracy * 100.0f).ToString("##");
        scoreText.text = "Score: " + fps.score;
        durationText.text = "Duration: " + roundManager.roundTimer.ToString("###");

        killsText.text = "Kills: " + fps.roundKills;
        deathsText.text = "Deaths: " + fps.roundDeaths;


        lagText.text =
    "MissAngle: " + fps.missAnglePublic.ToString("F2") +
    " | H: " + fps.horizontalMissAnglePublic.ToString("F2") +
    " | V: " + fps.verticalMissAnglePublic.ToString("F2") +
    " | InFront: " + fps.enemyIsInFrontPublic +
    " | VM: " + fps.validMissPublic +
    "\nWorldRadReq: " + fps.worldRadRequiredPublic.ToString("F2") +

    "WorldRadInit: " + fps.worldRadiusPublic.ToString("F2") +

    " | LocalRadReq: " + fps.localRadRequiredPublic.ToString("F2") +
    " | ExtraRad: " + fps.extraRadRequiredPublic.ToString("F2") +
    " | TOTEnemy: " + fps.timeOnTargetEachEnemy.ToString("F2") +
    " | TOTEvent: " + fps.timeOnTargetPerEvent.ToString("F2") +
    " | SurfDist: " + fps.distanceFromEnemyPublic.ToString("F2");



        if (!player.GetComponent<FPSController>().isPlayerReady)
            readyText.SetActive(true);
        else
            readyText.SetActive(false);

        qoeSliderGO.gameObject.SetActive(!player.GetComponent<FPSController>().isQoeDisabled);

        if (qoeSliderGO.activeSelf)
        {
            if (roundManager.currentRoundNumber <= roundManager.totalRoundNumber)
            {
                if (roundManager.currentRoundNumber == 1)// !roundManager.isFTStudy)
                    roundText.text = "Round\n " + roundManager.currentRoundNumber + "/" + roundManager.totalRoundNumber + "\nSmoothest Practice Round";
                else if (roundManager.currentRoundNumber == 2)// && !roundManager.isFTStudy)
                    roundText.text = "Round\n " + roundManager.currentRoundNumber + "/" + roundManager.totalRoundNumber + "\nChoppiest Practice Round";
                /* else if (roundManager.currentRoundNumber == 1 && roundManager.isFTStudy)
                     roundText.text = "Round\n " + roundManager.currentRoundNumber + "/" + roundManager.totalRoundNumber + "\n Practice Round";*/
                else
                    roundText.text = "Round\n " + roundManager.currentRoundNumber + "/" + roundManager.totalRoundNumber;
            }
            else
                roundText.text = "Thank You!";

            sliderText.text = (qoeSlider.value / 10.0).ToString("#.#");
            /*if (qoeSlider.value != 3.000f)
                qoeSubmitGO.SetActive(true);
            else
                qoeSubmitGO.SetActive(false);*/

            if (qoeSlider.value <= 30)
            {
                sliderHandleRed = 1; // From 10 to 30, red is full
            }
            else
            {
                sliderHandleRed = (50 - qoeSlider.value) / 20; // Decreases to 0 by the time it reaches 50
            }

            // sliderHandleGreen scaling
            if (qoeSlider.value <= 30)
            {
                sliderHandleGreen = (qoeSlider.value - 10) / 20; // Increases to 1 by the time it reaches 30
            }
            else
            {
                sliderHandleGreen = 1; // Remains full from 30 to 50
            }

            SliderHandle.color = new Color(sliderHandleRed, sliderHandleGreen, 0, 1);
        }
        else
        {
            roundText.text = "";
        }
    }

    void UpdateHitMarker()
    {
        // Determine which hitmarker color to show as before
        bool showHit = false;

        var fpsController = player.GetComponent<FPSController>();
        if (fpsController.killCooldown > 0)
        {
            hitMarkerImage.color = new Color(1, 0, 0, 1);
            showHit = true;
        }
        else if (fpsController.headshotCooldown > 0)
        {
            hitMarkerImage.color = new Color(1, 1, 0, 1);
            showHit = true;
        }
        else if (fpsController.regularHitCooldown > 0)
        {
            hitMarkerImage.color = new Color(1, 1, 1, 1);
            showHit = true;
        }
        else
        {
            hitMarkerImage.color = new Color(0, 0, 0, 0);
        }

        // --- NEW: Sync hitmarker UI to crosshair ---
        if (showHit && crosshairScript != null && hitMarkerImage != null)
        {
            // Get the latest screen-space position
            Vector2 screenPos = crosshairScript.CrosshairScreenPosition;

            // Convert to anchored position (assuming Screen Space - Overlay, anchor center)
            Vector2 anchoredPos = new Vector2(
                screenPos.x - Screen.width / 2f,
                screenPos.y - Screen.height / 2f
            );
            hitMarkerImage.rectTransform.anchoredPosition = anchoredPos;
            hitMarkerImage.enabled = true;
        }
        else if (hitMarkerImage != null)
        {
            hitMarkerImage.enabled = false;
        }
    }

    void SetHighAlertAlpha(float distance, float takehitTimer, AudioSource source)
    {
        if (distance == 0)
            return;

        float green = 1 / (distance / 10);

        if (takehitTimer > 0)
        {
            green = 0;
        }

        float red = 1 / (distance / 10);

        float blue = 0;
        float alpha = 1 / (distance / 10);

        highAlertTop.rectTransform.localScale = new Vector3(1 / ((distance / 5) + 1), 1, 0.9f);
        highAlertBottom.rectTransform.localScale = new Vector3(1 / (((distance / 5) + 0.9f)), 1, 1);
        highAlertLeft.rectTransform.localScale = new Vector3(1 / (((distance / 5) + 0.9f)), 1, 1);
        highAlertRight.rectTransform.localScale = new Vector3(1 / (((distance / 5) + 0.9f)), 1, 1);

        /*highAlertTop.color = new Color(red, green, blue, alpha);
        highAlertBottom.color = new Color(red, green, blue, alpha);
        highAlertLeft.color = new Color(red, green, blue, alpha);
        highAlertRight.color = new Color(red, green, blue, alpha);*/

        if (!highAlertBlinkOn)
        {
            highAlertTop.color = new Color(red, 33, blue, 0);
            highAlertBottom.color = new Color(red, 33, blue, 0);
            highAlertLeft.color = new Color(red, 33, blue, 0);
            highAlertRight.color = new Color(red, 33, blue, 0);
            beeperFuse = true;
        }
        else
        {
            if (beeperFuse)
            {
                PlayBeepSFX(source);
                beeperFuse = false;
            }

            highAlertTop.color = new Color(red, green, blue, alpha);
            highAlertBottom.color = new Color(red, green, blue, alpha);
            highAlertLeft.color = new Color(red, green, blue, alpha);
            highAlertRight.color = new Color(red, green, blue, alpha);
        }



        highAlertBlinkerValue = Mathf.PingPong(Time.time, (distance / 1 + 0.1f)); // change 1 to 5

        //Debug.Log("blink:"+highAlertBlinkerValue);

        if (highAlertBlinkerValue > (distance / 10 + .05f))
            highAlertBlinkOn = true;
        else highAlertBlinkOn = false;
    }

    void PlayBeepSFX(AudioSource source)
    {
        source.volume = UnityEngine.Random.Range(.2f, .3f);
        source.pitch = 0.7f;

        source.PlayOneShot(beepSFX);
    }

    public void QOESubmitPressed()
    {
        roundManager.qoeValue = (float)qoeSlider.value / 10.0f;

        qoeSlider.value = 30.000f;
        qoeSubmitGO.SetActive(false);
        acceptabilityGO.SetActive(true);
        player.GetComponent<FPSController>().isAcceptabilityDisabled = false;
        player.GetComponent<FPSController>().isQoeDisabled = true;
        /*player.GetComponent<FPSController>().isPlayerReady = false;

        roundManager.currentRoundNumber++;
        
        if (roundManager.currentRoundNumber > roundManager.totalRoundNumber)
        {
            TextWriter textWriter = null;
            string filename = "Data\\Configs\\SessionID.csv";

            while (textWriter == null)
                textWriter = File.CreateText(filename);

            roundManager.sessionID++;

            if (roundManager.sessionID > 12)
                roundManager.sessionID = 1;

            textWriter.WriteLine(roundManager.sessionID);
            textWriter.Close();

            Application.Quit(); 
        }
        roundManager.SetRounConfig();
        player.GetComponent<FPSController>().ResetRound();
        enemyManager.spawnTimer = 0;*/
    }

    public void YesPressed()
    {
        roundManager.acceptabilityValue = true;

        roundManager.LogRoundData();
        roundManager.LogPlayerData();
        roundManager.LogPerShotData();
        roundManager.LogPerShootingEventData();

        acceptabilityGO.SetActive(false);
        player.GetComponent<FPSController>().isAcceptabilityDisabled = true;
        player.GetComponent<FPSController>().isPlayerReady = false;

        roundManager.currentRoundNumber++;

        if (roundManager.currentRoundNumber > roundManager.totalRoundNumber)
        {
            TextWriter textWriter = null;
            string filename = "Data\\Configs\\SessionID.csv";

            while (textWriter == null)
                textWriter = File.CreateText(filename);

            roundManager.sessionID++;

            textWriter.WriteLine(roundManager.sessionID);
            textWriter.Close();

            Application.Quit();
        }
        roundManager.SetRounConfig();
        player.GetComponent<FPSController>().ResetRound();
        enemyManager.spawnTimer = 0;
    }

    public void NoPressed()
    {
        roundManager.acceptabilityValue = false;

        roundManager.LogRoundData();
        roundManager.LogPlayerData();
        roundManager.LogPerShotData();
        roundManager.LogPerShootingEventData();

        acceptabilityGO.SetActive(false);
        player.GetComponent<FPSController>().isAcceptabilityDisabled = true;
        player.GetComponent<FPSController>().isPlayerReady = false;

        roundManager.currentRoundNumber++;

        if (roundManager.currentRoundNumber > roundManager.totalRoundNumber)
        {
            TextWriter textWriter = null;
            string filename = "Data\\Configs\\SessionID.csv";

            while (textWriter == null)
                textWriter = File.CreateText(filename);

            roundManager.sessionID++;

            /*if (roundManager.sessionID > roundManager.totalRoundNumber/2) // MUST CHANGE
                roundManager.sessionID = 1;*/

            textWriter.WriteLine(roundManager.sessionID);
            textWriter.Close();

            Application.Quit();
        }
        roundManager.SetRounConfig();
        player.GetComponent<FPSController>().ResetRound();
        enemyManager.spawnTimer = 0;
    }
}
