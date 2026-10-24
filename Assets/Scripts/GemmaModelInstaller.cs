using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

// Main-thread networking and background SHA-256 validation. A .part file is never playable.
public sealed class GemmaModelInstaller : MonoBehaviour
{
    public sealed class Spec
    {
        public string Url,Target,Sha256;public long Bytes;
#if UNITY_EDITOR
        public CertificateHandler EditorLoopbackCertificate;
#endif
        public Spec(string url,string target,long bytes,string sha){Url=url;Target=target;Bytes=bytes;Sha256=sha;}
    }
    public enum Stage { Checking, Downloading, Paused, Failed, Ready }
    public Stage State {get;private set;}=Stage.Checking;
    public float Progress {get;private set;}
    public long BytesReceived {get;private set;}
    public long TotalBytes {get;private set;}
    public float BytesPerSecond {get;private set;}
    public string Message {get;private set;}="Проверяем файлы игры…";
    public bool Ready=>State==Stage.Ready;
    public bool Running {get;private set;}
    public event Action Changed;
    Spec spec;UnityWebRequest request;CancellationTokenSource cancellation;bool paused;
    [Serializable] sealed class Receipt {public string sha;public long bytes,ticks;}
    volatile float verificationProgress;
    void Set(Stage stage,string message,float progress=0){State=stage;Message=message;Progress=Mathf.Clamp01(progress);Changed?.Invoke();}
    public void Begin()
    {
        string path=GemmaModelFiles.ExistingPath;
        if(!File.Exists(path))path=GemmaModelFiles.WritablePath;
        Begin(new Spec(GemmaModelFiles.DownloadUrl,path,GemmaModelFiles.ExpectedBytes,GemmaModelFiles.ExpectedSha256));
    }
    public void Begin(Spec configuration)
    {
        if(Running)return;spec=configuration;paused=false;cancellation?.Dispose();cancellation=new CancellationTokenSource();StartCoroutine(Install());
    }
    public void Retry(){if(!Running&&spec!=null)Begin(spec);}
    public void Pause(){if(!Running)return;paused=true;request?.Abort();cancellation?.Cancel();}
    IEnumerator Verify(string path,Action<bool> completed)
    {
        verificationProgress=0;Set(Stage.Checking,"Проверяем целостность модели…");
        var token=cancellation.Token;
        Task<bool> task=Task.Run(()=>Validate(path,spec.Bytes,spec.Sha256,token,p=>verificationProgress=p),token);
        while(!task.IsCompleted){Progress=verificationProgress;Changed?.Invoke();yield return null;}
        completed(task.Status==TaskStatus.RanToCompletion&&task.Result);
    }
    public static bool Validate(string path,long bytes,string sha,CancellationToken token,Action<float> progress=null)
    {
        if(!File.Exists(path)||new FileInfo(path).Length!=bytes)return false;
        using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read,4*1024*1024,FileOptions.SequentialScan))
        using(var hash=SHA256.Create())
        {
            byte[] header=new byte[8];if(stream.Read(header,0,8)!=8||System.Text.Encoding.ASCII.GetString(header)!="LITERTLM")return false;stream.Position=0;
            byte[] buffer=new byte[4*1024*1024];int count;long read=0;
            while((count=stream.Read(buffer,0,buffer.Length))>0){token.ThrowIfCancellationRequested();hash.TransformBlock(buffer,0,count,buffer,0);read+=count;progress?.Invoke((float)((double)read/bytes));}
            hash.TransformFinalBlock(Array.Empty<byte>(),0,0);
            return BitConverter.ToString(hash.Hash).Replace("-","").Equals(sha,StringComparison.OrdinalIgnoreCase);
        }
    }
    IEnumerator Install()
    {
        Running=true;TotalBytes=spec.Bytes;BytesPerSecond=0;BytesReceived=0;
        bool valid=false;
        if(File.Exists(spec.Target))
        {
            // Reuse a previous full checksum only while the file's size and timestamp are unchanged.
            valid=HasReceipt();if(!valid)yield return Verify(spec.Target,v=>valid=v);
            if(valid){FinishReady();yield break;}if(paused){FinishPaused();yield break;}
        }
        // A bundled model may live in a read-only installation folder. Repairs go to user data.
        if(!Application.isEditor&&spec.Target==GemmaModelFiles.PortablePath)spec.Target=GemmaModelFiles.WritablePath;
        string part=spec.Target+".part";
        try{Directory.CreateDirectory(Path.GetDirectoryName(spec.Target));}catch(Exception e){Fail("Не удалось создать папку загрузки. Проверьте доступ к диску.",e);yield break;}
        long offset=File.Exists(part)?new FileInfo(part).Length:0;
        if(offset==spec.Bytes)
        {
            yield return Verify(part,v=>valid=v);if(valid){Promote(part);yield break;}if(paused){FinishPaused();yield break;}
        }
        if(offset>=spec.Bytes){try{File.Delete(part);}catch(Exception e){Fail("Не удалось подготовить файл загрузки.",e);yield break;}offset=0;}
        // The host rejects HEAD; use the actual GET response for total/resume metadata.
        Set(Stage.Downloading,"Загружаем модель для диалогов…",(float)((double)offset/spec.Bytes));
        UnityWebRequestAsyncOperation operation=null;
        try
        {
            request=UnityWebRequest.Get(spec.Url);request.downloadHandler=new DownloadHandlerFile(part,offset>0){removeFileOnAbort=false};request.timeout=0;
#if UNITY_EDITOR
            if(spec.EditorLoopbackCertificate!=null&&new Uri(spec.Url).IsLoopback){request.certificateHandler=spec.EditorLoopbackCertificate;request.disposeCertificateHandlerOnDispose=false;}
#endif
            request.SetRequestHeader("Range","bytes="+offset+"-");operation=request.SendWebRequest();
        }
        catch(Exception e){request?.Dispose();request=null;Fail("Не удалось начать загрузку. Проверьте подключение и место на диске.",e);yield break;}
        float sampleTime=Time.realtimeSinceStartup;ulong previous=0;bool invalidRange=false;
        while(!operation.isDone)
        {
            if(offset>0&&request.responseCode==200){invalidRange=true;request.Abort();break;}
            BytesReceived=offset+(long)request.downloadedBytes;Progress=Mathf.Clamp01((float)((double)BytesReceived/spec.Bytes));
            float now=Time.realtimeSinceStartup;if(now-sampleTime>.4f){float speed=(request.downloadedBytes-previous)/(now-sampleTime);BytesPerSecond=BytesPerSecond<=0?speed:Mathf.Lerp(BytesPerSecond,speed,.3f);sampleTime=now;previous=request.downloadedBytes;}
            Changed?.Invoke();yield return null;
        }
        var result=request.result;long code=request.responseCode;string range=request.GetResponseHeader("Content-Range");string networkError=request.error;
        request.Dispose();request=null;
        if(paused){FinishPaused();yield break;}
        if(invalidRange){File.Delete(part);Running=false;Begin(spec);yield break;}
        if(result!=UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("Model download failed: "+networkError);
            // An HTTP error page must not become part of a resumable model.
            if(code>=400)try{using(var file=new FileStream(part,FileMode.OpenOrCreate,FileAccess.Write))file.SetLength(offset);}catch{}
            Fail("Загрузка прервалась"+(code>0?" (HTTP "+code+")":"")+". Нажмите «Продолжить».");yield break;
        }
        if(offset>0&&(code!=206||range==null||!range.StartsWith("bytes "+offset+"-",StringComparison.Ordinal))){File.Delete(part);Fail("Сервер не подтвердил продолжение загрузки. Повторите попытку.");yield break;}
        BytesReceived=File.Exists(part)?new FileInfo(part).Length:0;
        yield return Verify(part,v=>valid=v);
        if(paused){FinishPaused();yield break;}
        if(!valid){try{File.Delete(part);}catch{}Fail("Проверка файла не пройдена. Повторите загрузку.");yield break;}
        Promote(part);
    }
    void Promote(string part)
    {
        try{if(File.Exists(spec.Target))File.Replace(part,spec.Target,null);else File.Move(part,spec.Target);FinishReady();}
        catch(Exception e){Fail("Не удалось сохранить проверенную модель. Проверьте свободное место и доступ к папке.",e);}
    }
    bool HasReceipt()
    {
        try
        {
            if(spec.Sha256!=GemmaModelFiles.ExpectedSha256)return false;
            var info=new FileInfo(spec.Target);var receipt=JsonUtility.FromJson<Receipt>(File.ReadAllText(spec.Target+".verified.json"));
            if(receipt==null||receipt.sha!=spec.Sha256||receipt.bytes!=spec.Bytes||info.Length!=spec.Bytes||receipt.ticks!=info.LastWriteTimeUtc.Ticks)return false;
            using(var stream=File.OpenRead(spec.Target)){byte[] magic=new byte[8];return stream.Read(magic,0,8)==8&&System.Text.Encoding.ASCII.GetString(magic)=="LITERTLM";}
        }
        catch{return false;}
    }
    void FinishReady()
    {
        if(spec.Sha256==GemmaModelFiles.ExpectedSha256)
            try{File.WriteAllText(spec.Target+".verified.json",JsonUtility.ToJson(new Receipt{sha=spec.Sha256,bytes=spec.Bytes,ticks=File.GetLastWriteTimeUtc(spec.Target).Ticks}));}catch{}
        Running=false;Progress=1;BytesReceived=spec.Bytes;Set(Stage.Ready,"Модель готова. Можно начинать игру.",1);
    }
    void FinishPaused(){Running=false;Set(Stage.Paused,"Загрузка приостановлена. Её можно продолжить.",Progress);}
    void Fail(string message,Exception error=null){Running=false;Set(Stage.Failed,message,Progress);if(error!=null)Debug.LogWarning("Model installer: "+error.GetType().Name);}
    void OnDestroy(){paused=true;cancellation?.Cancel();request?.Abort();request?.Dispose();request=null;cancellation?.Dispose();}
}
