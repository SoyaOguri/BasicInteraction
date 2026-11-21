using System.Collections.Generic;

// 実験2-b（学習用・第1段階）の試行の並び
//
// ■ 1 セット = 学習用 24 条件 ＋ 車ありダミー 4（右から 2・左から 2）＝ 28 試行
//   ・ダミーは 24 条件のどれかと同じレイアウトに、車を 1 台出すだけ（ミラーには勝手に映る）
//   ・24 試行を 6 試行ずつ 4 つに分け、それぞれの中に 1 つずつ入れる（連続しない・1 試行目にはならない）
// ■ 被験者 8 人 × 3 セット
//   ・基本の並び（BaseOrder）を 1 つ決め、開始位置をずらして巡回させる
//     被験者 p・セット s の開始位置 = (p-1) + 8(s-1) 番目
//     → 被験者1 は 1・9・17 番目、被験者2 は 2・10・18 番目 … から始まり、
//       8人×3セット＝24 回で、どの条件も「何番目に走るか」が 1 回ずつになる
// ■ 練習 3 試行：学習用のレイアウトを使い、2 試行目だけ右から車が出る（固定）
public enum Exp2bPhase { Practice, Set1, Set2, Set3 }
public enum Exp2bBaseOrder
{
    FixedShuffle,   // F1〜F24 を乱数の種固定で並べ替えたもの（おすすめ：規範あり／なしが前半・後半に偏らない）
    Numeric         // F1, F2, …, F24 の順そのまま
}

public static class Exp2bSchedule
{
    public const int Participants = 8;
    public const int Sets = 3;
    public const int DummiesPerSet = 4;
    public const int Seed = 20261111;

    // 練習：（レイアウト番号, 車）
    static readonly int[] PracticeLayouts = { 5, 11, 17 };          // 規範なし・中 / 規範なし・悪い・ミラー / 規範あり・中
    static readonly CarSpawn[] PracticeCars = { CarSpawn.None, CarSpawn.Right, CarSpawn.None };

    public static int[] BaseOrder(Exp2bBaseOrder type)
    {
        int n = TrialCondition.FactorialCount;
        var a = new int[n];
        for (int i = 0; i < n; i++) a[i] = i + 1;
        if (type == Exp2bBaseOrder.FixedShuffle)
        {
            var rng = new System.Random(Seed);
            Shuffle(a, rng);
        }
        return a;
    }

    public static int StartOffset(int participant, int set)
    {
        return ((participant - 1) + Participants * (set - 1)) % TrialCondition.FactorialCount;
    }

    public static List<TrialCondition> Practice()
    {
        var list = new List<TrialCondition>();
        for (int i = 0; i < PracticeLayouts.Length; i++)
        {
            var c = TrialCondition.Factorial(PracticeLayouts[i]);
            c.kind = TrialKind.Practice;
            c.carSpawn = PracticeCars[i];
            c.id = "P" + (i + 1) + "_F" + PracticeLayouts[i];
            list.Add(c);
        }
        return list;
    }

    public static List<TrialCondition> MainSet(int participant, int set, Exp2bBaseOrder orderType)
    {
        int n = TrialCondition.FactorialCount;
        int[] baseOrder = BaseOrder(orderType);
        int off = StartOffset(participant, set);
        var mains = new int[n];
        for (int i = 0; i < n; i++) mains[i] = baseOrder[(off + i) % n];

        var rng = new System.Random(Seed + 100 * participant + set);

        // ダミーの車の向き：左 2・右 2 をランダムな順に
        var sides = new[] { CarSpawn.Left, CarSpawn.Left, CarSpawn.Right, CarSpawn.Right };
        Shuffle(sides, rng);

        // ダミーを入れる位置：6 試行ごとの区切りの中で、j 番目（0 始まり）の学習用試行の直後
        int block = n / DummiesPerSet;
        var insertAfter = new int[DummiesPerSet];
        for (int q = 0; q < DummiesPerSet; q++) insertAfter[q] = q * block + rng.Next(0, block - 1);

        // ダミーのレイアウト：24 条件から重ならないように 4 つ。直前の試行と同じレイアウトは避ける
        var pool = new List<int>(baseOrder);
        Shuffle(pool, rng);
        var dummyLayout = new int[DummiesPerSet];
        for (int q = 0; q < DummiesPerSet; q++)
        {
            int prev = mains[insertAfter[q]];
            int pick = pool.FindIndex(x => x != prev);
            dummyLayout[q] = pool[pick];
            pool.RemoveAt(pick);
        }

        var list = new List<TrialCondition>();
        int d = 0;
        for (int j = 0; j < n; j++)
        {
            list.Add(TrialCondition.Factorial(mains[j]));
            if (d < DummiesPerSet && insertAfter[d] == j)
            {
                var c = TrialCondition.Factorial(dummyLayout[d]);
                c.kind = TrialKind.Dummy;
                c.carSpawn = sides[d];
                c.id = "D" + (d + 1) + "_F" + dummyLayout[d] + "_" + (sides[d] == CarSpawn.Left ? "L" : "R");
                list.Add(c);
                d++;
            }
        }
        return list;
    }

    public static List<TrialCondition> Phase(Exp2bPhase phase, int participant, Exp2bBaseOrder orderType)
    {
        switch (phase)
        {
            case Exp2bPhase.Practice: return Practice();
            case Exp2bPhase.Set1: return MainSet(participant, 1, orderType);
            case Exp2bPhase.Set2: return MainSet(participant, 2, orderType);
            default: return MainSet(participant, 3, orderType);
        }
    }

    static void Shuffle<T>(IList<T> a, System.Random rng)
    {
        for (int i = a.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            T t = a[i]; a[i] = a[j]; a[j] = t;
        }
    }
}
