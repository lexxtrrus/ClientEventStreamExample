#if !UNITY_WEBGL
using System.Net.Http;
using System.Threading;
using Cysharp.Threading.Tasks;

public class HttpClientImpl: IPostRequestSender
{
    private readonly CancellationToken _token;
    private HttpClient _httpClient;
    
    public HttpClientImpl(CancellationToken token)
    {
        _httpClient = new HttpClient();
        _token = token;
    }
    
    public async UniTask<bool> PostAsync(string url, string json)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(json);

        using HttpResponseMessage response = await _httpClient.SendAsync(request, _token);

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }
        else
        {
            return true;
        }
    }
}
#endif