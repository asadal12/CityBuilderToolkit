using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CityBuilderToolkit.OSM
{
    /// <summary>
    /// Downloads live OpenStreetMap data via the Overpass API with automatic mirror failover.
    /// Operates in both Unity Editor mode and Play mode.
    /// </summary>
    public static class OsmOverpassClient
    {
        public static readonly string[] OverpassMirrors = new string[]
        {
            "https://overpass-api.de/api/interpreter",
            "https://overpass.kumi.systems/api/interpreter",
            "https://overpass.private.coffee/api/interpreter"
        };

        /// <summary>
        /// Builds an Overpass QL query targeting buildings, highways, parks, and waterways around a coordinate.
        /// </summary>
        public static string BuildAroundQuery(double lat, double lon, float radiusMeters)
        {
            var ci = CultureInfo.InvariantCulture;
            string r = radiusMeters.ToString("F0", ci);
            string la = lat.ToString("F6", ci);
            string lo = lon.ToString("F6", ci);

            return $@"[out:xml][timeout:35];
(
  way[""building""](around:{r},{la},{lo});
  way[""highway""](around:{r},{la},{lo});
  way[""leisure""=""park""](around:{r},{la},{lo});
  way[""landuse""=""grass""](around:{r},{la},{lo});
  way[""natural""=""water""](around:{r},{la},{lo});
  way[""waterway""](around:{r},{la},{lo});
);
(._;>;);
out body;";
        }

        /// <summary>
        /// Initiates asynchronous download of OSM data for the given coordinates.
        /// </summary>
        public static void FetchData(double lat, double lon, float radiusMeters,
            Action<string> onSuccess, Action<string> onError, Action<float, string> onProgress = null)
        {
            string query = BuildAroundQuery(lat, lon, radiusMeters);
            FetchWithMirrors(query, 0, onSuccess, onError, onProgress);
        }

        private static void FetchWithMirrors(string query, int mirrorIndex,
            Action<string> onSuccess, Action<string> onError, Action<float, string> onProgress)
        {
            if (mirrorIndex >= OverpassMirrors.Length)
            {
                onError?.Invoke("All Overpass API mirrors failed or timed out. Please check your internet connection or use a local .osm file.");
                return;
            }

            string server = OverpassMirrors[mirrorIndex];
            string url = server + "?data=" + UnityWebRequest.EscapeURL(query);
            onProgress?.Invoke((float)(mirrorIndex) / OverpassMirrors.Length, $"Contacting Overpass server {mirrorIndex + 1}/{OverpassMirrors.Length}...");

            var req = UnityWebRequest.Get(url);
            req.timeout = 35;

            var op = req.SendWebRequest();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // In Editor, hook into EditorApplication.update
                EditorApplication.CallbackFunction updateHook = null;
                updateHook = () =>
                {
                    if (op.isDone)
                    {
                        EditorApplication.update -= updateHook;
                        HandleResponse(req, mirrorIndex, query, onSuccess, onError, onProgress);
                    }
                    else
                    {
                        onProgress?.Invoke(0.1f + op.progress * 0.8f, $"Downloading OSM data ({req.downloadedBytes / 1024} KB)...");
                    }
                };
                EditorApplication.update += updateHook;
                return;
            }
#endif
            // In Play mode, use a coroutine host
            var host = GetOrCreateCoroutineHost();
            host.StartCoroutine(WaitForRequest(req, mirrorIndex, query, onSuccess, onError, onProgress));
        }

        private static IEnumerator WaitForRequest(UnityWebRequest req, int mirrorIndex, string query,
            Action<string> onSuccess, Action<string> onError, Action<float, string> onProgress)
        {
            while (!req.isDone)
            {
                onProgress?.Invoke(0.1f + req.downloadProgress * 0.8f, $"Downloading OSM data ({req.downloadedBytes / 1024} KB)...");
                yield return null;
            }
            HandleResponse(req, mirrorIndex, query, onSuccess, onError, onProgress);
        }

        private static void HandleResponse(UnityWebRequest req, int mirrorIndex, string query,
            Action<string> onSuccess, Action<string> onError, Action<float, string> onProgress)
        {
            try
            {
                if (req.result == UnityWebRequest.Result.Success)
                {
                    string text = req.downloadHandler.text;
                    if (!string.IsNullOrEmpty(text) && text.Contains("<osm"))
                    {
                        onProgress?.Invoke(1.0f, "OSM download completed successfully!");
                        onSuccess?.Invoke(text);
                        return;
                    }
                }

                Debug.LogWarning($"[OsmOverpassClient] Mirror {OverpassMirrors[mirrorIndex]} failed: {req.error}. Trying next mirror...");
                FetchWithMirrors(query, mirrorIndex + 1, onSuccess, onError, onProgress);
            }
            finally
            {
                req.Dispose();
            }
        }

        private static CoroutineHost GetOrCreateCoroutineHost()
        {
            var host = UnityEngine.Object.FindAnyObjectByType<CoroutineHost>();
            if (host == null)
            {
                var go = new GameObject("[CityBuilder_CoroutineHost]");
                go.hideFlags = HideFlags.HideAndDontSave;
                host = go.AddComponent<CoroutineHost>();
            }
            return host;
        }

        private class CoroutineHost : MonoBehaviour { }
    }
}
