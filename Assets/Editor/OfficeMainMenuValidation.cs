using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using System.Security.Cryptography;

// Integration checks use a small local HTTP fixture, never the multi-gigabyte production model.
public static class OfficeMainMenuValidation
{
    [Serializable] sealed class Fixture {public long bytes;public string sha256,certSha256;}
    sealed class PinnedFixtureCertificate:CertificateHandler
    {
        readonly string expected;
        public PinnedFixtureCertificate(string sha){expected=sha;}
        protected override bool ValidateCertificate(byte[] certificateData)
        {using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(certificateData)).Replace("-","").Equals(expected,StringComparison.OrdinalIgnoreCase);}
    }
    static GemmaModelInstaller installer;static Fixture fixture;static string root,target;
    static int test;static float started;static bool resumed;static string report;
    static readonly string[] Names={"existing valid model","missing model download","HTTP Range resume","pause and resume","HTTP failure stays locked","invalid checksum rejected","corrupt existing file repaired","server ignores Range"};
    static PinnedFixtureCertificate certificate;
    const string Url="https://127.0.0.1:18766";
    [MenuItem("Tools/Office/Validate Main Menu Downloads")]
    public static void Run()
    {
        if(!Application.isPlaying||SceneManager.GetActiveScene().name!="MainMenu")throw new Exception("Play MainMenu first; run the local .local/MenuQA/server.py fixture.");
        if(installer!=null)throw new Exception("Checks already running.");
        root=Path.GetFullPath(".local/MenuQA");fixture=JsonUtility.FromJson<Fixture>(File.ReadAllText(Path.Combine(root,"spec.json")));
        certificate=new PinnedFixtureCertificate(fixture.certSha256);
        report="MainMenu integration checks\n";test=0;BeginCase();EditorApplication.update+=Tick;
    }
    static void BeginCase()
    {
        target=Path.Combine(root,"case-"+test+".litertlm");
        if(File.Exists(target))File.Delete(target);if(File.Exists(target+".part"))File.Delete(target+".part");
        byte[] data=File.ReadAllBytes(Path.Combine(root,"fixture.bin"));
        if(test==0)File.WriteAllBytes(target,data);
        if(test==2||test==7){var partial=new byte[262144];Array.Copy(data,partial,partial.Length);File.WriteAllBytes(target+".part",partial);}
        if(test==6){data[100]^=127;File.WriteAllBytes(target,data);}
        installer=new GameObject("Download integration case "+test).AddComponent<GemmaModelInstaller>();
        installer.Begin(new GemmaModelInstaller.Spec(Url+(test==4?"/missing":test==7?"/ignore":"/model"),target,fixture.bytes,test==5?new string('0',64):fixture.sha256){EditorLoopbackCertificate=certificate});
        started=Time.realtimeSinceStartup;resumed=false;
        if(test==1)
        {
            var menu=UnityEngine.Object.FindAnyObjectByType<OfficeMainMenu>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var field=typeof(OfficeMainMenu).GetField("installer",flags);var original=field.GetValue(menu);
            try
            {
                field.SetValue(menu,installer);typeof(OfficeMainMenu).GetMethod("UpdateDownload",flags).Invoke(menu,null);menu.StartSingle();
                var button=(Button)typeof(OfficeMainMenu).GetField("soloButton",flags).GetValue(menu);
                if(button.enabledSelf||SceneManager.GetActiveScene().name!="MainMenu")throw new Exception("Single player must stay locked until verified.");
                report+="PASS solo UI and programmatic start locked while downloading\n";
            }
            finally{field.SetValue(menu,original);typeof(OfficeMainMenu).GetMethod("UpdateDownload",flags).Invoke(menu,null);}
        }
    }
    static void Tick()
    {
        try
        {
            if(!Application.isPlaying)throw new Exception("Play stopped during checks.");
            if(Time.realtimeSinceStartup-started>35)throw new Exception("Download check timed out: "+Names[test]);
            if(test==3&&!resumed&&installer.State==GemmaModelInstaller.Stage.Downloading&&installer.BytesReceived>262144)installer.Pause();
            if(test==3&&!resumed&&!installer.Running&&installer.State==GemmaModelInstaller.Stage.Paused)
            {
                long partial=new FileInfo(target+".part").Length;if(partial<=0||partial>=fixture.bytes)throw new Exception("Pause lost partial file.");
                resumed=true;installer.Retry();return;
            }
            if(installer.Running)return;
            bool expectedFailure=test==4||test==5;
            if(expectedFailure)
            {if(installer.State!=GemmaModelInstaller.Stage.Failed||installer.Ready)throw new Exception("Invalid model became playable.");}
            else if(!installer.Ready||!GemmaModelInstaller.Validate(target,fixture.bytes,fixture.sha256,System.Threading.CancellationToken.None))throw new Exception(Names[test]+": "+installer.Message);
            if(test==3&&!resumed)throw new Exception("Pause was not exercised.");
            report+="PASS "+Names[test]+" ("+(Time.realtimeSinceStartup-started).ToString("F2")+"s)\n";
            UnityEngine.Object.Destroy(installer.gameObject);installer=null;
            test++;if(test<Names.Length){BeginCase();return;}
            EditorApplication.update-=Tick;certificate.Dispose();certificate=null;File.WriteAllText(Path.Combine(root,"Results.txt"),report);Debug.Log(report);
        }
        catch(Exception error)
        {
            EditorApplication.update-=Tick;if(installer!=null)UnityEngine.Object.Destroy(installer.gameObject);installer=null;
            certificate?.Dispose();certificate=null;
            report+="FAIL "+error.Message;File.WriteAllText(Path.Combine(root,"Results.txt"),report);Debug.LogError(report);
        }
    }
}
