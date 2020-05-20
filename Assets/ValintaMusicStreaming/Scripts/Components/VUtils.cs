using UnityEngine;
using System.Xml.Serialization;
using System.IO;
using System;
using System.Collections.Generic;

namespace ValintaMusicStreaming
{
    public static class VUtils
    {
        /// <summary>
        /// For deserializing XML
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static T Deserialize<T>(string xml)
        {
            T obj = default(T);
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (TextReader textReader = new StringReader(xml))
            {
                try
                {
                    obj = (T)serializer.Deserialize(textReader);
                }
                catch (FormatException e)
                {
                    Debug.Log("EXC: " + e.StackTrace.ToString());
                }
            }
            return obj;
        }

        /// <summary>
        /// Utility snippet for getting timestamp
        /// </summary>
        /// <returns></returns>
        public static double GetTimestamp()
        {
            TimeSpan span = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime());
            return (double)span.TotalSeconds;
        }

        /// <summary>
        /// Fisher-Yates shuffle
        /// more at http://stackoverflow.com/questions/273313/randomize-a-listt
        /// </summary>
        private static System.Random rng = new System.Random();
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while(n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
