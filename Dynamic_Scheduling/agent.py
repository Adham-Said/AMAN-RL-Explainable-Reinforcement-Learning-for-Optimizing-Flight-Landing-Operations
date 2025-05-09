import gymnasium as gym
from airplane_boarding import AirplaneEnv
from sb3_contrib import MaskablePPO
from sb3_contrib.common.maskable.utils import get_action_masks

from stable_baselines3.common.vec_env.subproc_vec_env import SubprocVecEnv
from stable_baselines3.common.env_util import make_vec_env
from sb3_contrib.common.maskable.callbacks import  MaskableEvalCallback
from stable_baselines3.common.callbacks import StopTrainingOnNoModelImprovement, StopTrainingOnRewardThreshold, BaseCallback

import os

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

if __name__ == '__main__':
    # train()
    test("manual_save_3000000")

#############################################################################################################
##XAI
###gpt 
import gymnasium as gym
from airplane_boarding import AirplaneEnv
from sb3_contrib import MaskablePPO
from sb3_contrib.common.maskable.utils import get_action_masks
import shap
import numpy as np
import torch

def explain_decision(model_name="manual_save_3000000"):
    # Load environment and model
    env = gym.make('airplane-boarding-v0', num_of_rows=4, seats_per_row=5, render_mode=None)
    model = MaskablePPO.load(f'agents/MaskablePPO/PPO_32/{model_name}', env=env)
    
    # Calculate num_of_seats directly
    num_of_rows = 4
    seats_per_row = 5
    num_of_seats = num_of_rows * seats_per_row
    
    # Reset environment and get initial observation
    obs, _ = env.reset(seed=42)
    action_masks = get_action_masks(env)
    
    # Get model's action
    action, _ = model.predict(observation=obs, deterministic=True, action_masks=action_masks)
    
    # Print decision
    print(f"\n✈️ Decision: Selected passenger in seat {action} to board next")
    
    # Prepare observation for SHAP
    obs_input = obs.reshape(1, -1)
    
    print(f"Observation shape: {obs.shape}, Action: {action}")
    print(f"Available actions: {np.where(action_masks)[0]}")
    
    try:
        # Create a simpler explanation based on the observation directly
        print("\n🔍 Simple explanation for controller:")
        
        # Extract passenger information from observation
        passengers = []
        for i in range(0, len(obs), 2):
            seat_num = obs[i]
            fuel_level = obs[i+1]
            if seat_num >= 0:  # Valid passenger
                passengers.append((seat_num, fuel_level, i//2))
        
        # Find the selected passenger
        selected = next((p for p in passengers if p[0] == action), None)
        
        if selected:
            seat_num, fuel_level, idx = selected
            if fuel_level == 1:  # Low fuel
                print(f"- Passenger in seat {seat_num} was selected because they have low fuel reserves")
            else:
                print(f"- Passenger in seat {seat_num} was selected based on optimal boarding sequence")
                
            # Compare with other passengers
            other_low_fuel = [p for p in passengers if p[1] == 1 and p[0] != seat_num]
            if other_low_fuel and fuel_level == 0:
                print(f"- Despite {len(other_low_fuel)} passengers with low fuel, this passenger's position was prioritized")
            
            # Check seat position (front vs back)
            if seat_num < num_of_seats // 2:
                print(f"- Passenger is seated in the front half of the plane (seat {seat_num})")
            else:
                print(f"- Passenger is seated in the back half of the plane (seat {seat_num})")
        
        # Try SHAP explanation if simple explanation isn't enough
        print("\nAttempting SHAP explanation...")
        # Function to get model's logits for SHAP
        def model_fn(input_array):
            input_tensor = torch.tensor(input_array, dtype=torch.float32)
            latent_pi, _ = model.policy.mlp_extractor(input_tensor)
            logits = model.policy.action_net(latent_pi)
            # Apply action mask to logits
            masked_logits = logits.clone()
            masked_logits[:, ~np.array(action_masks)] = -1e8
            return masked_logits.detach().numpy()
            
        # Create SHAP explainer with a simple background
        explainer = shap.KernelExplainer(model_fn, obs_input)
        shap_values = explainer.shap_values(obs_input)
        
        print(f"SHAP values shape: {np.array(shap_values).shape}")
        
        # Create human-readable feature names
        feature_names = []
        for i in range(len(obs)):
            if i % 2 == 0:
                feature_names.append(f"seat_{i//2}")
            else:
                feature_names.append(f"fuel_level_{i//2}")
        
        # Get top contributing features for this action
        if action < len(shap_values):
            values = shap_values[action][0]
            top_indices = np.argsort(np.abs(values))[::-1][:5]
            
            print("\n🔍 SHAP Explanation for controller:")
            print(f"The system selected passenger in seat {action} because:")
            
            for i in top_indices:
                if abs(values[i]) > 0.01:  # Only show significant contributions
                    feature = feature_names[i]
                    direction = "prioritized" if values[i] > 0 else "deprioritized"
                    magnitude = "strongly" if abs(values[i]) > 0.1 else "somewhat"
                    
                    if "fuel" in feature and values[i] > 0:
                        print(f"- This passenger's fuel level {magnitude} influenced the decision")
                    elif "seat" in feature:
                        print(f"- The passenger's seat position ({feature}) {magnitude} {direction} them")
    
    except Exception as e:
        print(f"Error generating SHAP explanation: {e}")
        print("Falling back to rule-based explanation:")
        
        # Rule-based explanation as fallback
        if action_masks[action]:
            print(f"- Passenger in seat {action} was selected based on the current boarding policy")
            if action < num_of_seats // 2:
                print(f"- This passenger is seated in the front half of the plane")
            else:
                print(f"- This passenger is seated in the back half of the plane")
    
    return action

if __name__ == '__main__':
    # train()
    test("manual_save_3000000")
    #############################################################################################################
    explain_decision("manual_save_3000000")

