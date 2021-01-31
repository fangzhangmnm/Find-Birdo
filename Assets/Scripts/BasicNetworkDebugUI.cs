using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using ExitGames.Client.Photon;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BasicNetworkDebugUI : MonoBehaviour
{
    PhotonPeer Peer;
    public TMP_Text pingText;
    public TMP_Text trafficText;
    public TMP_Text lagText;
    public TMP_Text performanceText;
    public Toggle enableSimulationToggle;
    public Slider lagSlider, jitterSlider, lossSlider;
    public float averageTime = 2f;
    float smoothedDeltaTime = 0;
    void Start()
    {
        Peer = PhotonNetwork.NetworkingClient.LoadBalancingPeer;
        lagSlider.minValue = 0;
        lagSlider.maxValue = 500;
        lagSlider.value = 0;
        jitterSlider.minValue = 0;
        jitterSlider.maxValue = 100;
        jitterSlider.value = 0;
        lossSlider.minValue = 0;
        lossSlider.maxValue = 10;
        lossSlider.value = 0;
        enableSimulationToggle.isOn = false;
        PhotonNetwork.NetworkingClient.LoadBalancingPeer.TrafficStatsEnabled = true;
        StartCoroutine(MainLoop());
    }
     IEnumerator MainLoop()
    {
        while (true)
        {
            UpdateGui(.1f);
            yield return new WaitForSecondsRealtime(.1f);
        }
    }
    private void Update()
    {
        smoothedDeltaTime = Mathf.Lerp(smoothedDeltaTime, Time.deltaTime, 1f / 100f);
    }

    public float uploadRateKB, downloadRateKB, uploadMessageRate,downloadMessageRate;
    int lastUploadByte=0, lastDownloadByte=0, lastUploadMessageCount=0,lastDownloadMessageCount=0;

    void UpdateGui(float dt)
    {
        performanceText.text = $"fps {1 / smoothedDeltaTime:F1}";

        if (PhotonNetwork.IsConnectedAndReady)
        {
            TrafficStatsGameLevel gls = PhotonNetwork.NetworkingClient.LoadBalancingPeer.TrafficStatsGameLevel;

            uploadRateKB = Mathf.Lerp(uploadRateKB, (gls.TotalOutgoingByteCount - lastUploadByte) / 1024f / dt, dt / averageTime);
            downloadRateKB = Mathf.Lerp(downloadRateKB, (gls.TotalIncomingByteCount - lastDownloadByte) / 1024f / dt, dt / averageTime);
            lastUploadByte = gls.TotalOutgoingByteCount;
            lastDownloadByte = gls.TotalIncomingByteCount;
            trafficText.text = $"Up {uploadRateKB:F}KB/s Down {downloadRateKB:F}KB/s";

            uploadMessageRate = Mathf.Lerp(uploadMessageRate, (gls.TotalOutgoingMessageCount - lastUploadMessageCount) / dt, dt / averageTime);
            downloadMessageRate = Mathf.Lerp(downloadMessageRate, (gls.TotalIncomingMessageCount - lastDownloadMessageCount) / dt, dt / averageTime);
            lastUploadMessageCount = gls.TotalOutgoingMessageCount;
            lastDownloadMessageCount = gls.TotalIncomingMessageCount;
            trafficText.text += "\n" + $"Up {uploadMessageRate:F1}msg/s Down {downloadMessageRate:F1}msg/s";

            pingText.text = string.Format("RTT: {0,4}¡À{1,3}", Peer.RoundTripTime, Peer.RoundTripTimeVariance);
            bool enableSimulation = enableSimulationToggle.isOn;
            int lag = (int)lagSlider.value;
            int jitter = (int)jitterSlider.value;
            int loss = (int)lossSlider.value;
            lagText.text = $"Lag: {lag} Jitter: {jitter} Loss: {loss}%";

            Peer.IsSimulationEnabled = enableSimulation;
            Peer.NetworkSimulationSettings.IncomingLag = lag;
            Peer.NetworkSimulationSettings.OutgoingLag = lag;
            Peer.NetworkSimulationSettings.IncomingJitter = jitter;
            Peer.NetworkSimulationSettings.OutgoingJitter = jitter;
            Peer.NetworkSimulationSettings.IncomingLossPercentage = loss;
            Peer.NetworkSimulationSettings.OutgoingLossPercentage = loss;
        }




    }
    public void DoQuitApplication()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
