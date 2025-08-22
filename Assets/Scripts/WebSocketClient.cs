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
            Debug.LogWarning("正在連線中，忽略此次 Connect()");
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
            Debug.LogWarning("WebSocket Closed. 將於幾秒後嘗試重連...");
            console.text = "WebSocket Closed. 將於幾秒後嘗試重連...";
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
            Debug.LogWarning("連線失敗：" + ex.Message);
            console.text = "連線失敗：" + ex.Message;
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
            Debug.Log("已連線，不需重新連線");
            return;
        }

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
        //if (Input.GetKeyDown(KeyCode.Alpha1))
        //{
        //    Debug.Log("按下 1：送出 mode: solfege");
        //    SendJson(new Mode { mode = "solfege" });
        //}

        //if (Input.GetKeyDown(KeyCode.Alpha6))
        //{
        //    Debug.Log("按下 6：送出 mode: db");
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
        fullJsonContent.text = $"第{jsonDataCount}筆json：\n{json}";
        try
        {
            Pitch pitch = JsonUtility.FromJson<Pitch>(json);
            jsonDataParse.text = $"mode：{pitch.mode}\n" +
                $"solfege：{pitch.solfege}\n" +
                $"window_sec：{pitch.window_sec}";
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
            jsonDataParse.text = $"mode：{volume.mode}\n" +
    $"solfege：{volume.dB}\n" +
    $"window_sec：{volume.window_sec}";
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
