using Edgegap;
using IO.Swagger.Model;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Networking;

public class EdgegapSampleHUD : MonoBehaviour
{
    /// <summary>
    /// Your private token. Beware that this is not safe to include in your application like this!
    /// </summary>
    [SerializeField] private string appToken;
    [SerializeField] private string appName;
    [SerializeField] private string appVersionName;
    private UnityTransport transport;
    private NetworkManager networkManager;
    private Deployment[] deployments = new Deployment[0];
    private Status pendingStatus;

    [SerializeField] private Vector2 drawOffset = new Vector2(10, 200);

    private void Awake()
    {
        transport = GetComponent<UnityTransport>();
        networkManager = GetComponent<NetworkManager>();
    }

#if !UNITY_SERVER
    private void Start()
    {
        StartCoroutine(GetDeploymentsList());
    }
#endif

#if !UNITY_SERVER
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(drawOffset, new Vector2(500, 500)));

        if (!networkManager.IsServer && !networkManager.IsClient && pendingStatus == null) {
            bool createRoom = GUILayout.Button("Create room (= Deploy)");
            bool createRoomAndConnect = GUILayout.Button("Create room & connect");
            if (createRoom || createRoomAndConnect) {
                // This use default sample ip in Europe
                // You may build your own system to get player public ip, aggregate it with his friends
                // so the server will be placed near all of them and not only near the room creator.
                DeployPostData deployPostData = new DeployPostData(
                    appName,
                    appVersionName,
                    new List<string>(new string[] {
                        "159.8.69.247",
                        "81.138.176.78",
                        "91.184.198.94",
                        "5.8.44.74"
                    })
                );
                StartCoroutine(DeployServer(deployPostData, createRoomAndConnect));
            }

            DrawDeploymentList();
        }
        GUILayout.EndArea();
    }
#endif

    private void DrawDeploymentList()
    {
        if (GUILayout.Button("Refresh list")) {
            StartCoroutine(GetDeploymentsList());
        }
        GUILayout.Label($"{deployments.Length} server{(deployments.Length > 1 ? "s" : "")}");

        foreach (Deployment d in deployments) {
            GUILayout.BeginHorizontal();
            GUI.enabled = d.Ready.HasValue && d.Ready.Value;
            PortMapping portMapping = d.Ports["netcode"];
            if (portMapping.External.HasValue && GUILayout.Button("Connect")) {
                Connect(d.PublicIp, (ushort)d.Ports["netcode"].External.Value, d.Fqdn);
            }
            GUI.enabled = true;
            if (GUILayout.Button("Delete")) {
                StartCoroutine(DeleteDeployment(d.RequestId));
            }
            GUILayout.Label(d.Fqdn + " " + d.Status);
            GUILayout.EndHorizontal();
        }
    }

    private void Connect(string ip, ushort port, string fqdn)
    {
        transport.ConnectionData.Address = ip;
        transport.ConnectionData.Port = port;
        pendingStatus = null;
        Debug.Log($"Connecting to {fqdn} [{ip}:{port}]");
        networkManager.StartClient();
    }

    private IEnumerator DeployServer(DeployPostData deployPostData, bool autoConnect)
    {
        UnityWebRequest www = CreateApiRequest("https://api.edgegap.com/v1/deploy", UnityWebRequest.kHttpVerbPOST, appToken, deployPostData);

        yield return www.SendWebRequest();
        yield return www.isDone;

        if (www.result != UnityWebRequest.Result.Success) {
            Debug.Log(www.error);
        } else {
            string jsonResponse = Encoding.UTF8.GetString(www.downloadHandler.data);
            Debug.Log(www.responseCode + " " + jsonResponse);
            Deployment requestResult = JsonConvert.DeserializeObject<Deployment>(jsonResponse);
            if (autoConnect) {
                do {
                    yield return new WaitForSeconds(3.0f);
                    yield return GetDeployementStatus(requestResult.RequestId);
                } while (pendingStatus.CurrentStatus != "Status.READY");
                Connect(pendingStatus.PublicIp, (ushort)pendingStatus.Ports["netcode"].External.Value, pendingStatus.Fqdn);
            } else {
                yield return new WaitForSeconds(1.0f);
                yield return GetDeploymentsList(); // fetch the status automatically the first time
            }
        }
    }

    private IEnumerator GetDeploymentsList()
    {
        using (UnityWebRequest www = CreateApiRequest("https://api.edgegap.com/v1/deployments", UnityWebRequest.kHttpVerbGET, appToken)) {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success) {
                Debug.Log(www.error);
            } else {
                string jsonResponse = Encoding.UTF8.GetString(www.downloadHandler.data);
                Debug.Log(www.responseCode + " " + jsonResponse);
                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.NullValueHandling = NullValueHandling.Ignore;
                PagginationPage<Deployment> result = JsonConvert.DeserializeObject<PagginationPage<Deployment>>(jsonResponse, settings);
                deployments = result.data;
            }
        }
    }

    private IEnumerator GetDeployementStatus(string request_id)
    {
        using (UnityWebRequest www = CreateApiRequest($"https://api.edgegap.com/v1/status/{request_id}", UnityWebRequest.kHttpVerbGET, appToken)) {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success) {
                Debug.Log(www.error);
            } else {
                string jsonResponse = Encoding.UTF8.GetString(www.downloadHandler.data);
                Debug.Log(www.responseCode + " " + jsonResponse);
                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.NullValueHandling = NullValueHandling.Ignore;
                pendingStatus = JsonConvert.DeserializeObject<Status>(jsonResponse, settings);
            }
        }
    }

   private IEnumerator DeleteDeployment(string request_id)
    {
        using (UnityWebRequest www = CreateApiRequest($"https://api.edgegap.com/v1/stop/{request_id}", UnityWebRequest.kHttpVerbDELETE, appToken)) {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success) {
                Debug.Log(www.error);
            } else {
                string jsonResponse = Encoding.UTF8.GetString(www.downloadHandler.data);
                Debug.Log(www.responseCode + " " + jsonResponse);
                if (networkManager.IsConnectedClient) {
                    networkManager.Shutdown();
                }
            }
        }
        StartCoroutine(GetDeploymentsList());
    }

    public static UnityWebRequest CreateApiRequest(string url, string method, string token, object body = null)
    {
        string bodyString = null;
        if (body is string) {
            bodyString = (string)body;
        } else if (body != null) {
            bodyString = JsonConvert.SerializeObject(body);
        }

        var request = new UnityWebRequest();
        request.url = url;
        request.method = method;
        request.downloadHandler = new DownloadHandlerBuffer();
        request.uploadHandler = new UploadHandlerRaw(string.IsNullOrEmpty(bodyString) ? null : Encoding.UTF8.GetBytes(bodyString));
        request.SetRequestHeader("Accept", "application/json");
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", token);
        request.timeout = 30;
        return request;
    }
}

public struct PagginationPage<T>
{
    public T[] data;
    public int total_count;
    public Paggination pagination;
    //public string messages;
}

public struct Paggination
{
    public int number;
    public bool has_next;
    public bool has_previous;
}
