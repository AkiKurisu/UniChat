#if WHISPER_INSTALL
using System.IO;
using Kurisu.UniChat.Chains;
using Kurisu.UniChat.STT;
using UnityEngine;
using UnityEngine.UI;
using Whisper.Utils;
using static Kurisu.UniChat.STT.WhisperModel;
namespace Kurisu.UniChat.Example
{
    public class STT_Chain_Example : MonoBehaviour
    {
        public MicrophoneRecord microphoneRecord;
        [Header("UI")]
        public Button button;
        public Text buttonText;
        public string modelPath = "Whisper/ggml-tiny.bin";
        private WhisperModel whisperModel;
        public WhisperSettings whisperSettings;
        private async void Start()
        {
            microphoneRecord.OnRecordStop += OnRecordStop;
            button.onClick.AddListener(OnButtonPressed);
            whisperModel = await FromPath(Path.Combine(Application.streamingAssetsPath, modelPath));
        }
        private void OnButtonPressed()
        {
            if (!microphoneRecord.IsRecording)
            {
                microphoneRecord.StartRecord();
                buttonText.text = "Stop";
            }
            else
            {
                microphoneRecord.StopRecord();
                buttonText.text = "Record";
            }
        }
        private async void OnRecordStop(AudioChunk recordedAudio)
        {
            buttonText.text = "Record";
            var chain = Chain.Set(recordedAudio, "audio")
                        | Chain.STT(whisperModel, whisperSettings);
            Debug.Log(await chain.Trace(true, true).Run("text"));
        }
    }
}
#else
using UnityEngine;
namespace Kurisu.UniChat.Example
{
    public class STT_Chain_Example : MonoBehaviour
    {
    }
}
#endif