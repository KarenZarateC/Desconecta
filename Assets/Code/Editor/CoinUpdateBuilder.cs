using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Desconecta;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Reproducible coin assets and reachable placements for the existing house.</summary>
public static class CoinUpdateBuilder
{
    public const string ScenePath = "Assets/Level/Scenes/Desconecta_Stage3.unity";
    public const string ArtPath = "Assets/Art/Coins";
    public const string PrefabPath = "Assets/Level/Prefabs/Collectibles/GoldCoin.prefab";

    [MenuItem("Desconecta/Coins/Apply Coin Update")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before applying coins.");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath);
        ApplyToCurrentScene();
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("COIN_UPDATE_BUILT: 24 reachable coins, animations, counter and Camilo audio.");
    }

    public static void ApplyToCurrentScene()
    {
        Directory.CreateDirectory(ArtPath);
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        AssetDatabase.Refresh();
        var session = UnityEngine.Object.FindFirstObjectByType<GameSession>();
        if (session == null) throw new InvalidOperationException("The scene must contain a GameSession.");
        var old = GameObject.Find("CollectibleCoins");
        if (old != null) UnityEngine.Object.DestroyImmediate(old);
        ConfigureAudio("Assets/Audio/Camilo/Musica de fondo.mp3", true);
        ConfigureAudio("Assets/Audio/Camilo/Coin.mp3", false);
        session.musicSource.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Camilo/Musica de fondo.mp3");
        session.musicSource.loop = true;
        session.musicSource.spatialBlend = 0;
        session.musicSource.playOnAwake = true;
        session.coinClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Camilo/Coin.mp3");
        if (session.musicSource.clip == null || session.coinClip == null) throw new InvalidOperationException("Camilo audio failed to import.");

        var gold = CreateMaterial("CoinGold", new Color(1f, .69f, .035f), .78f, .73f);
        var inset = CreateMaterial("CoinInsetGold", new Color(.72f, .36f, .015f), .65f, .6f);
        var sparkle = CreateMaterial("CoinSparkle", new Color(1f, .85f, .18f), 0, .3f);
        var mesh = CreateCoinMesh();
        string meshPath = ArtPath + "/BeveledGoldCoin.asset";
        var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (savedMesh == null) { AssetDatabase.CreateAsset(mesh, meshPath); savedMesh = mesh; }
        else { EditorUtility.CopySerialized(mesh, savedMesh); UnityEngine.Object.DestroyImmediate(mesh); }
        var spin = CreateSpinClip();
        var collect = CreateCollectClip();
        var template = new GameObject("GoldCoin");
        template.layer = 2; // Ignore Raycast: coins never block household interactions.
        var coin = template.AddComponent<CollectibleCoin>();
        var trigger = template.GetComponent<SphereCollider>(); trigger.radius = .64f; trigger.isTrigger = true;
        var body = template.GetComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
        coin.coinId = "assign_unique_id";
        coin.visual = new GameObject("Visual").transform; coin.visual.SetParent(template.transform, false);
        var model = new GameObject("BeveledCoin", typeof(MeshFilter), typeof(MeshRenderer));
        model.transform.SetParent(coin.visual, false);
        model.GetComponent<MeshFilter>().sharedMesh = savedMesh;
        model.GetComponent<Renderer>().sharedMaterials = new[] { gold, inset };
        // Raised vertical mark on BOTH faces, echoing the supplied reference.
        for (int side = -1; side <= 1; side += 2)
        {
            var mark = GameObject.CreatePrimitive(PrimitiveType.Cube); mark.name = "RaisedMark_" + side;
            mark.transform.SetParent(coin.visual, false);
            mark.transform.localPosition = new Vector3(0, 0, side * .103f);
            mark.transform.localScale = new Vector3(.105f, .48f, .044f);
            mark.GetComponent<Renderer>().sharedMaterial = gold;
            UnityEngine.Object.DestroyImmediate(mark.GetComponent<Collider>());
        }
        coin.coinAnimation = template.AddComponent<Animation>();
        coin.coinAnimation.playAutomatically = false;
        AnimationUtility.SetAnimationClips(coin.coinAnimation, new[] { spin, collect });
        coin.coinAnimation.clip = spin;
        var particlesObject = new GameObject("PickupSparkles", typeof(ParticleSystem));
        particlesObject.transform.SetParent(template.transform, false);
        coin.pickupParticles = particlesObject.GetComponent<ParticleSystem>();
        var main = coin.pickupParticles.main;
        main.playOnAwake = false; main.loop = false; main.duration = .45f;
        main.startLifetime = .4f; main.startSpeed = 1.1f; main.startSize = .055f;
        main.startColor = new Color(1f, .84f, .15f); main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = coin.pickupParticles.emission; emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0, 14) });
        var shape = coin.pickupParticles.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .28f;
        particlesObject.GetComponent<ParticleSystemRenderer>().sharedMaterial = sparkle;
        coin.pickupParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        SetLayer(template.transform, 2);
        AssetDatabase.SaveAssets();
        var prefab = PrefabUtility.SaveAsPrefabAsset(template, PrefabPath);
        UnityEngine.Object.DestroyImmediate(template);

        var reachable = ReachablePositions(session.explorer);
        // Six entry/corridor coins; the rest lead into all four rooms.
        var desired = new[] {
            new Vector2(5.5f,2.5f), new Vector2(5.5f,4.5f), new Vector2(4,6),
            new Vector2(5.5f,8.5f), new Vector2(5.5f,11), new Vector2(7.5f,12.5f),
            new Vector2(10,12), new Vector2(12,11), new Vector2(0,1),
            new Vector2(-4,1), new Vector2(-8,2), new Vector2(-11,4.5f),
            new Vector2(-11,7), new Vector2(-10.5f,9.5f), new Vector2(-11,11.5f),
            new Vector2(-10,-4), new Vector2(-9,-6.5f), new Vector2(-8.5f,-9),
            new Vector2(-11.5f,-8), new Vector2(-13,-6), new Vector2(8,-1),
            new Vector2(10,-4.5f), new Vector2(13.5f,-9), new Vector2(13.5f,-11.5f)
        };
        var root = new GameObject("CollectibleCoins");
        var placed = new List<Vector3>();
        var coins = new List<CollectibleCoin>();
        foreach (var target in desired)
        {
            var candidates = reachable.Where(p => Vector3.Distance(p, session.explorer.spawnPosition) > 1.2f && placed.All(other => Vector3.Distance(other, p) >= 1.55f));
            var sorted = candidates.OrderBy(p => (new Vector2(p.x, p.z) - target).sqrMagnitude).ToArray();
            if (sorted.Length == 0) throw new InvalidOperationException("No reachable coin position.");
            Vector3 position = sorted[0]; placed.Add(position);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            instance.name = "Coin_" + (coins.Count + 1).ToString("00");
            instance.transform.position = position + Vector3.up * .95f;
            var item = instance.GetComponent<CollectibleCoin>(); item.coinId = "house_coin_" + (coins.Count + 1).ToString("00");
            coins.Add(item);
        }
        session.coins = coins.ToArray();
        EditorUtility.SetDirty(session);
        EditorSceneManager.MarkSceneDirty(session.gameObject.scene);
    }

    private static void SetLayer(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        foreach (Transform child in root) SetLayer(child, layer);
    }

    private static void ConfigureAudio(string path, bool music)
    {
        var importer = AssetImporter.GetAtPath(path) as AudioImporter;
        if (importer == null) throw new InvalidOperationException("Expected AudioImporter: " + path);
        importer.forceToMono = !music;
        var settings = importer.defaultSampleSettings;
        settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = .8f; importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();
    }

    private static Material CreateMaterial(string name, Color color, float metallic, float smoothness)
    {
        string path = ArtPath + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color; material.SetFloat("_Metallic", metallic); material.SetFloat("_Smoothness", smoothness);
        material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * .18f);
        EditorUtility.SetDirty(material); return material;
    }

    private static Mesh CreateCoinMesh()
    {
        const int segments = 64;
        var profile = new[] {
            new Vector2(0,-.085f), new Vector2(.325f,-.085f), new Vector2(.35f,-.072f),
            new Vector2(.365f,-.118f), new Vector2(.448f,-.118f), new Vector2(.49f,-.082f),
            new Vector2(.5f,-.045f), new Vector2(.5f,.045f), new Vector2(.49f,.082f),
            new Vector2(.448f,.118f), new Vector2(.365f,.118f), new Vector2(.35f,.072f),
            new Vector2(.325f,.085f), new Vector2(0,.085f)
        };
        var vertices = new List<Vector3>(); var uv = new List<Vector2>();
        var triangles = new[] { new List<int>(), new List<int>() };
        for (int band = 0; band < profile.Length - 1; band++)
        {
            int start = vertices.Count;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                for (int j = 0; j < 2; j++)
                {
                    var p = profile[band + j];
                    vertices.Add(new Vector3(Mathf.Cos(angle) * p.x, Mathf.Sin(angle) * p.x, p.y));
                    uv.Add(new Vector2(Mathf.Cos(angle) * p.x + .5f, Mathf.Sin(angle) * p.x + .5f));
                }
            }
            int material = band == 1 || band == 2 || band == 10 || band == 11 ? 1 : 0;
            for (int i = 0; i < segments; i++)
            {
                int a = start + i * 2;
                triangles[material].AddRange(new[] { a, a + 3, a + 1, a, a + 2, a + 3 });
            }
        }
        var mesh = new Mesh { name = "BeveledGoldCoin_64Segments" };
        mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.subMeshCount = 2;
        mesh.SetTriangles(triangles[0], 0); mesh.SetTriangles(triangles[1], 1);
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }

    private static AnimationClip SaveClip(AnimationClip clip, string name)
    {
        string path = ArtPath + "/" + name + ".anim";
        var saved = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (saved == null) { AssetDatabase.CreateAsset(clip, path); return clip; }
        EditorUtility.CopySerialized(clip, saved); UnityEngine.Object.DestroyImmediate(clip); return saved;
    }

    private static AnimationClip CreateSpinClip()
    {
        var clip = new AnimationClip { name = "CoinSpin", legacy = true, wrapMode = WrapMode.Loop };
        clip.SetCurve("Visual", typeof(Transform), "localEulerAnglesRaw.y", AnimationCurve.Linear(0, 0, 2, 360));
        clip.SetCurve("Visual", typeof(Transform), "localPosition.y", new AnimationCurve(
            new Keyframe(0, 0, 0, 0), new Keyframe(.5f, .07f, 0, 0), new Keyframe(1, 0, 0, 0),
            new Keyframe(1.5f, -.07f, 0, 0), new Keyframe(2, 0, 0, 0)));
        return SaveClip(clip, "CoinSpin");
    }

    private static AnimationClip CreateCollectClip()
    {
        var clip = new AnimationClip { name = "CoinCollect", legacy = true, wrapMode = WrapMode.ClampForever };
        clip.SetCurve("Visual", typeof(Transform), "localPosition.y", AnimationCurve.EaseInOut(0, 0, .48f, .85f));
        clip.SetCurve("Visual", typeof(Transform), "localEulerAnglesRaw.y", AnimationCurve.Linear(0, 0, .48f, 540));
        foreach (string axis in new[] { "x", "y", "z" })
            clip.SetCurve("Visual", typeof(Transform), "localScale." + axis, new AnimationCurve(
                new Keyframe(0, 1), new Keyframe(.1f, 1.22f), new Keyframe(.48f, 0)));
        return SaveClip(clip, "CoinCollect");
    }

    /// <summary>Flood fill with the real controller radius, solid obstacles and standing clearance.</summary>
    public static List<Vector3> ReachablePositions(FirstPersonExplorer explorer)
    {
        const int size = 65; const float origin = -16; const float step = .5f;
        var controller = explorer.GetComponent<CharacterController>();
        bool enabled = controller.enabled; controller.enabled = false;
        var result = new List<Vector3>();
        try
        {
            Physics.SyncTransforms();
            var free = new bool[size, size]; var visited = new bool[size, size];
            for (int x = 0; x < size; x++) for (int z = 0; z < size; z++)
            {
                var p = new Vector3(origin + x * step, .1f, origin + z * step);
                free[x,z] = !Physics.CheckCapsule(p + Vector3.up * .34f, p + Vector3.up * 1.5f,
                    controller.radius + .04f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    && Physics.Raycast(p + Vector3.up * .15f, Vector3.down, .5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            }
            int sx = Mathf.RoundToInt((explorer.spawnPosition.x - origin) / step);
            int sz = Mathf.RoundToInt((explorer.spawnPosition.z - origin) / step);
            if (!free[sx,sz]) throw new InvalidOperationException("Player spawn is obstructed.");
            var queue = new Queue<Vector2Int>(); queue.Enqueue(new Vector2Int(sx,sz)); visited[sx,sz] = true;
            var offsets = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue(); result.Add(new Vector3(origin + cell.x * step, .1f, origin + cell.y * step));
                foreach (var offset in offsets)
                {
                    var next = cell + offset;
                    if (next.x < 0 || next.y < 0 || next.x >= size || next.y >= size || visited[next.x,next.y] || !free[next.x,next.y]) continue;
                    visited[next.x,next.y] = true; queue.Enqueue(next);
                }
            }
        }
        finally { controller.enabled = enabled; }
        return result;
    }
}
