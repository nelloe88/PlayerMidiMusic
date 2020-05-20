using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ValintaMusicStreaming
{
    [DisallowMultipleComponent]
    public class VPlayerUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField]
        private RectTransform m_statusTextObject;
        private Button m_buttonStatus;
        private Text m_statusText;

        [SerializeField]
        private Button m_buttonPlay;
        private Image m_playButtonImage;

        [SerializeField]
        private Button m_buttonSkip;
        [SerializeField]
        private Button m_buttonMenu;

        [SerializeField]
        private Sprite m_spritePlay;
        [SerializeField]
        private Sprite m_spritePause;


        [Header("Playlists")]
        [SerializeField]
        private RectTransform m_playlistWindow;
        [SerializeField]
        private RectTransform m_playlistParent;
        [SerializeField]
        private GameObject m_playlistPrefab;
        [SerializeField]
        private GameObject m_adBannerPrefab;
        [SerializeField]
        private GameObject m_splashPrefab;

        private List<Button> m_playlistButtons;

        private bool m_isPlayerPaused = true;
        private bool m_isInitialized = false;
        private bool m_isRegistered = false;
        private bool m_isPlaylistWindowShown;
        private bool m_isCatalogueReady = false;
        private bool m_isPlaylistsCreated = false;
        private string m_bannerClickUrl = string.Empty;

        void Awake()
        {
            m_buttonStatus = m_statusTextObject.gameObject.GetComponent<Button>();
            m_statusText = m_statusTextObject.gameObject.GetComponent<Text>();
            m_playButtonImage = m_buttonPlay.gameObject.GetComponent<Image>();
            m_playlistButtons = new List<Button>();
        }

        void Start()
        {
            if (m_isInitialized) return;

            Subscribe();
            m_isInitialized = true;
            m_isPlaylistWindowShown = m_playlistWindow.gameObject.activeInHierarchy;

            ShowSplashWindow();
        }

        void OnEnable()
        {
            if (!m_isInitialized) return;
            if (m_isRegistered) return;

            ShowSplashWindow();
            RefreshPlaylists();

            Subscribe();
        }

        void OnDisable()
        {
            if (!m_isRegistered) return;

            Unsubscribe();
            RemovePlaylists();
        }

        private void AddListeners()
        {
            if (m_buttonPlay != null)
                m_buttonPlay.onClick.AddListener(OnPlayClicked);
            if (m_buttonSkip != null)
                m_buttonSkip.onClick.AddListener(OnSkipClicked);
            if (m_buttonMenu != null)
                m_buttonMenu.onClick.AddListener(OnMenuClicked);
            if (m_buttonStatus != null)
                m_buttonStatus.onClick.AddListener(OnStatusClicked);
            if (m_adBannerPrefab != null)
                m_adBannerPrefab.GetComponent<Button>().onClick.AddListener(OnBannerClicked);
        }

        private void RemoveListeners()
        {
            if (m_buttonPlay != null)
                m_buttonPlay.onClick.RemoveAllListeners();
            if (m_buttonSkip != null)
                m_buttonSkip.onClick.RemoveAllListeners();
            if (m_buttonMenu != null)
                m_buttonMenu.onClick.RemoveAllListeners();
            if (m_buttonStatus != null)
                m_buttonStatus.onClick.RemoveAllListeners();
        }


        #region Register/Unregister

        /// <summary>
        /// Subscribe to PlayerController to get state updates.
        /// </summary>
        private void Subscribe()
        {
            AddListeners();

            if (VPlayerController.Instance == null)
            {
                Debug.LogError("No player controller present. Add PlayerController prefab to scene.");
                return;
            }
            VPlayerController.Instance.RegisterPlayerUI(this);
            m_isRegistered = true;

            RefreshPlaylists();
        }

        /// <summary>
        /// Unsubscribe from PlayerController.
        /// </summary>
        private void Unsubscribe()
        {
            RemoveListeners();

            if (VPlayerController.Instance != null)
            {
                VPlayerController.Instance.DeregisterPlayerUI(this);
            }

            m_isRegistered = false;
        }

        /// <summary>
        /// Update UI state.
        /// </summary>
        /// <param name="state"></param>
        public void UpdateState(VPlayerState state)
        {
            m_statusText.text = state.StatusText;
            ActivateButtons(state.ButtonsShown);
            SetButtonsInteractable(state.ButtonsEnabled);
            m_isCatalogueReady = state.CatalogueReady;

            m_isPlayerPaused = state.IsPaused;
            if (state.IsStopped)
                m_isPlayerPaused = state.IsStopped;

            ChangePlayButtonSprite();

            RefreshPlaylists();
        }

        #endregion


        #region Banner/Splash

        /// <summary>
        /// Assign downloaded texture to banner.
        /// </summary>
        /// <param name="tex"></param>
        public void AssignTextureToBanner(Texture2D tex)
        {
            Sprite adBanner = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));

            m_adBannerPrefab.transform.GetChild(0).GetComponent<Image>().sprite = adBanner;
        }

        /// <summary>
        /// Show ad banner. If menu is not active, show it.
        /// </summary>
        /// <param name="show"></param>
        public void ShowAdBanner(bool show)
        {
            m_splashPrefab.SetActive(false);
            VSettings.IsSplashWindowShown = true;

            if (!m_playlistWindow.gameObject.activeInHierarchy && !VSettings.IsPreRollAdPlayed)
            {
                OnMenuClicked();
            }

            m_adBannerPrefab.SetActive(show);
        }

        /// <summary>
        /// Set URL for banner click.
        /// </summary>
        /// <param name="url"></param>
        public void SetBannerClickUrl(string url)
        {
            m_bannerClickUrl = url;
        }

        /// <summary>
        /// Show custom splash window. Ideally once per session.
        /// </summary>
        public void ShowSplashWindow()
        {
            if (VSettings.IsSplashWindowShown) return;

            if (!m_playlistWindow.gameObject.activeInHierarchy)
            {
                OnMenuClicked();
            }

            m_splashPrefab.SetActive(true);
            VSettings.IsSplashWindowShown = true;
        }

        #endregion


        #region Button event handlers

        /// <summary>
        /// Handle play/pause button click.
        /// </summary>
        private void OnPlayClicked()
        {
            m_isPlayerPaused = !m_isPlayerPaused;
            ChangePlayButtonSprite();

            if (m_isPlayerPaused)
            {
                VPlayerController.Instance.Pause();
            }
            else
            {
                VPlayerController.Instance.Play();
            }
        }

        /// <summary>
        /// Handle skip button click.
        /// </summary>
        private void OnSkipClicked()
        {
            VPlayerController.Instance.Skip();
        }

        /// <summary>
        /// Handle menu button click.
        /// </summary>
        private void OnMenuClicked()
        {
            m_isPlaylistWindowShown = !m_isPlaylistWindowShown;
            if (m_isPlaylistWindowShown)
            {
                RefreshPlaylists();
            }
            m_playlistWindow.gameObject.SetActive(m_isPlaylistWindowShown);
        }

        /// <summary>
        /// Handle status text(button) click.
        /// </summary>
        private void OnStatusClicked()
        {
            VPlayerController.Instance.OpenURL();
        }

        /// <summary>
        /// Handle banner click. 
        /// </summary>
        private void OnBannerClicked()
        {
            VPlayerController.Instance.AdBannerClicked();
            Application.OpenURL(m_bannerClickUrl);
        }

        #endregion


        #region Controlling button visibility and interactability

        /// <summary>
        /// Enable/disable all buttons.
        /// </summary>
        /// <param name="interactable"></param>
        private void SetButtonsInteractable(bool interactable)
        {
            if (m_buttonPlay != null)
                m_buttonPlay.interactable = interactable;
            if (m_buttonSkip != null)
                m_buttonSkip.interactable = interactable;
            if (m_buttonMenu != null)
                m_buttonMenu.interactable = interactable;

            if (m_playlistButtons.Count > 0)
            {
                foreach (var b in m_playlistButtons)
                {
                    b.interactable = interactable;
                }
            }
        }

        /// <summary>
        /// Activate/deactivate control buttons.
        /// </summary>
        /// <param name="activated"></param>
        private void ActivateButtons(bool activated)
        {
            if (m_buttonPlay != null)
                m_buttonPlay.gameObject.SetActive(activated);
            if (m_buttonSkip != null)
                m_buttonSkip.gameObject.SetActive(activated);
            if (m_buttonMenu != null)
                m_buttonMenu.gameObject.SetActive(activated);
        }

        /// <summary>
        /// Toggles play button sprite between "play" and "pause".
        /// </summary>
        private void ChangePlayButtonSprite()
        {
            m_playButtonImage.sprite = (m_isPlayerPaused) ? m_spritePlay : m_spritePause;
        }

        #endregion


        #region Playlists

        /// <summary>
        /// Playlist creation.
        /// </summary>
        private void CreatePlaylists()
        {
            if (m_isPlaylistsCreated) return;

            m_playlistButtons.Clear();
            List<VPlaylist> playlists = ValintaPlayer.Instance.GetCatalogueInstance().GetAllPlaylists();
            for (int i = 0; i < playlists.Count; i++)
            {
                var playlistButton = Instantiate(m_playlistPrefab) as GameObject;
                if (m_playlistParent != null)
                {
                    playlistButton.transform.SetParent(m_playlistParent, false);
                }

                var playlistInfo = playlistButton.GetComponent<VPlaylistButton>();
                playlistInfo.SetInfo(playlists[i]);

                Button b = playlistButton.GetComponent<Button>();
                b.onClick.AddListener(delegate
                {
                    VPlayerController.Instance.Play(playlistInfo.AssignedPlaylist);
                    m_isPlayerPaused = false;
                    ChangePlayButtonSprite();
                });
                m_playlistButtons.Add(b);
            }

            m_isPlaylistsCreated = true;
        }

        /// <summary>
        /// Removes all playlists from UI.
        /// </summary>
        private void RemovePlaylists()
        {
            if (!m_isInitialized) return;

            foreach (Transform child in m_playlistParent.transform)
            {
                Destroy(child.gameObject);
            }

            m_isPlaylistsCreated = false;
        }


        /// <summary>
        /// Removes and recreates playslists.
        /// </summary>
        private void RefreshPlaylists()
        {
            if (m_isCatalogueReady)
            {
                if (m_playlistParent.childCount < ValintaPlayer.Instance.GetCatalogueInstance().GetAllPlaylists().Count)
                {
                    RemovePlaylists();
                    CreatePlaylists();
                }
            }
        }

        #endregion
    }
}
