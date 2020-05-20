using UnityEngine;
using System.Collections;

namespace ValintaMusicStreaming
{
    public class VMusicPlayback : MonoBehaviour
    {
        public delegate void PlaybackError(string s);
        public event PlaybackError OnPlaybackError;

        public delegate void PlaybackStarted();
        public event PlaybackStarted OnPlaybackStarted;

        public delegate void PlaybackGetNext();
        public event PlaybackStarted OnPlaybackGetNext;

        public delegate void PlaybackStopped();
        public event PlaybackStopped OnPlaybackStopped;

        public delegate void PlaybackLoading();
        public event PlaybackLoading OnPlaybackLoading;

        public delegate void PlaybackTimedOut();
        public event PlaybackTimedOut OnPlaybackTimedOut;

        private AudioSource m_audioSource;
        private VNetwork m_network;
        private VSong m_currentSong;

        private bool m_isSongSourceLoaded = false;

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

            ChangeState(PlayerState.Standby);
        }

        void Update()
        {
            if (m_currentState == PlayerState.Playing)
            {
                if (!m_audioSource.isPlaying)
                {
                    OnGetNext();
                }
            }
        }

        private void ChangeState(PlayerState newState)
        {
            m_currentState = newState;
        }


        #region Playback preparations

        /// <summary>
        /// Prepare song. Fetch source URL.
        /// </summary>
        /// <returns></returns>
        private IEnumerator PrepareSong()
        {
            if(m_currentSong == null)
            {
                OnError("No song specified");
                yield break;
            }

            OnLoading();

            m_isSongSourceLoaded = false;
            if(!m_network.CallGet(string.Concat(VStrings.APIV2, "genres/", m_currentSong.Playlist, "/musics/", m_currentSong.Id), SourceReceivedCallback))
            {
                m_network.CancelCalls();
                OnError("Can't get song source");
                yield break;
            }

            float timeOut = Time.realtimeSinceStartup + VSettings.BaseTimeOut;
            while (!m_isSongSourceLoaded)
            {
                if (Time.realtimeSinceStartup > timeOut)
                {
                    m_network.CancelCalls();
                    OnTimedOut();
                    StopAllCoroutines();
                    yield break;
                }
                yield return null;
            }
        }
        
        /// <summary>
        /// Callback for getting audio source URL.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="error"></param>
        private void SourceReceivedCallback(string response, string error)
        {
            m_isSongSourceLoaded = true;

            if (!string.IsNullOrEmpty(error))
            {
                OnError("Network error");
                return;
            }

            var N = SimpleJSON.JSON.Parse(response);
            string source = N["music"]["source"];

            m_network.GetAudioClipFromSource(source, AudioClipReceivedCallback);
        }

        /// <summary>
        /// Callback method for handling requested audio clip.
        /// If clip is received, start playback immediately
        /// </summary>
        /// <param name="clip"></param>
        private void AudioClipReceivedCallback(AudioClip clip)
        {
            if (clip != null)
            {
                DisposeCurrentAudio();

                m_audioSource.clip = clip;
                m_audioSource.Play();

                OnStarted();
            }
            else
            {
                OnError("Could not get audio clip");
            }
        }

        /// <summary>
        /// Dispose previous audio
        /// </summary>
        private void DisposeCurrentAudio()
        {
            if (m_audioSource.clip != null)
            {
                m_audioSource.Stop();
                Destroy(m_audioSource.clip);
                m_audioSource.clip = null;
            }
        }

        #endregion


        #region Playback controls

        /// <summary>
        /// Get song from player controller, prepare and play it.
        /// </summary>
        /// <param name="song"></param>
        public void Play(VSong song)
        {
            StopPlayback();

            m_currentSong = song;
            StartCoroutine(PrepareSong());
        }

        /// <summary>
        /// Pause playback
        /// </summary>
        public void Pause()
        {
            if (!m_audioSource.isPlaying) return;

            ChangeState(PlayerState.Standby);
            m_audioSource.Pause();
        }

        /// <summary>
        /// Resume playback
        /// </summary>
        public void Resume()
        {
            if (m_currentSong == null || m_audioSource.clip == null) return;
            if (m_audioSource.isPlaying) return;

            ChangeState(PlayerState.Playing);
            m_audioSource.Play();
            OnStarted();
        }

        /// <summary>
        /// Stop playback
        /// </summary>
        public void StopPlayback()
        {
            OnStopped();
            m_audioSource.Stop();
        }

        #endregion


        #region Event handling

        /// <summary>
        /// Playback started
        /// </summary>
        private void OnStarted()
        {
            if (OnPlaybackStarted != null)
                OnPlaybackStarted();

            ChangeState(PlayerState.Playing);
        }

        /// <summary>
        /// Playback stopped, get next automatically
        /// </summary>
        private void OnGetNext()
        {
            if (OnPlaybackGetNext != null)
                OnPlaybackGetNext();

            ChangeState(PlayerState.Standby);
        }

        /// <summary>
        /// Playback loading
        /// </summary>
        private void OnLoading()
        {
            if (OnPlaybackLoading != null)
                OnPlaybackLoading();

            ChangeState(PlayerState.Standby);
        }

        /// <summary>
        /// Playback stopped
        /// </summary>
        private void OnStopped()
        {
            if (OnPlaybackStopped != null)
                OnPlaybackStopped();

            ChangeState(PlayerState.Standby);
        }

        /// <summary>
        /// Error occurred
        /// </summary>
        /// <param name="s"></param>
        private void OnError(string s)
        {
            if (OnPlaybackError != null)
                OnPlaybackError(s);

            ChangeState(PlayerState.Standby);
        }

        /// <summary>
        /// Loading timed out
        /// </summary>
        private void OnTimedOut()
        {
            if (OnPlaybackTimedOut != null)
                OnPlaybackTimedOut();

            ChangeState(PlayerState.Standby);
        }

        #endregion
    }
}
