using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit opt-in standalone render check; ordinary game launches do not create this object.
public sealed class WindowsBuildSmokeTest : MonoBehaviour
{
    int errors;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void StartIfRequested()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if(Array.IndexOf(Environment.GetCommandLineArgs(), "-office-smoke-test")<0) return;
        Application.runInBackground=true;
        var runner=new GameObject("Standalone render validation").AddComponent<WindowsBuildSmokeTest>();
        DontDestroyOnLoad(runner.gameObject);
        Application.logMessageReceived+=runner.CountErrors;
#endif
    }

    void CountErrors(string message,string stack,LogType type)
    {
        if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) errors++;
    }

    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(8);
        yield return new WaitForEndOfFrame();
        var frame=ScreenCapture.CaptureScreenshotAsTexture();
        var pixels=frame.GetPixels32();
        int lit=0;
        foreach(var pixel in pixels) if(pixel.r+pixel.g+pixel.b>30) lit++;
        float fraction=(float)lit/pixels.Length;
        string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../OfficePreview/Windows_Standalone_Smoke.png"));
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        File.WriteAllBytes(output,frame.EncodeToPNG());
        Destroy(frame);
        bool passed=errors==0 && fraction>.1f && SceneManager.GetActiveScene().name=="ExecutiveOffice";
        Debug.Log($"STANDALONE_SMOKE {(passed ? "PASS" : "FAIL")}: scene={SceneManager.GetActiveScene().name}, litPixels={fraction:P1}, errors={errors}, screenshot={output}");
        Application.logMessageReceived-=CountErrors;
        Application.Quit(passed ? 0 : 1);
    }
}
