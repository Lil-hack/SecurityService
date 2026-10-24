using System;
using System.IO;
using UnityEngine;

public static class GemmaModelFiles
{
    public const string FileName="gemma-4-E2B-it.litertlm";
    public const string DownloadUrl="https://pub-11968092bf704f989cd7b21a37bd6991.r2.dev/models/gemma-4-E2B-it.litertlm";
    public const long ExpectedBytes=2588147712;
    public const string ExpectedSha256="181938105e0eefd105961417e8da75903eacda102c4fce9ce90f50b97139a63c";
    public static string PortablePath=>Path.GetFullPath(Path.Combine(Application.dataPath,"..","Models",FileName));
    public static string WritablePath=>Application.isEditor?PortablePath:Path.Combine(Application.persistentDataPath,"Models",FileName);
    public static string Resolve(string configured,string projectRoot)
    {
        string path=Path.IsPathRooted(configured)?configured:Path.Combine(projectRoot,configured);
        if(Path.GetFileName(configured)!=FileName)return path;
        if(!Application.isEditor&&File.Exists(WritablePath))return WritablePath;
        return path;
    }
    public static string ExistingPath=>!Application.isEditor&&File.Exists(WritablePath)?WritablePath:File.Exists(PortablePath)?PortablePath:WritablePath;
}
