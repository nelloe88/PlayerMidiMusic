using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ValintaMusicStreaming
{
    /// <summary>
    /// Playlist class
    /// </summary>
    public class VPlaylist
    {
        public int Id { get; private set; }

        public string Name { get; private set; }

        private List<VSong> m_songs;
        private int m_currentSong;

        public VPlaylist(int id, string name)
        {
            Id = id;
            Name = name;
            m_songs = new List<VSong>();
            m_currentSong = 0;
        }

        public void AddSong(VSong song)
        {
            if (m_songs == null)
                m_songs = new List<VSong>();

            if (!m_songs.Contains(song))
            {
                m_songs.Add(song);
            }
        }

        public VSong GetNextSong()
        {
            if (m_currentSong >= m_songs.Count)
                m_currentSong = 0;

            VSong song = m_songs[m_currentSong];
            m_currentSong++;

            return song;
        }

        public void RandomizeSongOrder()
        {
            m_songs.Shuffle();
        }

        public void Clear()
        {
            m_songs.Clear();
        }
    }

    /// <summary>
    /// Song class
    /// </summary>
    public class VSong
    {
        public string Id;
        public string Title;
        public string Artist;
        public string SongUrl;
        public string Site;
        public string Album;
        public string Duration;
        public int Playlist;
    }

    /// <summary>
    /// Builds and stores playlists
    /// </summary>
    public class VCatalogue : MonoBehaviour
    {
        private List<VPlaylist> m_playlists;

        void Awake()
        {
            if (m_playlists == null)
            {
                m_playlists = new List<VPlaylist>();
            }
        }

        private void AddPlaylist(VPlaylist playlist)
        {
            if (m_playlists.Contains(playlist))
                return;

            m_playlists.Add(playlist);
        }

        public VPlaylist GetPlaylistByID(int id)
        {
            foreach (var playlist in m_playlists)
            {
                if (playlist.Id == id)
                {
                    return playlist;
                }
            }

            return null;
        }

        public VPlaylist GetPlaylistByIndex(int index)
        {
            if (m_playlists.Count <= 0)
            {
                return null;
            }

            if (index < m_playlists.Count)
            {
                return m_playlists[index];
            }
            else
            {
                return m_playlists[0];
            }
        }

        public List<VPlaylist> GetAllPlaylists()
        {
            return m_playlists;
        }

        private void RemovePlaylist(int index)
        {
            m_playlists.RemoveAt(index);
        }

        /// <summary>
        /// Creates playlists from songs.
        /// </summary>
        /// <param name="json"></param>
        public void CreateCatalogue(string json)
        {
            var N = SimpleJSON.JSON.Parse(json);
            var playlistCount = N["catalog"].Count;

            for (int i = 0; i < playlistCount; i++)
            {
                VPlaylist playlist = new VPlaylist(N["catalog"][i]["id"].AsInt, N["catalog"][i]["title"].Value);
                AddPlaylist(playlist);

                var songsCount = N["catalog"][i]["songs"].Count;
                for (int j = 0; j < songsCount; j++)
                {
                    playlist.AddSong(new VSong
                    {
                        Id = N["catalog"][i]["songs"][j]["id"].Value,
                        Title = N["catalog"][i]["songs"][j]["title"].Value,
                        Artist = N["catalog"][i]["songs"][j]["artist"].Value,
                        Site = string.IsNullOrEmpty(N["catalog"][i]["songs"][j]["site"].Value) ? "http://www.apollomusic.dk/game-music" : N["catalog"][i]["songs"][j]["site"].Value,
                        Album = N["catalog"][i]["songs"][j]["album"].Value,
                        Duration = N["catalog"][i]["songs"][j]["duration"].Value,
                        Playlist = playlist.Id
                    });
                }
                playlist.RandomizeSongOrder();
            }

            VPlayerController.Instance.CatalogueReady();
        }
    }
}
