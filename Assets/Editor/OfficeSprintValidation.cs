using System;
using System.IO;
using UnityEngine;

public static class OfficeSprintValidation
{
    public static string Run()
    {
        if(!Application.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
        var player=UnityEngine.Object.FindAnyObjectByType<OfficeThirdPersonController>();
        var motor=player.GetComponent<CharacterController>();
        var animator=player.humanoidAnimator;
        var hips=animator.GetBoneTransform(HumanBodyBones.Hips);
        var home=player.transform.position;
        bool enabled=player.enabled;
        var yawField=typeof(OfficeThirdPersonController).GetField("yaw",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        var yaw=yawField.GetValue(player);
        float maxOffset=0;
        bool hitWall=false;
        try
        {
            player.enabled=false;
            yawField.SetValue(player,0f);
            motor.enabled=false; player.transform.position=new Vector3(.25f,.06f,-2.8f); motor.enabled=true;
            animator.Rebind(); animator.Update(0);
            if(animator.applyRootMotion) throw new Exception("Controller-driven player has root motion enabled.");
            Physics.SyncTransforms();
            for(int i=0;i<200;i++)
            {
                // Sprint into the room boundary, then release Shift and stop.
                var flags=player.TickMotor(i<160?Vector2.down:Vector2.zero,i<120,false,.02f);
                hitWall|=(flags&CollisionFlags.Sides)!=0;
                animator.Update(.02f);
                var relative=player.transform.InverseTransformPoint(hips.position);
                maxOffset=Mathf.Max(maxOffset,new Vector2(relative.x,relative.z).magnitude);
                if(maxOffset>.2f) throw new Exception("Sprint model escaped its capsule: "+maxOffset+" m");
            }
            var wall=GameObject.Find("Front solid blue wall").GetComponent<Collider>();
            if(!hitWall || player.transform.position.z<wall.bounds.max.z+motor.radius-.03f)
                throw new Exception("Sprint crossed the room wall or never reached it.");
            string result="SPRINT_PASS: sprint, walk and stop; actual player blocked by front wall at z="+
                player.transform.position.z.ToString("F3")+"; maximum horizontal hips offset "+maxOffset.ToString("F3")+" m (limit 0.20 m).";
            Directory.CreateDirectory("OfficePreview");
            File.WriteAllText("OfficePreview/SprintAnimation_Validation.txt",result);
            return result;
        }
        finally
        {
            motor.enabled=false; player.transform.position=home; motor.enabled=true;
            yawField.SetValue(player,yaw);
            animator.Rebind(); animator.Update(0);
            player.enabled=enabled;
        }
    }
}
