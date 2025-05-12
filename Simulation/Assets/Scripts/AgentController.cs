using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

[System.Serializable]
public class ObservationData
{
    public float[] obs;
}

public class AgentController : MonoBehaviour
{
    public event Action<int> OnDecisionMade;
    private bool usePythonServer = true; // Set to false to use simple fallback
    private bool isProcessing = false;

    private void HandleFallback(Plane[] planes)
    {
        if (planes == null || planes.Length == 0)
        {
            Debug.LogError("Cannot handle fallback: planes array is null or empty");
            return;
        }

        // Log all planes for debugging
        Debug.Log("Available planes for fallback:");
        for (int i = 0; i < planes.Length; i++)
        {
            if (planes[i] != null)
            {
                Debug.Log($"  Plane {i}: Priority={planes[i].HighPriority}, Processed={planes[i].IsProcessed}");
            }
        }
        
        // First try to find an unprocessed plane
        int fallbackAction = Array.FindIndex(planes, p => p != null && !p.IsProcessed);
        
        // If no unprocessed planes found, find any plane
        if (fallbackAction == -1)
        {
            Debug.LogWarning("No unprocessed planes found, looking for any plane");
            fallbackAction = Array.FindIndex(planes, p => p != null);
            
            if (fallbackAction != -1)
            {
                Debug.LogWarning($"Found plane at index {fallbackAction} but it's already processed");
                // If we're here, all planes are processed
                // We need to reset the processed state or handle this case appropriately
                // For now, just select the first plane and let the simulation handle it
            }
        }
        
        if (fallbackAction == -1)
        {
            Debug.LogError("No valid planes found for fallback");
            return;
        }
        
        Debug.Log($"Falling back to plane {fallbackAction}");
        OnDecisionMade?.Invoke(fallbackAction);
    }

    public void RequestDecision(Plane[] planes)
    {
        if (isProcessing) return;
        
        if (usePythonServer)
        {
            StartCoroutine(SendToPythonServer(planes));
        }
        else
        {
            // Simple fallback: pick first available plane
            int decision = Array.FindIndex(planes, p => !p.IsProcessed);
            Debug.Log($"Agent selected plane index (fallback): {decision}");
            OnDecisionMade?.Invoke(decision);
        }
    }

    private IEnumerator SendToPythonServer(Plane[] planes)
    {
        isProcessing = true;
        
        if (planes == null || planes.Length == 0)
        {
            Debug.LogError("No planes provided to SendToPythonServer");
            isProcessing = false;
            yield break;
        }
        
        // Log incoming planes data for debugging
        Debug.Log($"Processing {planes.Length} planes for decision");
        for (int i = 0; i < planes.Length; i++)
        {
            if (planes[i] != null)
            {
                Debug.Log($"Plane {i}: Priority={planes[i].HighPriority}, Processed={planes[i].IsProcessed}");
            }
        }
        
        // Create observation array with 20 elements (1 value per plane)
        // Each plane is represented by a single value:
        // -1: No plane
        // 0: Normal priority
        // 1: High priority
        float[] observation = new float[20];
        
        // Initialize all values to -1 (no plane)
        for (int i = 0; i < observation.Length; i++)
        {
            observation[i] = -1f;
        }
        
        // Fill in the actual plane data (only unprocessed planes)
        for (int i = 0; i < planes.Length && i < 20; i++)
        {
            if (planes[i] != null && !planes[i].IsProcessed)
            {
                observation[i] = (planes[i].HighPriority != 0) ? 1f : 0f;
                
                // Log the observation for this plane
                Debug.Log($"Plane {i}: Priority={planes[i].HighPriority}, Processed={planes[i].IsProcessed} => Observation={observation[i]}");
            }
        }
        
        Debug.Log($"Sending observation to Python server (first 10 values): {string.Join(", ", observation.Take(10))}...");
        
        // Create the request outside the try-catch to avoid yield in try-catch
        var obsData = new ObservationData { obs = observation };
        var json = JsonUtility.ToJson(obsData);
        
        using (var request = new UnityWebRequest("http://127.0.0.1:5000/predict", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 5; // 5 second timeout

            Debug.Log($"Sending request to Python server with data: {json}");
            
            // Send the request and wait for it to complete
            var asyncOp = request.SendWebRequest();
            float startTime = Time.time;
            
            // Wait for the request to complete or timeout
            while (!asyncOp.isDone)
            {
                // Check for timeout
                if (Time.time - startTime > request.timeout)
                {
                    request.Abort();
                    Debug.LogError("Request to Python server timed out");
                    HandleFallback(planes);
                    isProcessing = false;
                    yield break;
                }
                yield return null;
            }

            // Handle the response
            try
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text.Trim();
                    Debug.Log($"Raw server response: {response}");
                    
                    if (string.IsNullOrEmpty(response))
                    {
                        Debug.LogError("Received empty response from server");
                        HandleFallback(planes);
                        isProcessing = false;
                        yield break;
                    }
                    
                    try
                    {
                        // Try to parse the response as JSON
                        var jsonResponse = JsonUtility.FromJson<ServerResponse>(response);
                        if (jsonResponse != null)
                        {
                            Debug.Log($"Successfully parsed action: {jsonResponse.action}");
                            ProcessServerResponse(jsonResponse.action, planes);
                        }
                        else
                        {
                            Debug.LogError("Failed to parse JSON response");
                            HandleFallback(planes);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error parsing response: {ex.Message}");
                        HandleFallback(planes);
                    }
                }
                else
                {
                    Debug.LogError($"Error connecting to Python server: {request.error}. URL: {request.url}");
                    if (!string.IsNullOrEmpty(request.downloadHandler?.text))
                    {
                        Debug.LogError($"Server response: {request.downloadHandler.text}");
                    }
                    HandleFallback(planes);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception while processing server response: {ex.Message}\n{ex.StackTrace}");
                HandleFallback(planes);
            }
            finally
            {
                isProcessing = false;
            }
        }
    }
    
    [System.Serializable]
    private class ServerResponse
    {
        public int action = -1;
        
        // This will help with debugging
        public override string ToString()
        {
            return $"ServerResponse {{ action = {action} }}";
        }
    }

    private void ProcessServerResponse(int action, Plane[] planes)
    {
        try
        {
            Debug.Log($"Processing server response action: {action}");
            
            // Validate the action
            if (planes == null || planes.Length == 0)
            {
                Debug.LogError("Planes array is null or empty");
                return;
            }
            Debug.Log($"Parsed action from server: {action}");
            
            // Log available planes for debugging
            Debug.Log($"Available planes (index: priority, processed):");
            for (int i = 0; i < planes.Length; i++)
            {
                if (planes[i] != null)
                {
                    Debug.Log($"  {i}: Priority={planes[i].HighPriority}, Processed={planes[i].IsProcessed}");
                }
            }
            
            // Check if action is -1 (no valid action)
            if (action == -1)
            {
                Debug.Log("Server indicated no valid action available");
                
                // Check if all planes are processed
                bool allProcessed = planes.All(p => p == null || p.IsProcessed);
                if (allProcessed)
                {
                    Debug.Log("All planes are processed. No more actions needed.");
                    // Invoke with -1 to indicate no action is needed
                    OnDecisionMade?.Invoke(-1);
                    return;
                }
                
                HandleFallback(planes);
                return;
            }
            
            // Validate action is within bounds
            if (action < 0 || action >= planes.Length)
            {
                Debug.LogError($"Action {action} is out of bounds (0-{planes.Length - 1})");
                HandleFallback(planes);
                return;
            }
            
            // Validate the selected plane
            if (planes[action] == null)
            {
                Debug.LogError($"Selected plane {action} is null");
                HandleFallback(planes);
                return;
            }
            
            if (planes[action].IsProcessed)
            {
                Debug.LogError($"Selected plane {action} is already processed");
                HandleFallback(planes);
                return;
            }
            
            // If we got here, the action is valid
            Debug.Log($"Agent selected valid plane index: {action}");
            OnDecisionMade?.Invoke(action);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in JSON processing: {ex.Message}\n{ex.StackTrace}");
            HandleFallback(planes);
        }
    }
}
