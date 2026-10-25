using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

// Uses the game's existing worker and commands, including Jev -> Gemma fallback.
public sealed class OfficeDialogueLabWorker : MonoBehaviour
{
    [Serializable]public sealed class Reply
    {
        public string kind,text,outcome,backend,response_backend;
        public bool accepted,valid,novel,loan_request;
        public float evaluation_ms;
    }
    readonly ConcurrentQueue<string> incoming=new ConcurrentQueue<string>();
    Process worker;bool started;
    TaskCompletionSource<Reply> pending;string expected,diagnostics="";
    public bool Ready {get;private set;}
    public event Action<Reply> PacketReceived;
    public async Task StartAsync(OfficeDialogueLabProfile profile)
    {
        Stop();string root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        string python=Path.IsPathRooted(profile.pythonPath)?profile.pythonPath:Path.Combine(root,profile.pythonPath);
        string model=GemmaModelFiles.Resolve(profile.modelPath,root);
        if(!File.Exists(python)||!File.Exists(model))throw new FileNotFoundException("Не найдены локальная модель Gemma или Python. Проверьте вкладку «Запуск».");
        worker=new Process{StartInfo=new ProcessStartInfo{
            FileName=python,Arguments="-u \""+Path.Combine(root,"Tools/Gemma/worker.py")+"\" --model \""+model+"\" "+(profile.isBoss?"--interview":"--office-social"),
            WorkingDirectory=root,UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,
            StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8}};
        worker.StartInfo.EnvironmentVariables["PYTHONUTF8"]="1";
        var owner=worker;
        worker.OutputDataReceived+=(_,e)=>{if(owner==worker && e.Data!=null)incoming.Enqueue(e.Data);};
        worker.ErrorDataReceived+=(_,e)=>{if(owner==worker && e.Data!=null)incoming.Enqueue("# "+e.Data);};
        Prepare("ready");
        try{worker.Start();started=true;worker.BeginOutputReadLine();worker.BeginErrorReadLine();await WaitAsync();Ready=true;}
        catch{Stop();throw;}
    }
    public async Task<Reply> RequestAsync(object command,string resultKind)
    {
        if(!Ready)throw new InvalidOperationException("Сначала начните тест.");
        Prepare(resultKind);
        try
        {
            byte[] bytes=Encoding.UTF8.GetBytes(JsonUtility.ToJson(command)+"\n");
            worker.StandardInput.BaseStream.Write(bytes,0,bytes.Length);worker.StandardInput.BaseStream.Flush();
            return await WaitAsync();
        }
        catch{Stop();throw;}
    }
    void Prepare(string kind){expected=kind;pending=new TaskCompletionSource<Reply>();}
    async Task<Reply> WaitAsync()
    {
        var task=pending.Task;
        if(await Task.WhenAny(task,Task.Delay(180000))!=task)throw new TimeoutException("Ответ не получен за 180 секунд. Можно начать тест заново.");
        return await task;
    }
    void Update()
    {
        while(incoming.TryDequeue(out string line))
        {
            if(line.StartsWith("# ")){diagnostics=line.Substring(2);continue;}
            Reply reply;try{reply=JsonUtility.FromJson<Reply>(line);}catch{continue;}
            if(reply==null)continue;
            if(reply.kind=="fatal"){Ready=false;pending?.TrySetException(new Exception(reply.text));continue;}
            PacketReceived?.Invoke(reply);
            if(reply.kind==expected)pending?.TrySetResult(reply);
        }
        if(started && worker!=null && worker.HasExited){Ready=false;pending?.TrySetException(new Exception("Процесс диалога завершился. "+diagnostics));}
    }
    public void Stop()
    {
        Ready=false;started=false;var old=worker;worker=null;
        if(old!=null){try{if(!old.HasExited){old.Kill();old.WaitForExit(2000);}}catch(InvalidOperationException){}finally{old.Dispose();}}
        pending?.TrySetCanceled();pending=null;diagnostics="";
        while(incoming.TryDequeue(out _)){}
    }
    void OnDestroy()=>Stop();
}
