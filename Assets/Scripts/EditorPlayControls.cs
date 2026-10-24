#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Compiled exclusively for Editor Play Mode. No scene component is required.
public sealed class EditorPlayControls : MonoBehaviour
{
    static EditorPlayControls instance;
    OfficeThirdPersonController player;
    bool visible=true, ownsControl, previousControl, previousCursorVisible;
    CursorLockMode previousCursor;
    float volume, originalVolume;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register(){SceneManager.sceneLoaded-=SceneLoaded;SceneManager.sceneLoaded+=SceneLoaded;}
    static void SceneLoaded(Scene scene,LoadSceneMode mode){Install();}
    static void Install()
    {
        if(instance!=null||SceneManager.GetActiveScene().name=="MainMenu")return;
        var go=new GameObject("Editor only · Play controls");
        instance=go.AddComponent<EditorPlayControls>();DontDestroyOnLoad(go);
    }
    void Awake()
    {
        originalVolume=AudioListener.volume;
        volume=OfficeGameSettings.Master;AudioListener.volume=volume;
    }
    void Update()
    {
        if(Keyboard.current!=null&&Keyboard.current.f8Key.wasPressedThisFrame)
        {
            visible=!visible;
            if(!visible)ReleaseInput();
        }
    }
    void LateUpdate()
    {
        if(!visible)return;
        if(player==null)player=FindAnyObjectByType<OfficeThirdPersonController>();
        if(!ownsControl)
        {
            previousCursor=Cursor.lockState;previousCursorVisible=Cursor.visible;
            if(player!=null){previousControl=player.ControlEnabled;player.SetControlEnabled(false);}
            ownsControl=true;
        }
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
    }
    void ReleaseInput()
    {
        if(!ownsControl)return;
        if(player!=null)player.SetControlEnabled(previousControl);
        Cursor.lockState=previousCursor;Cursor.visible=previousCursorVisible;ownsControl=false;
    }
    void SetVolume(float value)
    {
        volume=Mathf.Clamp01(value);AudioListener.volume=volume;
        OfficeGameSettings.SetMaster(volume);PlayerPrefs.Save();
    }
    void ResetSaves()
    {
        string scene=SceneManager.GetActiveScene().path;
        ReleaseInput();visible=false;
        PlayerPrefs.DeleteAll();PlayerPrefs.Save();volume=1;AudioListener.volume=1;
        player=null;
        // Reload so cached character/name/hiring state is cleared as well.
        SceneManager.LoadScene(scene);
    }
    void OnGUI()
    {
        if(!visible)
        {
            GUI.Box(new Rect(12,12,240,28),"F8 · настройки тестирования");return;
        }
        float width=Mathf.Min(440,Screen.width-24);
        GUILayout.BeginArea(new Rect((Screen.width-width)*.5f,32,width,275),GUI.skin.box);
        GUILayout.Space(10);
        GUILayout.Label("UNITY EDITOR · ТЕСТИРОВАНИЕ");
        GUILayout.Label("Эта панель отсутствует в сборке игры.");
        GUILayout.Space(14);
        GUILayout.Label("Общая громкость: "+Mathf.RoundToInt(volume*100)+"%");
        float changed=GUILayout.HorizontalSlider(volume,0,1);
        if(!Mathf.Approximately(changed,volume))SetVolume(changed);
        GUILayout.Space(18);
        if(GUILayout.Button("Сбросить все сохранения и перезапустить сцену",GUILayout.Height(36)))ResetSaves();
        GUILayout.Label("Очистит имя, персонажа, найм и остальные PlayerPrefs.");
        GUILayout.Space(14);
        if(GUILayout.Button("Продолжить игру · F8",GUILayout.Height(34))){visible=false;ReleaseInput();}
        GUILayout.EndArea();
    }
    void OnDestroy()
    {
        ReleaseInput();AudioListener.volume=originalVolume;
        if(instance==this)instance=null;
    }
}
#endif
