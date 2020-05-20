using UnityEngine;
using UnityEngine.UI;

namespace ValintaMusicStreaming
{
    public class VPlaylistButton : MonoBehaviour
    {
        public VPlaylist AssignedPlaylist;
        [SerializeField]
        private Text PlaylistName;
           
        public void SetInfo(VPlaylist playlist)
        {
            AssignedPlaylist = playlist;
            PlaylistName.text = AssignedPlaylist.Name;
            gameObject.name = AssignedPlaylist.Name;
        }

    }
}
