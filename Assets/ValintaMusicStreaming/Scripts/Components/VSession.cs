using UnityEngine;
using System.Collections;

namespace ValintaMusicStreaming
{
    /// <summary>
    /// Prepare session. Download catalogue and settings.
    /// </summary>

    public class VSession : MonoBehaviour
    {
        public delegate void Session(bool isSuccess, string error);
        public static event Session OnSessionReady;

        private VNetwork m_network;
        private bool m_isWaitingResponse = false;

        private VCatalogue m_catalogue;

        void Awake()
        {
            if (m_network == null) m_network = ValintaPlayer.Instance.GetNetworkInstance();
            if (m_catalogue == null) m_catalogue = ValintaPlayer.Instance.GetCatalogueInstance();
        }

        public void StartSession()
        {
            StartCoroutine(DownloadCatalogueAndSettings());
        }

        private IEnumerator DownloadCatalogueAndSettings()
        {
            m_isWaitingResponse = true;

            if (!m_network.CallGet(VStrings.CATALOGUE, ProcessCatalogueCallback))
            {
                OnFinished(false, "error: could not download catalogue");
                yield break;
            }

            float timeOut = Time.realtimeSinceStartup + VSettings.BaseTimeOut;
            while (m_isWaitingResponse)
            {
                if (Time.realtimeSinceStartup > timeOut)
                {
                    m_isWaitingResponse = false;
                    m_network.CancelCalls();
                    OnFinished(false, "error: downloading catalogue timed out");
                    StopAllCoroutines();
                    yield break;
                }
                yield return null;
            }
        }

        private void ProcessCatalogueCallback(string response, string error)
        {
            m_isWaitingResponse = false;

            if (!string.IsNullOrEmpty(error))
            {
                OnFinished(false, "Network error");
                return;
            }

            if (!string.IsNullOrEmpty(response))
            {
                m_catalogue.CreateCatalogue(response);
                VSettings.CreateSettings(response);
                OnFinished(true);
            }
        }

        private void OnFinished(bool isSuccess, string error = "")
        {
            if (OnSessionReady != null)
                OnSessionReady(isSuccess, error);
        }
    }
}
