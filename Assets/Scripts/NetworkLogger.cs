using Unity.Netcode;
using UnityEngine;

public class NetworkLogger : MonoBehaviour
{
    private NetworkManager networkManager;

    private void Awake()
    {
        networkManager = GetComponent<NetworkManager>();
    }

    private void OnEnable()
    {
        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnect;
    }

    private void OnDisable()
    {
        networkManager.OnClientConnectedCallback -= OnClientConnected;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnect;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log("Client with id " + clientId + " connected.");
    }

    private void OnClientDisconnect(ulong clientId)
    {
        Debug.Log("Client with id " + clientId + " disconnected.");
    }
}
