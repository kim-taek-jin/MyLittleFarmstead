using UnityEngine;

// 회색 주민. E키로 대화하고, 소원(필요한 아이템)을 가져다주면 색이 1/3씩 돌아옵니다.
// WorldSpawner가 씬이 열릴 때 코드로 생성하므로 씬에 따로 배치할 필요가 없습니다.
public class Villager : MonoBehaviour
{
    public string villagerId;

    public static Villager InRange { get; private set; }

    private VillagerDef def;
    private SpriteRenderer body;
    private SpriteRenderer bubble;
    private int shownStage = -1;
    private int idleLine;
    private float pulse;
    private float baseScale = 1f;

    public static Villager Create(VillagerDef def, Material material, float characterScale)
    {
        GameObject go = new GameObject("Villager_" + def.id);
        go.transform.position = new Vector3(def.position.x, def.position.y, 0f);

        CircleCollider2D talkRange = go.AddComponent<CircleCollider2D>();
        talkRange.isTrigger = true;
        talkRange.radius = 1.0f;
        BoxCollider2D feet = go.AddComponent<BoxCollider2D>();   // 발밑만 막아서 자연스럽게 비켜 지나가도록
        feet.size = new Vector2(0.3f * characterScale, 0.14f * characterScale);
        feet.offset = new Vector2(0f, -0.41f * characterScale);

        GameObject bodyGO = new GameObject("Body");
        bodyGO.transform.SetParent(go.transform, false);
        bodyGO.transform.localScale = Vector3.one * characterScale;
        SpriteRenderer sr = bodyGO.AddComponent<SpriteRenderer>();
        if (material != null) sr.sharedMaterial = material;
        sr.sortingOrder = 10;

        GameObject bubbleGO = new GameObject("Bubble");
        bubbleGO.transform.SetParent(go.transform, false);
        bubbleGO.transform.localPosition = new Vector3(0.19f * characterScale, 0.57f * characterScale, 0f);
        bubbleGO.transform.localScale = Vector3.one * characterScale;
        SpriteRenderer bsr = bubbleGO.AddComponent<SpriteRenderer>();
        if (material != null) bsr.sharedMaterial = material;
        bsr.sprite = PixelArt.Load("World/bubble");
        bsr.sortingOrder = 30;

        Villager v = go.AddComponent<Villager>();
        v.villagerId = def.id;
        v.def = def;
        v.body = sr;
        v.bubble = bsr;
        v.baseScale = characterScale;
        v.RefreshVisual();
        return v;
    }

    DataManager Data => DataManager.instance;
    VillagerProgress Progress => Data != null ? Data.story.Get(villagerId) : null;
    bool Restored => Progress != null && Progress.wishIndex >= def.wishes.Length;
    WishDef CurrentWish => Restored || Progress == null ? null : def.wishes[Progress.wishIndex];

    void RefreshVisual()
    {
        if (Progress == null || body == null) return;
        int stage = Mathf.Clamp(Progress.wishIndex, 0, 3);
        if (stage != shownStage)
        {
            Sprite s = PixelArt.Load($"Villagers/{villagerId}_{stage}");
            if (s != null) body.sprite = s;
            if (shownStage >= 0 && stage > shownStage) pulse = 1f;
            shownStage = stage;
        }
    }

    bool HasSomethingToSay()
    {
        if (Progress == null) return false;
        if (!Progress.met) return true;
        WishDef wish = CurrentWish;
        return wish != null && HasItems(wish);
    }

    bool HasItems(WishDef wish)
    {
        foreach (var s in wish.items)
        {
            if (!Data.HasItem(s.itemName, s.count)) return false;
        }
        return true;
    }

    void Update()
    {
        if (def == null) def = VillagerDB.Get(villagerId);
        if (def == null || Data == null) return;
        RefreshVisual();

        if (bubble != null)
        {
            bool show = HasSomethingToSay() && !HUD.DialogueOpen;
            bubble.enabled = show;
            if (show) bubble.transform.localPosition = new Vector3(0.19f * baseScale, (0.57f + Mathf.Sin(Time.time * 4f) * 0.02f) * baseScale, 0f);
        }

        // 색이 돌아올 때 톡 튀는 연출
        if (pulse > 0f)
        {
            pulse = Mathf.Max(0f, pulse - Time.deltaTime * 2f);
            float k = 1f + Mathf.Sin(pulse * Mathf.PI) * 0.15f;
            body.transform.localScale = new Vector3(baseScale * k, baseScale * k, 1f);
        }

        bool canTalk = InRange == this && !HUD.DialogueOpen && Time.frameCount - HUD.DialogueClosedFrame > 1 && !Data.IsGameOver;
        if (canTalk && Input.GetKeyDown(KeyCode.E)) Talk();
    }

    void Talk()
    {
        VillagerProgress p = Progress;
        Sprite portrait = body.sprite;

        string seasonLine = def.seasonLines != null && def.seasonLines.Length == 4 ? def.seasonLines[Data.story.season] : null;
        bool useSeason = seasonLine != null && idleLine % 2 == 1;

        if (Restored)
        {
            HUD.Say(def.name, new[] { useSeason ? seasonLine : def.colorIdle[idleLine % def.colorIdle.Length] }, portrait);
            idleLine++;
            return;
        }

        WishDef wish = CurrentWish;
        if (!p.met)
        {
            p.met = true;
            Album.Report("villager_met", villagerId);
            HUD.Say(def.name, new[] { def.intro, wish.request }, portrait);
            return;
        }

        if (HasItems(wish))
        {
            HUD.Ask(def.name, $"{wish.request}\n<color=#ffd84a>Give {VillagerDB.ItemsText(wish.items)}?</color>", portrait,
                () => Fulfill(wish), null, "Give", "Not yet");
        }
        else
        {
            string idle = useSeason ? seasonLine : def.grayIdle[idleLine % def.grayIdle.Length];
            idleLine++;
            HUD.Say(def.name, new[] { idle, wish.request + $"\n<color=#c8c0b0>(Needs: {VillagerDB.ItemsText(wish.items)})</color>" }, portrait);
        }
    }

    void Fulfill(WishDef wish)
    {
        if (!HasItems(wish)) return;
        foreach (var s in wish.items) Data.RemoveItem(s.itemName, s.count);

        VillagerProgress p = Progress;
        p.wishIndex++;
        Data.AddKindness(3);
        Album.Report("wish", villagerId);
        if (p.wishIndex >= def.wishes.Length) Album.Report("restored", villagerId);

        var rewards = new System.Collections.Generic.List<string>();
        if (wish.gold > 0) { Data.AddGold(wish.gold); rewards.Add($"{wish.gold}G"); }
        foreach (var s in wish.rewardItems)
        {
            Data.AddItem(s.itemName, s.count);
            rewards.Add($"{s.itemName} x{s.count}");
        }
        if (Data.story.Unlock(wish.unlock) && !string.IsNullOrEmpty(wish.unlockText)) rewards.Add(wish.unlockText);
        if (wish.heart > 0f) Data.ChangeHeart(wish.heart, $"{def.name} smiled.");

        RefreshVisual();
        var lines = new System.Collections.Generic.List<string> { wish.thanks };
        if (rewards.Count > 0) lines.Add("<color=#9dff9d>Received:</color> " + string.Join(", ", rewards));
        if (Restored) lines.Add(def.restored);

        HUD.Say(def.name, lines.ToArray(), body.sprite, () =>
        {
            if (Restored)
            {
                HUD.Announce($"<size=40>{def.name}'s colors have returned!</size>\n\n{Data.story.RestoredVillagers} / {VillagerDB.All.Count} villagers restored", 5f);
                Data.ChangeHeart(5f, "The village feels a little brighter.");
            }
            StoryEvents.CheckEnding();
        });
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        InRange = this;
        if (def != null) HUD.Toast($"Press E to talk to {def.name}.", 2f);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && InRange == this) InRange = null;
    }

    void OnDestroy()
    {
        if (InRange == this) InRange = null;
    }
}
