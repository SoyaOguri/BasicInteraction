using System;
using System.Globalization;

// 実験2-b：1試行ぶんの交差点条件（属性 z）
// 規範・注意喚起・情報は「各軸から1種類まで」なので enum で持つ

public enum NormType { None, StopSet, RedFlash }            // なし / 停止線＋標識＋路面表示 / 赤点滅信号
public enum AttentionType { None, ColorPavement, CautionSign } // なし / カラー舗装 / 注意看板
public enum InfoType { None, Mirror, LedBoard }              // なし / カーブミラー / 車両接近LED表示板
public enum TrialKind { Factorial, Random, Dummy }
public enum CarSpawn { None, Left, Right }

[Serializable]
public class TrialCondition
{
    public string id = "F1";
    public TrialKind kind = TrialKind.Factorial;

    public NormType norm = NormType.None;
    public AttentionType attention = AttentionType.None;
    public InfoType info = InfoType.None;

    [UnityEngine.Tooltip("左の見通し D_L [m]（基準点から見た、左から来る車の走行線上の距離）")]
    public float sightLeft = 27f;
    [UnityEngine.Tooltip("右の見通し D_R [m]（基準点から見た、右から来る車の走行線上の距離）")]
    public float sightRight = 27f;

    public CarSpawn carSpawn = CarSpawn.None; // 車ありダミーのときだけ使う

    // 要因条件の見通し3水準（左右同じ）
    // 今のシーン（自転車は左の建物線から 2 m、右の建物線まで 6.4 m、基準点は交差道路の建物線の 7 m 手前）
    // では、塀を建物線より奥に置く限り右の見通しは 10.97 m 未満にできない。
    // そこで「悪い」を右の最小を切り上げた整数 11 m とし、8 m 刻みで 19 m・27 m とした（左右同じ）。
    public const float SightGood = 27f;
    public const float SightMid = 19f;
    public const float SightBad = 11f;

    public TrialCondition Clone()
    {
        return (TrialCondition)MemberwiseClone();
    }

    // 要因条件 F1〜F10（スライド「要因条件（10条件）」）
    public static TrialCondition Factorial(int no)
    {
        var c = new TrialCondition { id = "F" + no, kind = TrialKind.Factorial };
        switch (no)
        {
            case 1: Set(c, NormType.None, SightGood); break;
            case 2: Set(c, NormType.None, SightMid); break;
            case 3: Set(c, NormType.None, SightBad); break;
            case 4: Set(c, NormType.StopSet, SightGood); break;
            case 5: Set(c, NormType.StopSet, SightMid); break;
            case 6: Set(c, NormType.StopSet, SightBad); break;
            case 7: Set(c, NormType.None, SightBad); c.info = InfoType.Mirror; break;
            case 8: Set(c, NormType.StopSet, SightBad); c.info = InfoType.Mirror; break;
            case 9: Set(c, NormType.None, SightMid); c.attention = AttentionType.ColorPavement; break;
            case 10: Set(c, NormType.StopSet, SightMid); c.attention = AttentionType.ColorPavement; break;
            default: throw new ArgumentOutOfRangeException("no", "要因条件は F1〜F10");
        }
        return c;
    }

    static void Set(TrialCondition c, NormType norm, float sight)
    {
        c.norm = norm;
        c.sightLeft = sight;
        c.sightRight = sight;
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
    public const string ZHeader = "Line,Sign,Mark,RedFlash,D_L,D_R,Mirror,Led,Color,CautionSign";

    public string ZValues()
    {
        int stop = norm == NormType.StopSet ? 1 : 0;
        return string.Join(",", new[]
        {
            stop.ToString(), stop.ToString(), stop.ToString(),
            norm == NormType.RedFlash ? "1" : "0",
            sightLeft.ToString("F2", CultureInfo.InvariantCulture),
            sightRight.ToString("F2", CultureInfo.InvariantCulture),
            info == InfoType.Mirror ? "1" : "0",
            info == InfoType.LedBoard ? "1" : "0",
            attention == AttentionType.ColorPavement ? "1" : "0",
            attention == AttentionType.CautionSign ? "1" : "0",
        });
    }

    public override string ToString()
    {
        return string.Format("{0} [{1}] 規範:{2} 注意喚起:{3} 情報:{4} D_L:{5:F1} D_R:{6:F1} 車:{7}",
            id, kind, norm, attention, info, sightLeft, sightRight, carSpawn);
    }
}
