using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections.Generic;

public class ServerControl : NetworkBehaviour
{
    // UI Elements
    public InputField inputField;
    public Button confirmButton;
    public Text promptViewText;
    public Toggle autoGuessAllClientsToggle;
    public MatrixVisualizer matrixVisualizer;

    // Data Structures
    private int totalRows;
    private int totalColumns;
    private int currentRowIndex = 0;
    private int currentColumnIndex = 0;
    private List<float[]> clientGuesses = new List<float[]>();
    private float[] solutionVector;
    
    

    public override void OnNetworkSpawn()
    {
        if (IsServer) 
        {
            autoGuessAllClientsToggle.onValueChanged.AddListener(OnAutoGuessAllClientsToggleChanged);
            Debug.Log("ServerControl OnNetworkSpawn called, setting up button listener...");
            confirmButton.onClick.AddListener(OnConfirmInput);
            
            
            StartGameSetupFlow();
        }
    }

    private void StartGameSetupFlow()
    {
        matrixVisualizer.currentState = MatrixVisualizer.GameState.SetRows;
        Debug.Log("Game setup flow started. Prompting user to enter number of rows...");
        UpdatePrompt("Enter the number of rows:");
    }
    private void OnAutoGuessAllClientsToggleChanged(bool isOn)
    {
        if (IsServer)
        {
            NotifyClientsAutoGuessChangedClientRpc(isOn);
        }
    }
    [ClientRpc]
    private void NotifyClientsAutoGuessChangedClientRpc(bool autoGuessEnabled)
    {
        // This method called on every client
        ClientControl clientControl = FindObjectOfType<ClientControl>();
        if (clientControl != null)
        {
            clientControl.SetAutoGuessToggle(autoGuessEnabled);
        }
    }

    private void OnConfirmInput()
    {
        Debug.Log("Confirmed...");
        switch (matrixVisualizer.currentState)
        {
            case MatrixVisualizer.GameState.SetRows:
                if (int.TryParse(inputField.text, out totalRows))
                {
                    Debug.Log($"Number of rows set to: {totalRows}");
                    matrixVisualizer.SetTotalRows(totalRows);
                    matrixVisualizer.currentState = MatrixVisualizer.GameState.SetColumns;
                    UpdatePrompt("Enter the number of columns:");
                }
                else
                {
                    UpdatePrompt("Invalid input. Please enter a valid number of rows:");
                }
                break;

            case MatrixVisualizer.GameState.SetColumns:
                if (int.TryParse(inputField.text, out totalColumns))
                {
                    matrixVisualizer.SetTotalColumns(totalColumns);
                    matrixVisualizer.currentState = MatrixVisualizer.GameState.SetCoefficients;
                    currentRowIndex = 0;
                    currentColumnIndex = 0;
                    matrixVisualizer.SetupGridLayout(totalRows, totalColumns);
                    matrixVisualizer.GenerateMatrix(totalRows, totalColumns);
                    UpdatePrompt($"Enter coefficient A(1,1):");
                }
                else
                {
                    UpdatePrompt("Invalid input. Please enter a valid number of columns:");
                }
                break;

            case MatrixVisualizer.GameState.SetCoefficients:
                if (float.TryParse(inputField.text, out float coefficientValue))
                {
                    // From flattened list
                    matrixVisualizer.UpdateMatrixData(currentRowIndex, currentColumnIndex, coefficientValue);
                    currentColumnIndex++;

                    if (currentColumnIndex >= totalColumns)
                    {
                        currentRowIndex++;
                        currentColumnIndex = 0;
                    }

                    if (currentRowIndex < totalRows)
                    {
                        UpdatePrompt($"Enter coefficient A({currentRowIndex + 1},{currentColumnIndex + 1}):");
                    }
                    else
                    {

                        matrixVisualizer.currentState = MatrixVisualizer.GameState.SetSolution;
                        solutionVector = new float[totalColumns];
                        currentColumnIndex = 0;
                        UpdatePrompt("Enter solution value x1:");
                    }
                }
                else
                {
                    UpdatePrompt("Invalid input. Please enter a valid coefficient value:");
                }
                break;

            case MatrixVisualizer.GameState.SetSolution:
                if (float.TryParse(inputField.text, out float solutionValue))
                {
                    solutionVector[currentColumnIndex] = solutionValue;
                    currentColumnIndex++;

                    if (currentColumnIndex < totalColumns)
                    {
                        UpdatePrompt($"Enter solution value x{currentColumnIndex + 1}:");
                    }
                    else
                    {
                        CalculateAugmentedVectorB();
                        matrixVisualizer.SetSolutionVector(solutionVector);
                        matrixVisualizer.currentState = MatrixVisualizer.GameState.ConfirmSetup;
                        UpdatePrompt("Game board setup complete. Press confirm to continue.");
                    }
                }
                else
                {
                    UpdatePrompt("Invalid input. Please enter a valid solution value:");
                }
                break;

            case MatrixVisualizer.GameState.ConfirmSetup:
                matrixVisualizer.currentState = MatrixVisualizer.GameState.StartGame;
                UpdatePrompt("Press confirm to start the game.");
                break;

            case MatrixVisualizer.GameState.StartGame:
                StartGame();
                break;
        }
    }
    
    private void StartGame()
    {
        Debug.Log("Game started!");
        NotifyClientsMatrixSetupCompleteClientRpc();
    }

    [ClientRpc]
    private void NotifyClientsMatrixSetupCompleteClientRpc()
    {
        Debug.Log("Notifying clients that the matrix setup is complete...");
        FindObjectOfType<ClientControl>()?.OnMatrixSetupComplete();
    }

    private void CalculateAugmentedVectorB()
    {
        float[] augmentedVectorB = new float[totalRows];
        for (int i = 0; i < totalRows; i++)
        {
            augmentedVectorB[i] = 0;
            for (int j = 0; j < totalColumns; j++)
            {
                augmentedVectorB[i] += matrixVisualizer.GetCoefficient(i, j) * solutionVector[j];
            }
        }
        matrixVisualizer.SetAugmentedVectorB(augmentedVectorB);
    }

    private void UpdatePrompt(string message)
    {
        promptViewText.text = message;
    }

    public void CompareGuess(float[] guess, ulong clientId)
    {
        
        clientGuesses.Add(guess);

        // Wait for all clients to submit their guesses
        if (clientGuesses.Count == NetworkManager.Singleton.ConnectedClients.Count - 1) // Assuming 1 host and n clients
        {
            // Calculate the average guess
            float[] averageGuess = new float[guess.Length];
            for (int i = 0; i < guess.Length; i++)
            {
                averageGuess[i] = 0;
                foreach (float[] clientGuess in clientGuesses)
                {
                    averageGuess[i] += clientGuess[i];
                }
                averageGuess[i] /= clientGuesses.Count;
            }

            // Determine color vector based on comparison
            Color[] colorVector = new Color[guess.Length];
            for (int i = 0; i < guess.Length; i++)
            {
                float difference = Mathf.Abs(averageGuess[i] - solutionVector[i]);
                if (difference == 0)
                {
                    colorVector[i] = Color.yellow; // Exact match
                }
                else if (difference <= 10)
                {
                    colorVector[i] = Color.green; // Close
                }
                else
                {
                    colorVector[i] = Color.red; // Far off
                }
            }

            // Notify all clients
            matrixVisualizer.currentState = MatrixVisualizer.GameState.ViewingMatrix;
            Debug.Log("Current state is " + matrixVisualizer.currentState.ToString());
            NotifyClientsNewTurnUIClientRpc(colorVector, averageGuess);

            // Clear guesses for the next round
            clientGuesses.Clear();
        }
    }

    [ClientRpc]
    private void NotifyClientsNewTurnUIClientRpc(Color[] colorVector, float[] averageGuess)
    {
        if (colorVector == null || averageGuess == null)
         {
        Debug.LogError("Received invalid parameters for NotifyClientsNewTurnUIClientRpc.");
        return;
         }
        FindObjectOfType<ClientControl>()?.NewTurnUI(colorVector, averageGuess);
    }
}

