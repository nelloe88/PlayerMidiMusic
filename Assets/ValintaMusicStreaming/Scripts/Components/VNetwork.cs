using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine.Networking;
using System.Text;

namespace ValintaMusicStreaming
{
    public class VNetwork : MonoBehaviour
    {
        public bool CallGet(string url, System.Action<string, string> callback = null)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                return false;
            }

            if (string.IsNullOrEmpty(url))
            {
                return false;
            }

            StartCoroutine(DoGetCall(url, callback));

            return true;
        }

        public bool CallPost(string url, string body, System.Action<string> callback)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                return false;
            }

            if (string.IsNullOrEmpty(url))
            {
                return false;
            }

            StartCoroutine(DoPostCall(url, body, callback));

            return true;
        }

        private IEnumerator DoGetCall(string url, System.Action<string, string> callback)
        {
            using (UnityWebRequest www = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
            {
                DownloadHandlerBuffer dHandler = new DownloadHandlerBuffer();
                www.downloadHandler = dHandler;
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", VSettings.ValintaApplicationID);

                yield return www.Send();

                if (callback != null)
                {
                    callback(www.downloadHandler.text, www.error);
                }
            }
        }

        private IEnumerator DoPostCall(string url, string body, System.Action<string> callback)
        {
            using (UnityWebRequest www = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                DownloadHandlerBuffer dHandler = new DownloadHandlerBuffer();
                UploadHandlerRaw uHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                www.uploadHandler = uHandler;
                www.downloadHandler = dHandler;
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", VSettings.ValintaApplicationID);
                if(!string.IsNullOrEmpty(VSettings.SessionToken))
                {
                    www.SetRequestHeader("Client-Token", VSettings.SessionToken);
                }


                yield return www.Send();

                if (callback != null)
                {
                    if (www.isNetworkError)
                    {
                        callback(null);
                        yield break;
                    }
                    else
                    {
                        callback(dHandler.text);
                    }
                }
            }
        }

        public void GetAudioClipFromSource(string url, System.Action<AudioClip> audioCallback)
        {
            StartCoroutine(GetAudioClip(url, audioCallback));
        }

        private IEnumerator GetAudioClip(string url, System.Action<AudioClip> audioCallback)
        {
            if (VSettings.UseWWWForAudioClip)
            {
                WWW audioLoader = new WWW(url);
                yield return audioLoader;

                if (!string.IsNullOrEmpty(audioLoader.error))
                {
                    audioCallback(null);
                }
                else
                {
                    audioCallback(audioLoader.GetAudioClip(false, true, AudioType.MPEG));
                }
            }
            else
            {
                using (UnityWebRequest songLoader = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
                {
                    yield return songLoader.Send();

                    if (songLoader.isNetworkError)
                    {
                        audioCallback(null);
                        yield break;
                    }
                    else
                    {
                        audioCallback(DownloadHandlerAudioClip.GetContent(songLoader));
                    }
                }
            }
        }

        public void GetTextureFromSource(string url, System.Action<Texture2D> textureCallback)
        {
            StartCoroutine(GetTexture(url, textureCallback));
        }

        private IEnumerator GetTexture(string url, System.Action<Texture2D> textureCallback)
        {
            using (UnityWebRequest texLoader = UnityWebRequestTexture.GetTexture(url))
            {
                yield return texLoader.Send();

                if(texLoader.isNetworkError)
                {
                    textureCallback(null);
                    yield break;
                }
                else
                {
                    textureCallback(DownloadHandlerTexture.GetContent(texLoader));
                }
            }
        }

        public void CancelCalls()
        {
            StopAllCoroutines();
        }
    }
}
