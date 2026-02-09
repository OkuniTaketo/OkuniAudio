using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;

namespace Okuni.Audio
{
    public class AudioPlayer : MonoBehaviour
    {
        [SerializeField]
        private AudioSource _audioSource = null;

        private CancellationTokenSource _innerCts = null;

        public void Pause()
        {
            _audioSource.Pause();
        }

        public void Resume()
        {
            _audioSource.UnPause();
        }

        /// <summary>
        /// クリップ<paramref name="audioClipInfo"/>を再生する。
        /// </summary>
        /// <param name="audioClipInfo"></param>
        /// <param name="loop">クリップ</param>
        /// <param name="fadeInTime">フェードイン時間</param>
        public void PlayClip(AudioClipInfo audioClipInfo, bool loop = false, float fadeInTime = 0)
        {
            if (_audioSource == null)
            {
                return;
            }

            if (audioClipInfo == null || audioClipInfo.AudioClip == null)
            {
                return;
            }

            //音声が既に再生されている時は止める
            _audioSource.DOKill();

            _audioSource.Stop();

            //オーディオソースを初期化して再生する。
            _audioSource.volume = 0;
            _audioSource.loop = loop;
            _audioSource.clip = audioClipInfo.AudioClip;

            _innerCts?.Cancel();
            _innerCts = new();

            _audioSource.Play();

            _audioSource.DOFade(audioClipInfo.Volume, fadeInTime)
                .SetLink(gameObject);
        }

        /// <summary>
        /// クリップ<paramref name="audioClipInfo"/>を再生する。
        /// </summary>
        /// <param name="audioClipInfo"></param>
        /// <param name="loop">クリップ</param>
        /// <param name="fadeInTime">フェードイン時間</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <returns></returns>
        public async UniTask PlayClipAsync(AudioClipInfo audioClipInfo, bool loop = false, float fadeInTime = 0, CancellationToken cancellationToken = default)
        {
            if (_audioSource == null)
            {
                return;
            }

            if (audioClipInfo == null || audioClipInfo.AudioClip == null)
            {
                return;
            }

            //音声が既に再生されている時は止める
            _audioSource.DOKill();

            _audioSource.Stop();

            //オーディオソースを初期化して再生する。
            _audioSource.volume = 0;
            _audioSource.loop = loop;
            _audioSource.clip = audioClipInfo.AudioClip;

            _audioSource.Play();

            _audioSource.DOFade(audioClipInfo.Volume, fadeInTime)
                .SetLink(gameObject).ToUniTask(cancellationToken: cancellationToken).Forget();

            await WaitForClipToEnd(cancellationToken);
        }

        /// <summary>
        /// クリップが終了するまで待機する。
        /// </summary>
        /// <returns></returns>
        private async UniTask WaitForClipToEnd(CancellationToken cancellationToken = default)
        {
            if (_audioSource == null)
            {
                return;
            }

            //音声が終了するまで待機する。
            _innerCts?.Cancel();
            _innerCts = new();
            CancellationToken token = _innerCts.Token;

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _innerCts.Token);

            //音声が再生されるまで待機する。
            await UniTask.WaitUntil(() => _audioSource.isPlaying, cancellationToken: linkedCts.Token);

            //音声が終了するまで待機する。
            if (_audioSource != null)
            {
                await UniTask.WaitUntil(() => !_audioSource.isPlaying, cancellationToken: linkedCts.Token);
            }
        }


        /// <summary>
        /// 一時的な距離によってボリュームが変わらないオーディオソースを作成して音声<paramref name="audioClipInfo"/>>を再生する。
        /// </summary>
        /// <param name="audioClipInfo">音声</param>
        public void PlayClipAtPoint2D(AudioClipInfo audioClipInfo)
        {
            if (audioClipInfo == null || audioClipInfo.AudioClip == null)
            {
                Debug.LogWarning("無効なAudioClipInfoが指定されました。");
                return;
            }

            GameObject tempAudio = new("TempAudioSource");
            AudioSource audioSource = tempAudio.AddComponent<AudioSource>();

            // AudioSourceの設定
            audioSource.clip = audioClipInfo.AudioClip;
            audioSource.volume = audioClipInfo.Volume;
            audioSource.spatialBlend = 0f; // 完全に2D
            audioSource.playOnAwake = false;

            // 再生
            audioSource.transform.position = transform.position;
            audioSource.Play();

            // 再生終了後にオブジェクトを破棄
            Destroy(tempAudio, audioClipInfo.AudioClip.length);
        }


        /// <summary>
        /// 音声を止める。
        /// </summary>
        /// <param name="fadeTime">フェードアウト時間</param>
        public void StopSound(float fadeOutTime = 0)
        {
            if (_audioSource == null || !_audioSource.isPlaying)
            {
                return;
            }

            //音声が終了するまで待機する。
            _innerCts?.Cancel();

            _audioSource.DOKill();

            _audioSource.DOFade(0f, fadeOutTime)
                .OnComplete(() => _audioSource.Stop())
                .SetLink(gameObject);
        }

        /// <summary>
        /// 音声を止める。(非同期)
        /// </summary>
        /// <param name="fadeTime">フェードアウト時間</param>
        public async UniTask StopAsync(float fadeDuration = 0, CancellationToken cancellationToken = default)
        {
            if (_audioSource == null || !_audioSource.isPlaying)
            {
                return;
            }

            //音声が終了するまで待機する。
            _innerCts?.Cancel();

            _audioSource.DOKill();

            _innerCts = new();
            CancellationToken token = _innerCts.Token;

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _innerCts.Token);

            await _audioSource.DOFade(0f, fadeDuration)
                .OnComplete(() => _audioSource.Stop())
                .SetLink(gameObject)
                .WithCancellation(linkedCts.Token);
        }

        private void OnDestroy()
        {
            _innerCts?.Cancel();
            _innerCts?.Dispose();
            _innerCts = null;
        }
    }
}