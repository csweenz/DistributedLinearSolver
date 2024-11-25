using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;

public class NetworkController : MonoBehaviour
{
    public Button startHostButton;
    public Button startClientButton;

    private void Start()
    {
        // Add listeners for the buttons
        startHostButton.onClick.AddListener(StartHost);
        startClientButton.onClick.AddListener(StartClient);
    }

    public void StartHost()
{
    if (NetworkManager.Singleton != null)
    {
        Debug.Log("Starting Host...");
        NetworkManager.Singleton.StartHost();
        Debug.Log("Starting Host in Scene: " + SceneManager.GetActiveScene().name);
        startHostButton.gameObject.SetActive(false); 
        startClientButton.gameObject.SetActive(false);
    }
    else
    {
        Debug.LogError("NetworkManager is not set up correctly.");
    }
}

public void StartClient()
{
    if (NetworkManager.Singleton != null)
    {
        Debug.Log("Starting Client...");
        NetworkManager.Singleton.StartClient();
        Debug.Log("Starting Client in Scene: " + SceneManager.GetActiveScene().name);
        startHostButton.gameObject.SetActive(false); 
        startClientButton.gameObject.SetActive(false);
    }
    else
    {
        Debug.LogError("NetworkManager is not set up correctly.");
    }
}

}
