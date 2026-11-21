using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

// 走行のあとに、交差点の評定 9 問をテンキーで答えてもらう画面（正面の画面に重ねて出す）
//  ・問いと選択肢は動画調査（第2版）と同じ。5 段階、全部「そう思う」ほどその性質が強い向き。並びは規範→注意喚起→確認のしやすさで固定
//  ・1 問ずつ出す。テンキーの 1〜5 で選び、テンキーの Enter で決定して次の問いへ。BS（Backspace）で前の問いに戻る
//    （上の段の数字キー 1〜5 と、キーボードの Enter でも選べるが、キーボードの Enter は自転車を初期位置に戻すので、決定はテンキーの Enter）
//  ・実験者は Delete キーで、この試行の評定を飛ばせる（記録には skipped = 1）
//  ・NumLock が切れていると数字が入らないので、テンキーの NumLock を点けておく
//  ・答えは被験者のフォルダの P01_ratings.csv に 1 試行 1 行で追記する
public class Exp2bRatingPanel : MonoBehaviour
{
    public static readonly string[][] Items =
    {
        new[] { "norm1", "ここは、止まらなければいけない交差点だ" },
        new[] { "norm2", "ここで止まらずに進むと、交通違反になる" },
        new[] { "norm3", "どこで止まればよいかが、はっきり示されている" },
        new[] { "alert1", "遠くからでも、交差点があることが目立っていた" },
        new[] { "alert2", "早い段階で、この先に交差点があると気づけた" },
        new[] { "alert3", "この先は気をつけるべき場所だと感じた" },
        new[] { "chk1", "止まらなくても、横から車が来るかどうか確かめられそうだった" },
        new[] { "chk2", "横から来る車に気づきやすい交差点だった" },
        new[] { "chk3", "車が来ていないことを確かめやすい交差点だった" },
    };
    public static readonly string[] Agree = { "全く\nそう思わない", "あまり\nそう思わない", "どちらとも\nいえない", "やや\nそう思う", "とても\nそう思う" };

    [Tooltip("文字のフォント。空ならタイマーの文字と同じフォントを使う（Exp2bTrialManager が渡す）")]
    public Font font;
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
    int selected;
    float startTime, questionStart;
    string startClock;
    string savePath, rowPrefix;
    System.Action onDone;

    static readonly Color Bg = new Color(0f, 0f, 0f, 0.86f);
    static readonly Color BoxOff = new Color(0.22f, 0.22f, 0.25f, 1f);
    static readonly Color BoxOn = new Color(1f, 0.78f, 0.1f, 1f);

    // ratingsPath：保存する CSV。header / values：先頭に書く試行の情報（「,」区切り）
    public void Begin(string ratingsPath, string header, string values, string title, System.Action done)
    {
        Build();
        savePath = ratingsPath;
        rowPrefix = values;
        EnsureHeader(header);
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
            answers[question] = selected;
            answerSec[question] = Time.time - questionStart;
            question++;
            if (question >= Items.Length) { Finish(false); return; }
            selected = answers[question];
            questionStart = Time.time;
            Show();
        }
        if (Input.GetKeyDown(KeyCode.Backspace) && question > 0)
        {
            question--;
            selected = answers[question];
            questionStart = Time.time;
            Show();
        }
    }

    void Show()
    {
        questionText.text = string.Format("<size={0}>{1} / {2}</size>\n{3}",
            Mathf.RoundToInt(questionText.fontSize * 0.55f), question + 1, Items.Length, Items[question][1]);
        for (int i = 0; i < 5; i++)
        {
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
        for (int i = 0; i < Items.Length; i++) cols.Add(!skipped && answers[i] > 0 ? answers[i].ToString() : "");
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
        Debug.Log("[Exp2b] 評定 " + (skipped ? "（飛ばした）" : string.Join(" ", System.Array.ConvertAll(answers, a => a.ToString()))));
        var done = onDone;
        onDone = null;
        if (done != null) done();
    }

    void EnsureHeader(string header)
    {
        if (File.Exists(savePath)) return;
        var cols = new List<string> { header };
        foreach (var it in Items) cols.Add(it[0]);
        foreach (var it in Items) cols.Add(it[0] + "_sec");
        cols.Add("skipped");
        cols.Add("totalSec");
        cols.Add("startClock");
        cols.Add("endClock");
        File.WriteAllText(savePath, string.Join(",", cols) + "\r\n", new System.Text.UTF8Encoding(true));
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

        questionText = NewText(cgo.transform, "Question", 64, TextAnchor.MiddleCenter);
        Stretch(questionText.rectTransform, 0.05f, 0.52f, 0.95f, 0.80f);

        float w = 0.16f, gap = 0.02f, x0 = 0.5f - (5 * w + 4 * gap) / 2f;
        for (int i = 0; i < 5; i++)
        {
            float x = x0 + i * (w + gap);
            boxes[i] = NewImage(cgo.transform, "Choice" + (i + 1), BoxOff);
            Stretch(boxes[i].rectTransform, x, 0.20f, x + w, 0.46f);
            boxTexts[i] = NewText(boxes[i].transform, "Label", 38, TextAnchor.MiddleCenter);
            Stretch(boxTexts[i].rectTransform, 0f, 0f, 1f, 1f);
            boxTexts[i].text = string.Format("<size=72><b>{0}</b></size>\n{1}", i + 1, Agree[i]);
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
