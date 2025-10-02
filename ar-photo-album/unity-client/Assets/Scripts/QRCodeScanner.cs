using System;
using System.Collections;
using UnityEngine;
using ZXing;
using ZXing.Common;

public class QRCodeScanner : MonoBehaviour {
    public Action<string> OnUrlDetected;

    private WebCamTexture webcamTexture;
    private IBarcodeReader barcodeReader;
    private float scanInterval = 0.3f;
    private bool isScanning = false;

    void Start() {
        barcodeReader = new BarcodeReader {
            AutoRotate = true,
            Options = new DecodingOptions {
                TryHarder = true,
                PossibleFormats = new[] { BarcodeFormat.QR_CODE }
            }
        };
        StartCamera();
    }

    void OnDestroy() {
        StopCamera();
    }

    public void StartCamera() {
        if (webcamTexture != null) return;
        if (WebCamTexture.devices.Length == 0) {
            Debug.LogWarning("No camera devices found");
            return;
        }
        var deviceName = WebCamTexture.devices[0].name;
        webcamTexture = new WebCamTexture(deviceName, 1280, 720, 30);
        GetComponent<Renderer>()?.material?.SetTexture("_MainTex", webcamTexture);
        webcamTexture.Play();

        if (!isScanning) {
            isScanning = true;
            StartCoroutine(ScanLoop());
        }
    }

    public void StopCamera() {
        if (webcamTexture != null) {
            if (webcamTexture.isPlaying) webcamTexture.Stop();
            webcamTexture = null;
        }
        isScanning = false;
    }

    IEnumerator ScanLoop() {
        var wait = new WaitForSeconds(scanInterval);
        while (isScanning) {
            yield return wait;
            TryDecode();
        }
    }

    void TryDecode() {
        if (webcamTexture == null || !webcamTexture.isPlaying) return;
        try {
            var pixels = webcamTexture.GetPixels32();
            var result = barcodeReader.Decode(pixels, webcamTexture.width, webcamTexture.height);
            if (result != null && !string.IsNullOrEmpty(result.Text)) {
                Debug.Log("QR detected: " + result.Text);
                OnUrlDetected?.Invoke(result.Text);
                isScanning = false;
            }
        } catch (Exception ex) {
            Debug.LogWarning("QR decode error: " + ex.Message);
        }
    }
}

