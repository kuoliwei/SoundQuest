using System;
using System.Threading.Tasks;
using NativeWebSocket;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class WebSocketClient : MonoBehaviour
{
    public static WebSocketClient Instance { get; private set; }

    [SerializeField] private string serverUrl = "ws://127.0.0.1:8765";
    [SerializeField] private InputField webSocketUrlInputField;
    [SerializeField] Text console;
    [SerializeField] Text fullJsonContent;
    [SerializeField] Text jsonDataParse;
    private WebSocket websocket;
    private bool isConnecting = false;
    public event Action<string> OnMessageReceived;

    private bool shouldReconnect = true;
    private float reconnectDelay = 5f;
    private int jsonDataCount = 0;
    void Awake()
    {
        webSocketUrlInputField.text = serverUrl;
        //webSocketUrlInputField.text = "ws://192.168.50.25:8765";
        //webSocketUrlInputField.text = "ws://10.66.66.51:8765";
        webSocketUrlInputField.text = "ws://192.168.0.139:8765";

        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    void OnEnable()
    {
        OnMessageReceived += TryParseAndLog;
    }
    void OnDisable()
    {
        OnMessageReceived -= TryParseAndLog;
    }

    void Start()
    {
        Connect();
    }
    async void Connect()
    {
        if (websocket != null && websocket.State == WebSocketState.Connecting || isConnecting)
        {
            Debug.LogWarning("���b�s�u���A�������� Connect()");
            return;
        }

        isConnecting = true;

        websocket = new WebSocket(webSocketUrlInputField.text);

        websocket.OnOpen += () =>
        {
            Debug.Log("WebSocket Connected");
            console.text = "WebSocket Connected";
            isConnecting = false;
        };

        websocket.OnClose += (e) =>
        {
            Debug.LogWarning("WebSocket Closed. �N��X�����խ��s...");
            console.text = "WebSocket Closed. �N��X�����խ��s...";
            isConnecting = false;
            if (shouldReconnect)
            {
                Invoke(nameof(AttemptReconnect), reconnectDelay);
            }
        };

        websocket.OnError += (e) =>
        {
            Debug.LogError("WebSocket Error: " + e);
            console.text = "WebSocket Error: " + e;
            isConnecting = false;
            if (shouldReconnect)
            {
                Invoke(nameof(AttemptReconnect), reconnectDelay);
            }
        };

        websocket.OnMessage += (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            OnMessageReceived?.Invoke(message);
        };

        try
        {
            await websocket.Connect();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("�s�u���ѡG" + ex.Message);
            console.text = "�s�u���ѡG" + ex.Message;
            isConnecting = false;
            if (shouldReconnect)
            {
                Invoke(nameof(AttemptReconnect), reconnectDelay);
            }
        }
    }

    void AttemptReconnect()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            Debug.Log("�w�s�u�A���ݭ��s�s�u");
            return;
        }

        Debug.Log("���խ��s�s�u...");
        Connect();
    }

    public async void SendJson(object obj)
    {
        if (websocket.State == WebSocketState.Open)
        {
            string json = JsonUtility.ToJson(obj);
            Debug.Log("Sending: " + json);
            await websocket.SendText(json);
        }
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif
        //if (Input.GetKeyDown(KeyCode.Alpha1))
        //{
        //    Debug.Log("���U 1�G�e�X mode: solfege");
        //    SendJson(new Mode { mode = "solfege" });
        //}

        //if (Input.GetKeyDown(KeyCode.Alpha6))
        //{
        //    Debug.Log("���U 6�G�e�X mode: db");
        //    SendJson(new Mode { mode = "db" });
        //}
    }

    async void OnApplicationQuit()
    {
        await websocket.Close();
    }
    void TryParseAndLog(string json)
    {
        jsonDataCount++;
        fullJsonContent.text = $"��{jsonDataCount}��json�G\n{json}";
        try
        {
            Pitch pitch = JsonUtility.FromJson<Pitch>(json);
            jsonDataParse.text = $"mode�G{pitch.mode}\n" +
                $"solfege�G{pitch.solfege}\n" +
                $"window_sec�G{pitch.window_sec}";
            if (!string.IsNullOrEmpty(pitch.solfege))
            {
                Debug.Log($"������ Pitch�Gsolfege = {pitch.solfege}");
                return;
            }
        }
        catch { }

        try
        {
            Volume volume = JsonUtility.FromJson<Volume>(json);
            jsonDataParse.text = $"mode�G{volume.mode}\n" +
    $"solfege�G{volume.dB}\n" +
    $"window_sec�G{volume.window_sec}";
            if (!string.IsNullOrEmpty(volume.dB))
            {
                Debug.Log($"������ Volume�GdB = {volume.dB}");
                return;
            }
        }
        catch { }


        Debug.LogWarning("����L�k�ѪR�� JSON�G" + json);
    }

}
