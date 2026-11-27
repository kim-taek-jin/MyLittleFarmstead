using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// S-2: 국악풍 배경음악. 무엇을 언제 틀지는 SoundRules, 소리 파일은 Resources/Audio/Music (Tools/audio/make_music.py).
//  - 여러 겹짜리 곡(농장 3겹, 장터 2겹)은 겹마다 AudioSource를 하나씩 같은 순간에 틀고 크기만 바꿈
//  - 곡이 바뀌면 2초쯤 겹쳐서 넘어감
//  - 볼륨은 일시정지 메뉴의 "음악" 값(Sfx.Music)을 따름
//  - 장면에 원래 있던 반복 재생 음악(예전 Farm Country)은 끔
public class MusicManager : MonoBehaviour
{
    static MusicManager instance;
    const float FadeSeconds = 2.2f, LayerSpeed = 0.25f;

    class Playing
    {
        public string id;
        public AudioSource[] layers;
        public float[] layerVol;
        public float fade;       // 0 → 1 들어올 때, 1 → 0 나갈 때
        public bool leaving;
    }

    Playing current;
    readonly List<Playing> leaving = new List<Playing>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (instance != null) return;
        var go = new GameObject("[Music]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MusicManager>();
        go.AddComponent<Ambience>();
        SceneManager.sceneLoaded += (s, m) => SilenceSceneMusic();
        SilenceSceneMusic();
    }

    // 장면 파일에 들어 있던 예전 음악은 끔 (판매용 게임에 쓰기에는 사용 조건이 불분명한 곡이었음)
    static void SilenceSceneMusic()
    {
        foreach (AudioSource a in FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (a.loop && a.GetComponentInParent<MusicManager>() == null && a.GetComponentInParent<Ambience>() == null && a.GetComponentInParent<Sfx>() == null)
            {
                a.Stop();
                a.enabled = false;
            }
    }

    public static bool Ours(AudioSource a) => a.GetComponentInParent<MusicManager>() != null || a.GetComponentInParent<Ambience>() != null;

    void Update()
    {
        string want = WantedPiece(out float hour, out bool indoors, out float color01, out bool marketDay);
        if (current == null ? want != null : current.id != want)
        {
            if (current != null) { current.leaving = true; leaving.Add(current); }
            current = want != null ? Begin(want) : null;
        }

        float dt = Time.unscaledDeltaTime;
        if (current != null)
        {
            current.fade = Mathf.MoveTowards(current.fade, 1f, dt / FadeSeconds);
            float level = SoundRules.Level(current.id, hour, indoors);
            for (int i = 0; i < current.layers.Length; i++)
            {
                if (current.layers[i] == null) continue;
                float target = SoundRules.LayerVolume(current.id, i, color01, marketDay);
                current.layerVol[i] = Mathf.MoveTowards(current.layerVol[i], target, dt * LayerSpeed);
                current.layers[i].volume = Sfx.Music * level * current.fade * current.layerVol[i];
            }
        }
        for (int k = leaving.Count - 1; k >= 0; k--)
        {
            Playing p = leaving[k];
            p.fade = Mathf.MoveTowards(p.fade, 0f, dt / FadeSeconds);
            foreach (AudioSource a in p.layers)
                if (a != null) a.volume = Mathf.Min(a.volume, Sfx.Music * p.fade);
            if (p.fade <= 0f)
            {
                foreach (AudioSource a in p.layers) if (a != null) Destroy(a);
                leaving.RemoveAt(k);
            }
        }
    }

    Playing Begin(string id)
    {
        int n = SoundRules.Layers(id);
        var p = new Playing { id = id, layers = new AudioSource[n], layerVol = new float[n] };
        for (int i = 0; i < n; i++)
        {
            AudioClip clip = Resources.Load<AudioClip>("Audio/Music/" + (n > 1 ? $"{id}_{i}" : id));
            if (clip == null) continue;
            AudioSource a = gameObject.AddComponent<AudioSource>();
            a.clip = clip;
            a.loop = true;
            a.playOnAwake = false;
            a.ignoreListenerPause = true;
            a.volume = 0f;
            p.layers[i] = a;
            p.layerVol[i] = SoundRules.LayerVolume(id, i, Data != null ? Data.VisibleColor01 : 0f, false);
        }
        double at = AudioSettings.dspTime + 0.05;          // 겹들을 같은 순간에
        foreach (AudioSource a in p.layers) if (a != null) a.PlayScheduled(at);
        return p;
    }

    static DataManager Data => DataManager.instance;

    static string WantedPiece(out float hour, out bool indoors, out float color01, out bool marketDay)
    {
        DataManager d = Data;
        GameTimeManager time = GameTimeManager.instance;
        string scene = SceneManager.GetActiveScene().name;
        hour = time != null ? time.hour + time.minute / 60f : 12f;
        indoors = scene == "Home" || InteriorRooms.Inside;
        color01 = d != null ? d.VisibleColor01 : 0f;
        marketDay = d != null && MarketState.IsMarketDay(d.Today);
        bool title = TitleScreen.Open || Opening.Open;
        bool boreum = d != null && time != null && scene == "Farm" && d.CurrentSeason == Season.Winter && Rites.Night(d.story.rites, time.day, time.hour);
        return SoundRules.Piece(scene, title, DemoEnd.Open, boreum, hour);
    }
}

// S-1: 바깥 소리. 겨울바람(낮), 밤바람, 불 타는 소리(가까울수록), 가끔 까치·먼 개·새벽 닭.
public class Ambience : MonoBehaviour
{
    AudioSource wind, night, fire, frogs;
    float nextOccasional = 20f;
    int roosterDay = -1;

    void Awake()
    {
        wind = Loop("amb_wind_winter");
        night = Loop("amb_night");
        fire = Loop("amb_fire");
        frogs = Loop("amb_frogs");
    }

    AudioSource Loop(string name)
    {
        AudioSource a = gameObject.AddComponent<AudioSource>();
        a.clip = Resources.Load<AudioClip>("Audio/Ambience/" + name);
        a.loop = true;
        a.playOnAwake = false;
        a.volume = 0f;
        if (a.clip != null) a.Play();
        return a;
    }

    void Update()
    {
        DataManager d = DataManager.instance;
        GameTimeManager time = GameTimeManager.instance;
        string scene = SceneManager.GetActiveScene().name;
        bool quiet = d == null || time == null || TitleScreen.Open || Opening.Open || DemoEnd.Open || StarHill.Active;
        bool outdoors = (scene == "Farm" || scene == "Market") && !InteriorRooms.Inside;
        bool indoors = scene == "Home" || InteriorRooms.Inside;
        float hour = time != null ? time.hour + time.minute / 60f : 12f;
        bool winter = d != null && d.CurrentSeason == Season.Winter;
        float k = quiet ? 0f : Sfx.Effects;
        float dt = Time.unscaledDeltaTime;

        Ease(wind, SoundRules.WindVolume(outdoors, indoors, winter, d != null && d.snowing, hour) * k, dt);
        Ease(night, SoundRules.NightVolume(outdoors, indoors, hour) * k, dt);
        Ease(fire, (outdoors ? SoundRules.FireVolume(NearestFire(out float reach), reach) : 0f) * k, dt * 2f);
        bool flooded = d != null && Spring.IsSpring(d.story) && d.story.spring.watered;
        Ease(frogs, SoundRules.FrogsVolume(outdoors, indoors, flooded, hour) * k, dt);

        if (quiet) return;
        if (SoundRules.RoosterNow(hour, time.day, roosterDay))
        {
            roosterDay = time.day;
            Sfx.Play("rooster", indoors ? 0.5f : 0.85f, 1f, 0.03f);
        }
        nextOccasional -= Time.deltaTime;
        if (nextOccasional <= 0f)
        {
            nextOccasional = Random.Range(25f, 60f);
            string s = SoundRules.Occasional(outdoors, hour, Random.value, d.CurrentSeason == Season.Spring);
            if (s != null) Sfx.Play(s, 0.8f, 1f, 0.05f);
        }
    }

    static void Ease(AudioSource a, float target, float dt)
    {
        if (a == null) return;
        a.volume = Mathf.MoveTowards(a.volume, target, dt * 0.5f);
    }

    // 가장 가까운 불 (불꽃이 일렁이는 NightGlow = 아궁이, 달집, 쥐불)
    static float NearestFire(out float reach)
    {
        reach = 6f;
        PlayerAction p = PlayerAction.instance;
        if (p == null) return float.MaxValue;
        float best = float.MaxValue;
        foreach (NightGlow g in NightGlow.All)
        {
            if (g == null || g.flicker <= 0f || !g.Active) continue;
            float dist = Vector2.Distance(p.transform.position, g.Center);
            float r = g.radius * 2.2f;
            if (dist / r < best / reach) { best = dist; reach = r; }
        }
        return best;
    }
}
