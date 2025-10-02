using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class AlbumManifest {
    public string album_id;
    public string title;
    public ImageEntry[] images;
}

[System.Serializable]
public class ImageEntry {
    public string id;
    public string name;
    public string ref_image_url;
    public string video_path;
    public float physical_width_m;
    public float physical_height_m;
}

public class ManifestManager : MonoBehaviour {
    public static AlbumManifest Manifest;

    public IEnumerator LoadManifest(string url) {
        using (UnityWebRequest req = UnityWebRequest.Get(url)) {
            req.timeout = 15;
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogError("Manifest load failed: " + req.error);
                yield break;
            }

            try {
                Manifest = JsonUtility.FromJson<AlbumManifest>(req.downloadHandler.text);
                Debug.Log("Manifest loaded: " + (Manifest != null ? Manifest.title : "null"));
            } catch (System.Exception ex) {
                Debug.LogError("Manifest parse failed: " + ex.Message);
            }
        }
    }

    public static string GetCachedVideoPath(string albumId, string photoId) {
        string dir = Path.Combine(Application.persistentDataPath, "videos", albumId);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        return Path.Combine(dir, photoId + ".mp4");
    }
}

