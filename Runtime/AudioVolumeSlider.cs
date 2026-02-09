using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Okuni.MaroJam0116
{
    public class AudioVolumeSlider : MonoBehaviour
    {
        [Header("Graphics")]
        [SerializeField]
        private Slider _slider = null;

        [Header("Audio")]
        [SerializeField]
        private AudioMixer _audioMixer = null;

        [SerializeField]
        private AudioMixerGroupType _audioMixerGroupType = AudioMixerGroupType.Bgm;


        private string _groupID = string.Empty;

        public float Volume { get; private set; } = 0f;

        private enum AudioMixerGroupType
        {
            Bgm, Se
        }


        public void Initialize(float initialVolume)
        {
            _groupID = _audioMixerGroupType switch
            {
                AudioMixerGroupType.Bgm => "BgmVolume",
                AudioMixerGroupType.Se => "SeVolume",
                _ => "BgmVolume"
            };

            SetVolume(initialVolume);
        }

        /// <summary>
        /// ボリュームを変更する。
        /// </summary>
        /// <param name="volume">スライダーの値</param>
        public void SetVolume(float volume)
        {
            Volume = volume;

            _slider.value = volume;

            _audioMixer.SetFloat(_groupID, ConvertVolume2dB(volume));
        }

        /// <summary>
        /// 0 ~ 1の値をdB( デシベル )に変換
        /// </summary>
        /// <param name="volume">ボリューム</param>
        /// <returns>変換後のデシベル</returns>
        private static float ConvertVolume2dB(float value)
        {
            return value <= 0f ? -80f : Mathf.Clamp(20f * Mathf.Log10(value), -80f, 0f);
        }
    }
}