using System.Collections.Generic;
using UnityEngine;

namespace ValintaMusicStreaming
{
    public class VAnalytics : MonoBehaviour
    {
        public static VAnalytics Instance;

        private VNetwork m_network;

        void Awake()
        {
            if (Instance != null) return;

            if(m_network == null)
            {
                m_network = ValintaPlayer.Instance.GetNetworkInstance();
            }

            Instance = this;
        }

        public void SendSongState(VSong song, bool wasSkipped)
        {
            if (song == null) return;

            ASongInfo songInfo = new ASongInfo()
            {
                song_id = int.Parse(song.Id),
                genre_id = song.Playlist,
                state = (wasSkipped) ? 0 : 1
            };

            string postData = JsonUtility.ToJson(songInfo);
            m_network.CallPost(VStrings.ANALYTICS_PLAYBACK, postData, null);
        }

        public void SendSessionDuration(int multiplier)
        {
            ASessionDuration sessionCurrentDuration = new ASessionDuration() { duration = Mathf.RoundToInt(multiplier * VSettings.DataBundleFrequency) };
            string postData = JsonUtility.ToJson(sessionCurrentDuration);
            m_network.CallPost(VStrings.ANALYTICS_SESSION, postData, null);
        }
    }

    public class ASongInfo
    {
        public int song_id;
        public int genre_id;
        public int state; // 1: played 0: skipped
    }

    public class ASessionDuration
    {
        public int duration;
    }
}