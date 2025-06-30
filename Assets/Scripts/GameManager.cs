using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Profiling.LowLevel.Unsafe;
using Demo.Scripts.Runtime;

public class GameManager : MonoBehaviour
{

    // Frame Feed
    long frameStartTime;
    List<float> frames = new List<float>();
    List<float> delayDurationList = new List<float>();
    int delayDurationIndex;

    bool isFrameFeeding;
    int frameCount;
    public TMPro.TMP_Text frameCountText;
    float frameTimeCumulative;
    long lastWaitTime;
    long nowAfterLoop;
    long lastFrameOverWait;

    // Event Based
    public bool isEventBasedDelay;
    public float delayDuration; //ms

    public float fixedFtDelayDuration; //ns

    public bool isFixedFT;

    long actualWaitDuration = 0;

    public FPSController playerController;

    public bool isFrameAfterSpike;

    // Start is called before the first frame update
    void Start()
    {
        ReadCSV();
        isFrameFeeding = false;
        isFixedFT = true;
        delayDurationIndex = 0;
        isFrameAfterSpike = false;
    }

    // Update is called once per frame
    void Update()
    {
        frameStartTime = ProfilerUnsafeUtility.Timestamp;
        //nowAfterLoop = ProfilerUnsafeUtility.Timestamp;
        if (Input.GetKeyDown(KeyCode.ScrollLock))
        {
            isFrameFeeding = true;
            frameCount = 0;
            frameTimeCumulative = 0;
            lastWaitTime = 0;
            lastFrameOverWait = 0;
            nowAfterLoop = 0;
        }

        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            //isEventBasedDelay = true;
        }

        /*if (frameCount > 0)
        {
            frameTimeCumulative += Time.deltaTime * 1000;
        }*/
    }

    
    private void LateUpdate()
    {
        // Simple
        //frameCountText.text = frameCount.ToString();
        /*if (frameCount >= frames.Count)
            Application.Quit();*/

        /*while (isFrameFeeding)
        {
            //Regular
            if (ElapsedNanoseconds(frameStartTime) >= frames[frameCount] * 1000000)
            {
                frameCount++;
                return;
            }
        }
        */
        while (isEventBasedDelay)
        {
            actualWaitDuration = ElapsedNanoseconds(frameStartTime);
            if (actualWaitDuration >= delayDuration * 1000000)
            {
                playerController.spikeDurationCumulative += actualWaitDuration/ 1000000.0;
                isEventBasedDelay = false;
                playerController.UpdatePlayerLog(actualWaitDuration / 1000000.0);
                isFrameAfterSpike = true;
                return;
            }
        }

        if (playerController.isPlayerReady && playerController.isQoeDisabled && playerController.isAcceptabilityDisabled && !isFrameAfterSpike)
        {
            playerController.UpdatePlayerLog(Time.deltaTime * 1000.0);
        }

        isFrameAfterSpike = false;


        /*while (isFixedFT)
        {
            if (ElapsedNanoseconds(frameStartTime) >= fixedFtDelayDuration)
            {
                return;
            }
        }*/


        /*if (isFrameFeeding)
        {
            var conversionRatio = ProfilerUnsafeUtility.TimestampToNanosecondsConversionRatio;
            long cumulativeTime = (nowAfterLoop - lastWaitTime) * conversionRatio.Numerator / conversionRatio.Denominator;

            long duration = (long)frames[frameCount] * 1000000;


            long desiredWaitTime = (long)Math.Max(0, duration - cumulativeTime);
            OnWait((long)(Math.Max(0, desiredWaitTime - lastFrameOverWait) * 0.97));

            frameCount++;
            frameCountText.text = frameCount.ToString();
            // Update wait timers
            conversionRatio = ProfilerUnsafeUtility.TimestampToNanosecondsConversionRatio;
            lastWaitTime = ProfilerUnsafeUtility.Timestamp;
            long actualWaitTime = (lastWaitTime - nowAfterLoop) * conversionRatio.Numerator / conversionRatio.Denominator;

            // Learn how much onWait appears to overshoot by and compensate
            long thisOverWait = actualWaitTime - desiredWaitTime;
            if (Math.Abs(thisOverWait - lastFrameOverWait) / Math.Max(Math.Abs(lastFrameOverWait), Math.Abs(thisOverWait)) > 0.4)
            {
                // Abruptly change our estimate
                lastFrameOverWait = thisOverWait;
            }
            else
            {
                // Smoothly change our estimate
                lastFrameOverWait = (long)Mathf.Lerp(lastFrameOverWait, thisOverWait, .1f);
            }

           *//* Debug.Log("lastFrameOverWait : " + lastFrameOverWait);
            Debug.Log("thisOverWait : " + thisOverWait);
            Debug.Log("desiredWaitTime : " + desiredWaitTime);
            Debug.Log("duration : " + duration);
            Debug.Log("cumulativeTime : " + cumulativeTime);
            Debug.Log("conversionRatio : " + conversionRatio);
            Debug.Log("actualWaitTime : " + actualWaitTime);
            Debug.Log("nowAfterLoop : " + nowAfterLoop);
            Debug.Log("lastWaitTime : " + lastWaitTime);
            Debug.Log("(long)(Math.Max(0, desiredWaitTime - lastFrameOverWait) * 0.97)" + (long)(Math.Max(0, desiredWaitTime - lastFrameOverWait) * 0.97));*//*
        }*/
    }

    public void OnWait(long waitTime)
    {
        while (ElapsedNanoseconds(nowAfterLoop) < waitTime)
            //Debug.Log("ElapsedNanoseconds : " + ElapsedNanoseconds(nowAfterLoop));
        ;
    }

    public void ReadCSV()
    {
        string line = null;
        frames.Clear();
        StreamReader strReader = new StreamReader("Data\\FrametimerFeedSine.csv");
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
                frames.Add(float.Parse(line));
            }
        }

        delayDurationList.Clear();
        strReader = new StreamReader("Data\\DelayDuration.csv");
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
                delayDurationList.Add(float.Parse(line));
            }
        }
    }

    public static long ElapsedNanoseconds(long startTimestamp)
    {
        long now = ProfilerUnsafeUtility.Timestamp;
        var conversionRatio = ProfilerUnsafeUtility.TimestampToNanosecondsConversionRatio;
        return (now - startTimestamp) * conversionRatio.Numerator / conversionRatio.Denominator;
    }
}
