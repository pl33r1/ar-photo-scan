using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;
using UnityEngine.XR.ARFoundation;

public class ARImageVideoController : MonoBehaviour {
    public ARTrackedImageManager trackedImageManager;
    public GameObject videoPrefab;
    public string backendBaseUrl = "http://localhost:3000";

    void OnEnable() {
        if (trackedImageManager != null) {
            trackedImageManager.trackedImagesChanged += OnChanged;
        }
    }

    void OnDisable() {
        if (trackedImageManager != null) {
            trackedImageManager.trackedImagesChanged -= OnChanged;
        }
    }

    void OnChanged(ARTrackedImagesChangedEventArgs args) {
        foreach (var trackedImage in args.added) {
            StartCoroutine(StartVideoForTrackedImage(trackedImage));
        }

        foreach (var trackedImage in args.updated) {
            if (trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking) {
                if (trackedImage.transform.childCount > 0) {
                    trackedImage.transform.GetChild(0).gameObject.SetActive(true);
                }
            } else {
                if (trackedImage.transform.childCount > 0) {
                    trackedImage.transform.GetChild(0).gameObject.SetActive(false);
                }
            }
        }

        foreach (var trackedImage in args.removed) {
            foreach (Transform child in trackedImage.transform) {
                Destroy(child.gameObject);
            }
        }
    }

    IEnumerator StartVideoForTrackedImage(ARTrackedImage trackedImage) {
        if (ManifestManager.Manifest == null) {
            Debug.LogWarning("Manifest is not loaded yet.");
            yield break;
        }

        string albumId = ManifestManager.Manifest.album_id;
        string photoId = trackedImage.referenceImage.name;

        var prefab = Instantiate(videoPrefab, trackedImage.transform);
        prefab.transform.localPosition = Vector3.zero;
        prefab.transform.localRotation = Quaternion.identity;
        prefab.transform.localScale = new Vector3(trackedImage.size.x, 1f, trackedImage.size.y);

        var videoPlayer = prefab.GetComponentInChildren<VideoPlayer>();
        if (videoPlayer == null) {
            Debug.LogError("VideoPlayer component not found on videoPrefab.");
            yield break;
        }

        string localPath = ManifestManager.GetCachedVideoPath(albumId, photoId);
        if (!File.Exists(localPath)) {
            string signedUrlApi = $"{backendBaseUrl}/video/{albumId}/{photoId}";
            string signedUrl = null;

            using (UnityWebRequest req = UnityWebRequest.Get(signedUrlApi)) {
                req.timeout = 15;
                yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                if (req.result != UnityWebRequest.Result.Success)
#else
                if (req.isNetworkError || req.isHttpError)
#endif
                {
                    Debug.LogError("Failed to fetch presigned URL: " + req.error);
                    yield break;
                }

                var json = req.downloadHandler.text;
                signedUrl = ExtractUrlFromJson(json);
            }

            if (string.IsNullOrEmpty(signedUrl)) {
                Debug.LogError("Presigned URL is empty.");
                yield break;
            }

            using (UnityWebRequest download = UnityWebRequest.Get(signedUrl)) {
                download.timeout = 60;
                yield return download.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                if (download.result != UnityWebRequest.Result.Success)
#else
                if (download.isNetworkError || download.isHttpError)
#endif
                {
                    Debug.LogError("Video download failed: " + download.error);
                    yield break;
                }

                try {
                    string dir = Path.GetDirectoryName(localPath);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    File.WriteAllBytes(localPath, download.downloadHandler.data);
                } catch (System.Exception ex) {
                    Debug.LogError("Failed to save video: " + ex.Message);
                    yield break;
                }
            }
        }

        string fileUrl = "file://" + localPath;
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = fileUrl;
        videoPlayer.isLooping = true;
        videoPlayer.playOnAwake = true;

        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) {
            yield return null;
        }
        videoPlayer.Play();
    }

    string ExtractUrlFromJson(string json) {
        const string key = "\"url\":";
        int idx = json.IndexOf(key);
        if (idx < 0) return null;
        int start = json.IndexOf('"', idx + key.Length);
        if (start < 0) return null;
        int end = json.IndexOf('"', start + 1);
        if (end < 0) return null;
        return json.Substring(start + 1, end - start - 1);
    }
}

