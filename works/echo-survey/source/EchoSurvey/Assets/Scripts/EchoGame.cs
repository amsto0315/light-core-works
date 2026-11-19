using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoSurvey
{
    public class EchoGame : MonoBehaviour
    {
        public static EchoGame I;

        public bool Started;
        public bool Paused;
        public bool Complete;
        public EchoPlayer Player;
        public int Found;
        public float Elapsed;
        public Vector3[] BeaconPositions;

        private readonly Vector4[] _pulses = new Vector4[4];
        private int _pulseIndex;
        private int _pulseCount;
        private Material _arch;
        private Material _floor;
        private Material _beacon;
        private AudioSource _audio;
        private AudioClip _ping;
        private AudioClip _ambience;
        private Light _pulseLight;
        private Font _font;
        private GUIStyle _title;
        private GUIStyle _sub;
        private GUIStyle _small;
        private GUIStyle _button;
        private float _pulseFlash;
        private bool _autoshot;
        private bool[] _found = new bool[3];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            if (FindFirstObjectByType<EchoGame>() != null) return;
            var go = new GameObject("EchoSurvey");
            go.AddComponent<EchoGame>();
        }

        private void Awake()
        {
            I = this;
            Application.targetFrameRate = 60;
            BuildMaterials();
            BuildWorld();
            BuildAudio();
            BuildFont();
            _autoshot = HasArg("-autoshot");
            if (_autoshot)
            {
                Started = true;
                Player.ResetPlayer(new Vector3(0, 1.05f, 3.2f), 0f);
                StartCoroutine(AutoShot());
            }
        }

        private static bool HasArg(string arg)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length; i++)
                if (args[i] == arg) return true;
            return false;
        }

        private void BuildMaterials()
        {
            var shader = Resources.Load<Shader>("EchoSurface");
            if (shader == null) shader = Shader.Find("Echo/Surface");
            if (shader == null)
            {
                Debug.LogError("[EchoSurvey] Echo/Surface shader missing");
                shader = Shader.Find("Standard");
            }
            _arch = new Material(shader) { name = "EchoArch" };
            _arch.SetColor("_Color", new Color(0.18f, 0.35f, 0.38f));
            _arch.SetColor("_GridColor", new Color(0.38f, 0.95f, 0.86f));
            _arch.SetFloat("_Base", 0.008f);
            _arch.SetFloat("_Grid", 0.58f);

            _floor = new Material(shader) { name = "EchoFloor" };
            _floor.SetColor("_Color", new Color(0.20f, 0.30f, 0.32f));
            _floor.SetColor("_GridColor", new Color(0.32f, 0.80f, 0.74f));
            _floor.SetFloat("_Base", 0.005f);
            _floor.SetFloat("_Grid", 0.45f);

            _beacon = new Material(shader) { name = "EchoBeacon" };
            _beacon.SetColor("_Color", new Color(0.95f, 0.55f, 0.24f));
            _beacon.SetColor("_GridColor", new Color(1f, 0.72f, 0.34f));
            _beacon.SetFloat("_Base", 0.30f);
            _beacon.SetFloat("_Grid", 0.8f);
        }

        private void ConfigureAtmosphere()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.008f, 0.015f, 0.016f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.018f;
            RenderSettings.fogColor = new Color(0.006f, 0.014f, 0.017f);
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.antiAliasing = 4;
        }

        private GameObject Box(string name, Vector3 pos, Vector3 size, Material mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = size;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            if (!collider) Destroy(go.GetComponent<Collider>());
            return go;
        }

        private GameObject Column(string name, Vector3 pos, float height, float radius, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.position = pos + Vector3.up * height * 0.5f;
            go.transform.localScale = new Vector3(radius * 2, height * 0.5f, radius * 2);
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        private void BuildWorld()
        {
            ConfigureAtmosphere();

            var playerGo = new GameObject("Surveyor");
            playerGo.transform.position = new Vector3(0, 1.05f, 0.5f);
            playerGo.AddComponent<CharacterController>();
            Player = playerGo.AddComponent<EchoPlayer>();

            // connected plates
            Box("StartDeck", new Vector3(0, -0.5f, 4f), new Vector3(14, 1, 14), _floor);
            Box("NorthCorridor", new Vector3(0, -0.5f, 13f), new Vector3(5, 1, 8), _floor);
            Box("CentralDeck", new Vector3(0, -0.5f, 23f), new Vector3(18, 1, 18), _floor);
            Box("EastBridge", new Vector3(11f, -0.5f, 23f), new Vector3(12, 1, 3.2f), _floor);
            Box("EastDeck", new Vector3(22f, -0.5f, 23f), new Vector3(14, 1, 14), _floor);
            Box("NorthBridge", new Vector3(0, -0.5f, 35f), new Vector3(5, 1, 10), _floor);
            Box("NorthDeck", new Vector3(0, -0.5f, 46f), new Vector3(16, 1, 16), _floor);

            // low side walls / buttresses; they are invisible until the wave hits
            for (var s = -1; s <= 1; s += 2)
            {
                Box("StartWall_" + s, new Vector3(s * 6.7f, 1.6f, 4f), new Vector3(0.5f, 3.2f, 14f), _arch);
                Box("CentralWall_" + s, new Vector3(s * 8.7f, 1.8f, 23f), new Vector3(0.5f, 3.6f, 18f), _arch);
                Box("EastWall_" + s, new Vector3(22 + s * 6.7f, 1.6f, 23f), new Vector3(0.5f, 3.2f, 14f), _arch);
                Box("NorthWall_" + s, new Vector3(s * 7.7f, 1.7f, 46f), new Vector3(0.5f, 3.4f, 16f), _arch);
                Box("StartRail_" + s, new Vector3(s * 6.9f, 0.55f, 4f), new Vector3(0.12f, 0.8f, 14f), _arch);
            }

            // columns and beams
            for (var i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * 7.2f;
                float z = 17f + (i < 2 ? 0f : 12f);
                Column("CentralColumn_" + i, new Vector3(x, 0, z), 5.4f, 0.38f, _arch);
                Box("Cap_" + i, new Vector3(x, 5.4f, z), new Vector3(1.2f, 0.28f, 1.2f), _arch);
            }
            Box("CentralBeamL", new Vector3(-7.2f, 5.2f, 23f), new Vector3(0.3f, 0.35f, 18f), _arch);
            Box("CentralBeamR", new Vector3(7.2f, 5.2f, 23f), new Vector3(0.3f, 0.35f, 18f), _arch);
            Box("CentralLintel", new Vector3(0, 5.2f, 32f), new Vector3(14.4f, 0.35f, 0.35f), _arch);

            // loose blocks / debris
            var debris = new[]
            {
                new Vector3(-3.8f, .35f, 6.5f), new Vector3(3.4f, .5f, 8.2f),
                new Vector3(-2.2f, .25f, 20f), new Vector3(4.2f, .7f, 25f),
                new Vector3(9.2f, .4f, 22.5f), new Vector3(16f, .35f, 20.5f),
                new Vector3(-4f, .45f, 43f), new Vector3(4.8f, .3f, 46.2f),
                new Vector3(1.6f, .6f, 39f)
            };
            for (var i = 0; i < debris.Length; i++)
                Box("Debris_" + i, debris[i], new Vector3(1.2f + (i % 3) * .5f, .5f + (i % 2) * .35f,
                    1.1f + (i % 4) * .25f), _arch);

            // three beacons
            BeaconPositions = new[]
            {
                new Vector3(0f, 1.0f, 21.5f),
                new Vector3(22f, 1.0f, 23.5f),
                new Vector3(0f, 1.0f, 45f)
            };
            for (var i = 0; i < BeaconPositions.Length; i++)
                BuildBeacon(i, BeaconPositions[i]);

            // ambient pulse light that only brightens briefly
            var pl = new GameObject("PulseLight");
            pl.transform.SetParent(transform, false);
            _pulseLight = pl.AddComponent<Light>();
            _pulseLight.type = LightType.Point;
            _pulseLight.color = new Color(0.36f, 0.95f, 0.82f);
            _pulseLight.range = 28f;
            _pulseLight.intensity = 0f;
            _pulseLight.shadows = LightShadows.None;

            Shader.SetGlobalFloat("_PulseLife", 2.8f);
            Shader.SetGlobalFloat("_PulseSpeed", 24f);
            Shader.SetGlobalFloat("_PulseCount", 0f);
            Shader.SetGlobalVectorArray("_PulsePos", _pulses);
        }

        private void BuildBeacon(int index, Vector3 pos)
        {
            var root = new GameObject("Beacon_" + index);
            root.transform.position = pos;
            var core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            core.name = "Core";
            core.transform.SetParent(root.transform, false);
            core.transform.localPosition = new Vector3(0, 1.1f, 0);
            core.transform.localScale = new Vector3(0.42f, 2.2f, 0.42f);
            core.GetComponent<MeshRenderer>().sharedMaterial = _beacon;
            Destroy(core.GetComponent<Collider>());

            for (var i = 0; i < 3; i++)
            {
                var ring = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ring.name = "Ring_" + i;
                ring.transform.SetParent(root.transform, false);
                ring.transform.localPosition = new Vector3(0, 0.55f + i * .72f, 0);
                ring.transform.localScale = new Vector3(1.25f - i * .18f, .06f, 1.25f - i * .18f);
                ring.transform.localRotation = Quaternion.Euler(0, i * 22f, 0);
                var mr = ring.GetComponent<MeshRenderer>();
                mr.sharedMaterial = _beacon;
                Destroy(ring.GetComponent<Collider>());
            }

            var lightGo = new GameObject("BeaconLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0, 1.6f, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, .62f, .28f);
            light.range = 6.5f;
            light.intensity = 1.35f;
            light.shadows = LightShadows.None;
            root.AddComponent<EchoBeacon>().Index = index;
        }

        private void BuildAudio()
        {
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.spatialBlend = 0f;
            _audio.playOnAwake = false;
            _audio.volume = 0.9f;

            const int rate = 44100;
            const float dur = 2.4f;
            var ping = new float[Mathf.CeilToInt(rate * dur)];
            var rng = new System.Random(7);
            for (var i = 0; i < ping.Length; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Exp(-t * 2.0f) * Mathf.Clamp01(t * 50f);
                float f = Mathf.Lerp(720f, 140f, Mathf.Clamp01(t / dur));
                float s = Mathf.Sin(2f * Mathf.PI * f * t) * .35f
                          + Mathf.Sin(2f * Mathf.PI * f * 1.9f * t) * .08f
                          + ((float)rng.NextDouble() * 2f - 1f) * .018f;
                ping[i] = s * env;
            }
            _ping = AudioClip.Create("echo_ping", ping.Length, 1, rate, false);
            _ping.SetData(ping, 0);

            const float ambDur = 8f;
            var amb = new float[Mathf.CeilToInt(rate * ambDur)];
            for (var i = 0; i < amb.Length; i++)
            {
                float t = i / (float)rate;
                float a = Mathf.Sin(2f * Mathf.PI * 44f * t) * .018f;
                float b = Mathf.Sin(2f * Mathf.PI * 66f * t + 1.4f) * .012f;
                float c = Mathf.Sin(2f * Mathf.PI * 88f * t + .4f) * .006f;
                ampModulate(ref a, t);
                amb[i] = (a + b + c) * (0.7f + .3f * Mathf.Sin(t * .8f));
            }
            _ambience = AudioClip.Create("echo_ambience", amb.Length, 1, rate, false);
            _ambience.SetData(amb, 0);
            _audio.clip = _ambience;
            _audio.loop = true;
            _audio.volume = .20f;
            _audio.Play();
        }

        private void ampModulate(ref float a, float t)
        {
            a *= 0.6f + 0.4f * Mathf.Sin(t * 0.7f);
        }

        private void BuildFont()
        {
            var osFonts = new[] { "Microsoft YaHei", "PingFang SC", "Noto Sans CJK SC", "Arial" };
            _font = Font.CreateDynamicFontFromOSFont(osFonts, 20);
        }

        private void EnsureStyles()
        {
            if (_title != null) return;
            _title = new GUIStyle(GUI.skin.label) { font = _font, fontSize = 42, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _sub = new GUIStyle(GUI.skin.label) { font = _font, fontSize = 14, alignment = TextAnchor.MiddleCenter };
            _small = new GUIStyle(GUI.skin.label) { font = _font, fontSize = 12 };
            _button = new GUIStyle(GUI.skin.button) { font = _font, fontSize = 15, fixedHeight = 36 };
        }

        private void StartRun()
        {
            Started = true;
            Paused = false;
            Complete = false;
            Found = 0;
            _found = new bool[3];
            Elapsed = 0f;
            _pulseCount = 0;
            _pulseIndex = 0;
            Shader.SetGlobalFloat("_PulseCount", 0f);
            Player.ResetPlayer(new Vector3(0, 1.05f, 0.6f), 0f);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _audio.PlayOneShot(_ping, .8f);
        }

        private void Update()
        {
            if (!Started || Paused || Complete) return;
            Elapsed += Time.deltaTime;
            _pulseFlash = Mathf.MoveTowards(_pulseFlash, 0f, Time.deltaTime * 3f);
            if (_pulseLight != null)
                _pulseLight.intensity = Mathf.Lerp(_pulseLight.intensity, _pulseFlash * 5f, Time.deltaTime * 12f);

            for (var i = 0; i < 4; i++)
                _pulses[i] = _pulses[i];
            Shader.SetGlobalVectorArray("_PulsePos", _pulses);
            Shader.SetGlobalFloat("_PulseCount", _pulseCount);

            var p = Player.transform.position;
            for (var i = 0; i < BeaconPositions.Length; i++)
            {
                if (_found[i]) continue;
                var b = BeaconPositions[i];
                if (Vector3.Distance(new Vector3(p.x, 0, p.z), new Vector3(b.x, 0, b.z)) < 3.6f)
                {
                    _found[i] = true;
                    Found++;
                    _audio.PlayOneShot(_ping, 1f);
                    StartCoroutine(BeaconFound(Found));
                    if (Found >= BeaconPositions.Length)
                    {
                        Complete = true;
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                    }
                }
            }
        }

        private IEnumerator BeaconFound(int count)
        {
            yield return null;
            _pulseFlash = 1.5f;
            if (_pulseLight != null)
                _pulseLight.transform.position = Player.transform.position + Vector3.up * 1.2f;
        }

        public void EmitPulse(Vector3 position)
        {
            if (!Started || Paused || Complete) return;
            _pulses[_pulseIndex] = new Vector4(position.x, position.y, position.z, Time.time);
            _pulseIndex = (_pulseIndex + 1) % 4;
            _pulseCount = Mathf.Min(4, _pulseCount + 1);
            Shader.SetGlobalVectorArray("_PulsePos", _pulses);
            Shader.SetGlobalFloat("_PulseCount", _pulseCount);
            _pulseFlash = 1f;
            if (_pulseLight != null)
                _pulseLight.transform.position = position + Vector3.up * 1.2f;
            _audio.PlayOneShot(_ping, .85f);
        }

        public void TogglePause()
        {
            Paused = !Paused;
            Cursor.lockState = Paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = Paused;
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!focus && Started && !Complete) TogglePause();
        }

        private void OnGUI()
        {
            if (_font == null) BuildFont();
            EnsureStyles();
            float w = Screen.width, h = Screen.height;

            if (!Started)
            {
                Fill(new Color(.004f, .010f, .013f, .94f));
                GUI.Label(new Rect(0, h * .26f, w, 64), "ECHO SURVEY", _title);
                var t = new GUIStyle(_title) { fontSize = 26 };
                GUI.Label(new Rect(0, h * .34f, w, 44), "回 声 测 绘", t);
                GUI.Label(new Rect(0, h * .42f, w, 40),
                    "这里没有光。按住左键，让声呐替你看见。", _sub);
                GUI.Label(new Rect(0, h * .47f, w, 30),
                    "WASD 移动  ·  鼠标环视  ·  左键发射脉冲  ·  Esc 暂停", _sub);
                if (GUI.Button(new Rect(w * .5f - 92, h * .57f, 184, 42), "开始测绘", _button))
                    StartRun();
                GUI.Label(new Rect(0, h * .66f, w, 30),
                    "找到三座信标。它们只在回声里回应。", _sub);
                return;
            }

            // HUD
            var top = new GUIStyle(_small) { fontSize = 13, normal = { textColor = new Color(.64f, .95f, .86f) } };
            GUI.Label(new Rect(28, 24, 420, 24), "ECHO SURVEY / 回声测绘", top);
            GUI.Label(new Rect(28, 46, 520, 24), "目标：找到并靠近三座信标", _small);
            string dots = "";
            for (var i = 0; i < BeaconPositions.Length; i++) dots += i < Found ? "●  " : "○  ";
            GUI.Label(new Rect(28, 72, 320, 32), dots, new GUIStyle(_sub) { alignment = TextAnchor.MiddleLeft, fontSize = 18 });
            GUI.Label(new Rect(28, 102, 360, 22), $"信号 {Found} / {BeaconPositions.Length}   时间 {Elapsed:0.0}s", _small);

            DrawCrosshair(w, h);

            GUI.Label(new Rect(0, h - 44, w, 28),
                "左键：声呐脉冲    ·    WASD：移动    ·    鼠标：环视    ·    Shift：慢走", _sub);

            if (_pulseFlash > .1f)
            {
                Fill(new Color(.10f, .92f, .82f, .04f * _pulseFlash));
            }

            if (Paused)
            {
                Fill(new Color(.004f, .01f, .013f, .82f));
                GUI.Label(new Rect(0, h * .4f, w, 50), "暂 停", _title);
                if (GUI.Button(new Rect(w * .5f - 80, h * .52f, 160, 38), "继续", _button))
                    TogglePause();
            }

            if (Complete)
            {
                Fill(new Color(.004f, .01f, .013f, .84f));
                GUI.Label(new Rect(0, h * .33f, w, 58), "测 绘 完 成", _title);
                GUI.Label(new Rect(0, h * .43f, w, 30), $"三座信标已回应 · 用时 {Elapsed:0.0} 秒", _sub);
                if (GUI.Button(new Rect(w * .5f - 90, h * .53f, 180, 40), "重新测绘", _button))
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        private void DrawCrosshair(float w, float h)
        {
            var old = GUI.color;
            GUI.color = new Color(.7f, 1f, .92f, .65f);
            GUI.DrawTexture(new Rect(w * .5f - 1, h * .5f - 12, 2, 8), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(w * .5f - 1, h * .5f + 4, 2, 8), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(w * .5f - 12, h * .5f - 1, 8, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(w * .5f + 4, h * .5f - 1, 8, 2), Texture2D.whiteTexture);
            GUI.color = old;
        }

        private void Fill(Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = old;
        }

        private IEnumerator AutoShot()
        {
            yield return new WaitForSecondsRealtime(.9f);
            EmitPulse(Player.transform.position + Vector3.up * 1f);
            for (var i = 0; i < 3; i++)
            {
                yield return new WaitForSecondsRealtime(.65f);
                EmitPulse(Player.transform.position + Vector3.up * 1f);
            }
            yield return new WaitForSecondsRealtime(1.4f);
            string dir = null;
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
                if (args[i] == "-shotdir") dir = args[i + 1];
            if (string.IsNullOrEmpty(dir)) dir = Path.Combine(Application.dataPath, "../../Screenshots");
            Directory.CreateDirectory(dir);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, "echo-survey.png"));
            yield return new WaitForSecondsRealtime(.4f);
            Application.Quit();
        }
    }

    public class EchoBeacon : MonoBehaviour
    {
        public int Index;
        private void Update()
        {
            transform.Rotate(0, 16f * Time.deltaTime, 0, Space.World);
        }
    }
}
