from flask import Flask, request
from flask import jsonify
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
    # Create mask where 1 means the plane is available (not processed)
    # and 0 means it's already processed
    mask = [1 if obs != -1 else 0 for obs in observation]
    return np.array(mask, dtype=bool)

@app.route('/predict', methods=['POST'])
def predict():
    try:
        data = request.json
        obs = np.array(data['obs'], dtype=np.float32)
        
        # Simple fallback if model fails to load
        if model is None:
            valid_indices = [i for i, val in enumerate(obs) if val != -1]
            action = valid_indices[0] if valid_indices else 0
            return jsonify({'action': int(action)})
        
        # Compute action mask (only allow choosing unprocessed planes)
        mask = compute_action_mask(obs)
        
        # If no valid actions, return -1
        if not any(mask):
            return jsonify({'action': -1})
        
        # Get action from model
        action, _ = model.predict(observation=obs, deterministic=True, action_masks=mask)
        
        # Ensure action is within valid range
        valid_actions = np.where(mask)[0]
        if action not in valid_actions and len(valid_actions) > 0:
            action = valid_actions[0]  # Fallback to first valid action
        
        print(f"Agent selected plane index: {action}")
        return jsonify({'action': int(action)})
    
    except Exception as e:
        print(f"Error in predict: {e}")
        # Fallback: return first valid action
        valid_indices = [i for i, val in enumerate(data['obs']) if val != -1]
        action = valid_indices[0] if valid_indices else 0
        return jsonify({'action': int(action)})

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000, debug=True)
