using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Desconecta;

/// <summary>Checks scene integration and traversability using the player's actual capsule.</summary>
public static class StageThreeVerification
{
    public static object VerifyRuntime()
    {
        var session=GameSession.Instance;
        if(session==null || !Application.isPlaying) throw new InvalidOperationException("Run verification in Play Mode.");
        session.Begin();
        var checks=new List<object>();
        Action<string,bool> check=(name,passed)=> { checks.Add(new {name,passed}); if(!passed) Debug.LogError("VERIFY_FAILED: "+name); };
        check("Seven distinct situations",session.situations.Length==7 && session.situations.Select(x=>x.situationId).Distinct().Count()==7);
        check("Every target has descriptive audio and both options",session.situations.All(x=>!string.IsNullOrEmpty(x.soundDescription)&&!string.IsNullOrEmpty(x.optionA)&&!string.IsNullOrEmpty(x.optionB)));
        check("Background and feedback audio assigned",session.musicSource.clip!=null&&session.correctClip!=null&&session.incorrectClip!=null&&session.victoryClip!=null&&session.timeoutClip!=null);
        session.Inspect(session.situations[0]);
        check("Wrong decision leaves the situation unresolved",!session.Answer(1-session.situations[0].correctOption)&&!session.situations[0].IsResolved&&session.Progress.Completed==0);
        check("Correct decision resolves the target",session.Answer(session.situations[0].correctOption)&&session.Progress.Score==100&&session.Progress.Completed==1);
        session.Inspect(session.situations[0]);
        check("Resolved target cannot be scored again",session.Screen==SessionScreen.Playing&&session.Progress.Score==100);
        for(int i=1;i<session.situations.Length;i++){session.Inspect(session.situations[i]);session.Answer(session.situations[i].correctOption);}
        check("Seven successes reach results with 700 points",session.Progress.Won&&session.Progress.Score==700&&session.Screen==SessionScreen.Results);
        check("Resolved objects stop wasting resources",session.situations.All(x=>x.IsResolved&&(x.activeEffect==null||!x.activeEffect.activeSelf)&&(x.controlledLight==null||!x.controlledLight.enabled)&&!x.ambientSource.isPlaying));
        session.Begin();
        check("Restart clears score and restores all targets",session.Progress.Completed==0&&session.Progress.Score==0&&session.situations.All(x=>!x.IsResolved));
        session.Pause(); float time=session.Progress.RemainingSeconds; session.Progress.Tick(30,session.Screen==SessionScreen.Playing);
        check("Pause stops the clock",session.Progress.RemainingSeconds==time);
        session.Home();
        return new {checks,reachability=CheckReachability(session)};
    }

    private static object CheckReachability(GameSession session)
    {
        const int size=65;
        const float step=.5f;
        const float origin=-16;
        var free=new bool[size,size];
        var reached=new bool[size,size];
        var controller=session.explorer.GetComponent<CharacterController>();
        bool enabled=controller.enabled; controller.enabled=false;
        Physics.SyncTransforms();
        for(int x=0;x<size;x++) for(int z=0;z<size;z++)
        {
            var p=new Vector3(origin+x*step,0,origin+z*step);
            free[x,z]=!Physics.CheckCapsule(p+Vector3.up*.37f,p+Vector3.up*1.52f,.28f,~0,QueryTriggerInteraction.Ignore);
        }
        int sx=Mathf.RoundToInt((session.explorer.spawnPosition.x-origin)/step), sz=Mathf.RoundToInt((session.explorer.spawnPosition.z-origin)/step);
        var queue=new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(sx,sz)); reached[sx,sz]=true;
        var offsets=new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};
        while(queue.Count>0)
        {
            var p=queue.Dequeue();
            foreach(var offset in offsets)
            {
                var n=p+offset;
                if(n.x<0||n.x>=size||n.y<0||n.y>=size||reached[n.x,n.y]||!free[n.x,n.y]) continue;
                var world=new Vector3(origin+p.x*step,.37f,origin+p.y*step);
                var direction=new Vector3(offset.x,0,offset.y);
                if(Physics.CapsuleCast(world,world+Vector3.up*1.15f,.28f,direction,step,~0,QueryTriggerInteraction.Ignore)) continue;
                reached[n.x,n.y]=true; queue.Enqueue(n);
            }
        }
        var results=new List<object>();
        foreach(var target in session.situations)
        {
            bool accessible=false; Vector3 viewpoint=Vector3.zero;
            for(int x=0;x<size&&!accessible;x++) for(int z=0;z<size&&!accessible;z++)
            {
                if(!reached[x,z]) continue;
                var eye=new Vector3(origin+x*step,1.65f,origin+z*step);
                var direction=target.transform.position+Vector3.up*1.3f-eye;
                if(direction.magnitude>3.6f) continue;
                if(Physics.Raycast(eye,direction.normalized,out var hit,3.8f,~0,QueryTriggerInteraction.Collide)&&hit.collider.GetComponentInParent<HouseholdSituation>()==target) {accessible=true;viewpoint=eye;}
            }
            results.Add(new {id=target.situationId,accessible,viewpoint=viewpoint.ToString()});
            if(!accessible) Debug.LogError("TARGET_UNREACHABLE: "+target.situationId);
        }
        controller.enabled=enabled;
        return results;
    }
}
