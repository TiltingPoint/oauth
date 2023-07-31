using System;
using UnityEngine;

[Serializable]
public class TPAuthConfig : ScriptableObject
{
    public string issuer;
    public string clientId;
    public string callbackUrl;
    public string tokenUrl;
    public string verifyEmailUrl;
    public string logoutUrl;
}