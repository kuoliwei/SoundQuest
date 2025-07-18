using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

public class LocalWebSocketServer : MonoBehaviour
{
    private WebSocketServer wssv;
    [SerializeField] int port;

    void Start()
    {
        wssv = new WebSocketServer(port);

        wssv.AddWebSocketService<TestService>("/");

        wssv.Start();

        Debug.Log($"Local WebSocket server started at ws://localhost:{port}");
    }

    void OnApplicationQuit()
    {
        if (wssv != null && wssv.IsListening)
        {
            wssv.Stop();
            Debug.Log("Local WebSocket server stopped.");
        }
    }

    public class TestService : WebSocketBehavior
    {
        protected override void OnMessage(MessageEventArgs e)
        {
            Debug.Log("收到原始訊息：" + e.Data);

            try
            {
                var msg = JsonUtility.FromJson<Mode>(e.Data);
                if (!string.IsNullOrEmpty(msg.mode))
                {
                    Debug.Log($"解析成功：mode = {msg.mode}");

                    // 模擬回傳
                    //if (msg.mode == "6")
                    //{
                    //    Send("{\"solfege\":\"Do\"}");
                    //}
                    //else if (msg.mode == "16")
                    //{
                    //    Send("{\"dB\":\"70\"}");
                    //}
                }
                else
                {
                    Debug.LogWarning("JSON 中未包含 mode 欄位");
                }
            }
            catch
            {
                Debug.LogError("無法解析 JSON 為 Mode 結構：" + e.Data);
            }
        }
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
            SendJson(new Pitch { solfege = "Do" });

        if (Input.GetKeyDown(KeyCode.R))
            SendJson(new Pitch { solfege = "Re" });

        if (Input.GetKeyDown(KeyCode.M))
            SendJson(new Pitch { solfege = "Mi" });

        if (Input.GetKeyDown(KeyCode.S))
            SendJson(new Pitch { solfege = "Sol" });

        if (Input.GetKeyDown(KeyCode.Alpha5))
            SendJson(new Volume { dB = "50" });

        if (Input.GetKeyDown(KeyCode.Alpha7))
            SendJson(new Volume { dB = "70" });

        if (Input.GetKeyDown(KeyCode.Alpha9))
            SendJson(new Volume { dB = "90" });
    }
    void SendJson<T>(T obj)
    {
        string json = JsonUtility.ToJson(obj);
        foreach (var client in wssv.WebSocketServices["/"].Sessions.Sessions)
        {
            client.Context.WebSocket.Send(json);
            Debug.Log("Server 主動送出：" + json);
        }
    }

}
