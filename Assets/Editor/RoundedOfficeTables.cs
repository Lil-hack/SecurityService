using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class RoundedOfficeTables
{
 const string Folder="Assets/OfficeExtension/RoundedTables/";
 static bool Top(string n)=>n.StartsWith("Workstation oak desktop")||n=="Thick executive desktop"||n=="Kitchen counter top"||n=="Kitchen island top"||n=="Coffee station countertop"||n=="Coffee bar oak counter"||n=="Collaboration oak table"||n=="Meeting table"||n=="Meeting tabletop"||n=="Coffee countertop";
 static Vector2 Outline(float angle,float w,float d,float radius,float inset)
 {
  int corner=Mathf.FloorToInt(angle/90)%4;float a=angle*Mathf.Deg2Rad;
  float x=(corner==0||corner==3)?1:-1;float z=corner<2?1:-1;
  return new Vector2(x*(w*.5f-radius)+Mathf.Cos(a)*(radius-inset),z*(d*.5f-radius)+Mathf.Sin(a)*(radius-inset));
 }
 static Mesh Build(Vector3 scale)
 {
  float w=Mathf.Abs(scale.x),h=Mathf.Abs(scale.y),d=Mathf.Abs(scale.z);
  float radius=Mathf.Min(.14f,Mathf.Min(w,d)*.16f),bevel=Mathf.Min(.018f,h*.24f);
  var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
  const int N=48;const int Rings=10;
  for(int ring=0;ring<Rings;ring++)
  {
   bool upper=ring>=5;float a=(upper?(ring-5):4-ring)*Mathf.PI/8;
   float inset=bevel*(1-Mathf.Cos(a));float y=(upper?1:-1)*(h*.5f-bevel+bevel*Mathf.Sin(a));
   for(int i=0;i<N;i++)
   {
    float angle=(i/12)*90+(i%12)*90f/11;var p=Outline(angle,w,d,radius,inset);
    vertices.Add(new Vector3(p.x/scale.x,y/scale.y,p.y/scale.z));
    float ar=angle*Mathf.Deg2Rad;Vector3 normal=new Vector3(Mathf.Cos(ar)*Mathf.Cos(a),(upper?1:-1)*Mathf.Sin(a),Mathf.Sin(ar)*Mathf.Cos(a));
    normals.Add(Vector3.Scale(normal,scale).normalized);uv.Add(new Vector2(p.x/w+.5f,p.y/d+.5f));
   }
  }
  for(int r=0;r<Rings-1;r++)for(int i=0;i<N;i++){int a=r*N+i,b=r*N+(i+1)%N,c=a+N,e=b+N;triangles.AddRange(new[]{a,c,b,b,c,e});}
  for(int face=0;face<2;face++)
  {
   int start=vertices.Count;float y=(face==0?-1:1)*h*.5f;Vector3 normal=face==0?Vector3.down:Vector3.up;
   vertices.Add(new Vector3(0,y/scale.y,0));normals.Add(normal);uv.Add(new Vector2(.5f,.5f));
   for(int i=0;i<N;i++){var p=Outline((i/12)*90+(i%12)*90f/11,w,d,radius,bevel);vertices.Add(new Vector3(p.x/scale.x,y/scale.y,p.y/scale.z));normals.Add(normal);uv.Add(new Vector2(p.x/w+.5f,p.y/d+.5f));}
   for(int i=0;i<N;i++){int a=start+1+i,b=start+1+(i+1)%N;triangles.AddRange(face==0?new[]{start,a,b}:new[]{start,b,a});}
  }
  var mesh=new Mesh{name="Rounded tabletop"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;
 }
 static void SaveMesh(Mesh mesh,string path)
 {
  var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);}
 }
 [MenuItem("Tools/Office/Round Acoustic Desk Dividers")]
 public static void RoundDividers()
 {
  if(Application.isPlaying)throw new Exception("Stop Play before editing dividers.");
  Directory.CreateDirectory(Folder);int count=0;
  foreach(var f in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include).Where(f=>f.gameObject.scene.IsValid()&&f.name.StartsWith("Acoustic teal desk divider")))
  {
   var scale=f.transform.lossyScale;var mesh=Build(new Vector3(scale.x,scale.z,scale.y));
   mesh.name="Rounded acoustic divider";
   mesh.vertices=mesh.vertices.Select(v=>new Vector3(v.x,-v.z,v.y)).ToArray();
   mesh.normals=mesh.normals.Select(v=>new Vector3(v.x,-v.z,v.y)).ToArray();mesh.RecalculateBounds();
   string path=Folder+"Divider_"+(++count)+".asset";SaveMesh(mesh,path);
   Undo.RecordObject(f,"Round acoustic divider");f.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
   foreach(var c in f.GetComponents<Collider>())Undo.DestroyObjectImmediate(c);
   var collider=Undo.AddComponent<MeshCollider>(f.gameObject);collider.sharedMesh=f.sharedMesh;collider.convex=true;
   PrefabUtility.RecordPrefabInstancePropertyModifications(f);
  }
  AssetDatabase.SaveAssets();var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  Debug.Log("Rounded acoustic desk dividers saved: "+count);Capture();
 }
 [MenuItem("Tools/Office/Round All Table Edges")]
 public static void Apply()
 {
  if(Application.isPlaying)throw new Exception("Stop Play before rounding tables.");
  Directory.CreateDirectory(Folder);int count=0;var report=new List<string>();
  foreach(var f in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude).Where(f=>f.gameObject.scene.IsValid()&&Top(f.name)))
  {
   Undo.RecordObject(f,"Round table edges");var scale=f.transform.lossyScale;var mesh=Build(scale);
   string path=Folder+"Table_"+(++count)+".asset";SaveMesh(mesh,path);f.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
   foreach(var c in f.GetComponents<Collider>())Undo.DestroyObjectImmediate(c);
   var collider=Undo.AddComponent<MeshCollider>(f.gameObject);collider.sharedMesh=f.sharedMesh;collider.convex=true;
   PrefabUtility.RecordPrefabInstancePropertyModifications(f);report.Add(f.name+" "+scale);
  }
  // Imported coffee tables keep their legs, atlas UVs and material. Subdivision
  // supplies enough vertices for a genuinely curved tabletop silhouette.
  foreach(var f in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude).Where(f=>f.gameObject.scene.IsValid()&&f.name.StartsWith("SM_Prop_CoffeeTable_01_")&&!f.sharedMesh.name.StartsWith("Rounded")))
  {
   var source=f.sharedMesh;var v=source.vertices.ToList();var uv=source.uv.ToList();var ts=source.triangles.ToList();
   for(int pass=0;pass<3;pass++)
   {
    var next=new List<int>();var edges=new Dictionary<ulong,int>();
    Func<int,int,int> mid=(a,b)=>{ulong key=((ulong)Math.Min(a,b)<<32)|(uint)Math.Max(a,b);if(edges.TryGetValue(key,out int ix))return ix;ix=v.Count;v.Add((v[a]+v[b])*.5f);uv.Add((uv[a]+uv[b])*.5f);edges[key]=ix;return ix;};
    for(int i=0;i<ts.Count;i+=3){int a=ts[i],b=ts[i+1],c=ts[i+2],ab=mid(a,b),bc=mid(b,c),ca=mid(c,a);next.AddRange(new[]{a,ab,ca,ab,b,bc,ca,bc,c,ab,bc,ca});}ts=next;
   }
   var bounds=source.bounds;float radius=.085f/Mathf.Abs(f.transform.lossyScale.x);
   for(int i=0;i<v.Count;i++)if(v[i].y>bounds.max.y-.085f/Mathf.Abs(f.transform.lossyScale.y))
   {
    var p=v[i];float cx=Mathf.Clamp(p.x,bounds.min.x+radius,bounds.max.x-radius),cz=Mathf.Clamp(p.z,bounds.min.z+radius,bounds.max.z-radius);var delta=new Vector2(p.x-cx,p.z-cz);if(delta.magnitude>radius){delta=delta.normalized*radius;p.x=cx+delta.x;p.z=cz+delta.y;v[i]=p;}
   }
   var mesh=new Mesh{name="Rounded coffee table"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(ts,0);mesh.RecalculateNormals();mesh.RecalculateBounds();string path=Folder+"Table_"+(++count)+".asset";SaveMesh(mesh,path);f.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
   foreach(var c in f.GetComponents<MeshCollider>()){c.sharedMesh=f.sharedMesh;EditorUtility.SetDirty(c);}PrefabUtility.RecordPrefabInstancePropertyModifications(f);report.Add(f.name);
  }
  AssetDatabase.SaveAssets();var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  File.WriteAllLines("OfficePreview/RoundedTables.txt",report);Debug.Log("Rounded table surfaces saved: "+count);Capture();
 }
 [MenuItem("Tools/Office/Capture Rounded Tables")]
 public static void Capture()
 {
  var top=GameObject.Find("Workstation oak desktop 1").transform;var cam=new GameObject("Table preview").AddComponent<Camera>();cam.fieldOfView=45;cam.transform.position=top.position+new Vector3(2,2,-2.2f);cam.transform.LookAt(top.position);
  var rt=new RenderTexture(1100,850,24);cam.targetTexture=rt;try{cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1100,850,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1100,850),0,0);tex.Apply();File.WriteAllBytes("OfficePreview/RoundedTables.png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}finally{cam.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(cam.gameObject);}
 }
}
