using Cysharp.Threading.Tasks;
public interface IPostRequestSender
{
    UniTask<bool> PostAsync(string url, string json);
}
