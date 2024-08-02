using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

public class ClientEventStreamService : MonoBehaviour, IClientEventStreamSender
{
    private string _url;
    private float _cooldown;
    
    private bool _isCooldown = false;
    
    private const string EVENTS = "events";
    private string _path = Path.Combine(Application.streamingAssetsPath, "logs.json");
    
    private HttpClient _httpClient;
    private CancellationToken _token = new CancellationToken();
    
    private Dictionary<string, List<EventData>> _butch;
    private List<EventData> _pool = new List<EventData>();
    
    void Start()
    {
        DontDestroyOnLoad(this);
        SetupConfigs();
        _httpClient = CreateHttpClient();
        CheckLogsFromPreviousSession();
    }

    private void SetupConfigs()
    {
        var config = Resources.Load("Configs/CESConfig") as CESConfig;
        _url = config.URL;
        _cooldown = config.CooldownBeforeSend;
    }
    
    private HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        return client;
    }
    
    private void CheckLogsFromPreviousSession()
    {
        _butch = ReadJson();

        if (_butch.Count <= 0) return;
        
        foreach (var log in _butch[EVENTS])
        {
            TrackEvent(log);
        }
    }

    private Dictionary<string, List<EventData>> ReadJson()
    {
        if (File.Exists(_path))
        {
            using (FileStream fs = new FileStream(_path, FileMode.Open))
            {
                using (StreamReader reader = new StreamReader(fs))
                {
                    string json = reader.ReadToEnd();

                    var settings = new JsonSerializerSettings
                    {
                        Converters = new List<JsonConverter> { new EventLogsConverter() }
                    };

                    return JsonConvert.DeserializeObject<Dictionary<string, List<EventData>>>(json, settings);
                }
            }
        }
        else
        {
            return new Dictionary<string, List<EventData>>();
        }
    }
    
    private void WriteJson()
    {
        var settings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new EventLogsConverter() }
        };
        
        var json = JsonConvert.SerializeObject(_butch, settings);
        
        using (FileStream fs = new FileStream(_path, FileMode.Create))
        {
            using (StreamWriter writer = new StreamWriter(fs))
            {
                writer.Write(json);
            }
        }
    }
    
    public void TrackEvent(EventData eventData)
    {
        _pool.Add(eventData);

        if (!_isCooldown)
        {            
            _isCooldown = true;
            UniTask.Delay(TimeSpan.FromSeconds(_cooldown), ignoreTimeScale: false, cancellationToken: _token).ContinueWith(SendEvents);
        }
    }

    private async void SendEvents()
    {
        try
        {
            _butch[EVENTS] = _pool;
            _pool.Clear();

            var json = JsonConvert.SerializeObject(_butch);

            using var request = new HttpRequestMessage(HttpMethod.Post, _url);
            request.Content = new StringContent(json);

            using HttpResponseMessage response = await _httpClient.SendAsync(request, _token);

            if (!response.IsSuccessStatusCode)
            {
                WriteJson();
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
        finally
        {
            _isCooldown = false;

        }
    }
}
