from flask import Flask, request, jsonify
import torch
from sb3_contrib import MaskablePPO
from sb3_contrib.common.maskable.utils import get_action_masks
import numpy as np
from airplane_boarding import AirplaneEnv

app = Flask(__name__)

model = MaskablePPO.load("agents/MaskablePPO/PPO_32/manual_save_3000000.zip")
env = AirplaneEnv(num_of_rows=4, seats_per_row=5, num_of_plane_rows=4)  # match Unity logic

@app.route('/predict', methods=['POST'])
def predict():
    data = request.json
    obs = np.array(data['obs'], dtype=np.float32)

    if not hasattr(env, "lobby") or env.lobby is None:
        env.reset()

    print(f"Received observation from Unity: {obs.tolist()}\n\n")

    env.set_custom_observation(obs)  # optionally patch the env if needed
    mask = get_action_masks(env)
    action, _ = model.predict(obs, deterministic=True, action_masks=mask)

    print(f"action taken: {action}")

    return jsonify({'action': int(action)})

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)
