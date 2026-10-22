using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace GamePJ.Asymmetric
{
    // GDD §1.2 / ASYMMETRIC_DESIGN §§1–7: isolated, local rules-verification scene.
    public sealed class AsymmetricDemo : MonoBehaviour
    {
        AsymmetricMatch match;
        readonly List<Transform> pawns = new List<Transform>();
        readonly List<List<Transform>> followers = new List<List<Transform>>();
        readonly List<Vector3> objectives = new List<Vector3>();
        readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        readonly Vector3[] farms = { new Vector3(9, 1, 16), new Vector3(17, 1, 8), new Vector3(10, 1, 35), new Vector3(45, 1, 10) };
        readonly Color[] colors = { new Color(.3f,.9f,1), new Color(1,.7f,.2f), new Color(.8f,.4f,1), new Color(.4f,1,.5f) };
        readonly Vector3 bossPosition = new Vector3(49, 1, 43);
        readonly Vector3 crystalPosition = new Vector3(6, 1, 5);
        Transform boss, crystal, army, relic;
        Camera view;
        float[] attackCooldown, questProgress;
        int[] questIndex, completed;
        bool[] accepted;
        float bossCooldown, choiceDelay, armyProgress;
        bool started, showDebug;
        float speed = 1;
        Vector3 relicDrop;
        string notice = "選擇路線後開始；任務點按 F 互動，滑鼠左右鍵戰鬥。";
        GUIStyle label, title;
        Font font;

        void Awake()
        {
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft JhengHei", "Arial" }, 16);
            BuildMap();
            match = new AsymmetricMatch(4);
            attackCooldown = new float[4]; questProgress = new float[4];
            questIndex = new int[4]; completed = new int[4]; accepted = new bool[4];
            int offset = UnityEngine.Random.Range(0, farms.Length);
            for (int i = 0; i < 4; i++)
            {
                pawns.Add(Shape("Player " + (i + 1), PrimitiveType.Capsule, farms[(i + offset) % farms.Length], new Vector3(.7f, 1, .7f), colors[i]));
                followers.Add(new List<Transform>());
                questIndex[i] = i % objectives.Count;
            }
            match.Respawned += id => { pawns[id].position = match.Phase == MatchPhase.Hunt ? farms[id] : new Vector3(36 + id * 3, 1, 29); };
            match.PlayerDied += id => { relicDrop = pawns[id].position; };
            match.PhaseChanged += phase =>
            {
                choiceDelay = 0;
                if (phase == MatchPhase.Choice) notice = "討伐完成，所有模擬暫停。P" + (match.Chooser + 1) + " 擁有終局選擇權。";
                if (phase == MatchPhase.Demon)
                {
                    pawns[match.Demon].position = bossPosition;
                    foreach (var f in followers[match.Demon]) Destroy(f.gameObject);
                    followers[match.Demon].Clear();
                    notice = "魔化：獻祭完成，其他玩家城外重生；任務復活資格保留。";
                }
                if (phase == MatchPhase.Escort) notice = "護送：王國軍已出發；擊殺持有者並拾取聖器可接手。";
            };
            boss = Shape("Initial Demon", PrimitiveType.Capsule, bossPosition, new Vector3(2, 2.5f, 2), new Color(.8f,.15f,.25f));
            crystal = Shape("Kingdom Crystal", PrimitiveType.Cube, crystalPosition, new Vector3(1.7f, 3, 1.7f), Color.cyan);
            army = Shape("Unstoppable Royal Army", PrimitiveType.Cube, crystalPosition, new Vector3(2, 1, 2), Color.yellow);
            relic = Shape("Relic", PrimitiveType.Sphere, bossPosition, Vector3.one * .7f, Color.yellow);
            army.gameObject.SetActive(false); relic.gameObject.SetActive(false);
            var cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera";
            view = cameraObject.AddComponent<Camera>(); cameraObject.AddComponent<AudioListener>();
            view.fieldOfView = 55; view.farClipPlane = 200; view.backgroundColor = new Color(.045f,.065f,.1f);
            var light = new GameObject("Sun").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.3f;
            light.transform.rotation = Quaternion.Euler(55, -35, 0);
            RenderSettings.ambientLight = new Color(.55f,.6f,.7f);
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame) showDebug = !showDebug;
            if (!started || match.Phase == MatchPhase.Finished) return;
            if (match.Phase == MatchPhase.Choice)
            {
                // UI and bot decision use unscaled time; the entire simulation is frozen.
                choiceDelay += Time.unscaledDeltaTime;
                if (match.Chooser != 0 && choiceDelay > 3) match.ChooseRoute(match.Chooser, match.Chooser % 2 == 1);
                return;
            }
            float dt = Mathf.Min(Time.deltaTime, .1f) * speed;
            match.Tick(dt);
            if (!match.Running) return;
            for (int i = 0; i < pawns.Count; i++)
            {
                var p = match.Players[i];
                pawns[i].gameObject.SetActive(p.Alive);
                attackCooldown[i] -= dt;
                if (!p.Alive) { foreach (var f in followers[i]) f.gameObject.SetActive(false); continue; }
                SyncFollowers(i);
                if (i == 0) LocalInput(dt); else Bot(i, dt);
                if (!match.Running) break;
                UpdateQuest(i, dt);
                if (match.Phase == MatchPhase.Escort && match.Carrier < 0 && Vector3.Distance(pawns[i].position, relicDrop) < 2)
                    match.PickRelic(i);
            }
            if (!match.Running) return;
            boss.gameObject.SetActive(match.Phase == MatchPhase.Hunt);
            if (match.Phase == MatchPhase.Hunt)
            {
                bossCooldown -= dt;
                if (bossCooldown <= 0)
                {
                    bossCooldown = 1.5f;
                    foreach (var p in match.Players.Where(p => p.Alive))
                        if (Vector3.Distance(pawns[p.Id].position, bossPosition) < 7) match.DamagePlayer(p.Id, 24);
                }
            }
            if (match.Phase == MatchPhase.Escort)
            {
                army.gameObject.SetActive(true); relic.gameObject.SetActive(true);
                // Fixed route/progress, immune to damage, collision and carrier changes.
                armyProgress = Mathf.Min(1, armyProgress + dt / 150);
                float scaled = armyProgress * 2;
                Vector3 bridge = new Vector3(30, 1, 26);
                army.position = scaled < 1 ? Vector3.Lerp(crystalPosition, bridge, scaled) : Vector3.Lerp(bridge, bossPosition, scaled - 1);
                relic.position = match.Carrier >= 0 ? pawns[match.Carrier].position + Vector3.up * 2 : relicDrop;
                if (match.Carrier >= 0 && Vector3.Distance(army.position, pawns[match.Carrier].position) < 2.5f)
                    match.CompleteEscort(match.Carrier);
            }
        }
        void LateUpdate()
        {
            if (pawns.Count == 0 || view == null) return;
            Vector3 target = pawns[0].position + new Vector3(0, 18, -14);
            view.transform.position = Vector3.Lerp(view.transform.position, target, 1 - Mathf.Exp(-8 * Time.unscaledDeltaTime));
            view.transform.rotation = Quaternion.Euler(52, 0, 0);
        }
        void LocalInput(float dt)
        {
            var k = Keyboard.current; var mouse = Mouse.current;
            if (k == null || mouse == null || !match.Players[0].Alive) return;
            Vector3 direction = new Vector3((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0), 0,
                (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
            Move(0, pawns[0].position + direction.normalized * 10, dt);
            Ray ray = view.ScreenPointToRay(mouse.position.ReadValue());
            if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float enter))
            {
                Vector3 aim = ray.GetPoint(enter) - pawns[0].position; aim.y = 0;
                if (aim.sqrMagnitude > .1f) pawns[0].rotation = Quaternion.LookRotation(aim);
            }
            // HUD is on the left and bottom; clicks there never cast attacks.
            Vector2 pointer = mouse.position.ReadValue();
            bool overHud = pointer.x < Screen.width * 330f / 1440f || pointer.y < Screen.height * 90f / 900f;
            if (!overHud && (mouse.leftButton.isPressed || mouse.rightButton.isPressed)) Attack(0, mouse.rightButton.isPressed);
        }
        void Bot(int i, float dt)
        {
            Vector3 target;
            if (match.Phase == MatchPhase.Hunt)
                target = completed[i] < 2 && match.QuestRewardsOpen ? objectives[questIndex[i]] : bossPosition;
            else if (match.Phase == MatchPhase.Demon)
                target = i == match.Demon ? crystalPosition : pawns[match.Demon].position;
            else if (match.Carrier == i) target = army.position;
            else target = match.Carrier < 0 ? relicDrop : pawns[match.Carrier].position;
            Move(i, target, dt);
            if (completed[i] >= 2 || match.Phase != MatchPhase.Hunt) Attack(i, false);
        }
        void Move(int i, Vector3 target, float dt)
        {
            Vector3 pos = pawns[i].position;
            if (Vector3.Distance(pos, target) < 1.2f) return;
            if (i > 0 && (pos.x < 29 && target.x > 31 || pos.x > 31 && target.x < 29) && pos.z > 11 && pos.z < 43)
                target = Mathf.Abs(pos.z - 26) > 1.5f ? new Vector3(pos.x < 30 ? 27 : 33, 1, 26) : new Vector3(pos.x < 30 ? 33 : 27, 1, 26);
            Vector3 delta = target - pos; delta.y = 0;
            Vector3 next = Vector3.MoveTowards(pos, pos + delta.normalized * 10, 5 * dt);
            next.x = Mathf.Clamp(next.x, 1, 59); next.z = Mathf.Clamp(next.z, 1, 51);
            if (!Canyon(next.x, next.z)) pawns[i].position = next;
            if (i > 0 && delta.sqrMagnitude > .1f) pawns[i].rotation = Quaternion.LookRotation(delta);
        }
        void Attack(int id, bool magic)
        {
            if (!match.Running || attackCooldown[id] > 0 || !match.Players[id].Alive) return;
            var p = match.Players[id];
            attackCooldown[id] = magic ? 1.8f : .65f;
            float range = magic ? 10 : p.Path == GrowthPath.Adventurer ? 7 : 3.8f;
            float damage = (magic ? 30 : 22) + p.Level * (p.Path == GrowthPath.Hero ? 9 : 4) + followers[id].Count * 5;
            if (id == match.Demon) { damage *= 2.5f; range = 7; }
            if (match.Phase == MatchPhase.Escort && id == match.Carrier) damage *= 1.25f;
            if (match.Phase == MatchPhase.Hunt && CanHit(id, bossPosition, range)) match.DamageBoss(id, damage);
            else if (match.Phase == MatchPhase.Demon && id == match.Demon && CanHit(id, crystalPosition, range)) match.DamageCrystal(id, damage);
            else if (match.Phase != MatchPhase.Hunt)
            {
                foreach (var other in match.Players)
                {
                    if (other.Id == id || !other.Alive) continue;
                    if (match.Phase == MatchPhase.Demon && id != match.Demon && other.Id != match.Demon) continue;
                    if (CanHit(id, pawns[other.Id].position, range))
                    { match.DamagePlayer(other.Id, damage); if (!magic) break; }
                }
            }
            Debug.DrawRay(pawns[id].position, pawns[id].forward * range, colors[id], .3f);
        }
        bool CanHit(int id, Vector3 target, float range)
        {
            Vector3 delta = target - pawns[id].position; delta.y = 0;
            return delta.magnitude <= range && (id > 0 || Vector3.Dot(pawns[id].forward, delta.normalized) > .35f);
        }
        void UpdateQuest(int i, float dt)
        {
            if (!match.QuestRewardsOpen) { accepted[i] = false; questProgress[i] = 0; return; }
            bool near = Vector3.Distance(pawns[i].position, objectives[questIndex[i]]) < 3;
            bool interact = i > 0 || Keyboard.current != null && Keyboard.current.fKey.isPressed;
            if (!accepted[i] && near && interact && match.NewQuestsOpen) accepted[i] = true;
            if (!accepted[i] || !near || !interact) return;
            questProgress[i] += dt;
            if (questProgress[i] < 3) return;
            if (match.CompleteQuest(i, completed[i] % 3 == 1))
            {
                completed[i]++; questIndex[i] = (questIndex[i] + 1) % objectives.Count;
                notice = "P" + (i + 1) + " 完成任務：+80 分 / +100 XP" + (completed[i] % 3 == 2 ? " / 復活資格 +1" : "");
            }
            accepted[i] = false; questProgress[i] = 0;
        }
        void SyncFollowers(int id)
        {
            var p = match.Players[id];
            int count = id == match.Demon ? 0 : p.Path == GrowthPath.Hero ? 0 :
                p.Path == GrowthPath.Adventurer ? Mathf.Min(5, 2 + p.Level / 2) : Mathf.Min(12, 6 + p.Level);
            while (followers[id].Count < count)
                followers[id].Add(Shape("P" + (id + 1) + " Companion", PrimitiveType.Capsule, pawns[id].position, Vector3.one * .35f, colors[id]));
            for (int j = 0; j < followers[id].Count; j++)
            {
                var f = followers[id][j]; f.gameObject.SetActive(p.Alive);
                float angle = j * Mathf.PI * 2 / Mathf.Max(1, count);
                f.position = pawns[id].position + new Vector3(Mathf.Cos(angle), -.4f, Mathf.Sin(angle)) * 1.5f;
            }
        }
        static bool Canyon(float x, float z) => x > 28 && x < 32 && z > 12 && z < 42 && !(z > 23 && z < 29);
        void BuildMap()
        {
            // 60 x 60 offset hex cells; each cell is one metre across horizontally.
            // Merge ground into one mesh instead of creating 3600 GameObjects/materials.
            var vertices = new List<Vector3>(); var indices = new List<int>(); var vertexColors = new List<Color>();
            for (int row = 0; row < 60; row++) for (int col = 0; col < 60; col++)
            {
                float x = col + (row % 2) * .5f, z = row * .8660254f;
                Color c = Canyon(x,z) ? new Color(.12f,.16f,.2f) : x >= 34 && z >= 30 ? new Color(.25f,.19f,.28f) : new Color(.23f,.36f,.27f);
                // Mesh uses one neutral material; vertex colors are also saved for future shaders.
                float y = Canyon(x,z) ? -1.5f : 0;
                int start = vertices.Count; vertices.Add(new Vector3(x,y,z)); vertexColors.Add(c);
                for (int side = 0; side < 6; side++)
                {
                    float a = (30 + side * 60) * Mathf.Deg2Rad;
                    vertices.Add(new Vector3(x + Mathf.Cos(a) * .565f, y, z + Mathf.Sin(a) * .565f)); vertexColors.Add(c);
                }
                for (int side = 0; side < 6; side++) { indices.Add(start); indices.Add(start + 1 + (side + 1) % 6); indices.Add(start + 1 + side); }
            }
            var mesh = new Mesh { name = "60x60 Hex Terrain" };
            mesh.SetVertices(vertices); mesh.SetTriangles(indices,0); mesh.SetColors(vertexColors); mesh.RecalculateNormals();
            var ground = new GameObject("Hex Map 60x60"); ground.AddComponent<MeshFilter>().sharedMesh = mesh;
            ground.AddComponent<MeshRenderer>().sharedMaterial = Material(new Color(.3f,.42f,.32f));
            Shape("Canyon Floor", PrimitiveType.Cube, new Vector3(30,-1.6f,27), new Vector3(4,.2f,30), new Color(.08f,.1f,.14f));
            Shape("Central Bridge", PrimitiveType.Cube, new Vector3(30,.02f,26), new Vector3(5,.15f,6), new Color(.6f,.5f,.36f));
            Shape("Demon Territory ~1/5 map", PrimitiveType.Cube, new Vector3(47,-.04f,41), new Vector3(26,.08f,22), new Color(.28f,.19f,.28f));
            Castle(new Vector3(49,0,44), 8, new Color(.25f,.2f,.32f));
            Castle(new Vector3(6,0,5), 3, new Color(.55f,.6f,.65f));
            foreach (var farm in farms)
            {
                Shape("Farm", PrimitiveType.Cube, farm + new Vector3(2,0,1), new Vector3(2,2,2), new Color(.7f,.5f,.3f));
                Shape("Farm Roof", PrimitiveType.Cube, farm + new Vector3(2,1.3f,1), new Vector3(2.4f,.6f,2.4f), new Color(.5f,.23f,.17f));
            }
            Vector3[] points = { new Vector3(15,1,18), new Vector3(22,1,35), new Vector3(40,1,19), new Vector3(49,1,26), new Vector3(12,1,43), new Vector3(23,1,10) };
            foreach (var point in points)
            {
                objectives.Add(point);
                Shape("Quest Ruin / Camp", PrimitiveType.Cylinder, point, new Vector3(2,.4f,2), new Color(.85f,.65f,.2f));
                for (int j = 0; j < 3; j++) Shape("Ruin Pillar", PrimitiveType.Cube, point + new Vector3(j-1,1,2), new Vector3(.5f,2 + j*.3f,.5f), new Color(.45f,.46f,.43f));
            }
        }
        void Castle(Vector3 pos, float size, Color color)
        {
            for (int i = -1; i <= 1; i += 2) for (int j = -1; j <= 1; j += 2)
                Shape("Castle Tower", PrimitiveType.Cylinder, pos + new Vector3(i*size,2,j*size), new Vector3(2,2,2), color);
            Shape("Castle Keep", PrimitiveType.Cube, pos + new Vector3(0,1.8f,3), new Vector3(size,3.6f,3), color);
        }
        Material Material(Color color)
        {
            if (materials.TryGetValue(color, out var material)) return material;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { color = color }; materials.Add(color, material); return material;
        }
        Transform Shape(string objectName, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type); go.name = objectName;
            go.transform.position = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Material(color);
            Destroy(go.GetComponent<Collider>()); return go.transform;
        }
        void OnGUI()
        {
            if (match == null) return;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1440f, Screen.height / 900f, 1));
            GUI.skin.font = font;
            label ??= new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            title ??= new GUIStyle(label) { fontSize = 24, fontStyle = FontStyle.Bold };
            GUI.Box(new Rect(12,12,310,650), "");
            GUILayout.BeginArea(new Rect(25,22,282,630));
            GUILayout.Label("王國與魔王 · DEMO", title);
            GUILayout.Label("單機 + 3 AI｜規則驗證原型", label);
            GUILayout.Space(8);
            if (!started)
            {
                GUILayout.Label("選擇成長路線（本局固定）", label);
                if (GUILayout.Button("英雄：單體無上限等級", GUILayout.Height(36))) StartGame(GrowthPath.Hero);
                if (GUILayout.Button("冒險家：主角 + 2～5 同伴", GUILayout.Height(36))) StartGame(GrowthPath.Adventurer);
                if (GUILayout.Button("軍團：隊長 + 多名士兵", GUILayout.Height(36))) StartGame(GrowthPath.Legion);
            }
            GUILayout.Label($"{PhaseName()}   {Mathf.CeilToInt(match.Remaining)/60:00}:{Mathf.CeilToInt(match.Remaining)%60:00}", title);
            GUILayout.Label(match.NewQuestsOpen ? "任務開放（12 分鐘停止接取）" : match.QuestRewardsOpen ? "任務收束：20 分鐘截止" : "普通任務已關閉", label);
            var local = match.Players[0];
            GUILayout.Label($"P1 {PathName(local.Path)}  Lv.{local.Level}\nHP {local.Health:0}/{local.MaxHealth:0}  XP {local.XP}\n任務分 {local.Score}  復活資格 {local.RevivalTokens}\n討伐貢獻 {local.Contribution:0}", label);
            if (!local.Alive) GUILayout.Label(local.Eliminated ? "已永久淘汰，觀戰中" : $"復活倒數 {local.RespawnRemaining:0.0}s", label);
            if (match.QuestRewardsOpen) GUILayout.Label($"目標：金色據點 {questIndex[0]+1}\n靠近並按住 F：{questProgress[0]:0.0}/3 秒\n每第三輪第二項提供復活資格", label);
            if (match.Phase == MatchPhase.Hunt) GUILayout.Label($"初始魔王 HP {match.BossHealth:0}/2400", label);
            if (match.Phase == MatchPhase.Choice)
            {
                GUILayout.Label($"選擇權：P{match.Chooser+1}（模擬暫停）", label);
                if (match.Chooser == 0)
                {
                    if (GUILayout.Button("魔化：犧牲獎勵取得魔王力量", GUILayout.Height(42))) match.ChooseRoute(0,true);
                    if (GUILayout.Button("護送：持有聖器與王國軍會合", GUILayout.Height(42))) match.ChooseRoute(0,false);
                }
                else GUILayout.Label("AI 將於 3 秒後決定路線", label);
            }
            if (match.Phase == MatchPhase.Demon) GUILayout.Label($"玩家魔王 P{match.Demon+1}\n王國水晶 {match.CrystalHealth:0}/1800", label);
            if (match.Phase == MatchPhase.Escort) GUILayout.Label($"聖器持有者：{(match.Carrier < 0 ? "掉落" : "P"+(match.Carrier+1))}\n王國軍行進 {armyProgress:P0}", label);
            if (match.Phase == MatchPhase.Finished)
            {
                GUILayout.Label(match.Result, title);
                GUILayout.Label(match.Winner < 0 ? "無人獲勝" : $"勝者：P{match.Winner+1}", title);
                if (GUILayout.Button("重新開始", GUILayout.Height(35))) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            GUILayout.Space(8);
            foreach (var p in match.Players.OrderByDescending(p=>p.Score))
                GUILayout.Label($"P{p.Id+1} {PathName(p.Path)} | {p.Score}分 | {(p.Alive ? "存活" : p.Eliminated ? "淘汰" : "待復活")}", label);
            GUILayout.EndArea();
            DrawMinimap();
            GUI.Box(new Rect(12,805,1416,80), "");
            GUI.Label(new Rect(26,813,1370,60), "WASD 移動｜滑鼠瞄準｜左鍵：路線攻擊｜右鍵：遠程範圍攻擊｜F：據點任務／復活任務｜F1：測試工具\n" + notice, label);
            DrawWorldLabels();
            if (showDebug) DebugPanel();
        }
        void DrawMinimap()
        {
            Rect map = new Rect(1160,25,250,230); GUI.Box(map,"60 × 60 六角格");
            Vector2 Map(Vector3 v) => new Vector2(map.x + 10 + v.x/60*230, map.y+215-v.z/52*185);
            foreach (var p in match.Players)
            {
                GUI.color = colors[p.Id]; Vector2 pos = Map(pawns[p.Id].position);
                GUI.Label(new Rect(pos.x,pos.y,35,23),"P"+(p.Id+1));
            }
            GUI.color = Color.yellow;
            for (int i = 0; i < objectives.Count; i++) { Vector2 pos = Map(objectives[i]); GUI.Label(new Rect(pos.x,pos.y,25,22),(i+1).ToString()); }
            GUI.color = Color.white;
            Vector2 b = Map(bossPosition), c = Map(crystalPosition);
            GUI.Label(new Rect(b.x-20,b.y-20,80,22),"魔王城"); GUI.Label(new Rect(c.x,c.y,80,22),"王國");
            GUI.Label(new Rect(1215,240,180,24),"中央峽谷・三處通路");
        }
        void DrawWorldLabels()
        {
            if (view == null) return;
            for (int i=0;i<pawns.Count;i++)
            {
                Vector3 p = view.WorldToScreenPoint(pawns[i].position+Vector3.up*2);
                if (p.z<=0 || !match.Players[i].Alive) continue;
                GUI.Label(new Rect(p.x/Screen.width*1440-40,(1-p.y/Screen.height)*900,160,30),$"P{i+1}  {match.Players[i].Health:0}",label);
            }
        }
        void DebugPanel()
        {
            GUILayout.BeginArea(new Rect(340,20,290,265),GUI.skin.box);
            GUILayout.Label("測試工具（不代表正式規則）",label);
            GUILayout.Label($"模擬速度 {speed:0}x",label);
            if (GUILayout.Button("1x / 20x")) speed = speed == 1 ? 20 : 1;
            if (GUILayout.Button("跳至任務截止前 5 秒") && match.Phase == MatchPhase.Hunt)
                match.Tick(Mathf.Max(0,match.Rules.EndQuestsAt-match.HuntElapsed-5));
            if (GUILayout.Button("P1 完成討伐（測試分支）")) match.DamageBoss(0,10000);
            if (GUILayout.Button("測試 P1 死亡")) match.DamagePlayer(0,10000);
            if (GUILayout.Button("終局倒數至剩餘 1 秒") && (match.Phase == MatchPhase.Demon || match.Phase == MatchPhase.Escort)) match.Tick(match.Remaining-1);
            GUILayout.EndArea();
        }
        void StartGame(GrowthPath path) { match.Players[0].Path=path; started=true; }
        string PhaseName() => match.Phase switch { MatchPhase.Hunt=>"初始討伐",MatchPhase.Choice=>"路線選擇",MatchPhase.Demon=>"魔王終局",MatchPhase.Escort=>"護送終局",_=>"已結算" };
        static string PathName(GrowthPath path) => path switch { GrowthPath.Hero=>"英雄",GrowthPath.Adventurer=>"冒險家",_=>"軍團" };
        void OnDestroy() { foreach (var m in materials.Values) Destroy(m); if(font!=null) Destroy(font); }
    }
}
