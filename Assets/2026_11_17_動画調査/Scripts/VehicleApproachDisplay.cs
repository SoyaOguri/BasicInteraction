using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 車両接近LED表示板（作り直し版）
//  ・交差道路の車が交差点（自転車の進路との交点）に threshold 秒以内に着くとき、「車両接近」を点滅させる
//  ・消灯中も文字は暗い色でうっすら見える（何の表示板かは分かるが、光っていない）
//  ・文字の前に LED の粒の模様（dotMask）を重ねて、電光表示らしく見せる
//  ・force で点灯／消灯を固定できる（動画調査で、車を出さずに「点灯」の練習動画を撮るため）
//
//  部品は Editor/LedDisplayBuilder.cs（IntersectionLayout の ⋮ メニュー）が作る
[ExecuteAlways]
public class VehicleApproachDisplay : MonoBehaviour
{
    public enum Force { Auto, On, Off }

    [Header("点灯の判定（ApproachWarningBoard と同じ考え方）")]
    public IntersectionLayout layout;
    [Tooltip("空なら layout の Left Car / Right Car を使う")]
    public Transform[] cars;
    [Tooltip("交差点までの到着時間がこの秒数以内なら点灯")]
    public float threshold = 5f;
    [Tooltip("これより遅い車は止まっているとみなす [m/s]")]
    public float minSpeed = 0.5f;
    [Tooltip("車がこれ以上上下に離れていたら（地下に退避中）無視する [m]")]
    public float maxHeightGap = 10f;
    [Tooltip("Auto = 車の動きで判定。On / Off = 固定（動画調査の練習動画など）")]
    public Force force = Force.Auto;

    [Header("表示")]
    public TextMesh text;
    [Tooltip("表示する文字")]
    public string label = "車両接近";
    [Tooltip("文字を収める範囲 [m]（発光面の大きさより少し小さく）")]
    public Vector2 textArea = new Vector2(1.05f, 0.32f);
    public Color onColor = new Color(1f, 0.55f, 0.08f);
    public Color offColor = new Color(0.16f, 0.13f, 0.11f);
    [Tooltip("点灯中の点滅の周期 [s]。0 なら点滅しない")]
    public float blinkPeriod = 1.0f;
    [Range(0f, 1f)] public float blinkDuty = 0.6f;
    [Tooltip("日本語が出せるフォントの候補（上から順に、この PC に入っているものを使う）")]
    public string[] osFontNames = { "Meiryo", "Yu Gothic", "MS Gothic", "MS PGothic" };
    public int fontSize = 64;
    [Tooltip("文字のシェーダー（Custom/LedTextOccluded）。空なら標準の 3D テキスト用（塀を透けて見えるので非推奨）")]
    public Shader textShader;
    [Tooltip("文字を画像で出す（3D テキストの代わり）。日本語フォントの都合で文字が欠けることがないので、こちらを使う。" +
             "空ならエディタで Textures/LED_車両接近.png を自動で入れる")]
    public Texture2D labelTexture;
    [Tooltip("画像の文字のシェーダー（Custom/LedPanelText）。空ならエディタで自動で入れる")]
    public Shader labelShader;

    [Tooltip("LED の粒の模様の板（空なら子の「LEDの粒」を探す）")]
    public Renderer dotMask;
    [Tooltip("LED の粒の間隔 [m]")]
    public float dotPitch = 0.025f;

    [Header("確認用（読み取り専用）")]
    [SerializeField] bool lit;
    [SerializeField] float nearestEta = float.PositiveInfinity;
    [SerializeField] string usedFont = "";

    Font font;
    Material textMaterial;
    Renderer labelRenderer;
    Material labelMaterial;
    const string LabelTexturePath = "Assets/2026_11_17_動画調査/Textures/LED_車両接近.png";
    bool needsFit = true;
    Vector3[] lastLocal;
    bool[] hasLast;
    float litSince;

    public bool Lit { get { return lit; } }

    // LED の粒の模様は、小さな画像（黒地に丸い穴 1 つ）を発光面いっぱいに繰り返して作る。
    // 繰り返しの回数は作ったときに MaterialPropertyBlock で入れていたが、これはシーンに保存されないので、
    // 開き直すと画像 1 枚が引き伸ばされ、真ん中の楕円の穴からしか文字が見えなくなっていた（左右の文字が欠ける原因）。
    // 毎回ここで入れ直す
    void ApplyDotTiling()
    {
        if (dotMask == null)
        {
            Transform t = transform.Find("LEDの粒");
            if (t != null) dotMask = t.GetComponent<Renderer>();
        }
        if (dotMask == null || dotPitch <= 0f) return;
        Vector3 size = dotMask.transform.localScale;
        var block = new MaterialPropertyBlock();
        dotMask.GetPropertyBlock(block);
        block.SetVector("_MainTex_ST", new Vector4(Mathf.Abs(size.x) / dotPitch, Mathf.Abs(size.y) / dotPitch, 0f, 0f));
        dotMask.SetPropertyBlock(block);
    }

    void OnEnable()
    {
        ApplyDotTiling();
        lastLocal = null;
        SetupText();
        SetLit(force == Force.On);
    }

    // OnValidate の中ではオブジェクトを作れないので、次の Update で作り直す
    bool needsSetup;

    void OnValidate()
    {
        needsFit = true;
        needsSetup = true;
    }

    // 文字を画像の板で出す。3D テキストは隠す
    bool SetupLabelImage()
    {
#if UNITY_EDITOR
        bool changed = false;
        if (labelTexture == null) { labelTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(LabelTexturePath); changed |= labelTexture != null; }
        if (labelShader == null) { labelShader = Shader.Find("Custom/LedPanelText"); changed |= labelShader != null; }
        if (changed && !Application.isPlaying) EditorUtility.SetDirty(this);
#endif
        if (labelTexture == null || labelShader == null || text == null) return false;
        if (labelRenderer == null)
        {
            // 前に作った板が残っていれば使い回す（有効化の途中は消せないため）
            Transform old = text.transform.parent != null ? text.transform.parent.Find("文字（画像）") : null;
            GameObject go;
            if (old != null) go = old.gameObject;
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = "文字（画像）";
                go.hideFlags = HideFlags.DontSave;   // ここで作るので、シーンには保存しない
                var col = go.GetComponent<Collider>();
                if (col != null) DestroyImmediate(col);
                go.transform.SetParent(text.transform.parent, false);
            }
            labelRenderer = go.GetComponent<Renderer>();
            labelMaterial = new Material(labelShader) { hideFlags = HideFlags.DontSave, mainTexture = labelTexture };
            labelRenderer.sharedMaterial = labelMaterial;
        }
        Transform q = labelRenderer.transform;
        q.localPosition = text.transform.localPosition;
        q.localRotation = text.transform.localRotation;
        float aspect = (float)labelTexture.width / Mathf.Max(1, labelTexture.height);
        float w = Mathf.Min(textArea.x, textArea.y * aspect);
        q.localScale = new Vector3(w, w / aspect, 1f);
        var tr = text.GetComponent<MeshRenderer>();
        if (tr != null) tr.enabled = false;
        return true;
    }

    void SetupText()
    {
        if (text == null) return;
        if (SetupLabelImage()) return;
        if (font == null)
        {
            string found = null;
            var installed = new System.Collections.Generic.HashSet<string>(Font.GetOSInstalledFontNames());
            foreach (var n in osFontNames) if (installed.Contains(n)) { found = n; break; }
            if (found != null)
            {
                font = Font.CreateDynamicFontFromOSFont(found, fontSize);
                font.hideFlags = HideFlags.DontSave;
            }
            usedFont = found ?? "（日本語のフォントが見つからない）";
        }
        if (font != null)
        {
            text.font = font;
            var r = text.GetComponent<MeshRenderer>();
            if (r != null)
            {
                if (textShader != null)
                {
                    if (textMaterial == null)
                    {
                        textMaterial = new Material(textShader) { hideFlags = HideFlags.DontSave };
                        // 動的フォントは文字を足すとテクスチャが作り直されるので、そのたびに差し替える
                        Font.textureRebuilt += OnFontTextureRebuilt;
                    }
                    textMaterial.mainTexture = font.material.mainTexture;
                    r.sharedMaterial = textMaterial;
                }
                else r.sharedMaterial = font.material;
            }
        }
        text.text = label;
        text.fontSize = fontSize;
        text.characterSize = 1f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        needsFit = true;
    }

    // 動的フォントのテクスチャが作り直されると、文字の並び（UV）が変わる。
    // テクスチャだけ差し替えて文字のメッシュを作り直さないと、古い UV のまま描かれて文字が欠ける（左右が切れて見える）
    bool textDirty;

    void OnFontTextureRebuilt(Font f)
    {
        if (f != font) return;
        if (textMaterial != null) textMaterial.mainTexture = f.material.mainTexture;
        textDirty = true;
    }

    // 毎フレーム、使う文字がテクスチャに入っていることを確かめ、作り直されていたら文字のメッシュも作り直す
    void RefreshGlyphs()
    {
        if (text == null || font == null || string.IsNullOrEmpty(label)) return;
        font.RequestCharactersInTexture(label, fontSize, FontStyle.Normal);
        if (!textDirty) return;
        textDirty = false;
        if (textMaterial != null) textMaterial.mainTexture = font.material.mainTexture;
        text.text = "";
        text.text = label;
    }

    void OnDestroy()
    {
        Font.textureRebuilt -= OnFontTextureRebuilt;
    }

    void Update()
    {
        if (needsSetup) { needsSetup = false; SetupText(); }
        RefreshGlyphs();
        if (force != Force.Auto) SetLit(force == Force.On);
        else if (Application.isPlaying) SetLit(Detect());
        else SetLit(false);

        if (text != null)
        {
            bool bright = lit;
            if (lit && blinkPeriod > 0f)
            {
                float t = Application.isPlaying ? Time.time - litSince : 0f;
                bright = Mathf.Repeat(t, blinkPeriod) / blinkPeriod < blinkDuty;
            }
            text.color = bright ? onColor : offColor;
            if (labelMaterial != null) labelMaterial.color = bright ? onColor : offColor;
        }
    }

    void LateUpdate()
    {
        // 文字の大きさを textArea に合わせる。
        // TextMesh には大きさを読めるメッシュが無いので、フォントの文字幅から計算する
        // （TextMesh は characterSize = 1 のとき、フォントの 10 画素がおよそ 1 m になる）
        if (needsCheck) { LateUpdateCheck(); return; }
        if (!needsFit || text == null || font == null || string.IsNullOrEmpty(label)) return;
        font.RequestCharactersInTexture(label, fontSize, FontStyle.Normal);
        float widthPx = 0f, heightPx = 0f;
        foreach (char ch in label)
        {
            CharacterInfo ci;
            if (!font.GetCharacterInfo(ch, out ci, fontSize, FontStyle.Normal)) return;   // まだ用意できていない → 次のフレームで
            widthPx += ci.advance;
            heightPx = Mathf.Max(heightPx, ci.glyphHeight);
        }
        if (widthPx < 1f || heightPx < 1f) return;
        float w = widthPx * 0.1f, h = heightPx * 0.1f;   // characterSize = 1 のときの大きさ [m]
        float s = Mathf.Min(textArea.x / w, textArea.y / h);
        text.transform.localScale = new Vector3(s, s, s);
        expectedHeight = h * s;
        needsFit = false;
        needsCheck = true;
    }

    // 補正：表示されたあとの実際の文字の高さ（板は垂直に立っているので、ワールドの高さ＝文字の高さ）を測り、
    // 計算とずれていたら 1 回だけ合わせ直す
    float expectedHeight;
    bool needsCheck;

    void LateUpdateCheck()
    {
        if (!needsCheck || text == null) return;
        var r = text.GetComponent<MeshRenderer>();
        if (r == null) return;
        float actual = r.bounds.size.y;
        if (actual < 1e-4f) return;
        float k = expectedHeight * transform.lossyScale.y / actual;
        if (k < 0.8f || k > 1.25f) text.transform.localScale *= k;
        needsCheck = false;
    }

    void SetLit(bool on)
    {
        if (on && !lit) litSince = Application.isPlaying ? Time.time : 0f;
        lit = on;
    }

    bool Detect()
    {
        if (layout == null) return false;
        Transform[] t = (cars != null && cars.Length > 0) ? cars : new[] { layout.leftCar, layout.rightCar };
        if (lastLocal == null || lastLocal.Length != t.Length)
        {
            lastLocal = new Vector3[t.Length];
            hasLast = new bool[t.Length];
        }
        float best = float.PositiveInfinity;
        float dt = Time.deltaTime;
        for (int i = 0; i < t.Length; i++)
        {
            if (t[i] == null || !t[i].gameObject.activeInHierarchy) { hasLast[i] = false; continue; }
            Vector3 p = layout.transform.InverseTransformPoint(t[i].position);
            if (Mathf.Abs(p.y) > maxHeightGap) { hasLast[i] = false; continue; }
            if (hasLast[i] && dt > 0f)
            {
                float vx = (p.x - lastLocal[i].x) / dt;
                bool approaching = p.x * vx < 0f;
                if (approaching && Mathf.Abs(vx) > minSpeed)
                {
                    float eta = Mathf.Abs(p.x) / Mathf.Abs(vx);
                    if (eta < best) best = eta;
                }
            }
            lastLocal[i] = p;
            hasLast[i] = true;
        }
        nearestEta = best;
        return best <= threshold;
    }
}
