from flask import Flask, request, jsonify
import torch
from sb3_contrib import MaskablePPO
from sb3_contrib.common.maskable.utils import get_action_masks
import numpy as np
from airplane_boarding import AirplaneEnv
app = Flask(__name__)

model = MaskablePPO.load("agents/MaskablePPO/PPO_33/manual_save_5400000.zip")
env = AirplaneEnv(num_of_rows=4, seats_per_row=5, num_of_plane_rows=4)  # match Unity logic


def compute_action_mask(observation):
    # Assume observation is a flat list of 40 values
    mask = []
    for i in range(0, 40, 2):
        if observation[i] == -1.0 and observation[i+1] == -1.0:
            mask.append(False)  # Already used
        else:
            mask.append(True)   # Available
    return np.array(mask, dtype=bool)



@app.route('/predict', methods=['POST'])
def predict():
    data = request.json
    obs = np.array(data['obs'], dtype=np.float32)

    if not hasattr(env, "lobby") or env.lobby is None:
        env.reset(seed = 42)

    print(f"Received observation from Unity: {obs.tolist()}\n\n")

    env.set_custom_observation(obs)  # optionally patch the env if needed
    mask = compute_action_mask(obs)
    action, _ = model.predict(observation = obs, deterministic=True, action_masks=mask)

    print(f"action taken: {action}\n\n\n")

    return jsonify({'action': int(action)})

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)
