using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;
using WebSocketSharp.Server;

public class LocalWebSocketServer : MonoBehaviour
{
    private WebSocketServer wssv;
    [SerializeField] int port;
    private float dB = 0f;

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
            Debug.Log("�����l�T���G" + e.Data);
            try
            {
                var msg = JsonUtility.FromJson<Mode>(e.Data);
                if (!string.IsNullOrEmpty(msg.mode))
                {
                    Debug.Log($"�ѪR���\�Gmode = {msg.mode}");

                    // �����^��
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
                    Debug.LogWarning("JSON �����]�t mode ���");
                }
            }
            catch
            {
                Debug.LogError("�L�k�ѪR JSON �� Mode ���c�G" + e.Data);
            }
        }
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
            SendJson(new Pitch {mode = "solfege",  solfege = "Do", window_sec = "0.2"});

        if (Input.GetKeyDown(KeyCode.M))
            SendJson(new Pitch { mode = "solfege", solfege = "Mi", window_sec = "0.2" });

        if (Input.GetKeyDown(KeyCode.F))
            SendJson(new Pitch { mode = "solfege", solfege = "Fa", window_sec = "0.2" });

        if (Input.GetKeyDown(KeyCode.S))
            SendJson(new Pitch { mode = "solfege", solfege = "Sol", window_sec = "0.2" });

        if (Input.GetKeyDown(KeyCode.Alpha1))
            SendJson(new Volume { mode = "dB", dB = "10", window_sec = "0.2" });

        if (Input.GetKeyDown(KeyCode.Alpha2))
            SendJson(new Volume { mode = "dB", dB = "20", window_sec = "0.2" });

        if (Input.GetKeyDown(KeyCode.Alpha3))
            SendJson(new Volume { mode = "dB", dB = "30", window_sec = "0.2" });

        if (Input.GetKeyDown(KeyCode.Alpha4))
            SendJson(new Volume { mode = "dB", dB = "40", window_sec = "0.2" });

        if (Input.GetKeyDown(KeyCode.Alpha5))
            SendJson(new Volume { mode = "dB", dB = "50", window_sec = "0.2" });

        if (Input.GetKeyDown(KeyCode.Alpha6))
            SendJson(new Volume { mode = "dB", dB = "60", window_sec = "0.2" });

        if (Input.GetKeyDown(KeyCode.Alpha7))
            SendJson(new Volume { mode = "dB", dB = "70", window_sec = "0.2" });

        if (Input.GetKeyDown(KeyCode.Alpha8))
            SendJson(new Volume { mode = "dB", dB = "80", window_sec = "0.2" });

        if (Input.GetKeyDown(KeyCode.Alpha9))
            SendJson(new Volume { mode = "dB", dB = "90", window_sec = "0.2" });
        if (Input.GetKey(KeyCode.KeypadPlus))
        {
            dB += 10 * Time.deltaTime;
            dB = Mathf.Clamp(dB, 0f, 130f);
            SendJson(new Volume { mode = "dB", dB = $"{(int)dB}", window_sec = "0.2" });
        }
        if (Input.GetKey(KeyCode.KeypadMinus))
        {
            dB -= 10 * Time.deltaTime;
            dB = Mathf.Clamp(dB, 0f, 130f);
            SendJson(new Volume { mode = "dB", dB = $"{(int)dB}", window_sec = "0.2" });
        }
    }
    void SendJson<T>(T obj)
    {
        string json = JsonUtility.ToJson(obj);
        foreach (var client in wssv.WebSocketServices["/"].Sessions.Sessions)
        {
            client.Context.WebSocket.Send(json);
            Debug.Log("Server �D�ʰe�X�G" + json);
        }
    }

}
