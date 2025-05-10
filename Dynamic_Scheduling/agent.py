import gymnasium as gym
from airplane_boarding import AirplaneEnv
from sb3_contrib import MaskablePPO
from sb3_contrib.common.maskable.utils import get_action_masks

from stable_baselines3.common.vec_env.subproc_vec_env import SubprocVecEnv
from stable_baselines3.common.env_util import make_vec_env
from sb3_contrib.common.maskable.callbacks import  MaskableEvalCallback
from stable_baselines3.common.callbacks import StopTrainingOnNoModelImprovement, StopTrainingOnRewardThreshold, BaseCallback

import os


import matplotlib.pyplot as plt
import numpy as np

model_dir = "models"
agent_dir = "agents"
log_dir = "logs"


class PeriodicSaveCallback(BaseCallback):
    def __init__(self, save_freq, save_path, verbose=0):
        super().__init__(verbose)
        self.save_freq = save_freq
        self.save_path = save_path
        self.verbose = verbose

    def _on_step(self) -> bool:
        if self.num_timesteps % self.save_freq == 0:
            path = os.path.join(self.save_path, f"manual_save_{self.num_timesteps}.zip")
            self.model.save(path)
            if self.verbose:
                print(f"Saved model to {path}")
        return True

def train():


    env = make_vec_env(AirplaneEnv, n_envs=12, env_kwargs={"num_of_rows":4, "seats_per_row":5, "num_of_plane_rows":4}, vec_env_cls=SubprocVecEnv, seed = 42)

    # Increase ent_coef to encourage exploration, this resulted in a better solution.
    model = MaskablePPO('MlpPolicy', env, verbose=1, device='cpu', tensorboard_log=log_dir, ent_coef=0.05)

    save_callback = PeriodicSaveCallback(save_freq=100_000, save_path=os.path.join(agent_dir, 'MaskablePPO', 'PPO_32'), verbose=1)

    eval_callback = MaskableEvalCallback(
        env,
        eval_freq=10_000,
        # callback_on_new_best = StopTrainingOnRewardThreshold(reward_threshold=???, verbose=1)
        # callback_after_eval  = StopTrainingOnNoModelImprovement(max_no_improvement_evals=???, min_evals=???, verbose=1)
        verbose=1,
        best_model_save_path=os.path.join(model_dir, 'MaskablePPO'),
    )

    """
    total_timesteps: pass in a very large number to train (almost) indefinitely.
    callback: pass in reference to a callback fuction above
    """
    model.learn(total_timesteps=int(1e10), callback=[eval_callback, save_callback])

def test(model_name, render=True):

    env = gym.make('airplane-boarding-v0', num_of_rows=4, seats_per_row=5, num_of_plane_rows = 4,render_mode='terminal' if render else None)

    # Load model
    model = MaskablePPO.load(f'agents/MaskablePPO/PPO_32/{model_name}', env=env)

    rewards = 0
    # Run a test

    obs, _ = env.reset(seed = 42)
    terminated = False

    while True:
        action_masks = get_action_masks(env)
        action, _ = model.predict(observation=obs, deterministic=True, action_masks=action_masks) # Turn on deterministic, so predict always returns the same behavior
        obs, reward, terminated, _, _ = env.step(action)
        rewards += reward

        if terminated:
            break

    print(f"Total rewards: {rewards}")

# if __name__ == '__main__':
#     # train()
#     test("manual_save_3000000")

#############################################################################################################
##XAI
###gpt 
