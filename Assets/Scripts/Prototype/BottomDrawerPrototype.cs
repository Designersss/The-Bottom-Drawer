using System;
using System.Collections.Generic;
using System.IO;
using BottomDrawer.Core;
using UnityEngine;

// A self-contained, deliberately placeholder-art vertical slice.
// Opening any scene and pressing Play creates the playable setup.
public sealed class BottomDrawerPrototype : MonoBehaviour
{
    private const float MinX = -4.05f;
    private const float MaxX = 4.05f;
    private const float MinY = -3.38f;
    private const float MaxY = 2.82f;
    private const float ExtractionY = -2.75f;
    private const int MinimumObjects = 10;

    private readonly Dictionary<string, PrototypeItem> _world = new Dictionary<string, PrototypeItem>();
    private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
    private PlayerProgress _progress;
    private ExtractionSession _session;
    private PrototypeItem _dragged;
    private PrototypeItem _selected;
    private int _worldSeed;
    private int _spawnCursor;
    private int _chapter;
    private int _machineCharges;
    private bool _showUpgrades;
    private string _message = "Drag an item into the tray. Bank before stamina reaches zero.";
    private string SavePath => Path.Combine(Application.persistentDataPath, "prototype-save.json");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (FindObjectOfType<BottomDrawerPrototype>() == null)
        {
            var root = new GameObject("The Bottom Drawer - Prototype");
            root.AddComponent<BottomDrawerPrototype>();
        }
    }

    private void Awake()
    {
        ConfigureCamera();
        MakeDecor();
        Load();
        RefillDrawer();
        Save();
    }

    private void ConfigureCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.orthographicSize = 5.6f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.backgroundColor = new Color(.12f, .105f, .10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
    }

    private Sprite Square(Color color, bool rounded)
    {
        string key = ColorUtility.ToHtmlStringRGBA(color) + rounded;
        if (_sprites.TryGetValue(key, out var existing)) return existing;
        const int size = 48;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = (x + 0.5f - size / 2f) / (size / 2f);
            float dy = (y + 0.5f - size / 2f) / (size / 2f);
            bool visible = !rounded || dx * dx + dy * dy <= 0.92f;
            bool edge = rounded && dx * dx + dy * dy > 0.76f;
            pixels[y * size + x] = !visible ? Color.clear : edge ? color * .66f : color;
        }
        texture.SetPixels(pixels);
        texture.Apply();
        var sprite = Sprite.Create(texture, new Rect(0, 0, size, size),
            new Vector2(.5f, .5f), size);
        _sprites[key] = sprite;
        return sprite;
    }

    private void Decor(string name, Vector2 position, Vector2 size, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        go.transform.position = new Vector3(position.x, position.y, 0f);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = Square(color, false);
        renderer.sortingOrder = order;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    private void MakeDecor()
    {
        Decor("Warm desk", Vector2.zero, new Vector2(22f, 12f), new Color(.29f,.19f,.13f), -15);
        Decor("Drawer brass rim", new Vector2(0f,.02f), new Vector2(8.9f,6.28f),
            new Color(.67f,.46f,.26f), -12);
        Decor("Drawer wooden border", new Vector2(0f,.02f), new Vector2(8.7f,6.05f),
            new Color(.28f,.15f,.08f), -11);
        Decor("Drawer interior", new Vector2(0f,.30f), new Vector2(8.2f,5.30f),
            new Color(.40f,.28f,.17f), -10);
        Decor("Near edge", new Vector2(0f,-2.74f), new Vector2(8.8f,.36f),
            new Color(.54f,.32f,.17f), -7);
        Decor("Collection tray rim", new Vector2(0f,-3.60f), new Vector2(5.0f,.93f),
            new Color(.73f,.57f,.31f), -8);
        Decor("Collection tray", new Vector2(0f,-3.60f), new Vector2(4.72f,.72f),
            new Color(.18f,.30f,.24f), -6);
    }

    private void Load()
    {
        SaveData data = null;
        if (File.Exists(SavePath))
        {
            try { data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath)); }
            catch (Exception e) { Debug.LogWarning("Save cannot be read: " + e.Message); }
        }
        if (data == null)
        {
            _worldSeed = Environment.TickCount;
            _progress = new PlayerProgress();
            _session = new ExtractionSession(_progress);
            return;
        }

        _worldSeed = data.seed;
        _spawnCursor = Math.Max(0, data.cursor);
        _chapter = Mathf.Max(0, data.chapter);
        _progress = new PlayerProgress(Math.Max(0, data.coins));
        if (data.levels != null)
        for (int i = 0; i < Math.Min(5, data.levels.Length); i++)
            _progress.RestoreLevel((UpgradeType)i, Math.Max(0, Math.Min(5, data.levels[i])));
        _session = new ExtractionSession(_progress);
        var loot = new List<ExtractedItem>();
        if (data.banked != null)
        foreach (var item in data.banked)
        {
            try { loot.Add(new ExtractedItem(item.id, LootCatalog.Find(item.definition))); }
            catch (Exception e) { Debug.LogWarning("Skipped unknown banked item: " + e.Message); }
        }
        if (!Enum.TryParse(data.status, out SessionStatus status)) status = SessionStatus.Ready;
        _session.Restore(status, data.stamina, loot);

        if (data.items != null)
        foreach (var item in data.items)
        {
            try { Spawn(item.id, LootCatalog.Find(item.definition), new Vector2(item.x, item.y)); }
            catch (Exception e) { Debug.LogWarning("Skipped unknown world item: " + e.Message); }
        }
        _message = status == SessionStatus.Active
            ? "An interrupted extraction counts as a failed run. Existing drawer items remain."
            : "Progress restored.";
    }

    private void Save()
    {
        if (_session == null) return;
        var data = new SaveData
        {
            seed = _worldSeed,
            cursor = _spawnCursor,
            chapter = _chapter,
            coins = _progress.Coins,
            status = _session.Status.ToString(),
            stamina = _session.Stamina,
            levels = new int[5]
        };
        for (int i = 0; i < 5; i++) data.levels[i] = _progress.Level((UpgradeType)i);
        foreach (var pair in _world)
        {
            Vector2 p = pair.Value.transform.position;
            data.items.Add(new ItemData { id = pair.Key, definition = pair.Value.Definition.Id, x = p.x, y = p.y });
        }
        foreach (var item in _session.Loot)
            data.banked.Add(new ItemData { id = item.InstanceId, definition = item.Definition.Id });
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            string temporary = SavePath + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
            // Replace is atomic on supported platforms when the destination exists.
            if (File.Exists(SavePath)) File.Replace(temporary, SavePath, null);
            else File.Move(temporary, SavePath);
        }
        catch (Exception e) { Debug.LogError("Could not save: " + e.Message); }
    }

    private void OnApplicationQuit() => Save();
    private void OnApplicationPause(bool paused) { if (paused) Save(); }

    private void RefillDrawer()
    {
        while (_world.Count < MinimumObjects)
        {
            int index = _spawnCursor++;
            ItemDefinition item = LootPicker.Pick(_worldSeed, index, _progress.Level(UpgradeType.Luck),
                _chapter >= 3);
            int slot = _world.Count;
            float x = -3.35f + (slot % 5) * 1.68f;
            float y = 1.95f - ((slot / 5) % 2) * 2.15f;
            Spawn("object-" + index, item, new Vector2(x, y));
        }
    }

    private void Spawn(string id, ItemDefinition item, Vector2 point)
    {
        if (_world.ContainsKey(id)) return;
        var go = new GameObject(item.Name + " [" + id + "]");
        go.transform.SetParent(transform);
        go.transform.position = new Vector3(Mathf.Clamp(point.x, MinX, MaxX),
            Mathf.Clamp(point.y, -2.42f, MaxY), 0f);
        var renderer = go.AddComponent<SpriteRenderer>();
        Color color;
        switch (item.Rarity)
        {
            case Rarity.Uncommon: color = new Color(.59f, .72f, .59f); break;
            case Rarity.Rare: color = new Color(.95f, .74f, .35f); break;
            case Rarity.Anomalous: color = new Color(.67f, .53f, .88f); break;
            default: color = new Color(.80f, .69f, .55f); break;
        }
        bool round = item.IsMetal;
        renderer.sprite = Square(color, round);
        renderer.sortingOrder = 3;
        float size = Mathf.Clamp(.36f + Mathf.Sqrt(item.Weight) * .19f, .43f, 1.12f);
        go.transform.localScale = new Vector3(size, size, 1f);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.mass = item.Weight;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        if (round) go.AddComponent<CircleCollider2D>();
        else go.AddComponent<BoxCollider2D>();
        var actor = go.AddComponent<PrototypeItem>();
        actor.Id = id;
        actor.Definition = item;
        actor.Body = rb;
        _world.Add(id, actor);
    }

    private Vector2 MouseWorld()
    {
        Vector3 p = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return new Vector2(p.x, p.y);
    }

    private void Update()
    {
        if (_session.Status != SessionStatus.Active || _showUpgrades) return;
        if (Input.GetMouseButtonDown(0) && !OverUI())
        {
            var hit = Physics2D.OverlapPoint(MouseWorld());
            if (hit != null)
            {
                _dragged = hit.GetComponent<PrototypeItem>();
                if (_dragged != null) _selected = _dragged;
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            if (_dragged != null) Save();
            _dragged = null;
        }
        if (Input.GetKeyDown(KeyCode.Space)) ActivateAutoCollector();
    }

    private bool OverUI()
    {
        Vector2 m = Input.mousePosition;
        float top = Screen.height - m.y;
        return m.x < 285f || m.x > Screen.width - 285f || top < 60f || top > Screen.height - 108f;
    }

    private void FixedUpdate()
    {
        if (_dragged == null || _session.Status != SessionStatus.Active) return;
        if (!_session.DrainForDrag(_dragged.Definition.Weight, Time.fixedDeltaTime))
        {
            _dragged = null;
            _message = "Out of stamina: all unbanked items were lost.";
            Save();
            return;
        }
        Vector2 target = MouseWorld();
        target.x = Mathf.Clamp(target.x, MinX, MaxX);
        target.y = Mathf.Clamp(target.y, MinY, MaxY);
        Vector2 next = Vector2.MoveTowards(_dragged.Body.position, target,
            GameRules.DragSpeed(_dragged.Definition.Weight, _progress.Level(UpgradeType.Strength))
            * Time.fixedDeltaTime);
        _dragged.Body.MovePosition(next);
        if (next.y <= ExtractionY && Mathf.Abs(next.x) <= 2.45f)
        {
            ExtractActor(_dragged);
            _dragged = null;
        }
    }

    private void ExtractActor(PrototypeItem actor)
    {
        if (!_session.Extract(actor.Id, actor.Definition)) return;
        _world.Remove(actor.Id);
        if (_selected == actor) _selected = null;
        Destroy(actor.gameObject);
        _message = "Extracted: " + actor.Definition.Name;
        Save();
    }

    private void ActivateAutoCollector()
    {
        if (_machineCharges <= 0 || _session.Status != SessionStatus.Active) return;
        int level = _progress.Level(UpgradeType.Automation);
        float capacity = 1f + level * 1.2f;
        PrototypeItem candidate = null;
        foreach (var actor in _world.Values)
            if (actor.Definition.Weight <= capacity &&
                (candidate == null || actor.Definition.Weight < candidate.Definition.Weight))
                candidate = actor;
        if (candidate == null)
        {
            _message = "No item within the machine's weight capacity.";
            return;
        }
        _machineCharges--;
        ExtractActor(candidate);
        _message = "Auto collector extracted " + candidate.Definition.Name + ".";
    }

    private void StartRun()
    {
        if (_session.Loot.Count != 0) { _message = "Sell banked items first."; return; }
        RefillDrawer();
        _session.Begin();
        _machineCharges = _progress.Level(UpgradeType.Automation);
        _message = "Drag objects into the green tray. Stamina drains while dragging.";
        _showUpgrades = false;
        Save();
    }

    private void BankRun()
    {
        if (_session.Bank())
        {
            _dragged = null;
            _message = "Run banked. Sell your loot, then start again.";
            Save();
        }
    }

    private void Purchase(UpgradeType type)
    {
        if (_session.Buy(type))
        {
            _message = "Purchased " + type + " level " + _progress.Level(type);
            Save();
        }
        else _message = "Cannot buy: not enough coins, maximum level, or active session.";
    }

    private void OnGUI()
    {
        GUI.skin.label.fontSize = 14;
        GUI.skin.button.fontSize = 13;
        int w = Screen.width;
        int h = Screen.height;
        GUI.Box(new Rect(12, 12, 280, 48), "THE BOTTOM DRAWER — 2D PROTOTYPE");
        GUI.Box(new Rect(w - 265, 12, 250, 48), "Coins: " + _progress.Coins);
        GUILayout.BeginArea(new Rect(12, 70, 272, h - 180), GUI.skin.box);
        GUILayout.Label("SESSION · " + _session.Status);
        GUILayout.Space(8);
        GUILayout.Label("STAMINA: " + Mathf.CeilToInt(_session.Stamina) + " / "
            + GameRules.MaxStamina(_progress.Level(UpgradeType.Stamina)));
        Rect bar = GUILayoutUtility.GetRect(240, 16);
        GUI.Box(bar, "");
        float ratio = Mathf.Clamp01(_session.Stamina /
            GameRules.MaxStamina(_progress.Level(UpgradeType.Stamina)));
        var fill = new Rect(bar.x + 2, bar.y + 2, (bar.width - 4) * ratio, bar.height - 4);
        Color before = GUI.color;
        GUI.color = ratio > .2f ? new Color(.50f,.80f,.58f) : new Color(.93f,.45f,.35f);
        GUI.DrawTexture(fill, Texture2D.whiteTexture);
        GUI.color = before;
        GUILayout.Space(10);
        GUILayout.Label("Banked this run: " + _session.Loot.Count + " items");
        int total = 0;
        foreach (var item in _session.Loot)
            total += GameRules.SalePrice(item.Definition, _progress.Level(UpgradeType.Profit));
        GUILayout.Label("Potential sale: " + total + " coins");
        GUILayout.Space(12);
        if (_session.Status == SessionStatus.Active)
        {
            if (GUILayout.Button("BANK & END RUN", GUILayout.Height(38))) BankRun();
        }
        else if (_session.Loot.Count == 0)
        {
            if (GUILayout.Button("START SESSION", GUILayout.Height(38))) StartRun();
        }
        if (_session.Status == SessionStatus.Banked && _session.Loot.Count > 0)
        {
            if (GUILayout.Button("SELL ALL: " + total, GUILayout.Height(32)))
            {
                _session.SellAll(); Save();
            }
            // Copy to allow mutation by Sell() during GUI iteration.
            var pending = new List<ExtractedItem>(_session.Loot);
            foreach (var item in pending)
                if (GUILayout.Button("Sell " + item.Definition.Name + " (" +
                    GameRules.SalePrice(item.Definition, _progress.Level(UpgradeType.Profit)) + ")"))
                {
                    _session.Sell(item.InstanceId); Save();
                }
        }
        GUILayout.Space(14);
        if (_session.Status != SessionStatus.Active &&
            GUILayout.Button(_showUpgrades ? "CLOSE UPGRADES" : "UPGRADES", GUILayout.Height(32)))
            _showUpgrades = !_showUpgrades;
        GUILayout.Space(12);
        GUILayout.Label("OBJECTS REMAINING: " + _world.Count);
        GUILayout.Label("DEPTH: " + (_chapter + 1));
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(w - 275, 70, 263, h - 180), GUI.skin.box);
        GUILayout.Label("SELECTED OBJECT");
        if (_selected != null && _world.ContainsKey(_selected.Id))
        {
            var item = _selected.Definition;
            GUILayout.Space(10);
            GUILayout.Label(item.Name);
            GUILayout.Label("Rarity: " + item.Rarity);
            GUILayout.Label("Base value: " + item.BasePrice);
            GUILayout.Label("Weight: " + item.Weight.ToString("0.00") + " kg");
            GUILayout.Label("Stamina/s: " + GameRules.DrainPerSecond(item.Weight).ToString("0.0"));
            GUILayout.Label("Drag speed: " + GameRules.DragSpeed(item.Weight,
                _progress.Level(UpgradeType.Strength)).ToString("0.0"));
        }
        else GUILayout.Label("Click an item to inspect.");
        GUILayout.Space(18);
        GUILayout.Label("HAND — hold left mouse");
        GUILayout.Label("AUTO — press Space");
        GUILayout.Label("Machine charges: " + _machineCharges);
        GUILayout.Space(15);
        GUILayout.Label("No instant stamina refill within a run.");
        GUILayout.Label("Fail at zero stamina: lose current loot.");
        GUILayout.EndArea();

        if (_showUpgrades)
        {
            Rect center = new Rect(w * .5f - 230, h * .5f - 210, 460, 420);
            GUILayout.BeginArea(center, GUI.skin.box);
            GUILayout.Label("UPGRADE TREE · WORKING MECHANICS");
            GUILayout.Space(10);
            foreach (UpgradeType type in Enum.GetValues(typeof(UpgradeType)))
            {
                int level = _progress.Level(type);
                string name = type + "   Level " + level + "/" + GameRules.MaxUpgradeLevel;
                if (level < GameRules.MaxUpgradeLevel) name += "  ·  " + GameRules.UpgradePrice(type, level) + " coins";
                if (GUILayout.Button(name, GUILayout.Height(44))) Purchase(type);
            }
            GUILayout.Label("Strength: +18% drag speed / level");
            GUILayout.Label("Stamina: +20 max / level · Luck: +2pp Rare");
            GUILayout.Label("Profit: +5% sale / level · Auto: +1 charge / level");
            GUILayout.EndArea();
        }

        GUI.Box(new Rect(w * .5f - 330, h - 95, 660, 73),
            "DRAG TOWARD GREEN TRAY  •  " + _message);
    }

    [Serializable] private sealed class ItemData
    {
        public string id;
        public string definition;
        public float x;
        public float y;
    }

    [Serializable] private sealed class SaveData
    {
        public int seed;
        public int cursor;
        public int chapter;
        public int coins;
        public string status = "Ready";
        public float stamina;
        public int[] levels = new int[5];
        public List<ItemData> items = new List<ItemData>();
        public List<ItemData> banked = new List<ItemData>();
    }
}

public sealed class PrototypeItem : MonoBehaviour
{
    public string Id { get; set; }
    public ItemDefinition Definition { get; set; }
    public Rigidbody2D Body { get; set; }
}
