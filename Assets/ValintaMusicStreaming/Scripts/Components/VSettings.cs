using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ValintaMusicStreaming
{
    /// <summary>
    /// Keeps track on player settings.
    /// </summary>
    public class VSettings : MonoBehaviour
    {
        public static bool UseWWWForAudioClip { get; set; }
        // Set client side
        public static string ValintaApplicationID { get; set; }
        public static string ClientID { get; set; }
        public static bool IsClientIDGuid { get; set; }
        public static bool AdTrackingLimited { get; set; }

        public static float BaseTimeOut = 30f;
        public static bool IsSplashWindowShown = false;
        public static bool IsPreRollAdPlayed = false;

        // Fetched from back end
        public static string SessionToken { get; set; }
        public static int AdFrequency { get; set; }
        public static float DataBundleFrequency { get; set; }
        public static List<string> PrerollAdProviders;
        public static List<string> MidrollAdProviders;



        public static void CreateSettings(string json)
        {
            // Default values
            SessionToken = string.Empty;
            AdFrequency = 0;
            DataBundleFrequency = 180f;
            PrerollAdProviders = new List<string>();
            MidrollAdProviders = new List<string>();

            var N = SimpleJSON.JSON.Parse(json);

            // Tries to fetch values from JSON
            foreach (var key in N["settings"].Keys)
            {
                if (key.StartsWith("ad-network"))
                {
                    CreateAdUrls(N["settings"][key].Value, PrerollAdProviders);
                }
                if(key.StartsWith("ad-midroll"))
                {
                    CreateAdUrls(N["settings"][key].Value, MidrollAdProviders);
                }
                if(key.Equals("ad-freq"))
                {
                    int adFreq = 0;
                    int.TryParse(N["settings"][key].Value, out adFreq);
                    AdFrequency = adFreq;
                }
                if(key.Equals("session-freq"))
                {
                    float sessionFreq = 0;
                    float.TryParse(N["settings"][key].Value, out sessionFreq);
                    DataBundleFrequency = sessionFreq;
                }
            }

            if (!string.IsNullOrEmpty(N["token"].Value))
            {
                SessionToken = N["token"].Value;
            }
        }

        /// <summary>
        /// Add required info for ad network tags and add modified URLs to list
        /// - listener type: gaid, idfa, app
        /// - listener id: gaid, idfa or guid
        /// - extra info: timestamp
        /// </summary>
        /// <param name="url"></param>
        private static void CreateAdUrls(string url, List<string> urlList)
        {
            string modifiedUrl = url;
            string listenerType = string.Empty;
            string client = ClientID;

            if (AdTrackingLimited)
            {
                listenerType = "app";
            }
            else
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                listenerType = "gaid";
#elif UNITY_IOS && !UNITY_EDITOR
                listenerType = "idfa";
#else
                // Editor type
                listenerType = "app";
#endif
            }

            // One of the ad networks requires special treatment
            if(url.Contains("adswizz") && IsClientIDGuid)
            {
                listenerType = string.Empty;
                client = string.Empty;
            }

            if (url.Contains("<<<LISTENERTYPE>>>"))
            {
                modifiedUrl = modifiedUrl.Replace("<<<LISTENERTYPE>>>", listenerType);
            }
            if (url.Contains("<<<LISTENERID>>>"))
            {
                modifiedUrl = modifiedUrl.Replace("<<<LISTENERID>>>", client);
            }

            urlList.Add(modifiedUrl.Trim());
        }
    }
}

