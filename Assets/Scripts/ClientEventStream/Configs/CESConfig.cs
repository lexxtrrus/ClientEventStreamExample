using UnityEngine;

[CreateAssetMenu(fileName = "CESConfig", menuName = "ScriptableObjects/CESConfig", order = 1)]
public class CESConfig: ScriptableObject
{
    [SerializeField] private string _url;
    [SerializeField] private float _cooldownBeforeSend ;

    public string URL => _url;
    public float CooldownBeforeSend => _cooldownBeforeSend;
}