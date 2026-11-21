using System;
using System.Globalization;

// 実験2-b：1試行ぶんの交差点条件（属性 z）
// 規範・注意喚起・情報は「各軸から1種類まで」なので enum で持つ

public enum NormType { None, StopSet, RedFlash }            // なし / 停止線＋標識＋路面表示 / 赤点滅信号
public enum AttentionType { None, ColorPavement, CautionSign } // なし / カラー舗装 / 注意看板
public enum InfoType { None, Mirror, LedBoard }              // なし / カーブミラー / 車両接近LED表示板
public enum TrialKind { Factorial, Random, Dummy, Practice }
public enum CarSpawn { None, Left, Right }

[Serializable]
public class TrialCondition
{
    public string id = "F1";
    public TrialKind kind = TrialKind.Factorial;
    [UnityEngine.Tooltip("学習用条件の番号（F1〜F24）。ダミー・練習でも、使っているレイアウトの番号が入る。ランダム条件は 0")]
    public int factorialNo = 0;

    public NormType norm = NormType.None;
    public AttentionType attention = AttentionType.None;
    public InfoType info = InfoType.None;

    [UnityEngine.Tooltip("左の見通し D_L [m]（基準点から見た、左から来る車の走行線上の距離）")]
    public float sightLeft = 27f;
    [UnityEngine.Tooltip("右の見通し D_R [m]（基準点から見た、右から来る車の走行線上の距離）")]
    public float sightRight = 27f;

    [UnityEngine.Tooltip("左の塀の角の後退距離 W_L [m]（交差道路の建物線から手前へ）。0 以上なら見通しではなくこの位置に塀を置く。-1 = 見通し D_L から決める")]
    public float wallSetbackLeft = -1f;
    [UnityEngine.Tooltip("右の塀の角の後退距離 W_R [m]。-1 = 見通し D_R から決める")]
    public float wallSetbackRight = -1f;

    public CarSpawn carSpawn = CarSpawn.None; // 車ありダミーのときだけ使う

    // 要因条件の見通し3水準
    // 今のシーン（自転車は左の建物線から 2 m、右の建物線まで 6.4 m、基準点は交差道路の建物線の 7 m 手前）
    // では、塀を建物線より奥に置く限り、右の見通しは 10.97 m、左は 4.14 m より短くできない。
    //
    // 2026-11-13 に幅を広げた（旧：11 / 19 / 27 m、左右同じ）。予備実験で見通しの効果が弱かったため
    //   （45 m / 25 m / 左 5・右 11 m。塀の角は左 6.36 / 5.84 / 1.2 m、右 5.29 / 3.93 / 0.02 m 手前）
    // 2026-11-19 に、水準を塀の角の位置（後退距離）で決めることにした。
    //   基準点 1 点で測る見通し距離は、塀の角が数 m 動くだけで大きく変わる一方、交差点に近づくとどの水準でも横が見えてしまい、
    //   見通し 45 m / 25 m（角の差 0.5〜1.4 m）では、動画でも走行でも見通しの違いが行動に表れなかった。
    //   走っている人が感じる差は「横の道がどこから見え始めるか」なので、それが大きく離れるように塀の角の位置を決める。
    //   （左から来る 30 m 先の車＝30 km/h で約 3.6 秒前 が見え始める位置。自転車は左の塀から 2 m）
    //   悪い  塀の角が建物線ちょうど     → 交差点の中心から 6.6 m 手前で見え始める（基準点の見通し 左 4.15 m・右 11 m）
    //   中    塀の角が建物線から 5 m 手前  → 12.0 m 手前で見え始める
    //   良い  塀の角が建物線から 10 m 手前 → 17.4 m 手前で見え始める
    //   （2026-11-20 に 0 / 8 / 16 m から狭めた。予備調査で判断した位置が 12〜20 m 手前だったので、
    //     良いは判断の前に見え、中は判断の頃に見え始め、悪いは判断の後に見える、という並びにした）
    //   中と良いは基準点（建物線の 7 m 手前）より後ろに角があるため、基準点の見通し D は「遮る物なし」（999）になる。
    //   そのため条件は後退距離 W_L・W_R（wallSetbackLeft/Right）で持ち、ログにも W を出す。
    public const float SetbackBad = 0f;
    public const float SetbackMid = 5f;
    public const float SetbackGood = 10f;

    // このシーンの幾何（IntersectionLayout の既定値と同じ）。後退距離 → 基準点での見通し D（ログ用）
    const float LeftWallOffset = 2f, RightWallOffset = 6.4f;
    const float LeftLaneFromRef = 14.5f, RightLaneFromRef = 12f, EdgeFromRef = 7f;
    public const float SightUnobstructed = 999f;

    public static float SightForSetback(bool left, float setback)
    {
        float cornerFromRef = EdgeFromRef - setback;
        if (cornerFromRef <= 0.001f) return SightUnobstructed;
        return left ? LeftWallOffset * LeftLaneFromRef / cornerFromRef : RightWallOffset * RightLaneFromRef / cornerFromRef;
    }

    // 後退距離で塀を置く（どちらも 0 以上のとき）。見通し D はログ用に計算して入れる
    public void SetWalls(float setbackLeft, float setbackRight)
    {
        wallSetbackLeft = setbackLeft;
        wallSetbackRight = setbackRight;
        sightLeft = SightForSetback(true, setbackLeft);
        sightRight = SightForSetback(false, setbackRight);
    }

    public bool UsesSetback { get { return wallSetbackLeft >= 0f && wallSetbackRight >= 0f; } }

    public TrialCondition Clone()
    {
        return (TrialCondition)MemberwiseClone();
    }

    public const int FactorialCount = 24;

    // 学習用条件 F1〜F24（設計書 v4「5.2 条件一覧」）
    //   規範2 × 見通し3 × カーブミラー2 × カラー舗装2 の全組み合わせ
    //   F1〜F12 = 規範なし、F13〜F24 = 規範あり
    //   それぞれ 見通し 良い(4)→中(4)→悪い(4) の順、4つの中は
    //   なし / カラー舗装 / ミラー / ミラー＋カラー舗装 の順
    public static TrialCondition Factorial(int no)
    {
        if (no < 1 || no > FactorialCount)
            throw new ArgumentOutOfRangeException("no", "学習用条件は F1〜F24");
        int k = no - 1;
        var c = new TrialCondition { id = "F" + no, kind = TrialKind.Factorial, factorialNo = no };
        NormType norm = k / 12 == 0 ? NormType.None : NormType.StopSet;
        int sightIdx = (k % 12) / 4;
        c.norm = norm;
        if (sightIdx == 0) c.SetWalls(SetbackGood, SetbackGood);
        else if (sightIdx == 1) c.SetWalls(SetbackMid, SetbackMid);
        else c.SetWalls(SetbackBad, SetbackBad);
        int extra = k % 4;
        c.attention = (extra == 1 || extra == 3) ? AttentionType.ColorPavement : AttentionType.None;
        c.info = (extra == 2 || extra == 3) ? InfoType.Mirror : InfoType.None;
        return c;
    }

    // 見通しの水準名（ログ用）
    public string SightLevel()
    {
        if (UsesSetback)
        {
            if (IsW(SetbackGood)) return "good";
            if (IsW(SetbackMid)) return "mid";
            if (IsW(SetbackBad)) return "bad";
            return "asym";
        }
        if (sightLeft != sightRight) return "asym";
        return "other";
    }

    bool IsW(float w)
    {
        return Math.Abs(wallSetbackLeft - w) < 0.01f && Math.Abs(wallSetbackRight - w) < 0.01f;
    }

    // ランダム条件の CSV（Tools/exp2b/generate_random_conditions.py が出力）を読む。
    // set を指定するとそのセット（A〜D）の 12 行だけを返す
    public static System.Collections.Generic.List<TrialCondition> ParseRandomCsv(string text, string set = null)
    {
        var list = new System.Collections.Generic.List<TrialCondition>();
        if (string.IsNullOrEmpty(text)) return list;
        string[] lines = text.Replace("\r", "").Split('\n');
        if (lines.Length < 2) return list;
        string[] head = lines[0].Split(',');
        int Col(string name) { return Array.IndexOf(head, name); }
        int cId = Col("id"), cSet = Col("set"), cNorm = Col("norm"), cL = Col("sightLeft"), cR = Col("sightRight"),
            cInfo = Col("info"), cAtt = Col("attention");
        for (int i = 1; i < lines.Length; i++)
        {
            if (lines[i].Trim().Length == 0) continue;
            string[] v = lines[i].Split(',');
            if (set != null && v[cSet] != set) continue;
            list.Add(new TrialCondition
            {
                id = v[cId],
                kind = TrialKind.Random,
                norm = (NormType)Enum.Parse(typeof(NormType), v[cNorm]),
                sightLeft = float.Parse(v[cL], CultureInfo.InvariantCulture),
                sightRight = float.Parse(v[cR], CultureInfo.InvariantCulture),
                info = (InfoType)Enum.Parse(typeof(InfoType), v[cInfo]),
                attention = (AttentionType)Enum.Parse(typeof(AttentionType), v[cAtt]),
            });
        }
        return list;
    }

    // ログ用：z を 0/1 と距離の列に展開する
    public const string ZHeader = "Line,Sign,Mark,RedFlash,D_L,D_R,W_L,W_R,Mirror,Led,Color,CautionSign";

    public string ZValues()
    {
        int stop = norm == NormType.StopSet ? 1 : 0;
        return string.Join(",", new[]
        {
            stop.ToString(), stop.ToString(), stop.ToString(),
            norm == NormType.RedFlash ? "1" : "0",
            sightLeft.ToString("F2", CultureInfo.InvariantCulture),
            sightRight.ToString("F2", CultureInfo.InvariantCulture),
            wallSetbackLeft.ToString("F2", CultureInfo.InvariantCulture),
            wallSetbackRight.ToString("F2", CultureInfo.InvariantCulture),
            info == InfoType.Mirror ? "1" : "0",
            info == InfoType.LedBoard ? "1" : "0",
            attention == AttentionType.ColorPavement ? "1" : "0",
            attention == AttentionType.CautionSign ? "1" : "0",
        });
    }

    public override string ToString()
    {
        return string.Format("{0} [{1}] 規範:{2} 注意喚起:{3} 情報:{4} D_L:{5:F1} D_R:{6:F1} W_L:{7:F1} W_R:{8:F1} 車:{9}",
            id, kind, norm, attention, info, sightLeft, sightRight, wallSetbackLeft, wallSetbackRight, carSpawn);
    }
}
