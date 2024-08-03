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
    private string _path;
    
    private CancellationTokenSource _cnlTokenSource = new CancellationTokenSource();
    
    private Dictionary<string, List<EventData>> _batches;
    private List<EventData> _pool = new List<EventData>();

    private IPostRequestSender _postRequestSender;
    
    private void Start()
    {
        DontDestroyOnLoad(this);
        
#if UNITY_WEBGL
        _postRequestSender = new WebGLClientImpl();
#else
        _postRequestSender = new HttpClientImpl(_cnlTokenSource.Token);
#endif
        
        SetupConfigs();
        CheckLogsFromPreviousSession();
    }

    private void OnDestroy()
    {
        _cnlTokenSource.Dispose();
    }

    private void SetupConfigs()
    {
        _path = Path.Combine(Application.persistentDataPath, "logs.json");
        
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
        _batches = ReadJson();

        if (_batches.Count <= 0) return;
        
        foreach (var log in _batches[EVENTS])
        {
            TrackEvent(log);
        }
    }

    private Dictionary<string, List<EventData>> ReadJson()
    {
        if (File.Exists(_path))
        {
            var json = File.ReadAllText(_path);

            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new EventLogsConverter() }
            };

            return JsonConvert.DeserializeObject<Dictionary<string, List<EventData>>>(json, settings);
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
        
        var json = JsonConvert.SerializeObject(_batches, settings);

        File.WriteAllText(_path, json);
    }
    
    public void TrackEvent(EventData eventData)
    {
        _pool.Add(eventData);

        if (!_isCooldown)
        {            
            _isCooldown = true;
            UniTask.Delay(TimeSpan.FromSeconds(_cooldown), ignoreTimeScale: false, cancellationToken: _cnlTokenSource.Token).ContinueWith(SendEvents);
        }
    }

    private async void SendEvents()
    {
        try
        {
            _batches[EVENTS] = _pool;
            _pool.Clear();

            var json = JsonConvert.SerializeObject(_batches);

            var success = await _postRequestSender.PostAsync(_url, json);

            if (!success)
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