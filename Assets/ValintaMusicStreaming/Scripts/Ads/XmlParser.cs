using System.Xml.Serialization;

namespace ValintaMusicStreaming
{
    /// <summary>
    /// Deserializes the VAST response.
    /// </summary>

    #region VAST

    [XmlRoot("VAST")]
    public class VAST
    {
        [XmlAttribute("version")]
        public string Version { get; set; }

        [XmlElement("Ad")]
        public Ad[] Ads { get; set; }
    }

    public class Ad
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlElement("InLine")]
        public InLine[] InLines { get; set; }

        [XmlElement("Wrapper")]
        public Wrapper[] Wrappers { get; set; }
    }

    public class Wrapper
    {
        [XmlElement("AdSystem")]
        public AdSystem AdSystem { get; set; }

        [XmlElement("VASTAdTagURI")]
        public VASTAdTagURI AdTagURI { get; set; }

        [XmlElement("Error")]
        public Error Error { get; set; }

        [XmlElement("Impression")]
        public Impression[] Impressions { get; set; }

        [XmlElement("Creatives")]
        public Creatives Creatives { get; set; }

        [XmlElement("Extensions")]
        public Extension[] Extensions { get; set; }
    }

    public class InLine
    {
        [XmlElement("AdSystem")]
        public AdSystem AdSystem { get; set; }

        [XmlElement("AdTitle")]
        public AdTitle AdTitle { get; set; }

        [XmlElement("Error")]
        public Error Error { get; set; }

        [XmlElement("Impression")]
        public Impression[] Impressions { get; set; }

        [XmlElement("Creatives")]
        public Creatives Creatives { get; set; }

        [XmlElement("Extensions")]
        public Extensions Extensions { get; set; }
    }

    public class AdSystem
    {
        [XmlAttribute("version")]
        public string Version { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    public class AdTitle
    {
        [XmlText]
        public string Text { get; set; }
    }

    public class Impression
    {
        [XmlText]
        public string URL { get; set; }
    }

    public class Creatives
    {
        [XmlElement("Creative")]
        public Creative[] Creative { get; set; }
    }

    public class Extensions
    {
        [XmlElement("Extension")]
        public Extension[] Extension { get; set; }
    }

    public class Creative
    {
        [XmlAttribute("sequence")]
        public string Sequence { get; set; }
        // public int Sequence { get; set; }

        [XmlAttribute("id")]
        public string Id { get; set; }
        //public int Id { get; set; }

        [XmlElement("TrackingEvents")]
        public TrackingEvents[] TrackingEvents;

        [XmlElement("VideoClicks")]
        public VideoClicks VideoClicks { get; set; }

        [XmlElement("Linear")]
        public Linear[] LinearAds { get; set; }

        [XmlElement("NonLinearAds")]
        public NonLinearAd[] NonLinearAds { get; set; }

        [XmlElement("CompanionAds")]
        public CompanionAds[] CompanionAds { get; set; }
    }

    public class Linear
    {
        [XmlAttribute("skipoffset")]
        public string SkipOffset { get; set; }

        [XmlElement("Duration")]
        [XmlText]
        public string Duration { get; set; }

        [XmlElement("TrackingEvents")]
        public TrackingEvents TrackingEvents { get; set; }

        [XmlElement("VideoClicks")]
        public VideoClicks VideoClicks { get; set; }

        [XmlElement("MediaFiles")]
        public MediaFiles MediaFiles { get; set; }

        [XmlElement("AdParameters")]
        public AdParameter[] AdParameters { get; set; }
    }

    public class NonLinearAd
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("width")]
        public string Width { get; set; }
        // public int Width { get; set; }

        [XmlAttribute("height")]
        public string Height { get; set; }
        // public int Height { get; set; }

        [XmlAttribute("expandedWidth")]
        public string ExpandedWidth { get; set; }
        // public int ExpandedWidth { get; set; }

        [XmlAttribute("expandedHeight")]
        public string ExpandedHeight { get; set; }
        // public int ExpandedHeight { get; set; }

        [XmlAttribute("scalable")]
        public bool Scalable { get; set; }

        [XmlAttribute("maintainAspectRatio")]
        public bool MaintainAspectRatio { get; set; }

        [XmlAttribute("minSuggestedDuration")]
        public string MinSuggestedDuration { get; set; }

        [XmlAttribute("apiFramework")]
        public string ApiFramework { get; set; }

        [XmlElement("StaticResource")]
        public StaticResource StaticResourceURL { get; set; }

        [XmlElement("TrackingEvents")]
        public TrackingEvents TrackingEvents { get; set; }

        [XmlElement("NonLinearClickThrough")]
        public ClickThrough ClickThrough { get; set; }

        [XmlElement("AdParameters")]
        public AdParameter[] AdParameters { get; set; }
    }

    public class CompanionAds
    {
        [XmlElement("Companion")]
        public Companion[] Companions { get; set; }
    }

    public class Companion
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("width")]
        public int Width { get; set; }

        [XmlAttribute("height")]
        public int Height { get; set; }

        [XmlAttribute("expandedWidth")]
        public int ExpandedWidth { get; set; }

        [XmlAttribute("expandedHeight")]
        public int ExpandedHeight { get; set; }

        [XmlAttribute("apiFramework")]
        public string ApiFramework { get; set; }

        [XmlElement("StaticResource")]
        public StaticResource StaticResourceURL { get; set; }

        [XmlElement("TrackingEvents")]
        public TrackingEvents TrackingEvents { get; set; }

        [XmlElement("CompanionClickThrough")]
        public ClickThrough ClickThrough { get; set; }

        [XmlElement("AltText")]
        public AltText[] AltTexts { get; set; }

        [XmlElement("AdParameters")]
        public AdParameter[] AdParameters { get; set; }
    }

    public class TrackingEvents
    {
        [XmlElement("Tracking")]
        public Tracking[] Tracking { get; set; }
    }

    public class Tracking
    {
        [XmlAttribute("event")]
        public string EventType { get; set; }

        [XmlAttribute("offset")]
        public string Offset { get; set; }

        [XmlText]
        public string EventURL { get; set; }
    }

    public class VideoClicks
    {
        [XmlElement("ClickThrough")]
        public ClickThrough ClickThrough { get; set; }

        [XmlElement("ClickTracking")]
        public ClickTracking ClickTracking { get; set; }

        [XmlElement("CustomClick")]
        public CustomClick CustomClick { get; set; }
    }

    public class ClickThrough
    {
        [XmlText]
        public string Text { get; set; }
    }

    public class ClickTracking
    {
        [XmlText]
        public string Text { get; set; }
    }

    public class CustomClick
    {
        [XmlText]
        public string Text { get; set; }
    }

    public class MediaFiles
    {
        [XmlElement("MediaFile")]
        public MediaFile[] MediaFile { get; set; }
    }

    public class MediaFile
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("delivery")]
        public string DeliveryMethod { get; set; }

        [XmlAttribute("type")]
        public string MimeType { get; set; }

        [XmlAttribute("bitrate")]
        public string BitRate { get; set; }
        // public int BitRate { get; set; }

        [XmlAttribute("width")]
        public string Width { get; set; }
        // public int Width { get; set; }

        [XmlAttribute("height")]
        public int Height { get; set; }
        // public int Height { get; set; }

        [XmlAttribute("scalable")]
        public bool Scalable { get; set; }

        [XmlAttribute("maintainAspectRatio")]
        public bool MaintainAspect { get; set; }

        [XmlAttribute("apiFramework")]
        public string ApiFramework { get; set; }

        [XmlText]
        public string MediaUrl { get; set; }
    }

    public class Extension
    {
        [XmlAttribute("type")]
        public string Type { get; set; }

        [XmlText]
        public string Value { get; set; }
    }

    public class VASTAdTagURI
    {
        [XmlText]
        public string Text { get; set; }
    }

    public class Error
    {
        [XmlText]
        public string Text { get; set; }
    }

    public class AdParameter
    {
        [XmlText]
        public string Text { get; set; }
    }

    public class AltText
    {
        [XmlText]
        public string Text { get; set; }
    }

    public class StaticResource
    {
        [XmlText]
        public string Text { get; set; }
    }

    #endregion

    public class XmlParser
    {
        private VAST DeserializedXML;

        public XmlParser(string xml)
        {
            DeserializedXML = VUtils.Deserialize<VAST>(xml);
        }

        public VAST GetVAST()
        {
            return DeserializedXML;
        }
    }
}
