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

## How To Install
- Download one of the releases
- Run LeadRush.exe

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

# Lead Rush Logging Format

## 1. Round Log (`RoundData_*.csv`)

| Field                          | Type      | Units/Values       | Description                                             |
|---------------------------------|-----------|--------------------|---------------------------------------------------------|
| sessionID                      | int       |                    | Unique session identifier                               |
| LatinRow                       | int       |                    | Which Latin square row is used for this session         |
| currentRoundNumber             | int       |                    | Current round in session                                |
| sessionStartTime               | string    | timestamp          | When session started                                    |
| currentTime                    | string    | timestamp          | Time of log entry                                       |
| roundFPS                       | float     | Hz                 | Target framerate for round                              |
| spikeMagnitude                 | float     | ms                 | Frametime spike magnitude                               |
| onAimSpikeEnabled              | bool      | TRUE/FALSE         | Spike on aiming enabled                                 |
| onEnemySpawnSpikeEnabled       | bool      | TRUE/FALSE         | Spike on enemy spawn enabled                            |
| onMouseSpikeEnabled            | bool      | TRUE/FALSE         | Spike on mouse movement enabled                         |
| onReloadSpikeEnabled           | bool      | TRUE/FALSE         | Spike on reload enabled                                 |
| indexArray                     | int       |                    | Shuffled config index                                   |
| score                          | int       |                    | Player score for round                                  |
| shotsFired                     | int       |                    | Shots fired                                             |
| shotsHit                       | int       |                    | Successful hits                                         |
| headshots                      | int       |                    | Headshots                                               |
| reloadCount                    | int       |                    | Reloads performed                                       |
| tacticalReloadCount            | int       |                    | Tactical reloads performed                              |
| accuracy                       | float     | 0-1                | shotsHit/shotsFired                                     |
| roundKills                     | int       |                    | Kills this round                                        |
| roundDeaths                    | int       |                    | Deaths this round                                       |
| distanceTravelledPerRound      | float     | Unity units        | Distance traveled                                       |
| delXCumilative                 | float     |                    | Cumulative mouse X movement                             |
| delYCumilative                 | float     |                    | Cumulative mouse Y movement                             |
| totalMouseMovement             | float     |                    | delXCumilative + delYCumilative                         |
| frametimeCumulativeRound       | float     | ms                 | Cumulative frame time                                   |
| roundFrameCount                | int       |                    | Number of frames                                        |
| avgFT                          | float     | ms                 | Average frame time                                      |
| avgFPS                         | float     | Hz                 | Average FPS                                             |
| perRoundAimSpikeCount          | int       |                    | Aim spikes triggered                                    |
| perRoundReloadSpikeCount       | int       |                    | Reload spikes triggered                                 |
| perRoundMouseMovementSpikeCount| int       |                    | Mouse spikes triggered                                  |
| perRoundEnemySpawnSpikeCount   | int       |                    | Enemy spawn spikes triggered                            |
| spikeDurationCumulative        | float     | ms                 | Total spike duration                                    |
| avgSpikeDurationCumulative     | float     | ms                 | Avg. individual spike duration                          |
| degreeToShootXCumulative       | float     | degrees            | Cumulative deg. to shoot                                |
| degreeToTargetXCumulative      | float     | degrees            | Cumulative deg. to target                               |
| minAnlgeToEnemyCumulative      | float     | degrees            | Cumulative min angle to enemy                           |
| enemySizeCumulative            | float     | Unity units        | Cumulative enemy size in view                           |
| degXShootAvg                   | float     | degrees            | Avg. deg. to shoot                                      |
| degXTargetAvg                  | float     | degrees            | Avg. deg. to target                                     |
| enemySizeOnSpawnAvg            | float     | Unity units        | Avg. enemy size at spawn                                |
| timeToTargetEnemyCumulative    | float     | seconds            | Total time to target enemy                              |
| timeToHitEnemyCumulative       | float     | seconds            | Total time to hit enemy                                 |
| timeToKillEnemyCumulative      | float     | seconds            | Total time to kill enemy                                |
| aimDurationPerRound            | float     | seconds            | Total time aiming                                       |
| isFiringDurationPerRound       | float     | seconds or ms      | Total time firing                                       |
| qoeValue                       | int/float | 1-5                | Player-reported QoE                                     |
| acceptabilityValue             | bool      | TRUE/FALSE         | Player found round acceptable                           |

---

## 2. Player Log (`PlayerData_*.csv`)

| Field         | Type        | Units/Values  | Description                                      |
|---------------|-------------|---------------|--------------------------------------------------|
| sessionID     | int         |               | Session ID                                       |
| currentRoundNumber | int     |               | Current round                                    |
| roundFPS      | float       | Hz            | Target framerate                                 |
| spikeMagnitude| float       | ms            | Spike magnitude                                  |
| onAimSpikeEnabled | bool    | TRUE/FALSE    | Spike on aiming enabled                          |
| onEnemySpawnSpikeEnabled | bool | TRUE/FALSE| Spike on enemy spawn enabled                     |
| onMouseSpikeEnabled | bool  | TRUE/FALSE    | Spike on mouse movement enabled                  |
| onReloadSpikeEnabled | bool | TRUE/FALSE    | Spike on reload enabled                          |
| indexArray    | int         |               | Shuffled config index                            |
| roundTimer    | float       | seconds       | Time since round started                         |
| time          | string      | timestamp     | Time of log entry                                |
| mouseX        | float       |               | Mouse movement X                                 |
| mouseY        | float       |               | Mouse movement Y                                 |
| playerX       | float       | Unity units   | Player position X                                |
| playerY       | float       | Unity units   | Player position Y                                |
| playerZ       | float       | Unity units   | Player position Z                                |
| scorePerSec   | float       | points/sec    | Score accumulated per second                     |
| playerRot.w   | float       |               | Player rotation quaternion w                     |
| playerRot.x   | float       |               | Player rotation quaternion x                     |
| playerRot.y   | float       |               | Player rotation quaternion y                     |
| playerRot.z   | float       |               | Player rotation quaternion z                     |
| enemyPos.x    | float       | Unity units   | Enemy position X                                 |
| enemyPos.y    | float       | Unity units   | Enemy position Y                                 |
| enemyPos.z    | float       | Unity units   | Enemy position Z                                 |
| frametimeMS   | double      | ms            | Frame time (per tick)                            |
| isADS         | bool        | TRUE/FALSE    | Aiming down sights at this tick                  |

---

## 3. Enemy Log (`ShotData_*.csv` or similar)

| Field                | Type      | Units/Values       | Description                                    |
|----------------------|-----------|--------------------|------------------------------------------------|
| sessionID            | int       |                    | Session ID                                     |
| currentRoundNumber   | int       |                    | Current round                                  |
| sessionStartTime     | string    | timestamp          | Session start time                             |
| currentTime          | string    | timestamp          | Log entry time                                 |
| roundFPS             | float     | Hz                 | Target framerate                               |
| spikeMagnitude       | float     | ms                 | Spike magnitude                                |
| onAimSpikeEnabled    | bool      | TRUE/FALSE         | Spike on aiming enabled                        |
| onEnemySpawnSpikeEnabled | bool  | TRUE/FALSE         | Spike on enemy spawn enabled                   |
| onMouseSpikeEnabled  | bool      | TRUE/FALSE         | Spike on mouse movement enabled                |
| onReloadSpikeEnabled | bool      | TRUE/FALSE         | Spike on reload enabled                        |
| indexArray           | int       |                    | Shuffled config index                          |
| currentHealth        | float     | (unitless)         | Enemy's health at time of log                  |
| minAngleToPlayer     | float     | degrees            | Minimum angle to player                        |
| angularSizeOnSpawn   | float     | degrees            | Enemy's size in FOV at spawn                   |
| degreeToTargetX      | float     | degrees            | Horizontal angle: enemy to player              |
| degreeToTargetY      | float     | degrees            | Vertical angle: enemy to player                |
| degreeToShootX       | float     | degrees            | Horizontal angle at shot                       |
| degreeToShootY       | float     | degrees            | Vertical angle at shot                         |
| timeToTargetEnemy    | float     | seconds            | Time to initially aim at enemy                 |
| timeToHitEnemy       | float     | seconds            | Time to first hit enemy                        |
| timeToKillEnemy      | float     | seconds            | Time to kill enemy                             |
| targetMarked         | bool      | TRUE/FALSE         | Player aimed at enemy                          |
| targetShot           | bool      | TRUE/FALSE         | Player shot enemy                              |

---

## Logging Frequency

- **Round Log:** Once per round, after QoE is submitted.
- **Player Log:** Every game tick (written at round end).
- **Enemy Log:** Each time an enemy is destroyed (also at round end).

---

