using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections.Generic;

public class ClientControl : NetworkBehaviour
{    
//UI and Game Flow
    public GameObject sliderPrefab;
    public GameObject sliderLabelPrefab;
    public GameObject inputFieldPrefab;
    public Transform sliderDisplay;
    public Transform sliderLabelDisplay;
    public Transform inputFieldDisplay;
    public Button confirmButton;
    public Button autoGuessButton;
    public Toggle autoGuessToggle;
    public Text promptText;
    private GameObject[] sliderCells;
    private GameObject[] sliderLabels;
    private GameObject[] inputFields;
    private float[] guessVector;
    public MatrixVisualizer matrixVisualizer;

//Algorithm variables
    private float[] lastAverageGuess;
    private bool autoGuessEveryTurn = false;
    public float currentGuess; // Represents x_i(t)
    public float projectionParameter = 1f; // Represents P_i
    private List<int> neighbors; // List of neighbor indices for N_i(t)
    private Dictionary<int, float> neighborGuesses; // Stores x_j(t) values for neighbors


//Client Logic
    public override void OnNetworkSpawn()
    {
        Debug.Log("Client spawned...");
        if (IsClient && !IsServer)
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            matrixVisualizer.currentState = MatrixVisualizer.GameState.Connecting;

            UpdatePrompt("Connecting to server. Waiting for the server to set up the matrix...");
        }
    }

  public void OnMatrixSetupComplete()
    {
        if (!IsClient || IsServer)
        {
        Debug.Log("NewTurnUI should only be executed by clients, not the server.");
        return;
        }
        // This method is called by the server to notify the client that the matrix is ready
        matrixVisualizer.currentState = MatrixVisualizer.GameState.ViewingMatrix;
    if (matrixVisualizer == null)
    {
        Debug.LogError("matrixVisualizer is null!");
    }
    if (matrixVisualizer.matrixDisplay == null)
    {
        Debug.LogError("matrixDisplay is null!");
    }

    if (matrixVisualizer.matrixCellPrefab == null)
    {
        Debug.LogError("matrixCellPrefab is null!");
    }
        //convert network list to float array
    float[] resultArray = new float[matrixVisualizer.coefficientList.Count];
        for (int i = 0; i < matrixVisualizer.coefficientList.Count; i++)
        {
            resultArray[i] = matrixVisualizer.coefficientList[i];
        }
    matrixVisualizer.DisplayClientView();

    matrixVisualizer.GenerateMatrix(matrixVisualizer.totalRows.Value, matrixVisualizer.totalColumns.Value);
    matrixVisualizer.SetupGridLayout(matrixVisualizer.totalRows.Value, matrixVisualizer.totalColumns.Value);
        //update client cells
            for (int i = 0; i < matrixVisualizer.totalRows.Value; i++)
            {
               for (int j = 0; j < matrixVisualizer.totalColumns.Value; j++)
                {
                int index = i * matrixVisualizer.totalColumns.Value + j;
                matrixVisualizer.UpdateCellValue(i, j, resultArray[index]);
                }
            }
    matrixVisualizer.UpdateAugmentedMatrix(matrixVisualizer.augmentedVectorB);


        Debug.Log($"Generated matrix on client: {matrixVisualizer.totalRows.Value} rows x {matrixVisualizer.totalColumns.Value} columns");
        UpdatePrompt("Matrix setup complete. Viewing the matrix. Press confirm to start guessing.");
    }

    private void OnConfirmButtonClicked()
    {
        switch (matrixVisualizer.currentState)
        {
            case MatrixVisualizer.GameState.ViewingMatrix:
                // Transition to AdjustingSliders state
                ActivateSliders();
                matrixVisualizer. currentState = MatrixVisualizer.GameState.AdjustingSliders;
                Debug.Log("Adjust the sliders and press confirm to submit your guess.");
                break;

            case MatrixVisualizer.GameState.AdjustingSliders:
                // Gather guess from sliders and send to the server
                guessVector = GetGuessVector();
                SubmitGuessServerRpc(guessVector);
                Debug.Log("Guess submitted. Waiting for the server's response...");
                break;

        }
    }

    private void OnAutoGuessButtonClicked()
    {
        ApplyAutoGuess();
        Debug.Log("Auto-guess applied using the average values.");
    }


    private void OnAutoGuessToggleChanged(bool isOn)
    {
        autoGuessEveryTurn = isOn;
        Debug.Log("Auto-guess every turn: " + autoGuessEveryTurn);
    }

    private void ApplyAutoGuess()
    {
        if (matrixVisualizer.currentState == MatrixVisualizer.GameState.AdjustingSliders && lastAverageGuess != null)
        {
            for (int i = 0; i < sliderCells.Length; i++)
            {
                Slider slider = sliderCells[i].GetComponent<Slider>();
                slider.value = lastAverageGuess[i];
            }
        }
    }

   /* public void AutoGuessUsingAlgorithm()
{
    if (neighbors == null || neighbors.Count == 0)
    {
        Debug.LogWarning("No neighbors assigned for this client.");
        return;
    }

    // Compute m_i(t) - number of neighbors
    float m_i_t = neighbors.Count;

    // sum of guesses from all neighbors
    float sumNeighborGuesses = 0f;
    foreach (int neighborId in neighbors)
    {
        if (neighborGuesses.ContainsKey(neighborId))
        {
            sumNeighborGuesses += neighborGuesses[neighborId];
        }
    }

    // Apply equation to update the guess (x_i(t+1))
    float delta = projectionParameter * (m_i_t * currentGuess - sumNeighborGuesses);
    currentGuess = currentGuess - (1f / m_i_t) * delta;

    // Update the UI (slider) to reflect the new guess
    UpdateSliderWithGuess(currentGuess);
}*/

    public void SetAutoGuessToggle(bool autoGuessEnabled)
{
    autoGuessToggle.isOn = autoGuessEnabled;
    autoGuessEveryTurn = autoGuessEnabled; 
}

private void UpdateSliderWithGuess(float newGuess)
{
    for (int i = 0; i < sliderCells.Length; i++)
    {
        Slider slider = sliderCells[i].GetComponent<Slider>();
        slider.value = newGuess; // Set the slider to the new auto-guess value
    }
}

    private void ActivateSliders()
    {
        int numSliders = matrixVisualizer.totalColumns.Value; 
        sliderCells = new GameObject[numSliders];
        sliderLabels = new GameObject[numSliders];
        inputFields = new GameObject[numSliders];

        for (int i = 0; i < numSliders; i++)
        {
            // Instantiate slider
            GameObject sliderObj = Instantiate(sliderPrefab, sliderDisplay);
            Slider slider = sliderObj.GetComponent<Slider>();
            slider.minValue = -100;
            slider.maxValue = 100;
            slider.value = 0;
            sliderCells[i] = sliderObj;

            // Instantiate label
            GameObject labelObj = Instantiate(sliderLabelPrefab, sliderLabelDisplay);
            Text label = labelObj.GetComponent<Text>();
            label.text = $"X{i + 1}"; 
            sliderLabels[i] = labelObj;

            // Instantiate input field
            GameObject inputFieldObj = Instantiate(inputFieldPrefab, inputFieldDisplay);
            InputField inputField = inputFieldObj.GetComponent<InputField>();
            inputField.text = slider.value.ToString(); 
            inputFields[i] = inputFieldObj;

            // Listener to update slider when input field changes
            inputField.onValueChanged.AddListener(value => 
            {
                float newValue;
                if (float.TryParse(value, out newValue))
                {
                    slider.value = newValue;
                }
            });

            // Listener to update input field when slider changes
            slider.onValueChanged.AddListener(value => 
            {
                inputField.text = value.ToString();
            });
        }
    }

    private float[] GetGuessVector()
    {
        float[] guessVector = new float[sliderCells.Length];
        for (int i = 0; i < sliderCells.Length; i++)
        {
            Slider slider = sliderCells[i].GetComponent<Slider>();
            guessVector[i] = slider.value;
        }
        return guessVector;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitGuessServerRpc(float[] guess, ServerRpcParams rpcParams = default)
    {
        // Call server logic to compare guess
        Debug.Log($"Received guess from client: {rpcParams.Receive.SenderClientId}");
        FindObjectOfType<ServerControl>().CompareGuess(guess, rpcParams.Receive.SenderClientId);
    }

    private void UpdatePrompt(string message)
    {
        promptText.text = message;
    }

    public void NewTurnUI(Color[] colorVector, float[] averageGuess)
{
    matrixVisualizer.currentState = MatrixVisualizer.GameState.AdjustingSliders;
    lastAverageGuess = averageGuess;
    // Update UI to reflect how close the guess was and show the average guess
    for (int i = 0; i < sliderCells.Length; i++)
    {
        Slider slider = sliderCells[i].GetComponent<Slider>();
        Image handleImage = slider.handleRect.GetComponent<Image>();
        handleImage.color = colorVector[i]; // Set the color based on the comparison result

        // Display average guess as a visual indicator
        GameObject avgIndicator = new GameObject($"AvgIndicator_{i}");
        avgIndicator.transform.SetParent(slider.transform);
        RectTransform avgTransform = avgIndicator.AddComponent<RectTransform>();
        avgTransform.anchorMin = new Vector2(0.5f, 0.5f);
        avgTransform.anchorMax = new Vector2(0.5f, 0.5f);
        avgTransform.pivot = new Vector2(0.5f, 0.5f);
        avgTransform.localScale = Vector3.one;
        avgTransform.sizeDelta = new Vector2(10, 10); // Set appropriate size for the indicator

        // Set the position of the indicator based on the average guess value
        float sliderRange = slider.maxValue - slider.minValue;
        float normalizedValue = (averageGuess[i] - slider.minValue) / sliderRange;
        float indicatorPosition = normalizedValue * slider.GetComponent<RectTransform>().sizeDelta.x;
        avgTransform.anchoredPosition = new Vector2(indicatorPosition - (slider.GetComponent<RectTransform>().sizeDelta.x / 2), 0);

        // Add a visual component to represent the average indicator (e.g., an Image)
        Image avgImage = avgIndicator.AddComponent<Image>();
        avgImage.color = Color.blue; // Representing average with a blue color
    }

    // Apply auto-guess if toggle is enabled
    if (autoGuessEveryTurn)
    {
        ApplyAutoGuess();
        OnConfirmButtonClicked(); // Automatically confirm guess if auto-guess is enabled
    }

    UpdatePrompt("Adjust the sliders for the next guess.");
}


}
