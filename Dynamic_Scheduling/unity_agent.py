from flask import Flask, request, jsonify
import numpy as np
from sb3_contrib import MaskablePPO
import torch

app = Flask(__name__)

# Load your pre-trained model
try:
    model = MaskablePPO.load("Dynamic_Scheduling/agents/MaskablePPO/PPO_33/manual_save_5400000.zip")
    print("Successfully loaded the pre-trained model")
except Exception as e:
    print(f"Error loading model: {e}")
    model = None

def compute_action_mask(observation):
    # Mask only the plane entries (every 2nd value is a priority)
    # We assume observation = [id, prio, id, prio, ...]
    return np.array([
        1 if observation[i] != -1 else 0
        for i in range(0, len(observation), 2)
    ], dtype=bool)

@app.route('/predict', methods=['POST'])
def predict():
    try:
        data = request.json
        raw_obs = data['obs']

        obs = np.array(raw_obs, dtype=np.float32)

        # Reshape if flat (e.g., shape = (40,))
        if obs.ndim == 1:
            obs = obs.reshape(1, -1)

        # Validate shape
        if obs.shape[1] != 40:
            raise ValueError(f"Invalid observation shape: {obs.shape}")

        # If model failed to load, return fallback
        if model is None:
            valid_indices = [i for i in range(0, len(raw_obs), 2) if raw_obs[i] != -1]
            action = valid_indices[0] if valid_indices else -1
            return jsonify({'action': int(action)})

        # Compute mask (1 per plane)
        mask = compute_action_mask(raw_obs)

        if not any(mask):
            return jsonify({'action': -1})

        action, _ = model.predict(observation=obs, deterministic=True, action_masks=mask)

        # Validate the action
        valid_actions = np.where(mask)[0]
        if action not in valid_actions and len(valid_actions) > 0:
            action = valid_actions[0]

        print(f"Agent selected plane index: {action}")
        return jsonify({'action': int(action)})

    except Exception as e:
        print(f"Error in predict: {e}")
        valid_indices = [i for i in range(0, len(data['obs']), 2) if data['obs'][i] != -1]
        action = valid_indices[0] if valid_indices else -1
        return jsonify({'action': int(action)})

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000, debug=True)
