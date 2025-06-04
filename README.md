# Lead Rush

**Lead Rush** is a research-focused first-person shooter (FPS) game, purpose-built for experimental studies on framerate, latency spikes, and gameplay interruptions. It runs at ultra-high framerates (up to 1500 Hz) and features detailed, timestamped logging of player and game events. The game is designed to support controlled experiments, allowing easy setup of rounds and conditions via configuration files.

## Gameplay Overview

- **Objective:** Eliminate as many enemies as possible before being eliminated. Each enemy takes five shots to kill.
- **Enemies:** One enemy is active at a time, charging at the player from preset spawn points.
- **Player Elimination:** Contact with an enemy results in death and instant respawn (enemies reset).
- **Feedback:** Visual (flashing yellow bars for enemy direction/proximity) and auditory (distinct hit/kill sounds) cues enhance awareness.
- **Rounds:** Each session consists of multiple rounds, each with distinct framerate and/or spike settings.
- **Playability:** After every round, players rate the round for Quality of Experience (QoE).

## Demo and Docs

- [Lead Rush Dev Demos (YouTube Playlist)](https://youtu.be/EdAh_PRoJXg?si=DN55bJwyah-BBgVH)
- [Log System & Configuration Docs (Google Doc)](https://docs.google.com/document/d/15LuFogEgv7Oa9zzsT2A_U-TKY9eGEhcx7yjS035GDqI/edit?usp=sharing)

---

## Configuration Files

All configuration files are located in `Data/Configs/`. They control the overall experiment setup, per-round conditions, and session tracking.

### 1. `GlobalConfig.csv`

Sets parameters that are fixed for the entire session.

| Column                   | Type    | Units/Values        | Description                               |
|--------------------------|---------|---------------------|-------------------------------------------|
| roundDuration            | float   | seconds             | Length of each round                      |
| isFTStudy                | bool    | TRUE/FALSE          | Fixed-timing study                        |
| aimSpikeDelay            | float   | ms                  | Delay for aim spikes                      |
| mouseSpikeDelay          | float   | ms                  | Delay for mouse spikes                    |
| mouseSpikeDegreeOfMovement| float  | degrees             | Mouse spike movement threshold            |
| enemySpeedGlobal         | float   | Unity units/second  | Enemy movement speed                      |
| enemyHealthGlobal        | float   | (unitless)          | Enemy health value                        |
| reticleSizeMultiplier    | float   | (multiplier)        | Reticle scaling                           |
| onHitScore               | int     | points              | Points for a hit                          |
| onMissScore              | int     | points              | Points for a miss                         |
| onKillScore              | int     | points              | Points for a kill                         |
| onDeathScore             | int     | points              | Points for a death                        |

---

### 2. `SessionID.csv`

- **Type:** Integer  
- **Purpose:** Tracks which row of the round configuration is used for the current session.  
- **Automatically increments after each session.**
- **Example Content:**  

---

### 3. Per-Round Condition: `RoundConfig.csv` + `LatinMap.csv`

#### `RoundConfig.csv`
- Each row: sequence of indices (int), one row per session (Latin square).
- Each value points to a row in `LatinMap.csv`.

#### `LatinMap.csv`
Defines each unique round condition.

| Column                      | Type    | Units/Values        | Description                               |
|-----------------------------|---------|---------------------|-------------------------------------------|
| roundFPS                    | float   | Hz                  | Target framerate                          |
| spikeMagnitude              | float   | ms                  | Simulated spike magnitude                 |
| onAimSpikeEnabled           | bool    | TRUE/FALSE          | Spike when aiming                         |
| onReloadSpikeEnabled        | bool    | TRUE/FALSE          | Spike on reload                           |
| onMouseSpikeEnabled         | bool    | TRUE/FALSE          | Spike on fast mouse movement              |
| onEnemySpawnSpikeEnabled    | bool    | TRUE/FALSE          | Spike on enemy spawn                      |
| attributeScalingEnabled     | bool    | TRUE/FALSE          | Enable attribute scaling                  |

**How It Works:**  
- The current session ID (from `SessionID.csv`) selects a row in `RoundConfig.csv`.
- Each value in that row is used as an index to `LatinMap.csv`, which contains the full per-round settings.
- This ensures each participant gets a counterbalanced sequence of experimental conditions.

---

## Logging System

All logs are stored in the `Data/Logs/` directory as CSV files.

### 1. Round Log (`RoundData_*.csv`)

- **One row per round (saved after QoE is submitted)**
- **Columns (most important fields):**

| Column                    | Type        | Units/Values         | Description                                  |
|---------------------------|-------------|----------------------|----------------------------------------------|
| sessionID                 | int         |                      | Session ID                                   |
| LatinRow                  | int         |                      | Row in round config (Latin square)           |
| currentRoundNumber        | int         |                      | Current round index                          |
| sessionStartTime          | string      | timestamp            | Session start time                           |
| currentTime               | string      | timestamp            | Log entry timestamp                          |
| roundFPS                  | float       | Hz                   | Target framerate                             |
| spikeMagnitude            | float       | ms                   | Simulated spike magnitude                    |
| onAimSpikeEnabled         | bool        | TRUE/FALSE           | Spike when aiming                            |
| onEnemySpawnSpikeEnabled  | bool        | TRUE/FALSE           | Spike on enemy spawn                         |
| onMouseSpikeEnabled       | bool        | TRUE/FALSE           | Spike on mouse movement                      |
| onReloadSpikeEnabled      | bool        | TRUE/FALSE           | Spike on reload                              |
| indexArray                | int         |                      | Config index after shuffle                   |
| score                     | int         |                      | Player score                                 |
| shotsFired                | int         |                      | Shots fired                                  |
| shotsHit                  | int         |                      | Shots hit                                    |
| headshots                 | int         |                      | Headshots                                    |
| reloadCount               | int         |                      | Reload count                                 |
| tacticalReloadCount       | int         |                      | Tactical reloads                             |
| accuracy                  | float       | ratio                | shotsHit/shotsFired                          |
| roundKills                | int         |                      | Kills                                        |
| roundDeaths               | int         |                      | Deaths                                       |
| distanceTravelledPerRound | float       | Unity units          | Distance travelled                           |
| delXCumilative            | float       |                      | Cumulative mouse X                           |
| delYCumilative            | float       |                      | Cumulative mouse Y                           |
| totalMouseMovement        | float       |                      | delXCumilative + delYCumilative              |
| frametimeCumulativeRound  | float       | ms                   | Total frametime                              |
| roundFrameCount           | int         |                      | Total frames                                 |
| avgFT                     | float       | ms                   | Average frametime                            |
| avgFPS                    | float       | Hz                   | Average FPS                                  |
| perRoundAimSpikeCount     | int         |                      | # spikes on aim                              |
| perRoundReloadSpikeCount  | int         |                      | # spikes on reload                           |
| perRoundMouseMovementSpikeCount | int   |                      | # spikes on mouse movement                   |
| perRoundEnemySpawnSpikeCount | int      |                      | # spikes on enemy spawn                      |
| spikeDurationCumulative   | float       | ms                   | Total duration of all spikes                 |
| avgSpikeDurationCumulative| float       | ms                   | Avg spike duration (per round)               |
| degreeToShootXCumulative  | float       | degrees              | Cumulative degree shooting                   |
| degreeToTargetXCumulative | float       | degrees              | Cumulative degree aiming                     |
| ...many more fields for angles, timings, and averages...         |
| qoeValue                  | int/float   | 1–5                  | Quality of Experience (self-report)          |
| acceptabilityValue        | bool        | TRUE/FALSE           | Round acceptable (self-report)               |

- **Logged:** Once per round, after QoE response

---

### 2. Player Log (`PlayerData_*.csv`)

- **One row per game tick, flushed after each round**
- **Columns:**
  - sessionID, currentRoundNumber, roundFPS, spikeMagnitude,
  - onAimSpikeEnabled, onEnemySpawnSpikeEnabled, onMouseSpikeEnabled, onReloadSpikeEnabled,
  - indexArray, roundTimer, time, mouseX, mouseY,
  - playerX, playerY, playerZ, scorePerSec,
  - playerRot (w,x,y,z), enemyPos (x,y,z), frametimeMS, isADS (bool, aiming down sights)
- **Logged:** Every game tick (written at round end)

---

### 3. Enemy Log (`ShotData_*.csv`)

- **One row per enemy destroyed (or at round end)**
- **Columns (examples):**
  - sessionID, currentRoundNumber, sessionStartTime, currentTime,
  - roundFPS, spikeMagnitude, onAimSpikeEnabled, onEnemySpawnSpikeEnabled, onMouseSpikeEnabled, onReloadSpikeEnabled, indexArray,
  - currentHealth, minAngleToPlayer, angularSizeOnSpawn, degreeToTargetX/Y, degreeToShootX/Y,
  - timeToTargetEnemy, timeToHitEnemy, timeToKillEnemy,
  - targetMarked, targetShot (bool)
- **Logged:** When an enemy is destroyed (including at round end).

---

## Summary

- **Ultra-high framerate FPS** for user studies (up to 1500 Hz).
- **Fully configurable** via CSV files (global, per-round, session).
- **Comprehensive, timestamped logs** of all player and game actions.
- **Counterbalanced round assignment** using Latin square + map system.
- **Open-source and ready for laboratory or field user studies.**

For demo videos, log/CSV examples, or further documentation, see the links at the top of this file.
