using UnityEngine;
using System.Threading;

namespace Okuni.Audio
{
    public class AudioPlayer : MonoBehaviour
    {
        private bool _isPause = false;

        [SerializeField]
        private AudioSource _audioSource = null;

        private CancellationTokenSource _innerCts = null;

        public void Pause()
        {
            _isPause = true;
            _audioSource.Pause();
        }

        public void Resume()
        {
            _isPause = false;
            _audioSource.UnPause();
        }

        /// <summary>
        /// 指定された <see cref="AudioClipInfo"/> を再生します。
        /// フェードインやループ再生に対応し、ループする場合はフェードインまで待機します。
        /// すでに再生中の音声がある場合は停止されます。
        /// </summary>
        /// <param name="audioClipInfo">再生する音声情報</param>
        /// <param name="loop">ループ再生するかどうか</param>
        /// <param name="fadeInDuration">フェードインにかける時間（秒）</param>
        /// <param name="cancellationToken">外部から再生処理をキャンセルするためのトークン</param>
        /// <returns>再生が終了するかキャンセルされるまで待機する <see cref="Awaitable"/></returns>
        public async Awaitable Play(AudioClipInfo audioClipInfo, bool loop = false, float fadeInDuration = 0f, CancellationToken cancellationToken = default)
        {
            // 既存再生を停止
            _audioSource.Stop();

            // 初期化
            _audioSource.volume = 0f;
            _audioSource.loop = loop;
            _audioSource.clip = audioClipInfo.AudioClip;

            //前回の再生をキャンセル
            _innerCts?.Cancel();
            _innerCts = new();
            var token = _innerCts.Token;

            // 再生
            _audioSource.Play();

            float targetVolume = audioClipInfo.Volume;

            if (fadeInDuration > 0f)
            {
                //フェードインする場合
                float elapsedTime = 0f;

                //フェードインが完了するかキャンセルされまで待機
                //再生がフェードイン中に終了しても抜ける。
                while (elapsedTime < fadeInDuration && !token.IsCancellationRequested)
                {
                    if (_isPause)
                    {
                        // 一時停止状態の時、次のフレームまで待機してスキップ
                        await Awaitable.NextFrameAsync(cancellationToken);
                        continue;
                    }

                    if (!_audioSource.isPlaying)
                    {
                        //一時停止中でなければ再生終了時は抜ける。
                        break;
                    }


                    // 経過時間を0~1の範囲に制限・正規化
                    elapsedTime += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsedTime / fadeInDuration);

                    //正規化された経過時間を元にボリュームを変化させる
                    _audioSource.volume = Mathf.Lerp(0f, targetVolume, t);

                    // 次のフレームまで待機
                    await Awaitable.NextFrameAsync(cancellationToken);
                }
            }

            // まだ再生中ならボリュームを目標値に変更
            if (!token.IsCancellationRequested)
            {
                _audioSource.volume = targetVolume;
            }

            // 再生終了まで待機（loopなら待たない）
            if (!loop)
            {
                while (_audioSource.isPlaying && !token.IsCancellationRequested)
                {
                    await Awaitable.NextFrameAsync(cancellationToken);
                }
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

        public async Awaitable Stop(float fadeOutDuration = 0f, CancellationToken cancellationToken = default)
        {
            //実行中の非同期処理をキャンセル
            _innerCts?.Cancel();
            _innerCts = new();
            CancellationToken token = _innerCts.Token;

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _innerCts.Token);

            if (fadeOutDuration > 0f)
            {
                //フェードインする場合
                float elapsedTime = 0f;
                float initialVolume = _audioSource.volume;

                //フェードインが完了するかキャンセルされまで待機
                //再生がフェードイン中に終了しても抜ける。
                while (elapsedTime < fadeOutDuration && !token.IsCancellationRequested)
                {
                    if (_isPause)
                    {
                        // 一時停止状態の時、次のフレームまで待機してスキップ
                        await Awaitable.NextFrameAsync(cancellationToken);
                        continue;
                    }

                    if (!_audioSource.isPlaying)
                    {
                        //一時停止中でなければ再生終了時は抜ける。
                        break;
                    }


                    // 経過時間を0~1の範囲に制限・正規化
                    elapsedTime += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsedTime / fadeOutDuration);

                    //正規化された経過時間を元にボリュームを変化させる
                    _audioSource.volume = Mathf.Lerp(initialVolume, 0f, t);

                    // 次のフレームまで待機
                    await Awaitable.NextFrameAsync(cancellationToken);
                }
            }

            // まだ再生中ならボリュームを目標値に変更
            _audioSource.volume = 0f;
        }

        private void OnDestroy()
        {
            _innerCts?.Cancel();
            _innerCts?.Dispose();
            _innerCts = null;
        }
    }
}