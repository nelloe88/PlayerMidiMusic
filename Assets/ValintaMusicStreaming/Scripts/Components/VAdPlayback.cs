using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ValintaMusicStreaming
{
    public class VAdPlayback : MonoBehaviour
    {
        public delegate void AdPlaybackError(string s);
        public event AdPlaybackError OnPlaybackError;

        public delegate void AdPlaybackStarted(VPlayableAd ad);
        public event AdPlaybackStarted OnPlaybackStarted;

        public delegate void AdPlaybackComplete();
        public event AdPlaybackComplete OnPlaybackCompleted;

        public delegate void AdPlaybackLoading();
        public event AdPlaybackLoading OnPlaybackLoading;

        public delegate void AdPlaybackSkippable();
        public event AdPlaybackSkippable OnPlaybackSkippable;

        private AudioSource m_audioSource;
        private VNetwork m_network;
        private VAdHandler m_adHandler;
        private VPlayableAd m_currentAd;

        private bool m_adRequested = false;
        private bool m_isAdPrepared = false;
        private bool m_isAdStarted = false;
        private bool m_adContainsBanner = false;
        private bool m_isBannerTextureLoaded = false;
        private bool m_isAudioClipLoaded = false;

        private enum PlayerState
        {
            Standby,
            Playing
        }
        private PlayerState m_currentState;

        void Start()
        {
            if (GetComponent<AudioSource>() == null)
            {
                m_audioSource = gameObject.AddComponent<AudioSource>();
            }
            else
            {
                m_audioSource = GetComponent<AudioSource>();
            }

            m_network = ValintaPlayer.Instance.GetNetworkInstance();
            m_adHandler = gameObject.AddComponent<VAdHandler>();

            ChangeState(PlayerState.Standby);
        }

        void Update()
        {
            if (m_currentState == PlayerState.Playing)
            {
                DoAdTracking();

                if (!m_audioSource.isPlaying)
                {
                    OnCompleted();
                    ChangeState(PlayerState.Standby);
                }
            }
        }

        void OnApplicationFocus(bool focusStatus)
        {
            if (!m_isAdPrepared) return;

            if (!m_audioSource.isPlaying && focusStatus)
            {
                Play();
            }
            else
            {
                Pause();
            }
        }

        private void ChangeState(PlayerState newState)
        {
            m_currentState = newState;
        }


        #region Assemble audio ad

        /// <summary>
        /// Download banner texture and audio clip
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private IEnumerator PrepareAd(string url)
        {
            m_isAdPrepared = true;

            // Fetch banner texture
            m_isBannerTextureLoaded = false;

            string bannerUrl = m_currentAd.GetBannerImageUrl();
            if (!string.IsNullOrEmpty(bannerUrl))
            {
                m_adContainsBanner = true;
                m_network.GetTextureFromSource(bannerUrl, TextureReceivedCallback);

                float timeOut = Time.realtimeSinceStartup + VSettings.BaseTimeOut;
                while (!m_isBannerTextureLoaded)
                {
                    if (Time.realtimeSinceStartup > timeOut)
                    {
                        m_network.CancelCalls();
                        OnError("Texture loading timed out");
                        yield break;
                    }
                    yield return null;
                }
            }
            else
            {
                m_adContainsBanner = false;
            }

            // Fetch audio clip
            m_isAudioClipLoaded = false;

            if (!string.IsNullOrEmpty(url))
            {
                m_network.GetAudioClipFromSource(url, AudioClipReceivedCallback);

                float timeOut = Time.realtimeSinceStartup + VSettings.BaseTimeOut;
                while (m_isAudioClipLoaded)
                {
                    if (Time.realtimeSinceStartup > timeOut)
                    {
                        m_network.CancelCalls();
                        OnError("Ad audio loading timed out");
                        yield break;
                    }
                    yield return null;
                }
            }
            else
            {
                OnError("Ad URL is empty");
            }
        }

        /// <summary>
        /// Callback for texture download.
        /// </summary>
        /// <param name="texture"></param>
        private void TextureReceivedCallback(Texture2D texture)
        {
            m_isBannerTextureLoaded = true;

            if (texture != null)
            {
                VPlayerController.Instance.SetTextureForAdBanner(texture);
                VPlayerController.Instance.SetURLForBannerClick(m_currentAd.GetBannerClickUrl());
            }
        }

        /// <summary>
        /// Callback for audio clip download.
        /// </summary>
        /// <param name="clip"></param>
        private void AudioClipReceivedCallback(AudioClip clip)
        {
            m_isAudioClipLoaded = true;

            if (clip != null)
            {
                StartPlayback(clip);
            }
            else
            {
                OnError("Could not get audio clip");
            }
        }

        /// <summary>
        /// Start ad playback
        /// </summary>
        /// <param name="clip"></param>
        private void StartPlayback(AudioClip clip)
        {
            DisposeCurrentAudio();

            if (m_isBannerTextureLoaded)
            {
                VPlayerController.Instance.SetAdBannerActive(true);
            }

            if (m_adContainsBanner && m_isBannerTextureLoaded)
            {
                m_currentAd.DoEventTracking(VStrings.Ad_CreativeView);
            }

            m_currentAd.DoEventTracking(VStrings.Ad_Start);

            if (m_currentAd != null && m_currentAd.GetImpressionUrls().Count > 0)
            {
                // Adswizz requires impression calls at start, other networks at completion
                if (m_currentAd.GetMediaUrl().Contains("adswizz"))
                {
                    foreach (var imprToCall in m_currentAd.GetImpressionUrls())
                    {
                        m_network.CallGet(imprToCall, null);
                    }
                }
            }
            else
            {
                OnError("Error calling impressions!");
            }

            m_isAdStarted = true;

            m_audioSource.clip = clip;
            m_audioSource.Play();

            OnStarted();
        }

        /// <summary>
        /// Dispose previous audio
        /// </summary>
        private void DisposeCurrentAudio()
        {
            if (m_audioSource.clip != null)
            {
                Stop();
                Destroy(m_audioSource.clip);
                m_audioSource.clip = null;
            }
        }

        #endregion


        #region Main functions

        /// <summary>
        /// Requests ad, if there are no ads prepared yet.
        /// </summary>
        public void Play()
        {
            if (m_isAdPrepared && !m_audioSource.isPlaying)
            {
                Resume();
                return;
            }
            if (m_adRequested) return;

            OnLoading();

            m_adHandler.OnAdReady += OnAdReady;
            m_adHandler.OnAdError += OnAdError;

            m_adHandler.RequestAd();
            m_adRequested = true;
        }

        /// <summary>
        /// Tries to resume current ad playback.
        /// </summary>
        public void Resume()
        {
            if (m_currentAd == null)
            {
                Debug.LogWarning("Could not resume ad playback");
                return;
            }

            if (!m_audioSource.isPlaying && m_isAdStarted)
            {
                m_currentAd.StartTime = Time.realtimeSinceStartup;
                m_audioSource.Play();
                m_currentAd.DoEventTracking(VStrings.Ad_Resume);

                ChangeState(PlayerState.Playing);
            }
        }

        /// <summary>
        /// Pauses ad playback.
        /// </summary>
        public void Pause()
        {
            if (m_currentAd == null) return;

            if (m_audioSource.isPlaying)
            {
                m_currentAd.DoEventTracking(VStrings.Ad_Pause);
                ChangeState(PlayerState.Standby);
                m_currentAd.Duration = m_currentAd.TimeLeft;
                m_audioSource.Pause();
            }
        }

        /// <summary>
        /// Skips current ad
        /// </summary>
        public void Skip()
        {
            if (m_currentAd == null)
            {
                Play();
                return;
            }

            m_currentAd.DoEventTracking(VStrings.Ad_Skip);
            OnCompleted();
        }

        /// <summary>
        /// Stops ad playback.
        /// </summary>
        public void Stop()
        {
            ChangeState(PlayerState.Standby);
            m_audioSource.Stop();
        }

        /// <summary>
        /// Disposes current ad and resets
        /// </summary>
        private void Reset()
        {
            m_adHandler.OnAdReady -= OnAdReady;
            m_adHandler.OnAdError -= OnAdError;
            m_audioSource.Stop();
            m_audioSource.clip = null;

            VPlayerController.Instance.SetAdBannerActive(false);

            m_adHandler.Reset();

            m_adRequested = false;
            m_isAdPrepared = false;
            m_isAdStarted = false;

            m_currentAd = null;
        }

        #endregion


        #region Event handling

        /// <summary>
        /// AdHandler sent ad, preparations can be started.
        /// </summary>
        /// <param name="ad"></param>
        private void OnAdReady(VPlayableAd ad)
        {
            VPlayerController.Instance.SetSplashActive(false);
            m_currentAd = ad;

            if (m_currentAd == null)
            {
                OnError("Current ad is null");
                return;
            }

            StartCoroutine(PrepareAd(m_currentAd.GetMediaUrl()));
        }

        /// <summary>
        /// AdHandler could not build any playable ad. Continue with playing the music.
        /// </summary>
        /// <param name="error"></param>
        /// <param name="retryable"></param>
        private void OnAdError(string error, bool retryable)
        {
            if (!retryable)
            {
                OnError(error);
            }
        }

        /// <summary>
        /// Track ad's timed events
        /// </summary>
        private void DoAdTracking()
        {
            if (m_currentAd == null) return;

            m_currentAd.TimeLeft = m_currentAd.Duration - (Time.realtimeSinceStartup - m_currentAd.StartTime);
            m_currentAd.CurrentTimePosition = m_currentAd.TotalDuration - m_currentAd.TimeLeft;

            if (!VPlayerController.Instance.IsInteractable() && m_currentAd.CurrentTimePosition > m_currentAd.SkipTime)
            {
                OnSkippable();
            }

            // check for first quartile
            if (m_currentAd.CurrentTimePosition > m_currentAd.FirstQuartile && !m_currentAd.IsEventFired(VStrings.Ad_FirstQuartile))
            {
                m_currentAd.DoEventTracking(VStrings.Ad_FirstQuartile);
            }

            // check for midpoint
            if (m_currentAd.CurrentTimePosition > m_currentAd.Midpoint && !m_currentAd.IsEventFired(VStrings.Ad_Midpoint))
            {
                m_currentAd.DoEventTracking(VStrings.Ad_Midpoint);
            }

            // check third quartile
            if (m_currentAd.CurrentTimePosition > m_currentAd.ThirdQuartile && !m_currentAd.IsEventFired(VStrings.Ad_ThirdQuartile))
            {
                m_currentAd.DoEventTracking(VStrings.Ad_ThirdQuartile);
            }
        }

        /// <summary>
        /// Ad playback started. Send ad to player controller so UI can be updated.
        /// </summary>
        private void OnStarted()
        {
            if (OnPlaybackStarted != null)
                OnPlaybackStarted(m_currentAd);

            m_currentAd.StartTime = Time.realtimeSinceStartup;
            m_currentAd.UpdateEventTimeValues();

            ChangeState(PlayerState.Playing);
        }

        /// <summary>
        /// Error occurred, can't continue with ad playback.
        /// </summary>
        /// <param name="s"></param>
        private void OnError(string s)
        {
            Reset();
            if (OnPlaybackError != null)
            {
                OnPlaybackError(s);
            }

            ChangeState(PlayerState.Standby);
        }

        /// <summary>
        /// Ad is completed. Continue with music playback.
        /// </summary>
        private void OnCompleted()
        {
            // Adswizz requires impression calls at start, other networks at completion
            if (m_currentAd != null && m_currentAd.GetImpressionUrls().Count > 0)
            {
                if (!m_currentAd.GetMediaUrl().Contains("adswizz"))
                {
                    foreach (var imprToCall in m_currentAd.GetImpressionUrls())
                    {
                        m_network.CallGet(imprToCall, null);
                    }
                }
            }
            else
            {
                OnError("Error calling impressions!");
            }

            m_currentAd.DoEventTracking(VStrings.Ad_Complete);
            Reset();

            if (OnPlaybackCompleted != null)
            {
                OnPlaybackCompleted();
            }

            ChangeState(PlayerState.Standby);
        }

        /// <summary>
        /// Loading
        /// </summary>
        private void OnLoading()
        {
            if (OnPlaybackLoading != null)
                OnPlaybackLoading();

            ChangeState(PlayerState.Standby);
        }

        /// <summary>
        /// Inform controller if ad is skippable so controller can release the buttons accordingly
        /// </summary>
        private void OnSkippable()
        {
            if (m_currentAd == null)
                return;
            if (!m_currentAd.Skippable)
                return;

            if (OnPlaybackSkippable != null)
                OnPlaybackSkippable();
        }

        #endregion
    }
}
