using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ValintaMusicStreaming
{
    public class VPlayableAd
    {
        private VNetwork m_network;

        public float StartTime { get; set; }
        public float TimeLeft { get; set; }
        public float Duration { get; set; }

        public float CurrentTimePosition { get; set; }

        public float SkipTime { get; private set; }
        public float TotalDuration { get; private set; }
        public float FirstQuartile { get; private set; }
        public float Midpoint { get; private set; }
        public float ThirdQuartile { get; private set; }
        public float Complete { get; private set; }
        public string LinkText { get; private set; }

        // Inline
        public string AdSystem;
        public string AdSystemVersion;
        public string AdTitle;
        public bool Skippable;
        public Dictionary<int, string> Impressions = new Dictionary<int, string>();
        public Dictionary<int, string> CreativeViews = new Dictionary<int, string>();
        public Dictionary<string, string> Extensions = new Dictionary<string, string>();

        public List<VWrapper> Wrappers = new List<VWrapper>();
        public List<VCreative> Creatives = new List<VCreative>();
        private List<string> ConsumedEvents = new List<string>();

        public VPlayableAd()
        {
            m_network = ValintaPlayer.Instance.GetNetworkInstance();
        }

        /// <summary>
        /// Set values for timed events
        /// </summary>
        public void UpdateEventTimeValues()
        {
            bool durationSet = SetDuration();
            if (!durationSet)
            {
                AnalyticsErrorTracking("SetDuration", "Could not set duration");
            }

            bool skiptimeSet = SetSkipTime();
            if (!skiptimeSet)
            {
                AnalyticsErrorTracking("SetSkipTime", "Ad not skippable");
            }

            Duration = TotalDuration;
            TimeLeft = TotalDuration;
            FirstQuartile = TotalDuration * 0.25f;
            Midpoint = TotalDuration * 0.5f;
            ThirdQuartile = TotalDuration * 0.75f;

        }

        /// <summary>
        /// Ad duration in seconds
        /// </summary>
        /// <returns></returns>
        private bool SetDuration()
        {
            TotalDuration = ParseTimeFromString(Creatives[0].Duration);

            if (TotalDuration > 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Set skip time if defined in VAST
        /// </summary>
        /// <returns></returns>
        private bool SetSkipTime()
        {
            bool foundSkipTime = false;
            string extensionSkipTime = string.Empty;

            // check if extensions already contains skipTime or skipTime2 etc
            foreach (string s in Extensions.Keys)
            {
                if (s.Contains("skipTime") && !extensionSkipTime.Equals(Extensions[s]))
                {
                    foundSkipTime = true;
                    extensionSkipTime = Extensions[s];
                }
            }

            // Not skippable
            if (!Creatives[0].Skippable && !foundSkipTime)
            {
                return false;
            }

            SkipTime = ParseTimeFromString(!string.IsNullOrEmpty(Creatives[0].SkipOffset) ? Creatives[0].SkipOffset : extensionSkipTime);
            Skippable = true;
            if (SkipTime > 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Parse time from string
        /// </summary>
        /// <param name="stringToParse"></param>
        /// <returns></returns>
        private float ParseTimeFromString(string stringToParse)
        {
            int percentage = 0;
            float hours = 0;
            float minutes = 0;
            float seconds = 0;
            bool parsed = false;

            // Contains percentage
            if (stringToParse.Contains("%"))
            {
                string newStr = stringToParse.Replace("%", string.Empty);

                parsed = int.TryParse(newStr, out percentage);

                return TotalDuration != 0 ? TotalDuration * (percentage / 100) : 0;
            }
            else
            {
                bool parseHours = false;
                string[] split = stringToParse.Split(':');

                if (split.Length > 2)
                {
                    parseHours = true;
                    parsed = float.TryParse(split[0], out hours);
                    parsed = float.TryParse(split[1], out minutes);
                }
                parsed = float.TryParse(split[!parseHours ? 0 : 2], out seconds);

                return parsed ? ((hours * 3600) + (minutes * 60) + seconds) : 0;
            }
        }

        /// <summary>
        /// Get URL for playable media
        /// </summary>
        /// <returns></returns>
        public string GetMediaUrl()
        {
            foreach (var c in Creatives[0].MediaFiles)
            {
                if (c.Key.Equals("audio/mp3") || c.Key.Equals("audio/mpeg"))
                {
                    return c.Value;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Get all impression URLs
        /// </summary>
        /// <returns></returns>
        public List<string> GetImpressionUrls()
        {
            List<string> impressionList = new List<string>();

            foreach (var wr in Wrappers)
            {
                foreach (var imp in wr.Impressions)
                {
                    impressionList.Add(imp.Value);
                }
            }

            foreach (var imp in Impressions)
            {
                impressionList.Add(imp.Value);
            }
            return impressionList;
        }

        /// <summary>
        /// Get all creative view event URLS
        /// </summary>
        /// <returns></returns>
        public List<string> GetCreativeViewUrls()
        {
            List<string> creativeViewList = new List<string>();

            foreach (var url in CreativeViews)
            {
                creativeViewList.Add(url.Value);
            }
            return creativeViewList;
        }

        /// <summary>
        /// Get URL for banner image (texture)
        /// </summary>
        /// <returns></returns>
        public string GetBannerImageUrl()
        {
            foreach (var c in Creatives)
            {
                if (c.Type != null)
                {
                    if (c.Type.Equals("CompanionAd"))
                    {
                        return c.StaticImageUrl;
                    }
                }
            }
            return string.Empty;
        }


        /// <summary>
        /// Event tracking for various events
        /// </summary>
        /// <param name="eventToTrack"></param>
        public void DoEventTracking(string eventToTrack)
        {
            if (IsEventFired(eventToTrack))
            {
                return;
            }

            ConsumedEvents.Add(eventToTrack);

            List<string> urlsToCall = new List<string>();

            for (int i = 0; i < Wrappers.Count; i++)
            {
                foreach (var wc in Wrappers[i].Creatives)
                {
                    if (wc.TrackingEvents.ContainsKey(eventToTrack))
                    {
                        List<string> urls = new List<string>();
                        bool gotValue = wc.TrackingEvents.TryGetValue(eventToTrack, out urls);

                        if (gotValue)
                        {
                            urlsToCall.AddRange(urls);
                        }
                    }
                }
            }

            foreach (var c in Creatives)
            {
                if (c.TrackingEvents.ContainsKey(eventToTrack))
                {
                    List<string> urls = new List<string>();
                    bool gotValue = c.TrackingEvents.TryGetValue(eventToTrack, out urls);

                    if (gotValue)
                    {
                        urlsToCall.AddRange(urls);
                    }
                }
            }

            if (urlsToCall.Count > 0)
            {
                TrackEvent(eventToTrack, urlsToCall);
            }
            else
            {
                AnalyticsErrorTracking(eventToTrack, "No such event");
            }
        }

        public bool IsEventFired(string eventName)
        {
            if (ConsumedEvents.Contains(eventName))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Used only for "status clicks". Will be removed when some ad providers start to use up-to-date tech.
        /// </summary>
        public void DoClickTracking()
        {
            // Call every click tracking link in wrapper
            foreach (var v in Wrappers)
            {
                ParseClickTrackingUrls(v.Creatives);
            }

            // Do the same for inline
            ParseClickTrackingUrls(Creatives);
        }

        /// <summary>
        /// Get click tracking URLs
        /// </summary>
        /// <param name="list"></param>
        private void ParseClickTrackingUrls(List<VCreative> list)
        {
            List<string> urlsToCall = new List<string>();

            foreach (var c in list)
            {
                bool parsed = false;
                string link = string.Empty;

                if (c.VideoClicks.ContainsKey(VStrings.Ad_ClickTracking))
                {
                    parsed = c.VideoClicks.TryGetValue(VStrings.Ad_ClickTracking, out link);

                    if (parsed)
                    {
                        urlsToCall.Add(link);
                    }
                }

                if (c.VideoClicks.ContainsKey(VStrings.Ad_CustomClick))
                {
                    parsed = c.VideoClicks.TryGetValue(VStrings.Ad_CustomClick, out link);

                    if (parsed)
                        urlsToCall.Add(link);
                }
            }

            if (urlsToCall.Count > 0)
                TrackEvent("Clicks", urlsToCall);
        }

        /// <summary>
        /// Get URL where to navigate when "status" is clicked.
        /// </summary>
        /// <returns></returns>
        public string GetClickUrlForStatus()
        {
            DoClickTracking();

            string clickUrl = string.Empty;
            bool parsed = false;

            foreach (var c in Creatives)
            {
                if (c.VideoClicks.ContainsKey(VStrings.Ad_ClickThrough))
                {
                    parsed = c.VideoClicks.TryGetValue(VStrings.Ad_ClickThrough, out clickUrl);
                    if (parsed)
                    {
                        return clickUrl;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            foreach (var v in Wrappers)
            {
                foreach (var c in v.Creatives)
                {
                    if (c.VideoClicks.ContainsKey(VStrings.Ad_ClickThrough))
                    {
                        parsed = c.VideoClicks.TryGetValue(VStrings.Ad_ClickThrough, out clickUrl);

                        if (parsed)
                        {
                            return clickUrl;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            return clickUrl;
        }

        /// <summary>
        /// Get URL where to navigate when "banner" is clicked.
        /// </summary>
        /// <returns></returns>
        public string GetBannerClickUrl()
        {
            if (Creatives == null) return string.Empty;

            foreach (var c in Creatives)
            {
                if (c.Type != null)
                {
                    if (c.Type.Equals("CompanionAd"))
                    {
                        return c.CompanionClickThrough;
                    }
                }
            }

            return string.Empty;
        }

        public void SetLinkText()
        {
            if (Extensions != null)
            {
                if (Extensions.ContainsKey("linkTxt"))
                {
                    if (Extensions["linkTxt"].Length > 3)
                    {
                        LinkText = Extensions["linkTxt"];
                        return;
                    }
                }
            }

            LinkText = "Advertisement";

        }

        /// <summary>
        /// Not yet implemented. Send analytics about errors to Valinta back end.
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="desc"></param>
        private void AnalyticsErrorTracking(string ev, string desc)
        {
            ;
        }

        /// <summary>
        /// Call tracking URLs.
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="urls"></param>
        private void TrackEvent(string ev, List<string> urls)
        {
            foreach (string url in urls)
            {
                m_network.CallGet(url, null);
            }
        }
    }

    public class VWrapper
    {
        public string AdSystem;
        public string VASTAdTagURI;
        public string ErrorURI;
        public Dictionary<int, string> Impressions = new Dictionary<int, string>();

        public List<VCreative> Creatives = new List<VCreative>();
    }

    public class VCreative
    {
        public VCreative()
        {
            TrackingEvents = new Dictionary<string, List<string>>();
            VideoClicks = new Dictionary<string, string>();
            MediaFiles = new Dictionary<string, string>();
        }

        public string Type { get; set; }

        public string ID { get; set; }

        public string Sequence { get; set; }

        public string Duration { get; set; }

        public string SkipOffset { get; set; }

        public bool Skippable { get; set; }

        public string StaticImageUrl { get; set; }

        public string CompanionClickThrough { get; set; }

        public Dictionary<string, List<string>> TrackingEvents;
        public Dictionary<string, string> VideoClicks;
        public Dictionary<string, string> MediaFiles;
    }
}

