using System;
using System.Threading.Tasks;
using NativeWebSocket;
using Unity.VisualScripting;
using UnityEngine;

public class WebSocketClient : MonoBehaviour
{
    public static WebSocketClient Instance { get; private set; }

    [SerializeField] private string serverUrl = "ws://localhost:8080";
    private WebSocket websocket;

    public event Action<string> OnMessageReceived;

    private bool shouldReconnect = true;
    private float reconnectDelay = 5f;

    void Awake()
    {
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
        websocket = new WebSocket(serverUrl);

        websocket.OnOpen += () => Debug.Log("WebSocket Connected");
        websocket.OnClose += (e) =>
        {
            Debug.LogWarning("WebSocket Closed. 將於幾秒後嘗試重連...");
            if (shouldReconnect)
            {
                Invoke(nameof(AttemptReconnect), reconnectDelay);
            }
        };
        websocket.OnError += (e) =>
        {
            Debug.LogError("WebSocket Error: " + e);
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
            Debug.LogWarning("連線失敗：" + ex.Message);
            if (shouldReconnect)
            {
                Invoke(nameof(AttemptReconnect), reconnectDelay);
            }
        }
    }

    void AttemptReconnect()
    {
        Debug.Log("嘗試重新連線...");
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
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("按下 1：送出 mode: 6");
            SendJson(new Mode { mode = "6" });
        }

        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            Debug.Log("按下 6：送出 mode: 16");
            SendJson(new Mode { mode = "16" });
        }
    }

    async void OnApplicationQuit()
    {
        await websocket.Close();
    }
    void TryParseAndLog(string json)
    {
        try
        {
            Pitch pitch = JsonUtility.FromJson<Pitch>(json);
            if (!string.IsNullOrEmpty(pitch.solfege))
            {
                Debug.Log($"偵測到 Pitch：solfege = {pitch.solfege}");
                return;
            }
        }
        catch { }

        try
        {
            Volume volume = JsonUtility.FromJson<Volume>(json);
            if (!string.IsNullOrEmpty(volume.dB))
            {
                Debug.Log($"偵測到 Volume：dB = {volume.dB}");
                return;
            }
        }
        catch { }

        Debug.LogWarning("收到無法解析的 JSON：" + json);
    }

}
