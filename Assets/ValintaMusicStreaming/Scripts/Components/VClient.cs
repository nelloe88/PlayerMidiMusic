#pragma warning disable 0414 // Disable the "private field value not used" warning, value is used on Android
using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;

namespace ValintaMusicStreaming
{
    public class VClient : MonoBehaviour
    {
        public delegate void ClientInfoDelegate();
        public static event ClientInfoDelegate OnClientInfoReady;

        private string m_advertisingID;
        private bool m_limitAdTrackingEnabled;

        private bool m_isWaitingResponse;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        public static extern IntPtr GetIDFA();
#else
        public string GetIDFA() { return string.Empty; } // For editor
#endif

        public void GetInfo()
        {
            StartCoroutine(FetchDeviceInfo());
        }

        /// <summary>
        /// Gets advertising ID and limitAdTracking values from device.
        /// </summary>
        /// <returns></returns>
        private IEnumerator FetchDeviceInfo()
        {
            m_isWaitingResponse = true;
            yield return new WaitForEndOfFrame();
#if UNITY_ANDROID && !UNITY_EDITOR

            try
            {
                using (AndroidJavaClass activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    AndroidJavaObject context = activity.GetStatic<AndroidJavaObject>("currentActivity");
                    AndroidJavaClass adTracking = new AndroidJavaClass("com.zemeho.valinta.AdTracking");
                    if (adTracking != null)
                    {
                        adTracking.CallStatic("QueryForAdId", new object[] { context, gameObject.name, "HandleAndroidResponse" });
                    }
                }
            }
            catch(AndroidJavaException ex)
            {
                Debug.LogError(ex);

                m_isWaitingResponse = false;
                m_limitAdTrackingEnabled = true;

                CheckIfGUIDNeeded();
                yield break;
            }

            float timeOut = Time.realtimeSinceStartup + VSettings.BaseTimeOut - 15f; // Should be done in 15 seconds max
            while (m_isWaitingResponse)
            {
                if (Time.realtimeSinceStartup > timeOut)
                {
                    m_isWaitingResponse = false;
                    m_limitAdTrackingEnabled = true;
                    CheckIfGUIDNeeded();
                }
                yield return null;
            }

            if(!m_isWaitingResponse)
            {
                yield break;
            }

#elif UNITY_IOS && !UNITY_EDITOR

            m_limitAdTrackingEnabled = true;
            m_advertisingID = Marshal.PtrToStringAuto(GetIDFA());

            // iOS returns IDFA as 00000000-0000-0000-0000-000000000000 if ads are opted out
            if (!m_advertisingID.StartsWith("00000000-0000-0000-0000")) // if not opted out
            {
                m_limitAdTrackingEnabled = false;
            }

#else

            m_limitAdTrackingEnabled = true;

#endif

            CheckIfGUIDNeeded();
        }

        /// <summary>
        /// Android plugin calls this when ready
        /// </summary>
        /// <param name="message"></param>
        public void HandleAndroidResponse(string message)
        {
            m_isWaitingResponse = false;

            string[] tokens = message.Split(',');
            m_advertisingID = tokens[0];
            bool.TryParse(tokens[1], out m_limitAdTrackingEnabled);

            CheckIfGUIDNeeded();
        }

        /// <summary>
        /// Generate GUID if user has ad tracking opted out or
        /// if there are problems getting advertising ID. GUID is also
        /// used in editor.
        /// </summary>
        private void CheckIfGUIDNeeded()
        {
            if (m_limitAdTrackingEnabled || string.IsNullOrEmpty(m_advertisingID))
            {
                System.Guid myGUID = System.Guid.NewGuid();
                m_advertisingID = myGUID.ToString();
                VSettings.IsClientIDGuid = true;
            }

            OnFinish();
        }

        /// <summary>
        /// Client info is ready. Notify ValintaPlayer.
        /// </summary>
        private void OnFinish()
        {
            VSettings.ClientID = m_advertisingID;
            VSettings.AdTrackingLimited = m_limitAdTrackingEnabled;

            if (OnClientInfoReady != null)
                OnClientInfoReady();
        }
    }
}
