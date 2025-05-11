import gymnasium as gym
import shap
from airplane_boarding import AirplaneEnv
from sb3_contrib import MaskablePPO
from sb3_contrib.common.maskable.utils import get_action_masks

from stable_baselines3.common.vec_env.subproc_vec_env import SubprocVecEnv
from stable_baselines3.common.env_util import make_vec_env
from sb3_contrib.common.maskable.callbacks import  MaskableEvalCallback
from stable_baselines3.common.callbacks import StopTrainingOnNoModelImprovement, StopTrainingOnRewardThreshold, BaseCallback




import matplotlib.pyplot as plt
import numpy as np
import torch
import shap


import os
import gymnasium as gym
import numpy as np
import matplotlib.pyplot as plt
import pysindy as ps

from airplane_boarding import AirplaneEnv
from sb3_contrib import MaskablePPO
from sb3_contrib.common.maskable.utils import get_action_masks
from stable_baselines3.common.vec_env.subproc_vec_env import SubprocVecEnv
from stable_baselines3.common.env_util import make_vec_env
from sb3_contrib.common.maskable.callbacks import MaskableEvalCallback
from stable_baselines3.common.callbacks import BaseCallback
from gymnasium import spaces

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

    save_callback = PeriodicSaveCallback(save_freq=100_000, save_path=os.path.join(agent_dir, 'MaskablePPO', 'PPO_33'), verbose=1)

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

def test(model_name, render=True, explain=True):

    env = gym.make('airplane-boarding-v0', num_of_rows=4, seats_per_row=5, num_of_plane_rows = 4,render_mode='terminal' if render else None)

    # Load model
    model = MaskablePPO.load(f'agents/MaskablePPO/PPO_33/{model_name}', env=env)
    # Get the policy network from the model
    policy = model.policy  # This is a PyTorch neural net

    rewards = 0
    # Run a test

    obs, _ = env.reset(seed = 42)
    terminated = False
    # Prepare SHAP explainer only if explain is True
    if explain:
        
        def shap_model(input_array):
            input_tensor = torch.tensor(input_array, dtype=torch.float32)
            with torch.no_grad():
                logits = policy.forward(input_tensor)[0]  # Only get the action logits

                logits = logits.to(torch.float32)#
                probs = torch.softmax(logits, dim=0)#
                #probs = torch.softmax(logits, dim=1)
            return probs.numpy()

        # Background data: 100 random samples from the observation space
        background = np.array([env.observation_space.sample() for _ in range(100)])
        explainer = shap.Explainer(shap_model, background)
        shap_values = explainer(obs.reshape(1, -1))
        ###############3

    while True:
        action_masks = get_action_masks(env)
        action, _ = model.predict(observation=obs, deterministic=True, action_masks=action_masks) # Turn on deterministic, so predict always returns the same behavior


        obs_tensor = torch.tensor(obs, dtype=torch.float32)
        # if explain:
        #     print("\n🔍 SHAP Explanation for Current Observation:")
            #   shap_values = explainer(obs.reshape(1, -1))
        #     ##########
        #     # print("shapv=",shap_values)
        #     # shap.plots.waterfall(shap_values[0])
                    
        obs, reward, terminated, _, _ = env.step(action)
        rewards += reward




 
       
        

        if terminated:
            break
    
    # Run SHAP explanation BEFORE stepping if enabled
    print("shapv=",shap_values)
    shap.plots.waterfall(shap_values[0])



























    
    


    print(f"Total rewards: {rewards}")
    

# if __name__ == '__main__':
#     # train()
#     test("manual_save_5400000")
  #############################################################################elfoo2 bytala3 graph shap
#############################################################################################################
##XAI shap
###gpt 


def explain_decision(model_name="manual_save_5400000"):
    # Load environment and model
    env = gym.make('airplane-boarding-v0', num_of_rows=4, seats_per_row=5, render_mode=None)
    model = MaskablePPO.load(f'agents/MaskablePPO/PPO_33/{model_name}', env=env)
    
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
    print(f"\n✈️ Decision: Selected airplane in seat {action} to board next")
    
    # Prepare observation for SHAP
    obs_input = obs.reshape(1, -1)
    
    print(f"Observation shape: {obs.shape}, Action: {action}")
    print(f"Available actions: {np.where(action_masks)[0]}")
    
    try:
    #     # Create a simpler explanation based on the observation directly
    #     print("\n🔍 Simple explanation for controller:")
        
    #     # Extract airplane information from observation
    #     airplanes = []
    #     for i in range(0, len(obs), 2):
    #         seat_num = obs[i]
    #         fuel_level = obs[i+1]
    #         if seat_num >= 0:  # Valid airplane
    #             airplanes.append((seat_num, fuel_level, i//2))
        
    #     # Find the selected airplane
    #     selected = next((p for p in airplanes if p[0] == action), None)
        
    #     if selected:
    #         seat_num, fuel_level, idx = selected
    #         if fuel_level == 1:  # Low fuel
    #             print(f"- airplane in seat {seat_num} was selected because they have low fuel reserves")
    #         else:
    #             print(f"- airplane in seat {seat_num} was selected based on optimal boarding sequence")
                
    #         # Compare with other airplanes
    #         other_low_fuel = [p for p in airplanes if p[1] == 1 and p[0] != seat_num]
    #         if other_low_fuel and fuel_level == 0:
    #             print(f"- Despite {len(other_low_fuel)} airplanes with low fuel, this airplane's position was prioritized")
            
    #         # Check seat position (front vs back)
    #         if seat_num < num_of_seats // 2:
    #             print(f"- airplane is seated in the front half of the plane (seat {seat_num})")
    #         else:
    #             print(f"- airplane is seated in the back half of the plane (seat {seat_num})")
        
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
            
            
            
            for i in top_indices:
                if abs(values[i]) > 0.01:  # Only show significant contributions
                    print("\n🔍 SHAP Explanation for controller:")
                    print(f"The system selected airplane in seat {action} because:")
                    feature = feature_names[i]
                    direction = "prioritized" if values[i] > 0 else "deprioritized"
                    magnitude = "strongly" if abs(values[i]) > 0.1 else "somewhat"
                    
                    if "fuel" in feature and values[i] > 0:
                        print(f"- This airplane's fuel level {magnitude} influenced the decision")
                    elif "seat" in feature:
                        print(f"- The airplane's seat position ({feature}) {magnitude} {direction} them")
    
    except Exception as e:
        print(f"Error generating SHAP explanation: {e}")
        print("Falling back to rule-based explanation:")
        
        # Rule-based explanation as fallback
        if action_masks[action]:
            print(f"- airplane in seat {action} was selected based on the current boarding policy")
            if action < num_of_seats // 2:
                print(f"- This airplane is seated in the front half of the plane")
            else:
                print(f"- This airplane is seated in the back half of the plane")
    
    return action

# if __name__ == '__main__':
#     # train()
#     test("manual_save_5400000")
#     #############################################################################################################
#     explain_decision("manual_save_5400000")





# ####sindy rl gpt


# Directories
model_dir = "models"
agent_dir = "agents"
log_dir = "logs"

class PeriodicSaveCallback(BaseCallback):
    def __init__(self, save_freq, save_path, verbose=0):
        super().__init__(verbose)
        self.save_freq = save_freq
        self.save_path = save_path

    def _on_step(self) -> bool:
        if self.num_timesteps % self.save_freq == 0:
            path = os.path.join(self.save_path, f"manual_save_{self.num_timesteps}.zip")
            self.model.save(path)
            if self.verbose:
                print(f"Saved model to {path}")
        return True

# def train():
#     env = make_vec_env(
#         AirplaneEnv,
#         n_envs=12,
#         env_kwargs={"num_of_rows": 4, "seats_per_row": 5, "num_of_plane_rows": 4},
#         vec_env_cls=SubprocVecEnv,
#         seed=42
#     )
#     model = MaskablePPO('MlpPolicy', env, verbose=1, device='cpu', tensorboard_log=log_dir, ent_coef=0.05)
#     save_callback = PeriodicSaveCallback(save_freq=100_000, save_path=os.path.join(agent_dir, 'MaskablePPO', 'PPO_33'), verbose=1)
#     eval_callback = MaskableEvalCallback(
#         env,
#         eval_freq=10_000,
#         best_model_save_path=os.path.join(model_dir, 'MaskablePPO'),
#         verbose=1
#     )
#     model.learn(total_timesteps=int(1e10), callback=[eval_callback, save_callback])

# def test(model_name, render=True):
#     env = gym.make('airplane-boarding-v0', num_of_rows=4, seats_per_row=5, num_of_plane_rows=4, render_mode='terminal' if render else None)
#     model = MaskablePPO.load(f'agents/MaskablePPO/PPO_33/{model_name}', env=env)
#     rewards = 0
#     obs, _ = env.reset(seed=42)
#     terminated = False
#     while not terminated:
#         action_masks = get_action_masks(env)
#         action, _ = model.predict(observation=obs, deterministic=True, action_masks=action_masks)
#         obs, reward, terminated, _, _ = env.step(action)
#         rewards += reward
#     print(f"Total rewards: {rewards}")

def collect_data(env, model, num_episodes=50):
    X, U, X_dot = [], [], []
    for ep in range(num_episodes):
        obs, _ = env.reset()
        done = False
        while not done:
            action_masks = get_action_masks(env)
            action, _ = model.predict(obs, deterministic=True, action_masks=action_masks)
            next_obs, reward, done, _, _ = env.step(action)
            X.append(obs)
            U.append([action])
            X_dot.append(next_obs)
            obs = next_obs
        #print(f"Collected episode {ep+1}/{num_episodes}")
    return np.array(X), np.array(U), np.array(X_dot)

def train_sindy(X, U, X_dot):
    optimizer = ps.STLSQ(threshold=0.1)
    library = ps.PolynomialLibrary(degree=2)
    model = ps.SINDy(optimizer=optimizer, feature_library=library, discrete_time=True)
    model.fit(X, u=U, x_dot=X_dot)
    print("\nLearned SINDy model:")
    #model.print()
    return model



import matplotlib.pyplot as plt

def plot_predictions(sindy_model, X, U, X_dot, max_features=5):
    X_pred = sindy_model.predict(X, u=U)
    num_features = min(X.shape[1], max_features)
    plt.figure(figsize=(12, 2.5 * num_features))
    for i in range(num_features):
        plt.subplot(num_features, 1, i + 1)
        plt.plot(X_dot[:, i], 'k', label='True')
        plt.plot(X_pred[:, i], 'r--', label='SINDy Predicted')
        plt.ylabel(f"Feature {i}")
        if i == 0:
            plt.legend()
    plt.xlabel("Timestep")
    plt.suptitle(f"SINDy Predictions vs True Next States (First {num_features} features)")
    plt.tight_layout()
    plt.show()



class SINDySimEnv(gym.Env):
    def __init__(self, sindy_model, action_space):
        self.model = sindy_model
        self.action_space = action_space
        self.observation_space = spaces.Box(low=-1e10, high=1e10, shape=(sindy_model.n_features_in_,), dtype=np.float32)
        self.state = None

    def reset(self, seed=None, options=None):
        self.state = self.observation_space.sample()
        return self.state, {}

    def step(self, action):
            

        next_state = self.model.predict(np.array([self.state]), u=np.array([[action]]))[0]
        reward = -np.linalg.norm(next_state - self.state)  # placeholder reward
        done = False
        self.state = next_state
        state = np.array([state])
        if state.shape[1] < EXPECTED_SINDY_INPUT_DIM:
         pad = np.zeros((1, EXPECTED_SINDY_INPUT_DIM - state.shape[1]))
         state = np.concatenate([state, pad], axis=1)
        elif state.shape[1] > EXPECTED_SINDY_INPUT_DIM:
         state = state[:, :EXPECTED_SINDY_INPUT_DIM]

        return sindy_model.predict(state, u=np.array([[action]]))[0]



        # return next_state, reward, done, False, {}
    

from stable_baselines3 import PPO















if __name__ == '__main__':
    test("manual_save_5400000")
    explain_decision("manual_save_5400000")
    trained_model = MaskablePPO.load('agents/MaskablePPO/PPO_33/manual_save_5400000')
    env = gym.make('airplane-boarding-v0', num_of_rows=4, seats_per_row=5, num_of_plane_rows=4)
    print("Collecting data from environment using trained agent...")
    X, U, X_dot = collect_data(env, trained_model, num_episodes=100)
    print("Training SINDy model...")
    sindy_model = train_sindy(X, U, X_dot)
    print("Generating predictions and plotting...")
    # plot_predictions(sindy_model, X, U, X_dot)
    plot_predictions(sindy_model, X, U, X_dot, max_features=5)

    # print("Training PPO on SINDy-based simulation environment...")
    # sim_env = SINDySimEnv(sindy_model, env.action_space)
    # #Fix 1: Use regular PPO instead of MaskablePPO for SINDy
    
    # model_on_sindy = PPO('MlpPolicy', sim_env, verbose=1)

    # model_on_sindy.learn(total_timesteps=10000)