using UnityEngine;
using UnityEngine.SceneManagement;

public static class OfficeGameSettings
{
    public static float Master=>Mathf.Clamp01(PlayerPrefs.GetFloat("Settings.MasterVolume",1));
    public static float Voice=>Mathf.Clamp01(PlayerPrefs.GetFloat("Settings.VoiceVolume",1));
    public static float Sensitivity=>Mathf.Clamp(PlayerPrefs.GetFloat("Settings.MouseSensitivity",.12f),.03f,.30f);
    public static void SetMaster(float value){PlayerPrefs.SetFloat("Settings.MasterVolume",Mathf.Clamp01(value));AudioListener.volume=Master;}
    public static void SetVoice(float value){PlayerPrefs.SetFloat("Settings.VoiceVolume",Mathf.Clamp01(value));}
    public static void SetSensitivity(float value){PlayerPrefs.SetFloat("Settings.MouseSensitivity",Mathf.Clamp(value,.03f,.30f));var p=Object.FindAnyObjectByType<OfficeThirdPersonController>();if(p!=null)p.mouseSensitivity=Sensitivity;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;
        if(PlayerPrefs.HasKey("Settings.Quality"))QualitySettings.SetQualityLevel(Mathf.Clamp(PlayerPrefs.GetInt("Settings.Quality"),0,QualitySettings.names.Length-1),true);
        if(!Application.isEditor&&PlayerPrefs.HasKey("Settings.Fullscreen"))Screen.fullScreen=PlayerPrefs.GetInt("Settings.Fullscreen")==1;
    }
    static void Loaded(Scene scene,LoadSceneMode mode)
    {
        AudioListener.volume=Master;
        var p=Object.FindAnyObjectByType<OfficeThirdPersonController>();if(p!=null)p.mouseSensitivity=Sensitivity;
    }
}
