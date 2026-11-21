using UnityEngine;
using UnityEngine.UI;

// 学校の校名看板（towndata > 学校 > 看板 > Canvas > Text）の見た目を整える。
//  ・フォントは Windows に入っている明朝体を、その場で読み込んで使う（フォントファイルをプロジェクトにコピーしない）
//    上から順に探し、最初に見つかったものを使う。どれも無い PC では Unity 標準のフォントのまま
//  ・校名を「名古屋高等学校」のように正式名称にし、濃紺の明朝体で中央にそろえる
//  ・エディタでも再生中でも反映される（シーンを開くたびに付け直す）
[ExecuteAlways]
[RequireComponent(typeof(Text))]
public class SchoolSignStyle : MonoBehaviour
{
    [Tooltip("看板に出す文字。改行（\\n）も使える")]
    [TextArea] public string label = "名古屋高等学校";

    [Tooltip("使うフォントの候補（Windows に入っている名前）。上から順に探す")]
    public string[] osFontNames =
    {
        "Yu Mincho Demibold", "游明朝 Demibold", "Yu Mincho", "游明朝",
        "HG明朝E", "HGMinchoE", "BIZ UDPMincho", "BIZ UDP明朝 Medium", "MS PMincho", "ＭＳ Ｐ明朝",
    };

    public Color color = new Color(0.08f, 0.12f, 0.28f);   // 濃紺
    public FontStyle fontStyle = FontStyle.Normal;
    [Tooltip("看板の枠いっぱいに文字の大きさを合わせる")]
    public bool fitToBoard = true;
    [Range(8, 120)] public int maxFontSize = 60;
    [Tooltip("フォントを読み込むときの基準サイズ（大きいほど近くで見ても文字がぼやけにくい）")]
    [Range(16, 200)] public int fontLoadSize = 96;

    [Header("確認用（読み取り専用）")]
    [SerializeField] string usedFont = "";

    Font osFont;

    void OnEnable() { ApplyStyle(); }

#if UNITY_EDITOR
    void OnValidate() { UnityEditor.EditorApplication.delayCall += () => { if (this != null) ApplyStyle(); }; }
#endif

    public void ApplyStyle()
    {
        var t = GetComponent<Text>();
        if (t == null) return;

        if (osFont == null)
        {
            osFont = Font.CreateDynamicFontFromOSFont(osFontNames, fontLoadSize);
            if (osFont != null) osFont.hideFlags = HideFlags.DontSave;
        }
        // CreateDynamicFontFromOSFont は候補が 1 つも無くても Font を返すので、実際に入っている名前を確認する
        string found = FirstInstalled();
        if (osFont != null && found != null) t.font = osFont;
        usedFont = found ?? "（候補のフォントが見つからないので標準フォントのまま）";

        t.text = label.Replace("\\n", "\n");
        t.color = color;
        t.fontStyle = fontStyle;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        t.resizeTextForBestFit = fitToBoard;
        t.resizeTextMinSize = 4;
        t.resizeTextMaxSize = maxFontSize;
        t.fontSize = maxFontSize;
    }

    string FirstInstalled()
    {
        var installed = new System.Collections.Generic.HashSet<string>(Font.GetOSInstalledFontNames());
        foreach (var n in osFontNames) if (installed.Contains(n)) return n;
        return null;
    }

    [ContextMenu("この PC で使えるフォント名を Console に出す")]
    void ListFonts()
    {
        Debug.Log("[SchoolSignStyle] " + string.Join(" / ", Font.GetOSInstalledFontNames()));
    }
}
