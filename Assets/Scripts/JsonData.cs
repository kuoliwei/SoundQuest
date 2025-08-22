using System;

[Serializable]
public struct Pitch
{
    public string mode;
    public string solfege;
    public string window_sec;
}
[Serializable]
public struct Mode
{
    public string mode;
}
[Serializable]
public struct Volume
{
    public string mode;
    public string dB;
    public string window_sec;
}