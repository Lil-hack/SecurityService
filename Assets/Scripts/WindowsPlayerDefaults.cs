using UnityEngine;

public static class WindowsPlayerDefaults
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Apply()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if(QualitySettings.names[QualitySettings.GetQualityLevel()]!="Windows Balanced") return;
        QualitySettings.vSyncCount=0;
        Application.targetFrameRate=60;
#endif
    }
}
