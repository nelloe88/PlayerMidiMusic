using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ValintaMusicStreaming
{
    public class VAdHandler : MonoBehaviour
    {
        public delegate void AdReady(VPlayableAd ad);
        public event AdReady OnAdReady;
        public delegate void AdError(string error, bool retryable);
        public event AdError OnAdError;

        private VNetwork m_network;

        private VPlayableAd m_playableAd;
        private List<VWrapper> m_wrapperList;

        private bool m_isWaitingResponse = false;
        private int m_adUrlIndex = 0;
        private int m_wrapperCount = 0;
        private bool m_isMidRoll = false;


        void Awake()
        {
            m_wrapperList = new List<VWrapper>();
        }

        void Start()
        {
            m_network = ValintaPlayer.Instance.GetNetworkInstance();
        }

        /// <summary>
        /// Start ad request process.
        /// </summary>
        public void RequestAd()
        {
            StartCoroutine(DoAdRequest());
        }

        /// <summary>
        /// Ad has been set up successfully.
        /// </summary>
        private void Success()
        {
            m_playableAd.SetLinkText();
            m_playableAd.UpdateEventTimeValues();

            if (OnAdReady != null)
                OnAdReady(m_playableAd);
        }

        /// <summary>
        /// Error happened when building the ad.
        /// </summary>
        /// <param name="error">Error description</param>
        /// <param name="retryable">Is the error retryable</param>
        private void Error(string error, bool retryable)
        {
            if (OnAdError != null)
                OnAdError(error + " retrying: " + retryable, retryable);

            if (!retryable)
                StopAllCoroutines();
        }

        /// <summary>
        /// Reset ad handler.
        /// </summary>
        public void Reset()
        {
            m_playableAd = null;
            m_wrapperList.Clear();

            m_isMidRoll = false;
            m_isWaitingResponse = false;
            m_adUrlIndex = 0;
            m_wrapperCount = 0;
        }

        /// <summary>
        /// Get ad URL and call to that.
        /// </summary>
        /// <returns></returns>
        private IEnumerator DoAdRequest()
        {
            string adNetworkUrl;

            if (m_wrapperList.Count > 0)
            {
                adNetworkUrl = m_wrapperList[m_wrapperList.Count - 1].VASTAdTagURI;
            }
            else
            {
                if (VSettings.PrerollAdProviders.Count > 0 && !VSettings.IsPreRollAdPlayed)
                {
                    adNetworkUrl = VSettings.PrerollAdProviders[m_adUrlIndex];
                }
                else if (VSettings.MidrollAdProviders.Count > 0)
                {
                    adNetworkUrl = VSettings.MidrollAdProviders[m_adUrlIndex];
                    m_isMidRoll = true;
                }
                else
                {
                    Error("No ad providers", false);
                    yield break;
                }

                // Special treatment for URL containing EXTRA value
                if (adNetworkUrl.Contains("<<<EXTRA>>>"))
                {
                    adNetworkUrl = adNetworkUrl.Replace("<<<EXTRA>>>", VUtils.GetTimestamp().ToString());
                }
            }

            m_isWaitingResponse = true;
            if (!m_network.CallGet(adNetworkUrl, AdRequestCallback))
            {
                m_isWaitingResponse = false;
                Error("Call failed to " + adNetworkUrl, true);
                TryNext();
            }

            float timeOut = Time.realtimeSinceStartup + VSettings.BaseTimeOut;
            while (m_isWaitingResponse)
            {
                if (Time.realtimeSinceStartup > timeOut)
                {
                    m_isWaitingResponse = false;
                    Error("Ad loading timed out", true);
                    yield break;
                }
                yield return null;
            }
        }

        /// <summary>
        /// Got response from ad url. Error could be handled here but if response doesn't contain ads CheckVast will handle it.
        /// </summary>
        /// <param name="response">Response from VNetwork</param>
        /// <param name="error">Error response from VNetwork</param>
        private void AdRequestCallback(string response, string error)
        {
            m_isWaitingResponse = false;

            // Check response for VAST
            StartCoroutine(CheckVast(response));
        }

        /// <summary>
        /// Check response for VAST
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private IEnumerator CheckVast(string response)
        {
            if (string.IsNullOrEmpty(response))
            {
                Error("Response text is null or empty", true);
            }

            if (m_wrapperList.Count > 5)
            {
                StartCoroutine(ReportError());
                Error("Infinite loop in wrappers", true);
                yield break;
            }

            if (response.Contains("VAST"))
            {
                XmlParser parser = new XmlParser(response);
                StartCoroutine(BuildAdFromVast(parser.GetVAST()));
            }
            else
            {
                if (m_wrapperList != null && m_wrapperCount > 0)
                {
                    StartCoroutine(ReportError());
                    Error("Could not find ads where previous wrapper redirected", true);
                    yield break;
                }
            }

            if (!TryNext())
            {
                yield break;
            }
        }

        /// <summary>
        /// Try next network if previous one didn't provide ad or, if ad was found, stop trying to fetch from other providers.
        /// </summary>
        /// <returns></returns>
        private bool TryNext()
        {
            m_adUrlIndex++;

            if (m_isMidRoll)
            {
                if (m_adUrlIndex >= VSettings.MidrollAdProviders.Count && m_playableAd == null)
                {
                    Error("Ad providers didn't provide any mid roll ads", false);
                    return false;
                }

                if (IsLastMidrollProvider() || m_playableAd != null)
                {
                    m_adUrlIndex = 0;
                    return false;
                }
            }
            else
            {
                if (m_adUrlIndex >= VSettings.PrerollAdProviders.Count && m_playableAd == null)
                {
                    Error("Ad providers didn't provide any ads", false);
                    return false;
                }

                if (IsLastPrerollProvider() || m_playableAd != null)
                {
                    m_adUrlIndex = 0;
                    return false;
                }
            }

            RequestAd();
            return true;
        }

        /// <summary>
        /// Report errors to ad network. Could be case of "infinite wrapper loop".
        /// </summary>
        /// <returns></returns>
        private IEnumerator ReportError()
        {
            if (m_wrapperList.Count > 0)
            {
                foreach (var wrapper in m_wrapperList)
                {
                    m_network.CallGet(wrapper.ErrorURI, null);
                    yield return new WaitForEndOfFrame();
                }
            }

        }

        /// <summary>
        /// Build ad
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        private IEnumerator BuildAdFromVast(VAST xml)
        {
            if (xml == null)
            {
                Error("Could not parse VAST", true);
                yield break;
            }

            if (xml.Ads == null)
            {
                Error("No ads in VAST or parsing failed", true);
                yield break;
            }

            foreach (var ad in xml.Ads)
            {
                if (ad.InLines != null)
                {
                    // if InLine, already playable ad
                    foreach (var inline in ad.InLines)
                    {
                        VPlayableAd parsedAd = ParseInline(inline);
                        if (m_wrapperList.Count > 0)
                        {
                            parsedAd.Wrappers.AddRange(m_wrapperList);
                        }
                        m_playableAd = parsedAd;
                        m_wrapperList.Clear();

                        yield return null;
                    }
                }

                if (ad.Wrappers != null)
                {
                    // if wrapper, get more info from VASTAdTagURI
                    foreach (var wrapper in ad.Wrappers)
                    {
                        ExtractWrapper(wrapper);
                        yield break;
                    }
                }
            }

            Success();
        }

        /// <summary>
        /// Get all the needed info for playable ad.
        /// </summary>
        /// <param name="inline"></param>
        /// <returns></returns>
        private VPlayableAd ParseInline(InLine inline)
        {
            VPlayableAd ad = new VPlayableAd();

            ad.AdSystem = inline.AdSystem.Text;
            ad.AdSystemVersion = inline.AdSystem.Version;
            ad.AdTitle = inline.AdTitle.Text;

            // add impressions
            for (int i = 0; i < inline.Impressions.Length; i++)
            {
                ad.Impressions.Add(i, inline.Impressions[i].URL);
            }

            // create creatives
            for (int i = 0; i < inline.Creatives.Creative.Length; i++)
            {
                VCreative vc = new VCreative();
                vc.ID = inline.Creatives.Creative[i].Id;
                vc.Sequence = inline.Creatives.Creative[i].Sequence;

                if (inline.Creatives.Creative[i].LinearAds != null)
                {
                    foreach (var linear in inline.Creatives.Creative[i].LinearAds)
                    {
                        vc.Type = "Linear";
                        if (linear.SkipOffset != null)
                        {
                            vc.SkipOffset = linear.SkipOffset;
                            vc.Skippable = true;
                        }
                        vc.Duration = linear.Duration;

                        if (linear.TrackingEvents != null)
                        {
                            // Get tracking events
                            Tracking[] tracking = linear.TrackingEvents.Tracking;
                            for (int j = 0; j < tracking.Length; j++)
                            {
                                if (vc.TrackingEvents.ContainsKey(tracking[j].EventType))
                                {
                                    vc.TrackingEvents[tracking[j].EventType].Add(tracking[j].EventURL);
                                }
                                else
                                {
                                    vc.TrackingEvents.Add(tracking[j].EventType, new List<string>() { tracking[j].EventURL });
                                }
                            }
                        }

                        if (linear.VideoClicks != null)
                        {
                            // Get click events
                            VideoClicks clicks = linear.VideoClicks;
                            if (clicks != null)
                            {
                                if (clicks.ClickThrough != null && !string.IsNullOrEmpty(clicks.ClickThrough.Text))
                                    vc.VideoClicks.Add(VStrings.Ad_ClickThrough, clicks.ClickThrough.Text);
                                if (clicks.ClickTracking != null && !string.IsNullOrEmpty(clicks.ClickTracking.Text))
                                    vc.VideoClicks.Add(VStrings.Ad_ClickTracking, clicks.ClickTracking.Text);
                                if (clicks.CustomClick != null && !string.IsNullOrEmpty(clicks.CustomClick.Text))
                                    vc.VideoClicks.Add(VStrings.Ad_CustomClick, clicks.CustomClick.Text);
                            }
                        }

                        if (linear.MediaFiles != null)
                        {
                            // Get playable media file
                            MediaFile[] media = linear.MediaFiles.MediaFile;
                            if (media != null)
                            {
                                for (int j = 0; j < media.Length; j++)
                                {
                                    vc.MediaFiles.Add(media[j].MimeType, media[j].MediaUrl.Trim());
                                }
                            }
                        }
                    }
                }
                else if (inline.Creatives.Creative[i].NonLinearAds != null)
                {
                    vc.Type = "NonLinear";
                    Debug.Log("NonLinearAds are not supported");
                }
                else if (inline.Creatives.Creative[i].CompanionAds != null && !m_isMidRoll)
                {
                    vc.Type = "CompanionAd";

                    // TODO: calculate space for banner based on image size in UI
                    Companion companion = FindSuitableCompanion(inline.Creatives.Creative[i].CompanionAds, 300, 250);
                    if (companion != null)
                    {
                        vc.StaticImageUrl = companion.StaticResourceURL.Text;
                        vc.CompanionClickThrough = companion.ClickThrough.Text;

                        // get creative view events
                        if (companion.TrackingEvents != null && companion.TrackingEvents.Tracking.Length > 0)
                        {
                            for (int j = 0; j < companion.TrackingEvents.Tracking.Length; j++)
                            {
                                if (vc.TrackingEvents.ContainsKey(companion.TrackingEvents.Tracking[j].EventType))
                                {
                                    vc.TrackingEvents[companion.TrackingEvents.Tracking[j].EventType].Add(companion.TrackingEvents.Tracking[j].EventURL);
                                }
                                else
                                {
                                    vc.TrackingEvents.Add(companion.TrackingEvents.Tracking[j].EventType, new List<string>() { companion.TrackingEvents.Tracking[j].EventURL });
                                }
                            }
                        }
                    }
                }

                ad.Creatives.Add(vc);
            }

            if (inline.Extensions != null && inline.Extensions.Extension != null)
            {
                if (inline.Extensions.Extension.Length > 0)
                {
                    for (int i = 0; i < inline.Extensions.Extension.Length; i++)
                    {
                        ad.Extensions.Add(inline.Extensions.Extension[i].Type, inline.Extensions.Extension[i].Value);
                    }
                }
            }

            return ad;
        }

        /// <summary>
        /// Get companion based on width and height. Must be exact match for time being.
        /// </summary>
        /// <param name="ads"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private Companion FindSuitableCompanion(CompanionAds[] ads, int width, int height)
        {
            foreach (var companions in ads)
            {
                for (int j = 0; j < companions.Companions.Length; j++)
                {
                    if (companions.Companions[j].Width == width && companions.Companions[j].Height == height)
                    {
                        if (companions.Companions[j].StaticResourceURL == null) continue;
                        return companions.Companions[j];
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Extract ad wrapper
        /// </summary>
        /// <param name="wrapper"></param>
        private void ExtractWrapper(Wrapper wrapper)
        {
            // Create wrapper
            VWrapper wr = new VWrapper();
            wr.AdSystem = wrapper.AdSystem.Text;
            wr.VASTAdTagURI = wrapper.AdTagURI.Text;
            if (wrapper.Error != null)
                wr.ErrorURI = wrapper.Error.Text;

            for (int i = 0; i < wrapper.Impressions.Length; i++)
            {
                wr.Impressions.Add(i, wrapper.Impressions[i].URL);
            }

            for (int i = 0; i < wrapper.Creatives.Creative.Length; i++)
            {
                VCreative vc = new VCreative();
                vc.ID = string.Empty;
                vc.Sequence = string.Empty;

                if (wrapper.Creatives.Creative[i].LinearAds[0] != null)
                {
                    vc.Type = "Linear";
                    vc.Duration = string.Empty;

                    // Get tracking events
                    if (wrapper.Creatives != null && wrapper.Creatives.Creative[i] != null)
                    {
                        if (wrapper.Creatives.Creative[i].TrackingEvents != null)
                        {
                            for (int k = 0; k < wrapper.Creatives.Creative[i].TrackingEvents.Length; k++)
                            {
                                Tracking[] tracking = wrapper.Creatives.Creative[i].TrackingEvents[k].Tracking;
                                for (int j = 0; j < tracking.Length; j++)
                                {
                                    if (vc.TrackingEvents.ContainsKey(tracking[j].EventType))
                                    {
                                        vc.TrackingEvents[tracking[j].EventType].Add(tracking[j].EventURL);
                                    }
                                    else
                                    {
                                        vc.TrackingEvents.Add(tracking[j].EventType, new List<string>() { tracking[j].EventURL });
                                    }
                                }
                            }
                        }

                        if (wrapper.Creatives.Creative[i].VideoClicks != null)
                        {
                            VideoClicks clicks = wrapper.Creatives.Creative[i].VideoClicks;
                            if (clicks.ClickThrough != null && !string.IsNullOrEmpty(clicks.ClickThrough.Text))
                                vc.VideoClicks.Add(VStrings.Ad_ClickThrough, clicks.ClickThrough.Text);
                            if (clicks.ClickTracking != null && !string.IsNullOrEmpty(clicks.ClickTracking.Text))
                                vc.VideoClicks.Add(VStrings.Ad_ClickTracking, clicks.ClickTracking.Text);
                            if (clicks.CustomClick != null && !string.IsNullOrEmpty(clicks.CustomClick.Text))
                                vc.VideoClicks.Add(VStrings.Ad_CustomClick, clicks.CustomClick.Text);
                        }
                    }
                }
                else if (wrapper.Creatives.Creative[i].NonLinearAds[0] != null)
                {
                    // TODO: Handling for nonlinear
                    vc.Type = "NonLinear";
                    vc.Duration = string.Empty;
                }
                else if (wrapper.Creatives.Creative[i].CompanionAds[0] != null)
                {
                    // TODO: Handling for companions
                    vc.Type = "CompanionAd";
                    vc.Duration = "0";
                }

                wr.Creatives.Add(vc);
            }

            m_wrapperList.Add(wr);
            m_wrapperCount++;

            RequestAd();
        }

        /// <summary>
        /// If all preroll ad providers have been tried out
        /// </summary>
        /// <returns></returns>
        private bool IsLastPrerollProvider()
        {
            return !(m_adUrlIndex < VSettings.PrerollAdProviders.Count);
        }

        private bool IsLastMidrollProvider()
        {
            return !(m_adUrlIndex < VSettings.MidrollAdProviders.Count);
        }
    }
}
