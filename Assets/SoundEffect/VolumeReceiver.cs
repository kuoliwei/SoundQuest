using UnityEngine;
using NativeWebSocket;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Linq;
using TMPro;
namespace AIStageBGApp
{

    public class VolumeReceiver : MonoBehaviour
    {

        WebSocket ws;
        //[SerializeField] StageBgPlayer bgplayer;

        //public AudioReactiveBubbleController audioReactiveBubbleController;
        //public BubblesEffectController bubblesEffectController;
        public string ipaddr = "127.0.0.1";
        WebSocket volws;
        public string volport = "5566";
        public float lastVolume = 0;

        public AudioEffectController audioEffectController;

        public bool debug = false;
        public float debugvol = 0.5f;
        public TMP_Text vol_txt;

        //public AddEffectConfig EffectConfig;
        public TMP_Text vol_out_txt;
        public float volcd = 0;

        public float idletime = 0;
        private bool isReconnecting = false;



        async void Start()
        {

            volws = new WebSocket("ws://" + ipaddr + ":" + volport);
            volws.OnMessage += OnVolReceive;

            volws.OnError += (e) =>
            {
                Debug.LogError("WebSocket 錯誤: " + e);
            };

            volws.OnClose += (e) =>
            {
                Debug.LogWarning("WebSocket 連線關閉");
                volws = null;
            };
            Debug.Log("✅ 已連上 WebSocket: ws://" + ipaddr + ":" + volport);
            await volws.Connect();  // ✅ 正確使用 async

        }


        async void ReStart()
        {
            if (volws != null)
            {
                volws.OnMessage -= OnVolReceive; // ✅ 移除事件綁定
                await volws.Close();             // ✅ 關閉舊連線
                volws = null;
            }

            volws = new WebSocket("ws://" + ipaddr + ":" + volport);
            volws.OnMessage += OnVolReceive;

            volws.OnError += (e) =>
            {
                Debug.LogError("WebSocket 錯誤: " + e);
            };

            volws.OnClose += (e) =>
            {
                Debug.LogWarning("WebSocket 連線關閉");
                volws = null;
            };

            Debug.Log("✅ 重新連線 WebSocket: ws://" + ipaddr + ":" + volport);

            await volws.Connect();

            isReconnecting = false;
        }


        void OnVolReceive(byte[] bytes)
        {
            string json = Encoding.UTF8.GetString(bytes);
            var obj = JObject.Parse(json);
            if (!debug)
            {
                if (obj.ContainsKey("volume"))
                {
                    //Debug.Log("volume:"+obj["volume"] );

                    //Debug.Log("JSON RAW: " + json);
                    List<double> spectrumDouble = obj["spectrum"]?.ToObject<List<double>>();
                    List<float> audioData = spectrumDouble?.Select(d => (float)d).ToList();


                    float vol = Mathf.Abs(obj["volume"].Value<float>()) / 100;

                    lastVolume = Mathf.Abs(obj["volume"].Value<float>());
                    if (audioEffectController != null)
                    {
                        if (audioData != null)
                        {
                            audioEffectController.UpdateWaveform(audioData);
                        }
                        audioEffectController.SetVolume(vol);
                    }

                    //if (bgplayer != null)
                    //{
                    //    bgplayer.ReceivedAudio(vol);
                    //}


                }
            }
            /*

             string json = Encoding.UTF8.GetString(bytes);
            var obj = JObject.Parse(json);
            string text = obj["text"]?.ToString() ?? "";
            Debug.Log("收到文字：" + text);

            List<float> audioData = obj["audio"]?.ToObject<List<float>>();

            // 安全檢查 bgplayer
            if (bgplayer != null)
            {
                bgplayer.ReceivedString(text);
            }
            else
            {
                Debug.LogWarning("⚠️ bgplayer 尚未指定！");
            }

            // 安全檢查 AudioVisualizer
            if (visualizer != null && audioData != null)
            {
                visualizer.UpdateWaveform(audioData);
            }
            else
            {
                Debug.LogWarning("⚠️ AudioVisualizer 未掛或 audioData 為 null");
            }
            */
        }
        void Update()
        {

            if (debug)
            {
                if (audioEffectController != null)
                {
                    audioEffectController.SetVolume(debugvol);
                }

            }
            if (vol_txt != null)
            {


                if (vol_txt != null)
                {
                    volcd += Time.deltaTime;
                    if (volcd > 0.1f)
                    {
                        vol_txt.text = lastVolume.ToString();
                        //vol_out_txt.text = (lastVolume * EffectConfig.audioScaleVol).ToString("F0");
                        vol_out_txt.text = (lastVolume).ToString("F0");
                        volcd = 0;
                        lastVolume = 0;
                    }
                }

            }
            if (lastVolume == 0)
            {
                idletime += Time.deltaTime;
            }
            else
            {
                idletime = 0;
            }

            if ( idletime > 5 && !isReconnecting && (volws == null ||  (volws.State != WebSocketState.Open && volws.State != WebSocketState.Connecting)) )
            {
                idletime = 0;
                isReconnecting = true;
                ReStart();

                Debug.Log("ReStart WebSocket"); 
            }



#if !UNITY_WEBGL || UNITY_EDITOR
            if (volws != null)
            {
                volws.DispatchMessageQueue();

            }

#endif
        }
    }

}
