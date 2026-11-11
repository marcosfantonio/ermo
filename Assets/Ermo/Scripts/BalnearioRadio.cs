using System;
using UnityEngine;

namespace Ermo
{
    // Band limited, compressed and softly clipped on the audio thread.
    public sealed class BalnearioRadio : MonoBehaviour
    {
        private float envelope;
        private AudioSource source;
        private float startOffset;
        private void OnAudioFilterRead(float[] data, int channels)
        {
            for (int i = 0; i < data.Length; i++)
            {
                float x = data[i];
                envelope += (Math.Abs(x)-envelope)*(Math.Abs(x) > envelope ? .015f : .00015f);
                float gain = envelope > .12f ? (float)Math.Pow(.12f/envelope,.72f) : 1;
                data[i] = (float)Math.Tanh(x*gain*3.2f)*.65f;
            }
        }
        public void Initialize(float offset)
        {
            source = gameObject.AddComponent<AudioSource>(); startOffset = offset;
            source.playOnAwake = false; source.loop = true; source.spatialBlend = 1;
            source.minDistance = 1.5f; source.maxDistance = 23; source.volume = .42f;
            source.rolloffMode = AudioRolloffMode.Logarithmic; source.dopplerLevel = 0;
            source.clip = Resources.Load<AudioClip>("Ermo/Audio/balneario");
            gameObject.AddComponent<AudioHighPassFilter>().cutoffFrequency = 340;
            gameObject.AddComponent<AudioLowPassFilter>().cutoffFrequency = 3100;
            gameObject.AddComponent<AudioDistortionFilter>().distortionLevel = .24f;
            if (isActiveAndEnabled) StartPlayback();
        }
        private void OnEnable() => StartPlayback();
        private void StartPlayback()
        {
            if (source == null || source.clip == null) return;
            source.time = startOffset*source.clip.length; source.Play();
        }
    }
}
