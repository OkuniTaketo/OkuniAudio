using UnityEngine;

namespace Okuni.Audio
{
    [CreateAssetMenu(fileName = "AudioClipInfo", menuName = "Scriptable Objects/AudioClipInfo")]
    public class AudioClipInfo : ScriptableObject
    {
        [field: SerializeField]
        public AudioClip AudioClip { get; private set; } = null;

        [field: SerializeField, Range(0f, 1f)]
        public float Volume { get; private set; } = 1;
    }
}