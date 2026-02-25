# ML-Dungeon

Teaching an agent to navigate randomly generated and randomly-sized dungeons with [Unity ML-Agents](https://docs.unity3d.com/Packages/com.unity.ml-agents@latest "Unity ML-Agents"). See a [web demo](https://stevenrice.ca/ml-dungeon "ML-Dungeon").

- [Purpose](#purpose "Purpose")
- [Game Overview](#game-overview "Game Overview")
- [Agent Design](#agent-design "Agent Design")
- [Agent Rewards](#agent-rewards "Agent Rewards")
- [Agent Training](#agent-training "Agent Training")
  - [Heuristic Agent](#heuristic-agent "Heuristic Agent")
  - [Demonstration Recording](#demonstration-recording "Demonstration Recording")
  - [Curriculum Learning](#curriculum-learning "Curriculum Learning")
- [Results](#results "Results")
- [Running](#running "Running")
  - [Unity Editor](#unity-editor "Unity Editor")
    - [Run Training](#run-training "Run Training")
      - [Helper Functions](#helper-functions "Helper Functions")
- [Resources](#resources "Resources")

## Purpose

The purpose is this project is for use as a learning resources for [Unity ML-Agents](https://docs.unity3d.com/Packages/com.unity.ml-agents@latest "Unity ML-Agents"), highlighting how different methods can be applied to try and overcome a complex environment. As such, many of the final trained agents in their current states are not very effective.

## Game Overview

- The agent is placed in a randomly-sized, fully-connected square dungeon.
- Walls are randomly placed throughout the dungeon at a given percentage.
- The agent always starts in one corner of the dungeon and a weapon chest is placed in a different corner.
- If the agent reaches the chest, it will give them a sword.
- This sword can be used to eliminate enemies in the dungeon, of which a given number will spawn in the corner opposite of the player.
- Enemies will choose a random space in the dungeon to navigate to, and if they get within five units to the player and have line-of-sight on them, they will begin to follow the player.
- If the player makes contact with an enemy while they do not have the sword, they lose.
- If they have the sword, the enemy is eliminated.
- All enemies must be eliminated to win. If there were no enemies in the dungeon to begin, reaching the weapon chest wins the game.

## Agent Design

The agent's actions are simply movement along both the horizontal and vertical axes. In order to allow the agent to be able to play dungeons of all sizes, environment inputs had to be carefully crafted. The goal was to give as few inputs as possible, and ensure all inputs were normalized between `[0, 1]`, with a few special cases giving readings in `[-1, 1]` which are noted below. The internal architecture of the agent's brain is two layers of 128 neurons.

1. **Agent position** - The agent's current position in the dungeon is given along both the horizontal and vertical axes each in the range of `[0, 1]`.
2. **Chest position** - Same as the agent's position but for the chest that gives the agent a sword. If the agent has already reached the chest and obtained the sword, these inputs are given as `[-1, 1]` to the agent instead of the position of the chest. This was done to instead of adding another boolean input in addition to the chest coordinates, as given the chest's position becomes irrelevant to the agent once the sword is obtained, this allows us to reduce the input size.
3. **Nearest enemy's position** - Same as the chest's position where it will give the positon of the nearest enemy if there are any, or `[-1, 1]` if there are no more enemies in the level. This again was done to avoid needing to add another boolean input to signify the existence of any enemies.
4. **Local area map** - A local visual encoding of the local area of the world for the agent to navigate around local obstacles. This encodes a square of the local area consisting the agent's current dungeon tile as well as ten tiles in each direction. This creates a `21×21` grid which is encoded as a visual tensor, utilizing the `match3` Convolutional Neural Network (CNN) model based on the work ["Human-Like Playtesting with Deep Learning" by Gudmundsoon et al.](https://doi.org/10.1109/CIG.2018.8490442 "Human-Like Playtesting with Deep Learning") This CNN model was chosen as it "is a smaller CNN that can capture more granular spatial relationships and is optimized for board games", and the encoding we utilize is very efficient. The world is encoded such that if an enemy is within a cell, the cell is given a value of `0`. If the cell is a wall, it is given a value of `0.5`. Otherwise, it is a walkable space, and it is given a value of `1`. Any reading which falls outside of the dungeon is treated as a wall and thus given a value of `0.5`. Again, a single channel was chosen like this, rather than a one-hot categorical encoding, to reduce the CNN inputs by a third, as the data was able to be easily distinguished in the one channel.

## Agent Rewards

- A reward of `1` is given for reaching the weapon pickup.
- A reward of `1` is given for eliminating an enemy.
- A penalty of `-1` is given for being eliminated by an enemy.
- A penalty of `-0.0001` is given every step to encourage learning to complete levels efficiently.

## Agent Training

The agent was trained for a million steps using [Proximal Policy Optimization (PPO)](https://doi.org/10.48550/arXiv.1707.06347 "Proximal Policy Optimization Algorithms") using [training curriculum](#curriculum-learning "Curriculum Learning"). The agent is given a [curiosity reward signal](https://doi.org/10.48550/arXiv.1705.05363 "Curiosity-driven Exploration by Self-supervised Prediction") to encourage exploration, and imitation learning is utilized, both Behavioral Cloning (BC) and [Generative Adversarial Imitation Learning (GAIL)](https://doi.org/10.48550/arXiv.1606.03476 "Generative Adversarial Imitation Learning"). The [demonstrations](#demonstration-recording "Demonstration Recording") for imitation learning were recorded using the [heuristic agent](#heuristic-agent "Heuristic Agent"). Both the [heuristic agent](#heuristic-agent "Heuristic Agent") and details on the [demonstrations recorded](#demonstration-recording "Demonstration Recording") are in their sections below.

### Heuristic Agent

The heuristic agent evaluates the following criteria to make a decision each step until the level is complete or the agent loses by getting eliminated by an enemy.

1. If the agent does not have the sword, it navigates towards the chest. This does not avoid enemies which may be between the agent and the chest. As such, during this case of the heuristic decision-making, a human operator may take control of the agent. They can move them either manually using the arrow keys or WASD, or navigating by right-clicking with the mouse, to demonstrate how to avoid enemies.
2. Otherwise, the agent has the sword, so it navigates towards the nearest enemy.

All navigation is done by finding a path using A\* on the navigation mesh of the dungeon, and then determining the needed inputs to move the agent towards the first point along the found path.

### Demonstration Recording

The demonstration recording of the [heuristic agent](#heuristic-agent "Heuristic Agent") is done for a set number of trials across given dungeon parameters. A separate recording is made for each trial, with a recording being discarded in the event that the [heuristic agent](#heuristic-agent "Heuristic Agent") fails the level by being eliminated by an enemy. Demonstrations were run for a hundred thousand trials, each which had to following configurations:

- Size = `[10, 30]`
- Walls = `[0%, 20%]`
- Enemies = `[0, 3]`

### Curriculum Learning

There were three levels to the training, each allowing for more complex levels. To ensure generalization, agents would also be tested the lower-complexity levels in when in higher curriculums. The different curriculum levels were:

1. Easy
   - Size = `10`
   - Walls = `0%`
   - Enemies = `0`
2. Medium
   - Size = `[10, 20]`
   - Walls = `[0%, 10%]`
   - Enemies = `[0, 1]`
3. Hard
   - Size = `[10, 30]`
   - Walls = `[0%, 20%]`
   - Enemies = `[1, 3]`

## Results

TODO.

## Running

If you just wish to see the agent in action, you can run the [web demo](https://stevenrice.ca/ml-dungeon "ML-Dungeon") which allows you to change the size of the dungeon, percentage of walls, and number of enemies.

### Unity Editor

To run the project in the Unity editor, there are several scenes:

- **Main** - The same scene as the [web demo](https://stevenrice.ca/ml-dungeon "ML-Dungeon").
- **Recording** - The scene to perform the [demonstration recording](#demonstration-recording "Demonstration Recording"). These are saved to the `Demonstrations` folder in the `Assets` folder. If you wish to create new recordings, you will need to delete the existing recordings in the `Demonstrations` folder, or set for more trials.
- **Training** - A scene to train multiple instances of the agent in parallel. If you wish to train the agents, you will need to follow the [run training](#run-training "Run Training") instructions.

#### Run Training

To train the agent, you can either read the [Unity ML-Agents documentation](https://docs.unity3d.com/Packages/com.unity.ml-agents@latest "Unity ML-Agents") to learn how to install and run [Unity ML-Agents](https://docs.unity3d.com/Packages/com.unity.ml-agents@latest "Unity ML-Agents"), or use the provided [helper functions](#helper-functions "Helper Functions") to train the agent.

##### Helper Functions

The helper files have been made for Windows and you must [install uv](https://docs.astral.sh/uv/#installation "UV Installation"). One installed, from the top menu of the Unity editor, you can select `ML-Dungeon` followed by the desired command to run.

- `Behavioral Cloning` - This will train a model purely on the captured demonstrations.
- `Fine-Tuned` - After running the behavioral cloning, this will then train the model further using reinforcement learning, directly jumping to the "Hard" configuration of the [curriculum learning](#curriculum-learning "Curriculum Learning").
- `Curriculum Learning` - This will train a reinforcement learning model from scratch using [curriculum learning](#curriculum-learning "Curriculum Learning").
- `Fine-Tuned and Curriculum Learning` - After running the behavioral cloning, this will then train the model further using reinforcement learning using [curriculum learning](#curriculum-learning "Curriculum Learning").
- `TensorBoard` - This will open your browser to see the [TensorBoard](https://www.tensorflow.org/tensorboard "TensorBoard") logs of the training of all models.
- `Install` - If you have [uv](https://docs.astral.sh/uv "uv") installed for Python, this will set up your environment for running all other commands.
- `Activate` - This will open a terminal in your [uv](https://docs.astral.sh/uv "uv") Python virtual environment for this project, allowing you to run other commands.

## Resources

Assets are from the [Mini Dungeon](https://kenney.nl/assets/mini-dungeon "Mini Dungeon - Kenney") kit by [Kenney](https://kenney.nl "Kenney") under the [Creative Commons CC0 license](https://creativecommons.org/publicdomain/zero/1.0 "CC0 1.0 Universal").