
namespace ValintaMusicStreaming
{
    public class VPlayerState
    {
        public string StatusText;
        public bool ButtonsShown = true;
        public bool ButtonsEnabled = true;
        public bool CatalogueReady = false;

        public bool IsPaused = false;
        public bool IsStopped = true;
        public bool IsError = false;

        public VPlayerState()
        {
            ButtonsShown = true;
            ButtonsEnabled = true;
            IsPaused = false;
            IsStopped = true;
            IsError = false;

            CatalogueReady = false;
        }

        private void Reset()
        {
            ButtonsShown = true;
            ButtonsEnabled = true;
            IsPaused = false;
            IsStopped = true;
            IsError = false;
        }

        private void UpdateStatus(string s)
        {
            if (string.IsNullOrEmpty(s)) return;
            StatusText = s;
        }


        // Predefined states

        public void PlayerError(string s, bool retryable)
        {
            UpdateStatus(s);
            ButtonsEnabled = retryable;
            IsError = true;
        }

        public void PlayerReady(string s)
        {
            UpdateStatus(s);
            ButtonsEnabled = true;
            ButtonsShown = true;
            IsError = false;
        }

        public void PlayerInitializing(string s)
        {
            UpdateStatus(s);
            ButtonsEnabled = false;
            ButtonsShown = false;
            IsError = false;
        }

        public void PlayingMusic(string s)
        {
            UpdateStatus(s);
            ButtonsEnabled = true;
            IsStopped = false;
            IsPaused = false;
            IsError = false;
        }

        public void PlayingAd(string s)
        {
            UpdateStatus(s);

            ButtonsEnabled = false;
            IsStopped = false;
            IsPaused = false;
        }

        public void MusicPlaybackPause()
        {
            ButtonsEnabled = true;
            IsPaused = true;
            IsStopped = false;
        }

        public void PlaybackSkip()
        {
            IsStopped = false;
            IsPaused = false;
        }

        public void MusicPlaybackStop(string s)
        {
            UpdateStatus(s);
            IsPaused = false;
            IsStopped = true;
        }

        public void AdPlaybackStop()
        {
            ButtonsEnabled = true;
            ButtonsShown = true;
            IsPaused = true;
            IsStopped = true;
        }

        public void PlaybackLoading(string s)
        {
            UpdateStatus(s);
            ButtonsEnabled = false;
            IsStopped = false;
            IsPaused = false;
        }

        public void PlaybackError(string s)
        {
            UpdateStatus(s);
            ButtonsEnabled = true;
            ButtonsShown = true;
            IsStopped = true;
            IsPaused = false;
            IsError = true;
        }



        public void RetryingLogin(string s)
        {
            UpdateStatus(s);
            ButtonsEnabled = false;
            IsPaused = true;
            IsStopped = true;
            IsError = false;
        }
    }
}