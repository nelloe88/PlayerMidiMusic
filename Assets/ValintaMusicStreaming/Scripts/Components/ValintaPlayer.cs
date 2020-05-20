using UnityEngine;
using System.Collections;
using System;

namespace ValintaMusicStreaming
{
    /// <summary>
    /// Valinta Player starting point
    /// </summary>
    public class ValintaPlayer : MonoBehaviour
    {
        public static ValintaPlayer Instance;

        [SerializeField]
        public string ValintaApplicationID;

        [SerializeField]
        public bool UseWWWForAudioDownload;

        private VSession m_session;
        private VClient m_client;
        private VNetwork m_network;
        private VCatalogue m_catalogue;
        private VAnalytics m_analytics;

        void Awake()
        {
            // Prevents duplicate object instantiation when scene is changed
            if (FindObjectsOfType<ValintaPlayer>().Length > 1)
            {
                Destroy(gameObject);
            }

            Application.runInBackground = true;
            DontDestroyOnLoad(gameObject);

            if (Instance != null)
                return;

            Instance = this;

            InitComponents();
        }

        /// <summary>
        /// Add basic player components; network, catalogue, session, client and analytics
        /// </summary>
        void InitComponents()
        {
            gameObject.AddComponent<VPlayerController>();
            if (m_network == null) m_network = gameObject.AddComponent<VNetwork>();
            if (m_catalogue == null) m_catalogue = gameObject.AddComponent<VCatalogue>();
            if (m_session == null) m_session = gameObject.AddComponent<VSession>();
            if (m_client == null) m_client = gameObject.AddComponent<VClient>();
            if (m_analytics == null) m_analytics = gameObject.AddComponent<VAnalytics>();

            VSettings.ValintaApplicationID = string.IsNullOrEmpty(ValintaApplicationID) ? "DeveloperPreviewApplication000" : ValintaApplicationID;
            VSettings.UseWWWForAudioClip = UseWWWForAudioDownload;

            VClient.OnClientInfoReady += OnClientInfoReady;
            VSession.OnSessionReady += OnSessionReady;
        }

        void Start()
        {
            m_client.GetInfo();
        }

        /// <summary>
        /// Client info fetched, continue start up
        /// </summary>
        private void OnClientInfoReady()
        {
            m_session.StartSession();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isSuccess"></param>
        /// <param name="error"></param>
        private void OnSessionReady(bool isSuccess, string error)
        {
            if (isSuccess)
            {
                VPlayerController.Instance.ShowPlayerReadyInUI();
            }
            else
            {
                VPlayerController.Instance.ShowErrorInUI(VStrings.AuthorizationFailed, true);
            }
        }

        /// <summary>
        /// Get instance of network component
        /// </summary>
        /// <returns>VNetwork instance</returns>
        public VNetwork GetNetworkInstance()
        {
            return m_network;
        }

        /// <summary>
        /// Get instance of catalogue component
        /// </summary>
        /// <returns>VCatalogue instance</returns>
        public VCatalogue GetCatalogueInstance()
        {
            return m_catalogue;
        }

        /// <summary>
        /// Get instance of client component
        /// </summary>
        /// <returns>VClient instance</returns>
        public VClient GetClientInstance()
        {
            return m_client;
        }


        /// <summary>
        /// Called when user wants to try login again
        /// </summary>
        public void RetryLogin()
        {
            m_session.StartSession();
        }
    }
}
