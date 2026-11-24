using System.Collections.Generic;

// 実験2-b（学習用・第1段階）の試行の並び
//
// ■ 1 セット = 練習 3 ＋ 学習用 24 条件 ＋ 車ありダミー 4（右から 2・左から 2）＝ 31 試行
//   ・練習 3 試行：24 条件からランダムに 3 つ（互いに重ならない・最初の本番の試行とも違う）。
//     そのうち 1 回だけ車が出る（2・3 試行目のどちらか、左右はランダム）。2 試行目以降にするのは、1 試行目で自転車に慣れてもらうため
//   ・ダミー：24 条件のどれかと同じレイアウトに、車を 1 台出すだけ（ミラーには勝手に映る）。
//     24 試行の間のランダムな位置に入れる（起動のたびに変わる）。ダミーどうしは連続せず、本番の最初の試行にもならない。
//     ダミーのレイアウトは、直前・直後の試行と同じにならないようにする
// ■ 学習用 24 条件の順番は、これまでどおり被験者・セットで巡回させる（カウンターバランス）
//     被験者 p・セット s の開始位置 = (p-1) + 8(s-1) 番目
//     → 8人×3セット＝24 回で、どの条件も「何番目に走るか」が 1 回ずつになる
// ■ 練習とダミーの乱数の種（seed）は起動のたびに変わる。Exp2bTrialManager が被験者のフォルダに保存し、
//   途中から再開（startTrial > 1）するときは同じ種を読み直して、同じ並びにする
// ■ 独立の「練習」フェーズ（Practice：3 試行・2 試行目だけ右から車、固定）も残してある
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
    public const int PracticePerSet = 3;
    public const int Seed = 20261111;

    // 独立の練習フェーズ：（レイアウト番号, 車）
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

    // randomSeed：練習とダミーの乱数の種（起動のたびに Exp2bTrialManager が決める）
    public static List<TrialCondition> MainSet(int participant, int set, Exp2bBaseOrder orderType, int randomSeed)
    {
        int n = TrialCondition.FactorialCount;
        int[] baseOrder = BaseOrder(orderType);
        int off = StartOffset(participant, set);
        var mains = new int[n];
        for (int i = 0; i < n; i++) mains[i] = baseOrder[(off + i) % n];

        var rng = new System.Random(randomSeed);

        // ---- 練習 3 試行 ----
        var pPool = new List<int>();
        for (int k = 1; k <= n; k++) if (k != mains[0]) pPool.Add(k);
        Shuffle(pPool, rng);
        int carAt = rng.Next(1, PracticePerSet);                       // 2・3 試行目のどちらか（0 始まりで 1〜2）
        CarSpawn pSide = rng.Next(2) == 0 ? CarSpawn.Left : CarSpawn.Right;
        var list = new List<TrialCondition>();
        for (int i = 0; i < PracticePerSet; i++)
        {
            var c = TrialCondition.Factorial(pPool[i]);
            c.kind = TrialKind.Practice;
            c.carSpawn = i == carAt ? pSide : CarSpawn.None;
            c.id = "P" + (i + 1) + "_F" + pPool[i] + (i == carAt ? (pSide == CarSpawn.Left ? "_L" : "_R") : "");
            list.Add(c);
        }

        // ---- ダミー 4 試行 ----
        // 向き：左 2・右 2 をランダムな順に
        var sides = new[] { CarSpawn.Left, CarSpawn.Left, CarSpawn.Right, CarSpawn.Right };
        Shuffle(sides, rng);

        // 入れる位置：j 番目（0 始まり）の学習用試行の直後。j は 0〜22 から重ならないように 4 つ
        // （同じ j が無いので、ダミーどうしは連続しない。j ≥ 0 なので本番の最初の試行にもならない）
        var gaps = new List<int>();
        for (int j = 0; j < n - 1; j++) gaps.Add(j);
        Shuffle(gaps, rng);
        var insertAfter = gaps.GetRange(0, DummiesPerSet);
        insertAfter.Sort();

        // レイアウト：24 条件から重ならないように 4 つ。直前・直後の試行と同じレイアウトは避ける
        var pool = new List<int>(baseOrder);
        Shuffle(pool, rng);
        var dummyLayout = new int[DummiesPerSet];
        for (int q = 0; q < DummiesPerSet; q++)
        {
            int prev = mains[insertAfter[q]];
            int next = mains[insertAfter[q] + 1];
            int pick = pool.FindIndex(x => x != prev && x != next);
            dummyLayout[q] = pool[pick];
            pool.RemoveAt(pick);
        }

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

    public static List<TrialCondition> Phase(Exp2bPhase phase, int participant, Exp2bBaseOrder orderType, int randomSeed)
    {
        switch (phase)
        {
            case Exp2bPhase.Practice: return Practice();
            case Exp2bPhase.Set1: return MainSet(participant, 1, orderType, randomSeed);
            case Exp2bPhase.Set2: return MainSet(participant, 2, orderType, randomSeed);
            default: return MainSet(participant, 3, orderType, randomSeed);
        }
    }

    // 種を指定しないとき（資料用の書き出しなど）：被験者・セットから決まる種
    public static int DefaultSeed(int participant, Exp2bPhase phase)
    {
        return Seed + 100 * participant + (int)phase;
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
