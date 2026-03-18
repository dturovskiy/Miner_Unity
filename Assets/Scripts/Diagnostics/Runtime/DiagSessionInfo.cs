using System;

[Serializable]
public sealed class DiagSessionInfo
{
    public string sessionId;
    public string projectName;
    public string companyName;
    public string productName;
    public string unityVersion;
    public string platform;
    public string appVersion;
    public string buildType;
    public string startedUtc;
    public string endedUtc;
    public string sessionFolderName;
    public string sessionFolderPath;
    public string eventsFilePath;
    public string summaryFilePath;
}
