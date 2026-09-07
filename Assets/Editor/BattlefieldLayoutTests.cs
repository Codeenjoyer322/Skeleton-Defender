using System;
using UnityEngine;

namespace SkeletonDefender.Editor
{
    public static class BattlefieldLayoutTests
    {
        public static int Validate()
        {
            int checks = 0;
            Action<bool, string> check = (ok, message) => {
                checks++;
                if (!ok) throw new InvalidOperationException("Battlefield layout: " + message);
            };
            var model = new GameModel();
            var path = GameModel.Path;
            check(path.Length > 30, "rounded route is missing");
            check(Vector2.Distance(path[0], new Vector2(50,120)) < .001f, "enemy gate foot");
            check(Vector2.Distance(path[path.Length-1], new Vector2(1000,234)) < .001f, "castle gate foot");
            float distance = 0;
            for (int i=0; i<path.Length; i++)
            {
                check(path[i].x >= 0 && path[i].x <= 1056 && path[i].y >= 0 && path[i].y <= 640, "route outside field");
                if (i>0)
                {
                    float length=Vector2.Distance(path[i-1],path[i]);
                    check(length > .01f, "zero length segment");
                    distance+=length;
                }
                check(Vector2.Distance(model.Position(distance),path[i]) < .02f, "distance lookup skipped a bend");
                if (i>0 && i<path.Length-1)
                    check(Vector2.Angle(path[i]-path[i-1],path[i+1]-path[i]) < 18, "abrupt corner");
            }
            // Compare binary segment lookup with an independent linear traversal.
            for (int sample=0; sample<=400; sample++)
            {
                float target=model.PathLength*sample/400, remaining=target;
                Vector2 expected=path[path.Length-1];
                for (int i=0;i<path.Length-1;i++)
                {
                    float length=Vector2.Distance(path[i],path[i+1]);
                    if (remaining<=length) { expected=Vector2.Lerp(path[i],path[i+1],remaining/length);break; }
                    remaining-=length;
                }
                check(Vector2.Distance(model.Position(target),expected)<.03f,"binary lookup differs from path distance");
            }
            check(model.Position(-100)==path[0],"negative distance clamp");
            check(model.Position(model.PathLength+100)==path[path.Length-1],"exit distance clamp");
            check(GameModel.Sites.Length==9,"the marked lower build location was not removed");
            foreach (Vector2 site in GameModel.Sites)
            {
                check(Vector2.Distance(site, new Vector2(489,559)) > 1, "removed lower location is still buildable");
                float clearance=float.PositiveInfinity;
                for (int i=0;i<path.Length-1;i++)
                {
                    Vector2 delta=path[i+1]-path[i];
                    Vector2 nearest=path[i]+delta*Mathf.Clamp01(Vector2.Dot(site-path[i],delta)/delta.sqrMagnitude);
                    clearance=Mathf.Min(clearance,Vector2.Distance(site,nearest));
                }
                check(clearance>=69.9f,"foundation is too close to the road");
            }
            return checks;
        }
    }
}
