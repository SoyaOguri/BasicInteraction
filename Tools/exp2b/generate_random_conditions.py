"""実験2-b ランダム条件 48 パターンの生成（乱数の種を固定して 1 回だけ生成し、全被験者で共通に使う）

使い方:  python generate_random_conditions.py
出力:    Assets/2026_11_06_複数インフラ/Conditions/random_conditions.csv
         Assets/2026_11_06_複数インフラ/Conditions/random_conditions_report.txt

■ 配分ルール（スライド「ランダム条件（48パターン）」を、実験環境に合わせて調整）
  規範      なし / 停止線＋標識＋路面表示 / 赤点滅信号 から 1 つ ........ 各 16
  見通し    D_L・D_R とも 11–27 m を 3 区分（11–16.33, 16.33–21.67, 21.67–27）に各 16。
            区分内は均等に配置（層別＋ゆらぎ）、0.1 m 単位。左右の組み合わせはランダム。
            ※ 右の見通しは塀を建物線の奥に置く限り 10.97 m 未満にできないため、
              左右とも要因条件の「悪い 11 m」から「良い 27 m」の範囲にした。
  情報      なし 24・ミラー 12・LED 12。ミラー・LED は見通しの短い方が 18 m 未満のときだけ
  注意喚起  なし 24・カラー舗装 12・注意看板 12
  情報×注意喚起  両方あり 12・情報のみ 12・注意喚起のみ 12・どちらもなし 12
  1 つの軸に置くのは 1 種類まで（enum なので自動的に満たす）
  規範は 4 つのセルそれぞれに 4 ずつ（両方ありのセルは 情報2×注意喚起2×規範3 の 12 通りを 1 回ずつ）
  セット A〜D（各 12）には、各セルから 3、規範 4/4/4、ミラー・LED・カラー・看板 各 3、
  見通しの各区分 左右とも 4 ずつ
  生成後に属性間の相関を確認（同じ軸どうし、情報と見通しの組は除いて |r| < 0.1 を目安）
"""
import csv, os, random, math
import numpy as np

SEED = 20261107
HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
OUTDIR = os.path.join(ROOT, "Assets", "2026_11_06_複数インフラ", "Conditions")
D_MIN, D_MAX = 11.0, 27.0
EDGES = [D_MIN, D_MIN + (D_MAX - D_MIN) / 3, D_MIN + 2 * (D_MAX - D_MIN) / 3, D_MAX]
INFO_SIGHT_LIMIT = 18.0
SETS = "ABCD"

rng = random.Random(SEED)
np_rng = np.random.default_rng(SEED)


# ---------- 1. 規範・情報・注意喚起の組（48 行） ----------
def infra_rows():
    rows = []
    norms = ["None", "StopSet", "RedFlash"]
    # 両方あり：情報2 × 注意喚起2 × 規範3 = 12 通りを 1 回ずつ
    for info in ["Mirror", "LedBoard"]:
        for att in ["ColorPavement", "CautionSign"]:
            for n in norms:
                rows.append(dict(cell="both", info=info, attention=att, norm=n))
    # 情報のみ：情報2 × 規範3 を 2 回ずつ
    for info in ["Mirror", "LedBoard"]:
        for n in norms:
            for _ in range(2):
                rows.append(dict(cell="info", info=info, attention="None", norm=n))
    # 注意喚起のみ：注意喚起2 × 規範3 を 2 回ずつ
    for att in ["ColorPavement", "CautionSign"]:
        for n in norms:
            for _ in range(2):
                rows.append(dict(cell="attention", info="None", attention=att, norm=n))
    # どちらもなし：規範3 を 4 回ずつ
    for n in norms:
        for _ in range(4):
            rows.append(dict(cell="none", info="None", attention="None", norm=n))
    assert len(rows) == 48
    return rows


# ---------- 2. 見通しの値（左右それぞれ、各区分 16 個を層別に） ----------
def sight_values():
    vals = []
    for b in range(3):
        a, z = EDGES[b], EDGES[b + 1]
        for i in range(16):
            u = rng.random()
            v = a + (i + u) / 16.0 * (z - a)
            v = min(max(round(v, 1), D_MIN), D_MAX)
            vals.append(v)
    return vals


def bin_of(v):
    if v < EDGES[1]:
        return 1
    if v < EDGES[2]:
        return 2
    return 3


# ---------- 3. 評価関数 ----------
FEATURES = ["Stop", "RedFlash", "D_L", "D_R", "Mirror", "Led", "Color", "Caution"]
AXIS = {"Stop": "norm", "RedFlash": "norm", "D_L": "sight", "D_R": "sight",
        "Mirror": "info", "Led": "info", "Color": "att", "Caution": "att"}


def feature_matrix(rows, dl, dr):
    m = np.zeros((48, len(FEATURES)))
    for i, r in enumerate(rows):
        m[i] = [r["norm"] == "StopSet", r["norm"] == "RedFlash", dl[i], dr[i],
                r["info"] == "Mirror", r["info"] == "LedBoard",
                r["attention"] == "ColorPavement", r["attention"] == "CautionSign"]
    return m


def checked_pairs():
    out = []
    for i in range(len(FEATURES)):
        for j in range(i + 1, len(FEATURES)):
            a, b = FEATURES[i], FEATURES[j]
            if AXIS[a] == AXIS[b] and AXIS[a] != "sight":
                continue  # 同じ軸の排他的な属性どうしは負の相関が当然出るので除く
            if {AXIS[a], AXIS[b]} == {"info", "sight"}:
                continue  # ミラー・LED と見通しの相関は設置条件を反映したものとして報告のみ
            out.append((i, j))
    return out


PAIRS = checked_pairs()


def score(rows, dl, dr, sets):
    s = 0.0
    for i, r in enumerate(rows):
        if r["info"] != "None" and min(dl[i], dr[i]) >= INFO_SIGHT_LIMIT:
            s += 1000.0
    c = np.corrcoef(feature_matrix(rows, dl, dr), rowvar=False)
    for i, j in PAIRS:
        r = abs(c[i, j])
        s += 50.0 * max(0.0, r - 0.1) + 2.0 * r
    # ミラーと LED は同じ見通しの範囲で比べたいので、左右それぞれの平均見通しをそろえる
    mi = [i for i in range(48) if rows[i]["info"] == "Mirror"]
    li = [i for i in range(48) if rows[i]["info"] == "LedBoard"]
    for d in (dl, dr):
        s += 2.0 * abs(np.mean([d[i] for i in mi]) - np.mean([d[i] for i in li]))
    s += 2.0 * abs(np.mean([min(dl[i], dr[i]) for i in mi]) - np.mean([min(dl[i], dr[i]) for i in li]))
    for k in SETS:
        idx = [i for i in range(48) if sets[i] == k]
        cnt = lambda f: sum(1 for i in idx if f(i))
        for n in ["None", "StopSet", "RedFlash"]:
            s += 5 * abs(cnt(lambda i: rows[i]["norm"] == n) - 4)
        for key, val in [("info", "Mirror"), ("info", "LedBoard"), ("attention", "ColorPavement"), ("attention", "CautionSign")]:
            s += 5 * abs(cnt(lambda i: rows[i][key] == val) - 3)
        for cell in ["both", "info", "attention", "none"]:
            s += 5 * abs(cnt(lambda i: rows[i]["cell"] == cell) - 3)
        for b in (1, 2, 3):
            s += 2 * abs(cnt(lambda i: bin_of(dl[i]) == b) - 4)
            s += 2 * abs(cnt(lambda i: bin_of(dr[i]) == b) - 4)
    return s


# ---------- 4. 山登り（乱数の種固定なので毎回同じ結果） ----------
def optimise():
    rows = infra_rows()
    rng.shuffle(rows)
    dl = sight_values(); rng.shuffle(dl)
    dr = sight_values(); rng.shuffle(dr)
    sets = [SETS[i % 4] for i in range(48)]; rng.shuffle(sets)
    best = score(rows, dl, dr, sets)
    temp = 2.0
    for step in range(60000):
        kind = rng.random()
        i, j = rng.sample(range(48), 2)
        arr = dl if kind < 0.35 else dr if kind < 0.7 else sets
        arr[i], arr[j] = arr[j], arr[i]
        new = score(rows, dl, dr, sets)
        if new <= best or rng.random() < math.exp(-(new - best) / max(temp, 1e-6)):
            best = new
        else:
            arr[i], arr[j] = arr[j], arr[i]
        temp *= 0.9999
    return rows, dl, dr, sets, best


def main():
    rows, dl, dr, sets, best = optimise()
    order = sorted(range(48), key=lambda i: (sets[i], rows[i]["cell"], rows[i]["norm"], rows[i]["info"], rows[i]["attention"]))
    os.makedirs(OUTDIR, exist_ok=True)
    csv_path = os.path.join(OUTDIR, "random_conditions.csv")
    with open(csv_path, "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow(["id", "set", "norm", "sightLeft", "sightRight", "info", "attention", "cell", "binLeft", "binRight"])
        for k, i in enumerate(order):
            r = rows[i]
            w.writerow(["R%02d" % (k + 1), sets[i], r["norm"], "%.1f" % dl[i], "%.1f" % dr[i],
                        r["info"], r["attention"], r["cell"], bin_of(dl[i]), bin_of(dr[i])])

    # ---------- レポート ----------
    lines = ["seed = %d, final score = %.3f" % (SEED, best),
             "sight bins (m): " + ", ".join("%.2f" % e for e in EDGES), ""]
    m = feature_matrix(rows, dl, dr)
    c = np.corrcoef(m, rowvar=False)
    lines.append("correlation matrix (|r| >= 0.1 marked *; '-' = same axis or info x sight, not targeted)")
    lines.append("          " + " ".join("%8s" % f for f in FEATURES))
    pairset = set(PAIRS)
    for i, f in enumerate(FEATURES):
        cells = []
        for j in range(len(FEATURES)):
            if i == j:
                cells.append("%8s" % "1")
                continue
            key = (min(i, j), max(i, j))
            mark = "*" if key in pairset and abs(c[i, j]) >= 0.1 else ("-" if key not in pairset else " ")
            cells.append("%7.3f%s" % (c[i, j], mark))
        lines.append("%-10s" % f + " ".join(cells))
    viol = [i for i in range(48) if rows[i]["info"] != "None" and min(dl[i], dr[i]) >= INFO_SIGHT_LIMIT]
    lines += ["", "mirror/LED with min(D) >= %.0f m: %d" % (INFO_SIGHT_LIMIT, len(viol)), ""]
    lines.append("per set: norm(None/Stop/Red)  Mirror Led Color Caution  cells(both/info/att/none)  binsL  binsR")
    for k in SETS:
        idx = [i for i in range(48) if sets[i] == k]
        cnt = lambda f: sum(1 for i in idx if f(i))
        lines.append("  %s: %d/%d/%d   %d %d %d %d   %d/%d/%d/%d   %s  %s" % (
            k,
            cnt(lambda i: rows[i]["norm"] == "None"), cnt(lambda i: rows[i]["norm"] == "StopSet"), cnt(lambda i: rows[i]["norm"] == "RedFlash"),
            cnt(lambda i: rows[i]["info"] == "Mirror"), cnt(lambda i: rows[i]["info"] == "LedBoard"),
            cnt(lambda i: rows[i]["attention"] == "ColorPavement"), cnt(lambda i: rows[i]["attention"] == "CautionSign"),
            cnt(lambda i: rows[i]["cell"] == "both"), cnt(lambda i: rows[i]["cell"] == "info"),
            cnt(lambda i: rows[i]["cell"] == "attention"), cnt(lambda i: rows[i]["cell"] == "none"),
            "/".join(str(cnt(lambda i, b=b: bin_of(dl[i]) == b)) for b in (1, 2, 3)),
            "/".join(str(cnt(lambda i, b=b: bin_of(dr[i]) == b)) for b in (1, 2, 3))))
    lines.append("")
    lines.append("D_L mean %.2f  D_R mean %.2f  |  info rows min(D): %.1f - %.1f" % (
        np.mean(dl), np.mean(dr),
        min(min(dl[i], dr[i]) for i in range(48) if rows[i]["info"] != "None"),
        max(min(dl[i], dr[i]) for i in range(48) if rows[i]["info"] != "None")))
    for name, key in (("Mirror", "Mirror"), ("LED", "LedBoard")):
        idx = [i for i in range(48) if rows[i]["info"] == key]
        lines.append("%-6s n=%d  mean D_L %.2f  mean D_R %.2f  mean min(D) %.2f" % (
            name, len(idx), np.mean([dl[i] for i in idx]), np.mean([dr[i] for i in idx]),
            np.mean([min(dl[i], dr[i]) for i in idx])))
    rep = "\n".join(lines)
    with open(os.path.join(OUTDIR, "random_conditions_report.txt"), "w", encoding="utf-8", newline="\n") as f:
        f.write(rep + "\n")
    print(rep)
    print("\nwrote", csv_path)


if __name__ == "__main__":
    main()
