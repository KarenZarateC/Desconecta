using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Desconecta;

/// <summary>Rebuilds the playable stage from the preserved original house.</summary>
public static class StageThreeBuilder
{
    private const string Generated = "Assets/Art/StageThree";
    private static Material wood, navy, cream, green, blue, amber;
    private static Font font;

    [MenuItem("Desconecta/Build Stage Three")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop play mode before building the scene.");
        EditorSceneManager.OpenScene("Assets/Level/Scenes/Nivel1_Recovered.unity", OpenSceneMode.Single);
        Directory.CreateDirectory(Generated);
        AssetDatabase.Refresh();
        var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        GraphicsSettings.defaultRenderPipeline = pipeline;
        QualitySettings.renderPipeline = pipeline;
        wood = Material("WarmFloor", new Color32(180,157,121,255));
        navy = Material("DeepNavy", new Color32(17,35,48,255));
        cream = Material("WarmWhite", new Color32(243,237,215,255));
        green = Material("ForestGreen", new Color32(46,111,79,255));
        blue = Material("WaterBlue", new Color32(54,156,199,255));
        amber = Material("WarmAmber", new Color32(245,178,52,255));
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
        var originalRoots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        var structure = new GameObject("PreservedHouseStructure");
        foreach (var root in originalRoots)
        {
            // Keep the recovered source untouched; all cleanup happens in the new scene.
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            if (root.name == "Player" || root.name == "Main Camera" || root.name == "Global Volume") { UnityEngine.Object.DestroyImmediate(root); continue; }
            if (root.name == "Directional Light") continue;
            root.transform.SetParent(structure.transform, true);
            if (root.name == "Plane") root.GetComponent<Renderer>().sharedMaterial = wood;
            else if (root.TryGetComponent<Renderer>(out var renderer)) renderer.sharedMaterial = cream;
        }
        structure.transform.localScale = Vector3.one * .5f;
        var plane = structure.transform.Find("Plane");
        if (plane != null) plane.localScale = new Vector3(6.7f,1,6.7f);
        var light = UnityEngine.Object.FindFirstObjectByType<Light>();
        if (light != null) { light.transform.rotation = Quaternion.Euler(55,-25,0); light.intensity=.65f; light.color=new Color(1,.95f,.85f); light.shadows=LightShadows.Soft; }
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.22f,.25f,.28f);
        RenderSettings.fog = false;

        var house = new GameObject("HouseFurnishings");
        FloorZone("Bathroom",new Vector3(-11, .02f,10),new Vector3(10,.04f,12),new Color32(181,211,210,255));
        FloorZone("Kitchen",new Vector3(9,.025f,10),new Vector3(14,.04f,12),new Color32(220,213,187,255));
        FloorZone("Bedroom",new Vector3(-11,.03f,-10),new Vector3(10,.04f,12),new Color32(207,198,181,255));
        FloorZone("LivingRoom",new Vector3(11,.035f,-8),new Vector3(10,.04f,16),new Color32(194,205,177,255));
        Prefab("Assets/Level/Prefabs/sala/SofaDouble.prefab", new Vector3(12,0,-7), 3.3f,180, house.transform);
        Prefab("Assets/Level/Prefabs/sala/TableRectangleShort.prefab",new Vector3(12,0,-10),2.4f,0,house.transform);
        Prefab("Assets/Level/Prefabs/sala/ShelvesMediumA.prefab",new Vector3(15,0,-3),2.7f,270,house.transform);
        Prefab("Assets/Level/Prefabs/bedroom/Bed_Full_BR003.prefab",new Vector3(-12,0,-12),3.2f,90,house.transform);
        Prefab("Assets/Level/Prefabs/bedroom/Nightstand_25in_BR001.prefab",new Vector3(-14.5f,0,-10),1.1f,0,house.transform);
        Prefab("Assets/Level/Prefabs/bedroom/Dresser_55in_BR001.prefab",new Vector3(-7.2f,0,-13),2.3f,0,house.transform);
        Prefab("Assets/Level/Prefabs/kitchen/Refrigerator_30Wx26.5Dx70H_KN002.prefab",new Vector3(14.8f,0,13.8f),2.4f,180,house.transform);
        Prefab("Assets/Level/Prefabs/kitchen/Island_67Wx36Dx36H_KN001a.prefab",new Vector3(9,0,9),2.7f,0,house.transform);
        Prefab("Assets/Level/Prefabs/kitchen/Barstool_29in_KN001.prefab",new Vector3(8,0,7.5f),1.5f,0,house.transform);
        Prefab("Assets/Level/Prefabs/kitchen/Barstool_29in_KN001.prefab",new Vector3(10,0,7.5f),1.5f,0,house.transform);
        Prefab("Assets/Prefabs/Objects/Houseplant_8.5in_LR001.prefab",new Vector3(14.5f,0,-.5f),1.5f,0,house.transform);
        Prefab("Assets/Prefabs/Objects/Houseplant_8.5in_LR001.prefab",new Vector3(-14.5f,0,6),1.5f,0,house.transform);
        RoomSign("COCINA",new Vector3(7,2.2f,4.4f));
        RoomSign("BAÑO",new Vector3(-11.5f,2.2f,4.4f));
        RoomSign("HABITACIÓN",new Vector3(-11,2.2f,-4.4f),180);
        RoomSign("SALA",new Vector3(12,2.2f,-2.4f),180);
        var posterMaterial=Material("HomePoster",Color.white);
        posterMaterial.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/StageThree/HomeIllustration.png");
        var poster=Primitive("SustainabilityPoster",PrimitiveType.Quad,null,new Vector3(16.02f,1.65f,1),new Vector3(2,2,1),posterMaterial);
        poster.transform.rotation=Quaternion.Euler(0,90,0);

        var player = new GameObject("FirstPersonPlayer");
        var controller = player.AddComponent<CharacterController>(); controller.height=1.8f; controller.radius=.28f; controller.center=new Vector3(0,.9f,0); controller.stepOffset=.25f;
        var explorer = player.AddComponent<FirstPersonExplorer>();
        var camObj = new GameObject("PlayerCamera",typeof(Camera),typeof(AudioListener));
        camObj.tag="MainCamera"; camObj.transform.SetParent(player.transform,false); camObj.transform.localPosition=new Vector3(0,1.6f,0);
        var camera = camObj.GetComponent<Camera>(); camera.nearClipPlane=.05f; camera.farClipPlane=110; camera.fieldOfView=72;
        explorer.viewCamera=camera; player.transform.position=explorer.spawnPosition;
        var game = new GameObject("DesconectaSession");
        var session = game.AddComponent<GameSession>(); session.explorer=explorer;
        var ui=game.AddComponent<GameInterface>(); ui.font=font; session.interfaceView=ui;
        ui.illustration=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/StageThree/HomeIllustration.png");
        session.musicSource=game.AddComponent<AudioSource>(); session.musicSource.clip=Audio("HomeAmbience"); session.musicSource.loop=true; session.musicSource.playOnAwake=true;
        session.feedbackSource=game.AddComponent<AudioSource>(); session.feedbackSource.playOnAwake=false;
        session.correctClip=Audio("Correct"); session.incorrectClip=Audio("Retry"); session.victoryClip=Audio("Victory"); session.timeoutClip=Audio("TimeUp");
        var situations=new List<HouseholdSituation>();
        situations.Add(Tap("kitchen_tap",1,"Grifo de cocina","Cocina",new Vector3(10.7f,0,14.6f)));
        situations.Add(Tap("bathroom_tap",2,"Grifo del baño","Baño",new Vector3(-12f,0,13.8f)));
        var lamp=Situation("living_light",3,"Luz de la sala","Sala",new Vector3(10,0,-2.5f),"La luz sigue encendida y nadie está usando esta parte de la sala. ¿Qué puedes hacer?","Dejarla encendida aunque no se necesite.","Apagar la luz que no se está utilizando.",1,"Apagaste una luz innecesaria.","Si no se necesita, la luz puede apagarse.","[Zumbido leve de lámpara encendida]");
        LampModel(lamp,new Vector3(0,0,0)); situations.Add(lamp);
        var tv=Situation("television",4,"Televisor","Sala",new Vector3(14.8f,0,-13.3f),"El televisor está encendido, pero nadie lo está viendo. ¿Qué decides?","Apagar el televisor.","Dejarlo encendido para tener compañía.",0,"Apagaste el televisor que nadie miraba.","Un aparato encendido sigue consumiendo energía.","[Sonido de televisor encendido]");
        Primitive("TelevisionStand",PrimitiveType.Cube,tv.transform,new Vector3(0,.45f,0),new Vector3(2.5f,.9f,.7f),wood);
        Primitive("TelevisionFrame",PrimitiveType.Cube,tv.transform,new Vector3(0,1.55f,0),new Vector3(2.5f,1.35f,.2f),navy);
        tv.activeEffect=Primitive("LitScreen",PrimitiveType.Cube,tv.transform,new Vector3(0,1.55f,-.12f),new Vector3(2.25f,1.12f,.025f),blue); situations.Add(tv);
        var charger=Situation("charger",5,"Cargador","Habitación",new Vector3(-7.5f,0,-11),"Hay un cargador conectado sin ningún dispositivo. ¿Cuál es la mejor decisión?","Dejarlo conectado sin usarlo.","Pedir a un adulto que lo desconecte.",1,"El cargador quedó desconectado con ayuda.","Pide ayuda para desconectar de forma segura lo que no se usa.","[Sin sonido] Cargador conectado sin dispositivo.");
        Primitive("ChargerTable",PrimitiveType.Cube,charger.transform,new Vector3(0,.4f,0),new Vector3(1.2f,.8f,.8f),wood);
        Primitive("ChargerBody",PrimitiveType.Cube,charger.transform,new Vector3(0,.95f,0),new Vector3(.3f,.3f,.25f),cream);
        charger.activeEffect=Primitive("PowerIndicator",PrimitiveType.Sphere,charger.transform,new Vector3(0,1.1f,-.14f),Vector3.one*.08f,amber); situations.Add(charger);
        var bedside=Situation("bedroom_lamp",6,"Lámpara","Habitación",new Vector3(-14.5f,0,-8.5f),"La lámpara quedó encendida después de leer y ya no se necesita. ¿Qué haces?","Apagar la lámpara.","Dejarla encendida todo el día.",0,"Apagaste la lámpara al terminar.","Al terminar una actividad, apaga la luz que ya no necesitas.","[Zumbido leve de lámpara encendida]");
        LampModel(bedside,Vector3.zero); situations.Add(bedside);
        var stove=Situation("stove",7,"Estufa","Cocina",new Vector3(5,0,14.5f),"La estufa sigue encendida y no hay un adulto cerca. ¿Qué debes hacer?","Intentar ajustar la estufa sin ayuda.","Alejarme y avisar a un adulto para que la revise.",1,"Avisaste a un adulto para revisar y apagar la estufa.","No manipules la estufa sin supervisión. Pide ayuda a un adulto.","[Sonido leve de estufa encendida]");
        Primitive("StoveBase",PrimitiveType.Cube,stove.transform,new Vector3(0,.55f,0),new Vector3(1.6f,1.1f,1),cream);
        Primitive("StoveTop",PrimitiveType.Cube,stove.transform,new Vector3(0,1.12f,0),new Vector3(1.65f,.08f,1.05f),navy);
        stove.activeEffect=new GameObject("ActiveBurners"); stove.activeEffect.transform.SetParent(stove.transform,false);
        for(int i=0;i<2;i++) Primitive("WarmBurner",PrimitiveType.Cylinder,stove.activeEffect.transform,new Vector3(-.4f+i*.8f,1.19f,0),new Vector3(.4f,.03f,.4f),amber);
        situations.Add(stove); session.situations=situations.ToArray();
        foreach(var item in situations) { item.ResetSituation(); }
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Level/Scenes/Desconecta_Stage3.unity",true)};
        PlayerSettings.productName="Desconecta"; PlayerSettings.companyName="Desconecta Team";
        PlayerSettings.defaultScreenWidth=1600; PlayerSettings.defaultScreenHeight=1000;
        PlayerSettings.runInBackground=true;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/Level/Scenes/Desconecta_Stage3.unity");
        Debug.Log("STAGE_THREE_BUILT: 7 situations; preserved original house; audio and accessibility integrated.");
    }

    private static Material Material(string name,Color color)
    {
        string path=Generated+"/"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null) { material=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material,path); }
        material.color=color; material.SetFloat("_Smoothness",.25f); return material;
    }
    private static AudioClip Audio(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Generated/"+name+".wav");

    private static GameObject Primitive(string name,PrimitiveType kind,Transform parent,Vector3 position,Vector3 scale,Material material)
    {
        var obj=GameObject.CreatePrimitive(kind); obj.name=name; obj.transform.SetParent(parent,false); obj.transform.localPosition=position; obj.transform.localScale=scale; obj.GetComponent<Renderer>().sharedMaterial=material; return obj;
    }

    private static void FloorZone(string name,Vector3 position,Vector3 scale,Color color)
    {
        Primitive(name+"Floor",PrimitiveType.Cube,null,position,scale,Material(name+"Floor",color));
    }

    private static GameObject Prefab(string path,Vector3 position,float width,float yaw,Transform parent)
    {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(asset==null) throw new InvalidOperationException("Missing prefab: "+path);
        var obj=(GameObject)PrefabUtility.InstantiatePrefab(asset); obj.transform.SetParent(parent,false); obj.transform.rotation=Quaternion.Euler(0,yaw,0);
        var renderers=obj.GetComponentsInChildren<Renderer>(); if(renderers.Length==0) return obj;
        foreach(var renderer in renderers)
        {
            string key=(renderer.name+" "+obj.name).ToLowerInvariant();
            Material finish=key.Contains("plant")?green:key.Contains("sofa")?green:key.Contains("bed_")?blue:key.Contains("refrigerator")?cream:wood;
            renderer.sharedMaterials=renderer.sharedMaterials.Select(m => m != null && m.mainTexture != null ? m : finish).ToArray();
        }
        var bounds=renderers[0].bounds; foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        float factor=width/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z); obj.transform.localScale*=factor;
        bounds=renderers[0].bounds; foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        obj.transform.position+=position-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        // Imported furniture may contain only visual meshes; add solid collision shapes.
        if(obj.GetComponentsInChildren<Collider>().Length==0)
            foreach(var filter in obj.GetComponentsInChildren<MeshFilter>()) if(filter.sharedMesh!=null) filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
        return obj;
    }

    private static void RoomSign(string text,Vector3 position,float yaw=0)
    {
        var root=new GameObject("RoomSign_"+text); root.transform.position=position; root.transform.rotation=Quaternion.Euler(0,yaw,0);
        Primitive("SignBack",PrimitiveType.Cube,root.transform,Vector3.zero,new Vector3(3.2f,.6f,.08f),navy);
        var obj=new GameObject("RoomName",typeof(TextMesh)); obj.transform.SetParent(root.transform,false); obj.transform.localPosition=new Vector3(0,0,-.06f);
        var label=obj.GetComponent<TextMesh>(); label.text=text; label.anchor=TextAnchor.MiddleCenter; label.font=font; label.fontSize=72; label.characterSize=.075f; label.color=Color.white; obj.GetComponent<Renderer>().sharedMaterial=font.material;
    }

    private static HouseholdSituation Situation(string id,int number,string title,string room,Vector3 position,string question,string a,string b,int correct,string success,string retry,string sound)
    {
        var root=new GameObject("Situation_"+number+"_"+id); root.transform.position=position;
        var item=root.AddComponent<HouseholdSituation>(); item.situationId=id; item.number=number; item.shortLabel=title; item.room=room; item.question=question; item.optionA=a; item.optionB=b; item.correctOption=correct; item.successMessage=success; item.retryMessage=retry; item.soundDescription=sound;
        var trigger=root.AddComponent<BoxCollider>(); trigger.center=new Vector3(0,1.3f,0); trigger.size=new Vector3(1.8f,2.6f,1.2f); trigger.isTrigger=true;
        var sign=new GameObject("ReadableStatus",typeof(TextMesh)); sign.transform.SetParent(root.transform,false); sign.transform.localPosition=new Vector3(0,2.2f,0);
        var label=sign.GetComponent<TextMesh>(); label.font=font; label.fontSize=72; label.characterSize=.04f; label.anchor=TextAnchor.MiddleCenter; label.alignment=TextAlignment.Center; label.color=Color.white; sign.GetComponent<Renderer>().sharedMaterial=font.material; item.label=label;
        var back=Primitive("StatusBack",PrimitiveType.Cube,sign.transform,new Vector3(0,0,.035f),new Vector3(2.6f,.65f,.04f),navy); UnityEngine.Object.DestroyImmediate(back.GetComponent<Collider>());
        var marker=Primitive("StatusMarker",PrimitiveType.Cylinder,root.transform,new Vector3(0,.07f,0),new Vector3(1.9f,.025f,1.9f),amber); UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>()); item.statusRenderer=marker.GetComponent<Renderer>();
        item.ambientSource=root.AddComponent<AudioSource>(); item.ambientSource.clip=id=="charger"?null:Audio(id.Contains("tap")?"WaterLoop":"ApplianceLoop"); item.ambientSource.loop=true; item.ambientSource.playOnAwake=false; item.ambientSource.spatialBlend=1; item.ambientSource.minDistance=1; item.ambientSource.maxDistance=7; item.ambientSource.rolloffMode=AudioRolloffMode.Linear; item.ambientSource.volume=.15f;
        var particles=new GameObject("CompletionParticles",typeof(ParticleSystem)); particles.transform.SetParent(root.transform,false); particles.transform.localPosition=new Vector3(0,1.4f,0);
        item.completionParticles=particles.GetComponent<ParticleSystem>(); var main=item.completionParticles.main; main.playOnAwake=false; main.loop=false; main.duration=.8f; main.startLifetime=.8f; main.startSpeed=1.2f; main.startSize=.08f; main.startColor=new Color(.45f,.8f,.6f); var emission=item.completionParticles.emission; emission.rateOverTime=0; emission.SetBursts(new[]{new ParticleSystem.Burst(0,18)}); var shape=item.completionParticles.shape; shape.shapeType=ParticleSystemShapeType.Sphere; shape.radius=.3f;
        particles.GetComponent<ParticleSystemRenderer>().sharedMaterial=green;
        item.completionParticles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        return item;
    }

    private static HouseholdSituation Tap(string id,int number,string title,string room,Vector3 position)
    {
        var item=Situation(id,number,title,room,position,"El grifo está abierto y no se está utilizando el agua. ¿Cómo puedes evitar el desperdicio?","Cerrar el grifo.","Dejar correr el agua aunque no se use.",0,"Cerraste el grifo y evitaste desperdiciar agua.","Si no estás usando agua, cierra el grifo.","[Agua corriendo] El grifo está abierto.");
        Primitive("Vanity",PrimitiveType.Cube,item.transform,new Vector3(0,.5f,0),new Vector3(1.8f,1,.9f),cream);
        Primitive("Basin",PrimitiveType.Cylinder,item.transform,new Vector3(0,1.02f,0),new Vector3(1.2f,.06f,.65f),navy);
        Primitive("FaucetStem",PrimitiveType.Cylinder,item.transform,new Vector3(0,1.3f,.25f),new Vector3(.08f,.3f,.08f),cream);
        Primitive("FaucetSpout",PrimitiveType.Cube,item.transform,new Vector3(0,1.55f,.04f),new Vector3(.1f,.1f,.5f),cream);
        item.activeEffect=Primitive("WaterStream",PrimitiveType.Cylinder,item.transform,new Vector3(0,1.3f,-.16f),new Vector3(.055f,.22f,.055f),blue);
        return item;
    }

    private static void LampModel(HouseholdSituation item,Vector3 position)
    {
        var lamp=Prefab("Assets/Level/Prefabs/Lights/LampTall.prefab",item.transform.position+position,1.8f,0,item.transform);
        foreach(var embeddedLight in lamp.GetComponentsInChildren<Light>()) embeddedLight.enabled=false;
        var bulb=new GameObject("LampLight",typeof(Light)); bulb.transform.SetParent(item.transform,false); bulb.transform.localPosition=new Vector3(0,1.6f,0);
        item.controlledLight=bulb.GetComponent<Light>(); item.controlledLight.type=LightType.Point; item.controlledLight.range=4; item.controlledLight.intensity=2; item.controlledLight.color=new Color(1,.85f,.5f);
    }
}
