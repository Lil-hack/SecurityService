using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class KitchenRoomBuilder
{
    const string Mesh = "Assets/models/meshes/data/SM_Prop/";
    const string Wood = "Assets/models/RaccoonGame/Content/Airship/Textures/T_WoodFloor_A.png";

    [MenuItem("Tools/Office/Build Kitchen Room")]
    public static void Build()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Stop Play mode first.");
        var old = GameObject.Find("Kitchen room");
        if (old != null) Object.DestroyImmediate(old);
        var root = new GameObject("Kitchen room").transform;
        var oak = Load("Kitchen honey oak", new Color(.66f, .43f, .21f), .35f);
        var wood = Load("Kitchen oak floor", new Color(.86f, .72f, .52f), .32f);
        wood.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Wood);
        wood.mainTextureScale = new Vector2(7f, 6f);
        var plaster = Load("Kitchen plaster", new Color(.84f, .83f, .73f), .18f);
        var white = Load("Kitchen porcelain", new Color(.93f, .94f, .92f), .4f);
        var ink = Load("Kitchen graphite", new Color(.07f, .08f, .09f), .4f);
        var teal = Load("Kitchen teal", new Color(.055f, .38f, .40f), .25f);
        var blue = Load("Kitchen ocean", new Color(.08f, .32f, .55f), .3f);
        var yellow = Load("Kitchen sunflower", new Color(.96f, .62f, .08f), .28f);
        var orange = Load("Kitchen tangerine", new Color(.9f, .28f, .05f), .28f);
        var green = Load("Kitchen leaf", new Color(.18f, .55f, .28f), .28f);
        var charcoal = Load("Kitchen charcoal wall", new Color(.11f, .12f, .13f), .2f);
        var glass = AssetDatabase.LoadAssetAtPath<Material>("Assets/ExecutiveOffice/Materials/Window clear blue glass.mat");
        var glow = Load("Kitchen pendant glow", new Color(1f, .86f, .55f), .1f, 2.2f);
        var city = AssetDatabase.LoadAssetAtPath<Material>("Assets/OfficeExtension/Materials/City view.mat");

        const float x0 = 20.45f, x1 = 28.32f, z0 = 7.72f, z1 = 15.42f, top = 4.2f;
        Box(root, "Kitchen floor", new Vector3((x0 + x1) * .5f, .01f, (z0 + 15.55f) * .5f), new Vector3(x1 - x0 + .2f, .02f, 15.55f - z0), wood);
        Box(root, "Kitchen threshold", new Vector3(26.4f, .01f, 7.62f), new Vector3(1.7f, .02f, .7f), wood);
        Box(root, "Kitchen ceiling", new Vector3((x0 + x1) * .5f, 4.16f, (z0 + z1) * .5f), new Vector3(x1 - x0, .16f, z1 - z0 + .2f), plaster);
        Box(root, "Kitchen west wall", new Vector3(x0, top * .5f, (7.55f + z1) * .5f), new Vector3(.2f, top, z1 - 7.55f), plaster);
        Box(root, "Kitchen north wall", new Vector3((x0 + x1) * .5f, top * .5f, z1), new Vector3(x1 - x0, top, .2f), plaster);
        Box(root, "Kitchen charcoal backsplash", new Vector3(23.55f, 2.35f, z1 - .12f), new Vector3(5.5f, 2.7f, .06f), charcoal);
        Box(root, "Kitchen east lower wall", new Vector3(x1, .48f, 11.53f), new Vector3(.2f, .96f, 7.94f), plaster);
        Box(root, "Kitchen east upper wall", new Vector3(x1, 3.82f, 11.53f), new Vector3(.2f, .76f, 7.94f), plaster);
        if (glass != null) Box(root, "Kitchen window glass", new Vector3(x1 - .02f, 2.25f, 11.35f), new Vector3(.04f, 2.35f, 7.2f), glass);
        if (city != null) Box(root, "Kitchen city view", new Vector3(x1 + .18f, 2.25f, 11.35f), new Vector3(.04f, 2.5f, 7.3f), city, false);
        for (float z = 8.4f; z <= 14.6f; z += 2.05f)
            Box(root, "Kitchen window mullion", new Vector3(x1 - .02f, 2.25f, z), new Vector3(.08f, 2.4f, .08f), oak);
        Box(root, "Kitchen window sill", new Vector3(x1 - .28f, .98f, 11.35f), new Vector3(.36f, .08f, 7.3f), oak);
        Box(root, "Kitchen skirting west", new Vector3(x0 + .08f, .08f, 11.5f), new Vector3(.06f, .16f, 7.4f), oak);
        Box(root, "Kitchen skirting north", new Vector3(24.3f, .08f, z1 - .08f), new Vector3(7.5f, .16f, .06f), oak);

        Counter(root, oak, white, blue, charcoal);
        Island(root, oak, white);
        Stools(root, ink, yellow, orange, blue, green);
        Dining(root, yellow, orange, blue, green, white);
        Bench(root, oak, teal);
        Props(root, white, yellow, orange, blue, green);
        Signs(root, yellow, white);
        Lights(root, ink, glow);

        var scene = root.gameObject.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void Counter(Transform root, Material oak, Material white, Material blue, Material charcoal)
    {
        Box(root, "Kitchen counter base", new Vector3(23.55f, .46f, 14.82f), new Vector3(5.2f, .92f, .68f), oak);
        Box(root, "Kitchen counter doors", new Vector3(23.55f, .48f, 14.46f), new Vector3(5.05f, .78f, .04f), blue);
        Box(root, "Kitchen counter top", new Vector3(23.55f, .96f, 14.78f), new Vector3(5.4f, .08f, .78f), white);
        for (int i = 0; i < 3; i++)
            Box(root, "Kitchen open shelf", new Vector3(23.5f, 1.72f + i * .48f, 15.22f), new Vector3(4.8f, .05f, .28f), oak);
        Box(root, "Kitchen shelf light", new Vector3(23.5f, 1.58f, 15.08f), new Vector3(4.6f, .02f, .04f), charcoal, false);
    }

    static void Island(Transform root, Material oak, Material white)
    {
        Box(root, "Kitchen island base", new Vector3(23.55f, .46f, 11.15f), new Vector3(3.3f, .9f, 1.05f), oak);
        Box(root, "Kitchen island top", new Vector3(23.55f, .96f, 11.15f), new Vector3(3.55f, .08f, 1.22f), white);
        Box(root, "Kitchen island shelf", new Vector3(23.55f, .28f, 10.72f), new Vector3(2.4f, .04f, .42f), oak);
        Box(root, "Kitchen island kick", new Vector3(23.55f, .06f, 11.15f), new Vector3(3.1f, .1f, .9f), white);
    }

    static void Stools(Transform root, Material ink, Material yellow, Material orange, Material blue, Material green)
    {
        var colors = new[] { yellow, orange, blue, green };
        for (int i = 0; i < 4; i++)
            Stool(root, new Vector3(22.35f + i * .78f, 0f, 10.15f), colors[i], ink);
    }

    static void Stool(Transform root, Vector3 floor, Material seat, Material ink)
    {
        var group = new GameObject("Kitchen bar stool").transform;
        group.SetParent(root, false);
        group.position = floor;
        Cylinder(group, "Seat", floor + new Vector3(0f, .68f, 0f), new Vector3(.42f, .035f, .42f), seat);
        Cylinder(group, "Footrest", floor + new Vector3(0f, .28f, 0f), new Vector3(.34f, .015f, .34f), ink);
        Cylinder(group, "Post", floor + new Vector3(0f, .34f, 0f), new Vector3(.07f, .34f, .07f), ink);
        Box(group, "Back", floor + new Vector3(0f, .86f, -.16f), new Vector3(.36f, .28f, .04f), seat);
    }

    static void Dining(Transform root, Material yellow, Material orange, Material blue, Material green, Material white)
    {
        Cylinder(root, "Kitchen dining top", new Vector3(26.15f, .74f, 10.55f), new Vector3(1.15f, .04f, 1.15f), white);
        Cylinder(root, "Kitchen dining leg", new Vector3(26.15f, .36f, 10.55f), new Vector3(.12f, .36f, .12f), white);
        var colors = new[] { yellow, orange, blue, green };
        var seats = new[] { new Vector3(26.15f, 0f, 9.82f), new Vector3(26.85f, 0f, 10.55f), new Vector3(26.15f, 0f, 11.28f), new Vector3(25.45f, 0f, 10.55f) };
        var yaws = new[] { 0f, 90f, 180f, 270f };
        for (int i = 0; i < 4; i++)
            Prop(Mesh + "SM_Prop_Chair_Plastic_01.obj", "Kitchen dining chair", seats[i], yaws[i], .82f, root, colors[i]);
    }

    static void Bench(Transform root, Material oak, Material teal)
    {
        Box(root, "Kitchen bench base", new Vector3(27.55f, .22f, 11.35f), new Vector3(.48f, .44f, 1.9f), oak);
        Box(root, "Kitchen bench cushion", new Vector3(27.55f, .5f, 11.35f), new Vector3(.54f, .12f, 1.95f), teal);
        Box(root, "Kitchen bench back", new Vector3(27.78f, .78f, 11.35f), new Vector3(.1f, .48f, 1.95f), teal);
    }

    static void Props(Transform root, Material white, Material yellow, Material orange, Material blue, Material green)
    {
        Prop(Mesh + "SM_Prop_CoffeeMachine_01.obj", "Kitchen coffee machine", new Vector3(21.35f, 1.01f, 14.78f), 180f, .62f, root);
        Prop(Mesh + "SM_Prop_Coffee_Pot_01.obj", "Kitchen coffee pot", new Vector3(22.05f, 1.01f, 14.72f), 40f, .28f, root);
        Prop(Mesh + "SM_Prop_Cafe_Milk_Jug_01.obj", "Kitchen milk jug", new Vector3(22.45f, 1.01f, 14.78f), 20f, .2f, root);
        Prop(Mesh + "SM_Prop_Microwave_01.obj", "Kitchen microwave", new Vector3(25.35f, 1.01f, 14.82f), 180f, .32f, root);
        var steel = AssetDatabase.LoadAssetAtPath<Material>("Assets/ExecutiveOffice/Materials/Atlas_T_PolygonOffice_01_A.mat");
        Prop("Assets/models/RaccoonGame/Content/AssetPacks/PolygonOffice/Meshes/Props/Kitchen/SM_Prop_Fridge_01_Combined.obj", "Kitchen fridge", new Vector3(27.05f, 0f, 14.55f), 180f, 1.9f, root, steel);
        FruitBowl(root, white, yellow, orange);
        Prop(Mesh + "SM_Prop_PotPlant_01.obj", "Kitchen floor plant", new Vector3(20.95f, 0f, 8.35f), 0f, 1.45f, root);
        Prop(Mesh + "SM_Prop_PotPlant_01.obj", "Kitchen window plant", new Vector3(27.35f, 0f, 8.55f), 30f, 1.25f, root);
        Prop(Mesh + "SM_Prop_PotPlant_01.obj", "Kitchen corner plant", new Vector3(20.95f, 0f, 14.15f), 10f, 1.15f, root);
        for (int i = 0; i < 3; i++)
        {
            Prop(Mesh + "SM_Prop_PotPlant_01.obj", "Kitchen shelf plant", new Vector3(21.7f + i * 1.5f, 1.78f + (i % 2) * .48f, 15.18f), i * 25f, .32f, root);
            Prop(Mesh + "SM_Prop_Book_Group_02.obj", "Kitchen island books", new Vector3(22.7f + i * .55f, .32f, 10.72f), 90f, .16f, root);
        }
        Prop(Mesh + "SM_Prop_Bar_Bottle_01.obj", "Kitchen bottle", new Vector3(24.55f, 1.01f, 14.7f), 0f, .32f, root);
        Prop(Mesh + "SM_Prop_Bar_Bottle_02.obj", "Kitchen bottle", new Vector3(24.85f, 1.01f, 14.78f), 15f, .36f, root);
        Prop(Mesh + "SM_Prop_Cafe_Sugar_Box_01.obj", "Kitchen sugar", new Vector3(24.15f, 1.01f, 14.75f), 10f, .16f, root);
        Prop(Mesh + "SM_Prop_Book_Group_02.obj", "Kitchen shelf books", new Vector3(24.9f, 2.72f, 15.16f), 8f, .16f, root);
        var mugs = new[] { white, yellow, orange, blue, green, white };
        for (int i = 0; i < mugs.Length; i++)
        {
            Mug(root, new Vector3(21.9f + i * .46f, 2.225f, 15.16f), mugs[i]);
            Mug(root, new Vector3(22.15f + i * .46f, 1.745f, 15.16f), mugs[(i + 3) % mugs.Length]);
        }
    }

    static void Mug(Transform root, Vector3 bottom, Material body)
    {
        Cylinder(root, "Kitchen mug", bottom + new Vector3(0f, .055f, 0f), new Vector3(.1f, .05f, .1f), body, false);
    }

    static void FruitBowl(Transform root, Material white, Material yellow, Material orange)
    {
        Cylinder(root, "Kitchen fruit bowl", new Vector3(23.55f, 1.06f, 11.15f), new Vector3(.42f, .035f, .42f), white, false);
        var spots = new[] { new Vector3(-.08f, .08f, -.02f), new Vector3(.07f, .08f, .04f), new Vector3(.01f, .08f, .09f), new Vector3(-.02f, .08f, -.09f), new Vector3(.1f, .14f, -.04f), new Vector3(-.06f, .14f, .05f) };
        var colors = new[] { orange, yellow, orange, yellow, orange, yellow };
        for (int i = 0; i < spots.Length; i++)
        {
            var fruit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fruit.name = "Kitchen orange";
            fruit.transform.SetParent(root, false);
            fruit.transform.position = new Vector3(23.55f, 1.08f, 11.15f) + spots[i];
            fruit.transform.localScale = Vector3.one * .11f;
            fruit.GetComponent<Renderer>().sharedMaterial = colors[i];
            Object.DestroyImmediate(fruit.GetComponent<Collider>());
        }
    }

    static void Signs(Transform root, Material yellow, Material white)
    {
        Box(root, "Kitchen motto board", new Vector3(21.7f, 3.42f, 15.28f), new Vector3(1.85f, .78f, .03f), yellow, false);
        Words(root, "КОФЕ — НАШ ТИМЛИД\nБЕЗ НЕГО\nНИКТО НЕ РАБОТАЕТ", new Vector3(21.7f, 3.42f, 15.24f), 0f, .018f, new Color(.16f, .1f, .05f));
        Box(root, "Kitchen fridge note", new Vector3(27.05f, 1.15f, 14.16f), new Vector3(.42f, .5f, .02f), white, false);
        Words(root, "ЧУЖОЙ ОБЕД\nНЕ БАГ.\nНЕ ТРОГАЙ.", new Vector3(27.05f, 1.15f, 14.14f), 0f, .009f, new Color(.12f, .16f, .2f));
        Box(root, "Kitchen break poster", new Vector3(20.58f, 2.55f, 10.3f), new Vector3(.03f, 1.05f, .82f), white, false);
        Words(root, "НЕ МЕШАЙ!\nЯ НЕ ЕМ,\nЯ ЗАГРУЖАЮ\nОБНОВЛЕНИЯ.", new Vector3(20.62f, 2.55f, 10.3f), 270f, .016f, new Color(.05f, .35f, .32f));
    }

    static void Lights(Transform root, Material ink, Material glow)
    {
        Pendant(root, new Vector3(22.9f, 2.55f, 11.15f), ink, glow);
        Pendant(root, new Vector3(24.25f, 2.55f, 11.15f), ink, glow);
        var day = new GameObject("Kitchen window light").AddComponent<Light>();
        day.transform.SetParent(root, false);
        day.transform.position = new Vector3(27.2f, 2.6f, 11.2f);
        day.type = LightType.Point;
        day.color = new Color(.75f, .86f, 1f);
        day.intensity = 2.1f;
        day.range = 9f;
        var warm = new GameObject("Kitchen ceiling light").AddComponent<Light>();
        warm.transform.SetParent(root, false);
        warm.transform.position = new Vector3(23.6f, 3.7f, 12.6f);
        warm.type = LightType.Point;
        warm.color = new Color(1f, .9f, .75f);
        warm.intensity = 1.7f;
        warm.range = 8f;
    }

    static void Pendant(Transform root, Vector3 pos, Material ink, Material glow)
    {
        const float ceiling = 4.05f;
        Cylinder(root, "Kitchen pendant cord", new Vector3(pos.x, (pos.y + ceiling) * .5f, pos.z), new Vector3(.02f, (ceiling - pos.y) * .5f, .02f), ink, false);
        var shade = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shade.name = "Kitchen pendant shade";
        shade.transform.SetParent(root, false);
        shade.transform.position = pos;
        shade.transform.localScale = new Vector3(.55f, .28f, .55f);
        shade.GetComponent<Renderer>().sharedMaterial = ink;
        Object.DestroyImmediate(shade.GetComponent<Collider>());
        var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bulb.name = "Kitchen pendant bulb";
        bulb.transform.SetParent(root, false);
        bulb.transform.position = pos + Vector3.down * .08f;
        bulb.transform.localScale = Vector3.one * .12f;
        bulb.GetComponent<Renderer>().sharedMaterial = glow;
        Object.DestroyImmediate(bulb.GetComponent<Collider>());
        var light = new GameObject("Kitchen pendant light").AddComponent<Light>();
        light.transform.SetParent(root, false);
        light.transform.position = pos + Vector3.down * .15f;
        light.type = LightType.Point;
        light.color = new Color(1f, .84f, .6f);
        light.intensity = 1.5f;
        light.range = 4.5f;
    }

    static void Words(Transform root, string text, Vector3 world, float yaw, float size, Color color)
    {
        var go = new GameObject("Words");
        go.transform.SetParent(root, false);
        go.transform.position = world;
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        var mesh = go.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.fontSize = 64;
        mesh.characterSize = size;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = color;
        mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var renderer = mesh.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = mesh.font.material;
        go.AddComponent<KitchenWorldText>();
    }

    static GameObject Prop(string path, string name, Vector3 bottom, float yaw, float height, Transform root, Material replacement = null)
    {
        if (!File.Exists(path)) return null;
        var g = ExecutiveOfficeBuilder.PlaceDecoration(path, root, bottom, yaw);
        g.name = name;
        var bounds = BoundsOf(g);
        if (bounds.size.y > .01f) g.transform.localScale *= height / bounds.size.y;
        if (height >= .5f) FitCollider(g);
        if (replacement != null)
            foreach (var renderer in g.GetComponentsInChildren<Renderer>())
            {
                var slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++) slots[i] = replacement;
                renderer.sharedMaterials = slots;
            }
        return g;
    }

    static Material Load(string name, Color color, float smooth, float emission = 0f)
    {
        var shader = Shader.Find(GraphicsSettings.currentRenderPipeline == null ? "Standard" : "Universal Render Pipeline/Lit");
        var created = new Material(shader) { name = name, color = color };
        if (created.HasProperty("_Smoothness")) created.SetFloat("_Smoothness", smooth);
        if (created.HasProperty("_BaseColor")) created.SetColor("_BaseColor", color);
        if (emission > 0f)
        {
            created.EnableKeyword("_EMISSION");
            created.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            if (created.HasProperty("_EmissionColor")) created.SetColor("_EmissionColor", color * emission);
        }
        if (created.HasProperty("_Surface")) created.SetFloat("_Surface", color.a < .99f ? 1f : 0f);
        string path = "Assets/ExecutiveOffice/Materials/" + name + ".mat";
        var old = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (old != null) { EditorUtility.CopySerialized(created, old); Object.DestroyImmediate(created); return old; }
        AssetDatabase.CreateAsset(created, path);
        return created;
    }

    static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool collide = true)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.position = position;
        box.transform.localScale = size;
        box.GetComponent<Renderer>().sharedMaterial = material;
        if (!collide) Object.DestroyImmediate(box.GetComponent<Collider>());
        return box;
    }

    static GameObject Cylinder(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool collide = true)
    {
        var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        cylinder.transform.SetParent(parent, false);
        cylinder.transform.position = position;
        cylinder.transform.localScale = size;
        cylinder.GetComponent<Renderer>().sharedMaterial = material;
        if (!collide) Object.DestroyImmediate(cylinder.GetComponent<Collider>());
        return cylinder;
    }

    static void FitCollider(GameObject go)
    {
        var box = go.AddComponent<BoxCollider>();
        var local = new Bounds(go.transform.InverseTransformPoint(BoundsOf(go).center), Vector3.zero);
        foreach (var renderer in go.GetComponentsInChildren<Renderer>())
        {
            var world = renderer.bounds;
            var ext = world.extents;
            var centre = world.center;
            local.Encapsulate(go.transform.InverseTransformPoint(centre + new Vector3(ext.x, ext.y, ext.z)));
            local.Encapsulate(go.transform.InverseTransformPoint(centre + new Vector3(ext.x, ext.y, -ext.z)));
            local.Encapsulate(go.transform.InverseTransformPoint(centre + new Vector3(ext.x, -ext.y, ext.z)));
            local.Encapsulate(go.transform.InverseTransformPoint(centre + new Vector3(ext.x, -ext.y, -ext.z)));
            local.Encapsulate(go.transform.InverseTransformPoint(centre + new Vector3(-ext.x, ext.y, ext.z)));
            local.Encapsulate(go.transform.InverseTransformPoint(centre + new Vector3(-ext.x, ext.y, -ext.z)));
            local.Encapsulate(go.transform.InverseTransformPoint(centre + new Vector3(-ext.x, -ext.y, ext.z)));
            local.Encapsulate(go.transform.InverseTransformPoint(centre + new Vector3(-ext.x, -ext.y, -ext.z)));
        }
        box.center = local.center;
        box.size = local.size;
    }

    static Bounds BoundsOf(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }
}
