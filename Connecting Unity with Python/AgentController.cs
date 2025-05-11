using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

[System.Serializable]
public class ObservationData
{
    public int[] obs;
}

public class AgentController : MonoBehaviour
{
    int[] currentObservation = new int[40];
    bool isDone = false;

    void Start()
    {
        GenerateInitialObservation();
        StartCoroutine(SendObservationLoop());
    }

    void GenerateInitialObservation()
    {
        int x = 0;
        for (int i = 0; i < 40; i += 2)
        {
            currentObservation[i] = x; // Row (0 to 19)
            currentObservation[i + 1] = Random.Range(0, 2); // Boolean (0 or 1)
            x++;
        }
    }

    IEnumerator SendObservationLoop()
    {
        while (!isDone)
        {
            yield return SendObservation(currentObservation);

            // Check if all values are -1 to mark as done
            isDone = true;
            foreach (int val in currentObservation)
            {
                if (val != -1f)
                {
                    isDone = false;
                    break;
                }
            }

            //yield return new WaitForSeconds(0.2f); // Optional delay for pacing
        }

        Debug.Log("Episode finished: all observations are -1");
    }

    IEnumerator SendObservation(int[] observation)
    {
        var json = JsonUtility.ToJson(new ObservationData { obs = observation });
        var request = new UnityWebRequest("http://127.0.0.1:5000/predict", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Raw response: " + request.downloadHandler.text);
            int action = int.Parse(request.downloadHandler.text.Split(':')[1].Replace("}", ""));
            Debug.Log("Received action: " + action);
            ApplyAction(action);
        }
        else
        {
            Debug.LogError("Error sending observation: " + request.error);
        }
    }

    void ApplyAction(int action)
    {
        //Debug.Log("Apply action logic for action: " + action + currentObservation[index+1]);

        // Example logic: mark the selected pair as -1 (simulate masking used rows/seats)
        int index = action * 2;
        Debug.Log("Apply action logic for action: " + action + " Priority: " + currentObservation[index + 1]);

        if (index + 1 < currentObservation.Length)
        {
            currentObservation[index] = -1;
            currentObservation[index + 1] = -1;
        }

    }
}
