#if UNITY_WEBGL
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

public class WebGLClientImpl: IPostRequestSender
{
    public async UniTask<bool> PostAsync(string url, string json)
    {
        var request = new UnityWebRequest(url, "POST");
        var jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
        request.uploadHandler = (UploadHandler) new UploadHandlerRaw(jsonToSend);
        request.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        await request.SendWebRequest();

        return request.result == UnityWebRequest.Result.Success;
    }
}
#endif