using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 효과음과 볼륨 설정. 장면 파일은 건드리지 않고 실행 중에 만듭니다.
//  - 소리 파일: Assets/Resources/Audio/Sfx/*.wav (Tools/audio/make_sfx.py가 만듦)
//  - 쓰는 법: Sfx.Play("harvest")
//  - 볼륨(전체/음악/효과음)은 PlayerPrefs에 저장되고, 일시정지 메뉴(ESC)에서 바꿉니다.
//  - 음악: 장면에 있는 반복 재생 AudioSource를 음악으로 보고 음악 볼륨을 적용합니다.
//  - 버튼을 누르면 자동으로 클릭 소리가 납니다.
public class Sfx : MonoBehaviour
{
    public static Sfx Instance { get; private set; }

    const string KeyMaster = "vol_master", KeyMusic = "vol_music", KeySfx = "vol_sfx";
    public static float Master { get; private set; } = 1f;
    public static float Music { get; private set; } = 0.7f;
    public static float Effects { get; private set; } = 0.8f;

    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
    private AudioSource source;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Instance != null) return;
        var go = new GameObject("Sfx");
        DontDestroyOnLoad(go);
        go.AddComponent<Sfx>();
        SceneManager.sceneLoaded += (s, m) => ApplyMusicVolume();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.ignoreListenerPause = true;   // 일시정지 중에도 메뉴 소리는 나게
        Master = PlayerPrefs.GetFloat(KeyMaster, 1f);
        Music = PlayerPrefs.GetFloat(KeyMusic, 0.7f);
        Effects = PlayerPrefs.GetFloat(KeySfx, 0.8f);
        AudioListener.volume = Master;
        foreach (AudioClip c in Resources.LoadAll<AudioClip>("Audio/Sfx")) clips[c.name] = c;
        ApplyMusicVolume();
    }

    // 같은 소리가 한 프레임에 여러 번 나지 않도록 짧은 간격(minGap)을 둡니다.
    public static void Play(string name, float volume = 1f, float minGap = 0.05f, float pitchJitter = 0.06f)
    {
        if (Instance == null || Effects <= 0f) return;
        if (!Instance.clips.TryGetValue(name, out AudioClip clip)) return;
        float now = Time.unscaledTime;
        if (Instance.lastPlayed.TryGetValue(name, out float last) && now - last < minGap) return;
        Instance.lastPlayed[name] = now;
        Instance.source.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        Instance.source.PlayOneShot(clip, volume * Effects);
    }

    public static void SetMaster(float v) { Master = Mathf.Clamp01(v); AudioListener.volume = Master; PlayerPrefs.SetFloat(KeyMaster, Master); }
    public static void SetMusic(float v) { Music = Mathf.Clamp01(v); PlayerPrefs.SetFloat(KeyMusic, Music); ApplyMusicVolume(); }
    public static void SetEffects(float v) { Effects = Mathf.Clamp01(v); PlayerPrefs.SetFloat(KeySfx, Effects); }

    static void ApplyMusicVolume()
    {
        foreach (AudioSource a in FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (a.loop && (Instance == null || a != Instance.source)) a.volume = Music;
    }

    void Update()
    {
        // 버튼 클릭 소리
        if (Input.GetMouseButtonDown(0) && EventSystem.current != null)
        {
            GameObject over = CurrentUIObject();
            if (over != null && over.GetComponentInParent<Selectable>() != null) Play("click", 0.8f);
        }
    }

    static readonly List<RaycastResult> hits = new List<RaycastResult>();
    static GameObject CurrentUIObject()
    {
        var data = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        hits.Clear();
        EventSystem.current.RaycastAll(data, hits);
        return hits.Count > 0 ? hits[0].gameObject : null;
    }
}
