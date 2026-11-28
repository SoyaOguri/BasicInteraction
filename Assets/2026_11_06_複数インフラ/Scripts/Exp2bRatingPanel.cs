using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

// 走行のあとに、交差点の評定をテンキーで答えてもらう画面（正面の画面に重ねて出す）
//  ・問いは 4 つ：条件の 4 つの要素（規範・見通し・情報・注意喚起）に 1 つずつ。5 段階、全部「そう思う」ほどその性質が強い向き
//  ・問いの文は Setting/exp2b_ratings.txt（exe の横の Setting フォルダ）に「列名 = 問いの文」で書けば、ビルドし直さずに変えられる
//    （ファイルが無ければ下の Items の文を使う。問いを変えたら、評定の CSV は別の被験者フォルダか新しいファイルにすること。
//      列名は最初に書いた見出しのまま追記されるため）
//  ・1 問ずつ出す。テンキーの 1〜5 で選び、テンキーの Enter で決定して次の問いへ。BS（Backspace）で前の問いに戻る
//    （上の段の数字キー 1〜5 と、キーボードの Enter でも選べるが、キーボードの Enter は自転車を初期位置に戻すので、決定はテンキーの Enter）
//  ・実験者は Delete キーで、この試行の評定を飛ばせる（記録には skipped = 1）
//  ・NumLock が切れていると数字が入らないので、テンキーの NumLock を点けておく
//  ・答えは被験者のフォルダの P01_ratings.csv に 1 試行 1 行で追記する
//    どの試行でも列は同じ（その試行で聞かなかった問いは空）。問いを増やしたなどで、すでにあるファイルの見出しと違うときは
//    P01_ratings_2.csv のように新しいファイルに書く
//  ・ボタンのタイミングの問い（列名が btnTiming で始まるもの：1 回目 btnTiming、2 回目 btnTiming2）は、選んだ番号ではなく「何秒ずれていたか」を書く
//    （1 = −2（2 秒早い）, 2 = −1, 3 = 0（ちょうど）, 4 = +1, 5 = +2（2 秒遅い））
//  ・問いごとに選択肢の文を変えられる：exp2b_ratings.txt で「列名 = 問いの文 | 選択肢1 / 選択肢2 / … / 選択肢5」
public class Exp2bRatingPanel : MonoBehaviour
{
    // 仮の問い（内容はあとで決める）。Setting/exp2b_ratings.txt があればそちらが優先
    // 並び：ボタンのタイミング 2 問を先に聞き（走った直後のうちに）、そのあと交差点の 4 問
    public static readonly string[][] DefaultItems =
    {
        new[] { TimingKey, "今の 1 回目のボタンを押したタイミングは、適切なタイミングで押せましたか？", TimingLabels },
        new[] { TimingKey + "2", "今の 2 回目のボタンを押したタイミングは、適切なタイミングで押せましたか？", TimingLabels },
        new[] { "norm", "ここは、止まらなければいけない交差点だ" },
        new[] { "sight", "横から来る車が見えやすい交差点だった" },
        new[] { "info", "横から車が来るかどうかを、確かめる手がかりがあった" },
        new[] { "alert", "交差点に着く前の早い段階で、この先は気をつけるべき場所だと感じた" },
    };
    // ボタンのタイミングの問いか（列名が btnTiming で始まる）
    public static bool IsTimingKey(string key) { return key != null && key.StartsWith(TimingKey); }
    // ボタンのタイミングの問いの列名と選択肢（「/」で区切る。「\n」で改行）
    public const string TimingKey = "btnTiming";
    public const string TimingLabels = "2 秒\\n早い / 1 秒\\n早い / ちょうど\\n（0 秒） / 1 秒\\n遅い / 2 秒\\n遅い";
    public string[][] Items = DefaultItems;
    [Tooltip("問いの文を読むファイル（exe の横の Setting フォルダ）")]
    public string itemsFileName = "exp2b_ratings.txt";
    [Tooltip("確認用：読んだ問いのファイル")]
    [SerializeField] string loadedItems = "";
    public static readonly string[] Agree = { "全く\nそう思わない", "あまり\nそう思わない", "どちらとも\nいえない", "やや\nそう思う", "とても\nそう思う" };

    [Tooltip("文字のフォント。空ならタイマーの文字と同じフォントを使う（Exp2bTrialManager が渡す）")]
    public Font font;
    [Tooltip("問いの文の大きさ（画面の高さ 1080 px のとき）")]
    public int questionFontSize = 50;
    [Tooltip("選択肢の文の大きさ（番号はこの 1.8 倍）")]
    public int choiceFontSize = 30;
    [Tooltip("実験者がこの試行の評定を飛ばすキー")]
    public KeyCode skipKey = KeyCode.Delete;

    [Header("確認用（読み取り専用）")]
    [SerializeField] bool active;
    [SerializeField] int question;

    public bool Active { get { return active; } }

    Canvas canvas;
    Text titleText, questionText, hintText, warnText;
    readonly Image[] boxes = new Image[5];
    readonly Text[] boxTexts = new Text[5];
    int[] answers;
    float[] answerSec;
    int[] ask;          // この試行で聞く問い（Items の番号）。並びは Items の順
    int selected;
    float startTime, questionStart;
    string startClock;
    string savePath, rowPrefix;
    System.Action onDone;

    static readonly Color Bg = new Color(0f, 0f, 0f, 0.86f);
    static readonly Color BoxOff = new Color(0.22f, 0.22f, 0.25f, 1f);
    static readonly Color BoxOn = new Color(1f, 0.78f, 0.1f, 1f);

    // 問いの読み込み（まだなら）
    public void EnsureItems()
    {
        if (string.IsNullOrEmpty(loadedItems)) LoadItems();
    }

    // ratingsPath：保存する CSV。header / values：先頭に書く試行の情報（「,」区切り）
    // askKeys：この試行で聞く問いの列名（Items の順に聞く）。1 つも無ければ何もせずに done を呼ぶ
    public void Begin(string ratingsPath, string header, string values, string title, ICollection<string> askKeys, System.Action done)
    {
        EnsureItems();
        Build();
        var a = new List<int>();
        for (int i = 0; i < Items.Length; i++) if (askKeys.Contains(Items[i][0])) a.Add(i);
        if (a.Count == 0) { if (done != null) done(); return; }
        ask = a.ToArray();
        savePath = ChooseFile(ratingsPath, header);
        rowPrefix = values;
        onDone = done;
        answers = new int[Items.Length];
        answerSec = new float[Items.Length];
        question = 0;
        selected = 0;
        startTime = Time.time;
        questionStart = Time.time;
        startClock = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        titleText.text = title;
        active = true;
        canvas.gameObject.SetActive(true);
        Show();
    }

    void Update()
    {
        if (!active) return;
        warnText.text = Input.GetKey(KeyCode.End) || Input.GetKey(KeyCode.PageDown) || Input.GetKey(KeyCode.Home)
            ? "数字が入りません。テンキーの NumLock を点けてください" : "";

        if (Input.GetKeyDown(skipKey)) { Finish(true); return; }

        for (int k = 1; k <= 5; k++)
        {
            if (Input.GetKeyDown(KeyCode.Keypad0 + k) || Input.GetKeyDown(KeyCode.Alpha0 + k))
            {
                selected = k;
                Show();
            }
        }
        if (Input.GetKeyDown(KeyCode.KeypadEnter) && selected > 0)
        {
            answers[ask[question]] = selected;
            answerSec[ask[question]] = Time.time - questionStart;
            question++;
            if (question >= ask.Length) { Finish(false); return; }
            selected = answers[ask[question]];
            questionStart = Time.time;
            Show();
        }
        if (Input.GetKeyDown(KeyCode.Backspace) && question > 0)
        {
            question--;
            selected = answers[ask[question]];
            questionStart = Time.time;
            Show();
        }
    }

    void Show()
    {
        string[] item = Items[ask[question]];
        questionText.text = string.Format("<size={0}>{1} / {2}</size>\n{3}",
            Mathf.RoundToInt(questionText.fontSize * 0.55f), question + 1, ask.Length, item[1]);
        string[] labels = Labels(item);
        for (int i = 0; i < 5; i++)
        {
            boxTexts[i].text = string.Format("<size={0}><b>{1}</b></size>\n{2}", Mathf.RoundToInt(choiceFontSize * 1.8f), i + 1, labels[i]);
            bool on = selected == i + 1;
            boxes[i].color = on ? BoxOn : BoxOff;
            boxTexts[i].color = on ? Color.black : Color.white;
        }
        hintText.text = selected > 0
            ? "テンキーの Enter で決定（Backspace で前の問いに戻る）"
            : "テンキーの 1〜5 で選んでください";
    }

    void Finish(bool skipped)
    {
        active = false;
        canvas.gameObject.SetActive(false);
        var f = CultureInfo.InvariantCulture;
        var cols = new List<string> { rowPrefix };
        for (int i = 0; i < Items.Length; i++) cols.Add(!skipped && answers[i] > 0 ? Value(i, answers[i]) : "");
        for (int i = 0; i < Items.Length; i++) cols.Add(!skipped && answers[i] > 0 ? answerSec[i].ToString("F2", f) : "");
        cols.Add(skipped ? "1" : "0");
        cols.Add((Time.time - startTime).ToString("F2", f));
        cols.Add(startClock);
        cols.Add(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        try
        {
            File.AppendAllText(savePath, string.Join(",", cols) + "\r\n", new System.Text.UTF8Encoding(false));
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Exp2b] 評定を保存できませんでした: " + savePath + " (" + e.Message + ")");
        }
        Debug.Log("[Exp2b] 評定 " + (skipped ? "（飛ばした）" : string.Join(" ", System.Array.ConvertAll(answers, x => x.ToString()))));
        var done = onDone;
        onDone = null;
        if (done != null) done();
    }

    // 記録する値：ボタンのタイミングは −2〜+2 秒、ほかは選んだ番号 1〜5
    string Value(int item, int choice)
    {
        return IsTimingKey(Items[item][0]) ? (choice - 3).ToString() : choice.ToString();
    }

    // 選択肢の文：問いに「|」で書いてあればそれ（「\n」は改行）、無ければ「そう思う」の 5 段階
    static string[] Labels(string[] item)
    {
        if (item.Length >= 3 && !string.IsNullOrEmpty(item[2]))
        {
            string[] l = item[2].Split('/');
            if (l.Length == 5)
            {
                for (int i = 0; i < 5; i++) l[i] = l[i].Trim().Replace("\\n", "\n");
                return l;
            }
        }
        return Agree;
    }

    // Setting/exp2b_ratings.txt：1 行 1 問、「列名 = 問いの文 | 選択肢1 / … / 選択肢5」（選択肢は省略可。# 以降はコメント）。並んだ順に出す
    void LoadItems()
    {
        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Setting", itemsFileName));
        loadedItems = "（" + path + " が無いので既定の問い）";
        if (!File.Exists(path)) return;
        var list = new List<string[]>();
        foreach (string raw in File.ReadAllLines(path))
        {
            string line = raw;
            int hash = line.IndexOf('#');
            if (hash >= 0) line = line.Substring(0, hash);
            int eq = line.IndexOf('=');
            if (eq < 0) continue;
            string key = line.Substring(0, eq).Trim();
            string text = line.Substring(eq + 1).Trim();
            string labels = "";
            int bar = text.IndexOf('|');
            if (bar >= 0) { labels = text.Substring(bar + 1).Trim(); text = text.Substring(0, bar).Trim(); }
            if (IsTimingKey(key) && labels.Length == 0) labels = TimingLabels;
            if (key.Length == 0 || text.Length == 0 || key.Contains(",")) continue;
            list.Add(new[] { key, text, labels });
        }
        if (list.Count == 0) return;
        Items = list.ToArray();
        loadedItems = path;
        Debug.Log("[Exp2b] 評定の問いを読みました（" + Items.Length + " 問）: " + path);
    }

    // 見出しが同じファイルに追記する。すでにあるファイルの見出しが違う（問いを増やしたなど）ときは _2, _3 … の新しいファイル
    string ChooseFile(string path, string header)
    {
        var cols = new List<string> { header };
        foreach (var it in Items) cols.Add(it[0]);
        foreach (var it in Items) cols.Add(it[0] + "_sec");
        cols.Add("skipped");
        cols.Add("totalSec");
        cols.Add("startClock");
        cols.Add("endClock");
        string line = string.Join(",", cols);
        string dir = Path.GetDirectoryName(path), name = Path.GetFileNameWithoutExtension(path);
        for (int k = 1; ; k++)
        {
            string p = k == 1 ? path : Path.Combine(dir, name + "_" + k + ".csv");
            if (!File.Exists(p))
            {
                File.WriteAllText(p, line + "\r\n", new System.Text.UTF8Encoding(true));
                if (k > 1) Debug.LogWarning("[Exp2b] 評定の問いが前のファイルと違うので、新しいファイルに書きます: " + p);
                return p;
            }
            string first;
            using (var r = new StreamReader(p, System.Text.Encoding.UTF8)) first = r.ReadLine() ?? "";
            if (first.TrimStart('\uFEFF') == line) return p;
        }
    }

    // ---------------- 画面を作る（初回だけ） ----------------

    void Build()
    {
        if (canvas != null) return;
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        var cgo = new GameObject("Exp2bRatingCanvas");
        cgo.transform.SetParent(transform, false);
        canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = cgo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var bg = NewImage(cgo.transform, "Bg", Bg);
        Stretch(bg.rectTransform, 0f, 0f, 1f, 1f);

        titleText = NewText(cgo.transform, "Title", 40, TextAnchor.MiddleCenter);
        Stretch(titleText.rectTransform, 0.05f, 0.82f, 0.95f, 0.94f);

        questionText = NewText(cgo.transform, "Question", questionFontSize, TextAnchor.MiddleCenter);
        Stretch(questionText.rectTransform, 0.05f, 0.52f, 0.95f, 0.80f);

        float w = 0.16f, gap = 0.02f, x0 = 0.5f - (5 * w + 4 * gap) / 2f;
        for (int i = 0; i < 5; i++)
        {
            float x = x0 + i * (w + gap);
            boxes[i] = NewImage(cgo.transform, "Choice" + (i + 1), BoxOff);
            Stretch(boxes[i].rectTransform, x, 0.20f, x + w, 0.46f);
            boxTexts[i] = NewText(boxes[i].transform, "Label", choiceFontSize, TextAnchor.MiddleCenter);
            Stretch(boxTexts[i].rectTransform, 0f, 0f, 1f, 1f);
        }

        hintText = NewText(cgo.transform, "Hint", 36, TextAnchor.MiddleCenter);
        Stretch(hintText.rectTransform, 0.05f, 0.08f, 0.95f, 0.16f);

        warnText = NewText(cgo.transform, "Warn", 34, TextAnchor.MiddleCenter);
        warnText.color = new Color(1f, 0.4f, 0.4f);
        Stretch(warnText.rectTransform, 0.05f, 0.01f, 0.95f, 0.08f);

        cgo.SetActive(false);
    }

    Image NewImage(Transform parent, string name, Color c)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = c;
        return img;
    }

    Text NewText(Transform parent, string name, int size, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = anchor;
        t.color = Color.white;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    static void Stretch(RectTransform rt, float x0, float y0, float x1, float y1)
    {
        rt.anchorMin = new Vector2(x0, y0);
        rt.anchorMax = new Vector2(x1, y1);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
