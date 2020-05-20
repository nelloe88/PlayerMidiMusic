using UnityEngine;
using System.Collections;

namespace ValintaMusicStreaming
{
    public static class VStrings
    {
        public const string APIV3 = "https://valinta.zemeho.com/api/v3/";
        public const string CATALOGUE = APIV3 + "catalog";
        public const string ANALYTICS_SESSION = APIV3 + "analytics/sessions";
        public const string ANALYTICS_PLAYBACK = APIV3 + "analytics/songs/playback";
        public const string APIV2 = "https://valinta.zemeho.com/api/v2/";

        public const string ValintaPlayer = "Valinta music streaming";
        public const string ChooseGenre = "Choose playlist or press play";

        public const string PlayerInitializing = "Player initializing";

        public const string Authorizing = "Authorizing player";
        public const string AuthorizationFailed = "Authorization failed";
        public const string AuthorizationSuccess = "Authorization success";

        public const string Loading = "Loading...";
        public const string Paused = "Paused";
        public const string TimedOut = "Timed out";
        public const string InternetConnectionError = "No internet connection";
        public const string ErrorPlaylists = "Playlists could not be fetched";
        public const string Retrying = "Retrying...";


        public const string AdvertisementBuff = "Advertisement playing...";
        // Ad specific strings
        public static readonly string Ad_Click = "click";      // Fired when ad clicked
        public static readonly string Ad_Skip = "skip";        // Fired when ad skipped
        public static readonly string Ad_Start = "start";      // Fired when the audio begins moving (shortly after the impression)
        public static readonly string Ad_FirstQuartile = "firstQuartile";  // 25%
        public static readonly string Ad_Midpoint = "midpoint";            // 50%
        public static readonly string Ad_ThirdQuartile = "thirdQuartile";  // 75%
        public static readonly string Ad_Complete = "complete";            // 100%
        public static readonly string Ad_Mute = "mute";                    // Fired when muted
        public static readonly string Ad_Unmute = "unmute";                // Fired when unmuted
        public static readonly string Ad_Pause = "pause";                  // Ad paused
        public static readonly string Ad_Resume = "resume";    // Ad resumed
        public static readonly string Ad_Progress = "progress";
        public static readonly string Ad_CreativeView = "creativeView";

        public static readonly string Ad_ClickThrough = "clickThrough";
        public static readonly string Ad_ClickTracking = "clickTracking";
        public static readonly string Ad_CustomClick = "customClick";

        public static readonly string LAST_PLAYED = "VALINTA_LAST_PLAYED";
    }
}
