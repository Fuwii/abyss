using System.IO;
using Steamworks;
using Unity.Netcode;
using UnityEngine;

namespace Game.Player.Voice
{
    public class PlayerVoiceController : NetworkBehaviour
    {
        [SerializeField] private AudioSource source;

        private MemoryStream output;
        private MemoryStream stream;
        private MemoryStream input;

        private int optimalRate;
        private int clipBufferSize;
        private float[] clipBuffer;

        private int playbackBuffer;
        private int dataPosition;
        private int dataReceived;

        private void Start()
        {
            optimalRate = (int)SteamUser.OptimalSampleRate;

            clipBufferSize = optimalRate * 5;
            clipBuffer = new float[clipBufferSize];

            stream = new MemoryStream();
            output = new MemoryStream();
            input = new MemoryStream();

            if (IsOwner) return;

            SteamUser.VoiceRecord = true;

            source.clip = AudioClip.Create("VoiceData", 256, 1, optimalRate, true, OnAudioRead, null);
            source.loop = true;
            source.Play();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            SteamUser.VoiceRecord = false;
        }

        private void Update()
        {
            if (!IsOwner) return;
            if (!SteamUser.HasVoiceData) return;

            var compressedWritten = SteamUser.ReadVoiceData(stream);
            stream.Position = 0;

            RpcVoiceDataRpc(stream.GetBuffer(), compressedWritten);
        }

        [Rpc(SendTo.NotOwner)]
        private void RpcVoiceDataRpc(byte[] compressed, int bytesWritten)
        {
            if (IsOwner) return;
            input.Write(compressed, 0, bytesWritten);
            input.Position = 0;

            var uncompressedWritten = SteamUser.DecompressVoice(input, bytesWritten, output);
            input.Position = 0;

            var outputBuffer = output.GetBuffer();
            WriteToClip(outputBuffer, uncompressedWritten);
            output.Position = 0;
        }

        private void WriteToClip(byte[] uncompressed, int iSize)
        {
            for (var i = 0; i < iSize; i += 2)
            {
                var converted = (short)(uncompressed[i] | uncompressed[i + 1] << 8) / 32767.0f;
                clipBuffer[dataReceived] = converted;

                dataReceived = (dataReceived + 1) % clipBufferSize;

                playbackBuffer++;
            }
        }

        private void OnAudioRead(float[] data)
        {
            for (var i = 0; i < data.Length; ++i)
            {
                data[i] = 0;

                if (playbackBuffer <= 0) continue;

                dataPosition = (dataPosition + 1) % clipBufferSize;
                data[i] = clipBuffer[dataPosition];

                playbackBuffer--;
            }
        }
    }
}