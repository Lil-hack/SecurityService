using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildCharacterChat
{
    public static string Build()
    {
        const string path = "Assets/Scenes/CharacterChat.unity";
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first");
        var previous = SceneManager.GetActiveScene();
        if (previous.isDirty && !string.IsNullOrEmpty(previous.path)) EditorSceneManager.SaveScene(previous);
        if (System.IO.File.Exists(path))
        {
            EditorSceneManager.OpenScene(path);
            return path;
        }
        if (previous.isDirty) throw new InvalidOperationException("Unsaved untitled scene; save it first");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera").AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.035f, .05f, .085f);
        var chat = new GameObject("Character Chat").AddComponent<GemmaTestChat>();
        var serialized = new SerializedObject(chat);
        serialized.FindProperty("characterChatMode").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene, path);
        Selection.activeGameObject = chat.gameObject;
        return path;
    }
}
