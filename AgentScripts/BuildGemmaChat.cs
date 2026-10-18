using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildGemmaChat
{
    public static string Build()
    {
        const string path = "Assets/Scenes/GemmaChat.unity";
        if (System.IO.File.Exists(path))
        {
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            return path;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            throw new InvalidOperationException("Scene switch cancelled.");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera").AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.035f, .05f, .085f);
        camera.tag = "MainCamera";
        var type = Type.GetType("GemmaTestChat, Assembly-CSharp", true);
        new GameObject("Gemma Chat").AddComponent(type);
        EditorSceneManager.SaveScene(scene, path);
        return path;
    }
}
