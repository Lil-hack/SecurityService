using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OfficeBossNpcValidation
{
    public static string RunStartup()
    {
        var npc=GameObject.Find("boss").GetComponent<OfficeBossNpc>();
        var animator=npc.GetComponent<Animator>();
        var player=npc.player; var position=player.position;
        var visited=new HashSet<OfficeBossNpc.Action>();
        try
        {
            player.position=new Vector3(10,0,-10);
            animator.Rebind(); npc.ResetRoutine(true,true); animator.Update(0);
            if(npc.IsSeated || Vector3.Distance(npc.transform.position,npc.standingAnchor.position)>.01f)
                throw new Exception("Boss did not start standing beside the chair.");
            for(int i=0;i<190;i++)
            {
                var previous=npc.transform.position;
                npc.TickNpc(.05f); animator.Update(.05f);
                npc.GetComponent<TripoArmClearance>().ApplyPose(); npc.ApplyInteractionPose();
                visited.Add(npc.CurrentAction);
                if(i<37 && (npc.CurrentAction!=OfficeBossNpc.Action.Idle || Vector3.Distance(npc.transform.position,npc.standingAnchor.position)>.001f))
                    throw new Exception("Boss moved before the initial two-second pause finished.");
                if(Vector3.Distance(previous,npc.transform.position)>.1f) throw new Exception("Startup movement teleported.");
                if(float.IsNaN(animator.GetBoneTransform(HumanBodyBones.Hips).position.y)) throw new Exception("Invalid startup pose.");
            }
            if(!npc.IsSeated || npc.CurrentAction!=OfficeBossNpc.Action.Typing || Vector3.Distance(npc.transform.position,npc.seatAnchor.position)>.01f ||
                !visited.Contains(OfficeBossNpc.Action.Idle) || !visited.Contains(OfficeBossNpc.Action.Walk) || !visited.Contains(OfficeBossNpc.Action.Turn) || !visited.Contains(OfficeBossNpc.Action.SitDown))
                throw new Exception("Startup did not complete: "+string.Join(",",visited));
            string result="PASS: starts beside chair, waits, walks smoothly, turns, sits and begins typing; no position jumps or invalid poses.";
            File.WriteAllText("OfficePreview/BossStartup_Validation.txt",result);
            return result;
        }
        finally
        {
            player.position=position;
            npc.ResetRoutine(); OfficeBossNpcSetup.ConfigureStandingStart();
        }
    }

    public static string Run()
    {
        var npc=GameObject.Find("boss").GetComponent<OfficeBossNpc>();
        var animator=npc.GetComponent<Animator>(); var clearance=npc.GetComponent<TripoArmClearance>();
        var player=npc.player; var playerPosition=player.position;
        var report=new List<string>();
        try
        {
            player.position=new Vector3(10,0,-10);
            foreach(OfficeBossNpc.Action action in Enum.GetValues(typeof(OfficeBossNpc.Action)))
            {
                npc.ResetRoutine(); animator.Rebind(); npc.PlayAction(action,8,false); animator.Update(0);
                var clip=animator.GetCurrentAnimatorClipInfo(0)[0].clip;
                if(!clip.humanMotion || clip.length<=0) throw new Exception("Invalid Humanoid clip: "+action);
                for(int i=0;i<60;i++) animator.Update(clip.length/120);
                clearance.ApplyPose(); npc.ApplyInteractionPose();
                var bounds=TripoHumanoidAnimationSetup.SkinnedBoundsOf(npc.gameObject);
                if(float.IsNaN(bounds.size.x) || bounds.size.y<.5f || bounds.size.y>3) throw new Exception("Invalid pose: "+action);
                report.Add(action+": Humanoid, length="+clip.length.ToString("F2")+", pose height="+bounds.size.y.ToString("F2"));
                if(action==OfficeBossNpc.Action.Typing || action==OfficeBossNpc.Action.Wave || action==OfficeBossNpc.Action.DrinkCoffee || action==OfficeBossNpc.Action.Reading)
                    Capture("OfficePreview/Boss_"+action+".png");
            }
            npc.ResetRoutine(); animator.Rebind(); animator.Update(0);
            var visited=new HashSet<OfficeBossNpc.Action>(); float maximumX=0;
            for(int i=0;i<1000;i++)
            {
                npc.TickNpc(.1f); animator.Update(.1f); clearance.ApplyPose(); npc.ApplyInteractionPose();
                visited.Add(npc.CurrentAction); maximumX=Mathf.Max(maximumX,npc.transform.position.x);
            }
            if(visited.Count!=11 || maximumX<2.5f) throw new Exception("Routine did not complete: "+string.Join(",",visited));
            report.Add("100-second routine: all 11 action types; walk reaches x="+maximumX.ToString("F2"));
            npc.ResetRoutine(); animator.Rebind(); animator.Update(0); player.position=new Vector3(0,.035f,-.65f);
            var greeting=new HashSet<OfficeBossNpc.Action>();
            for(int i=0;i<180;i++)
            {
                npc.TickNpc(.1f); animator.Update(.1f); clearance.ApplyPose(); npc.ApplyInteractionPose(); greeting.Add(npc.CurrentAction);
            }
            if(npc.GreetingCount!=1 || !greeting.Contains(OfficeBossNpc.Action.Wave) || !greeting.Contains(OfficeBossNpc.Action.Talk) ||
                !greeting.Contains(OfficeBossNpc.Action.Nod) || !greeting.Contains(OfficeBossNpc.Action.Shrug) || npc.CurrentAction!=OfficeBossNpc.Action.Typing)
                throw new Exception("Greeting/resume test failed: "+string.Join(",",greeting));
            report.Add("Approaching player: one greeting, wave/talk/nod/shrug, then resumes typing.");
            player.position=new Vector3(10,0,-10); npc.TickNpc(.1f);
            player.position=new Vector3(0,.035f,-.65f); npc.TickNpc(.1f);
            if(npc.GreetingCount!=2) throw new Exception("Greeting did not rearm after leaving.");
            report.Add("Leaving and returning: greeting rearms correctly.");
            File.WriteAllLines("OfficePreview/BossNpc_Validation.txt",report);
            return string.Join(" | ",report.Skip(18));
        }
        finally
        {
            player.position=playerPosition; npc.ResetRoutine(); animator.Rebind(); animator.Update(.2f);
            clearance.ApplyPose(); npc.ApplyInteractionPose();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
    }

    public static void Capture(string path)
    {
        var c=Camera.main; var p=c.transform.position; var q=c.transform.rotation; var f=c.fieldOfView; var mask=c.cullingMask;
        // Editor-side Animator.Update updates bones before GPU skinning receives a new frame.
        // Bake this preview only so the screenshot reflects the evaluated interaction pose.
        var renderer=GameObject.Find("boss").GetComponentInChildren<SkinnedMeshRenderer>();
        var mesh=new Mesh(); renderer.BakeMesh(mesh,true);
        var preview=new GameObject("Temporary boss pose preview");
        preview.transform.SetPositionAndRotation(renderer.transform.position,renderer.transform.rotation);
        preview.transform.localScale=renderer.transform.lossyScale;
        preview.AddComponent<MeshFilter>().sharedMesh=mesh;
        preview.AddComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
        bool wasEnabled=renderer.enabled;
        try
        {
            renderer.enabled=false;
            c.cullingMask=~(1<<2); c.fieldOfView=47;
            c.transform.position=new Vector3(2.7f,2.1f,.3f); c.transform.LookAt(new Vector3(0,1.2f,1.5f));
            ExecutiveOfficeBuilder.Capture(); File.Copy("OfficePreview/ExecutiveOffice.png",path,true);
        }
        finally
        {
            renderer.enabled=wasEnabled;
            c.transform.SetPositionAndRotation(p,q); c.fieldOfView=f; c.cullingMask=mask;
            UnityEngine.Object.DestroyImmediate(preview); UnityEngine.Object.DestroyImmediate(mesh);
        }
    }
}
