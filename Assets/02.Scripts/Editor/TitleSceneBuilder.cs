#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

// 타이틀 씬(Assets/01.Scenes/TitleScene.unity)을 만든다. 먼저 "1. 타이틀 에셋 생성"을 실행해 두어야 한다.
// 메뉴: Tools > Vesper > 2. 타이틀 씬 만들기. 다시 실행하면 씬을 처음부터 다시 만들고, 빌드 설정에 TitleScene(0번), CityScene(1번)을 넣는다.
// 모든 좌표는 캔버스 단위(1920x1080)이며 왼쪽 위가 원점, y는 아래로 늘어난다.
public static class TitleSceneBuilder
{
    private const string ScenePath = "Assets/01.Scenes/TitleScene.unity";
    private const string GameScenePath = "Assets/01.Scenes/CityScene.unity";
    private const string FontDir = "Assets/08.Fonts/Title/";
    private const float CanvasPpu = 100f; // 캔버스 기준 PPU. 스프라이트 PPU가 1이라 Sliced 테두리를 1:1로 그리려면 이 값을 곱해 준다.

    private static readonly Color Amber = new Color(1f, 0.72f, 0.26f, 1f);
    private static readonly Color Blue = new Color(0.25f, 0.5f, 1f, 1f);

    [MenuItem("Tools/Vesper/2. 타이틀 씬 만들기")]
    public static void Build()
    {
        TMP_FontAsset bodyFont = GetOrCreateFont("NanumMyeongjo-Regular.ttf", "NanumMyeongjo SDF.asset", 48, 6, "아이디비밀번호로그인회원가입어둠이내리면불을밝혀라 ");

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 카메라(오버레이 캔버스만 쓰지만 카메라가 없으면 경고가 뜬다)
        GameObject camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        Camera cam = camGo.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;

        // 이벤트 시스템(게임 씬과 같은 새 Input System UI 모듈)
        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

        // 캔버스
        GameObject canvasGo = new GameObject("TitleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        Transform root = canvasGo.transform;

        // ── 배경 (바깥 여분 16칸 = 캔버스 32) ──
        Image sky = Layer(root, "Sky", "title_sky");
        Image far = Layer(root, "CityFar", "title_city_far");
        RawImage fogFar = Fog(root, "FogFar", "title_fog_a", 0.2f);
        Image mid = Layer(root, "CityMid", "title_city_mid");
        RawImage fogMid = Fog(root, "FogMid", "title_fog_c", 0.22f);
        Image near = Layer(root, "CityNear", "title_city_near");

        // 꺼져 가는 가로등의 빛: 가까운 건물 층의 자식이라 시차로 함께 움직인다. 가로등 머리는 캔버스 (550, 598).
        Vector2 lamp = new Vector2(550f + 32f, 598f + 32f);
        Image cone = Img(near.transform, "LampCone", Spr("title_cone"), lamp.x - 280f, lamp.y + 12f, 560f, 340f);
        cone.color = new Color(1f, 0.74f, 0.32f, 0f);
        Image groundGlow = Img(near.transform, "GroundGlow", Spr("title_glow"), lamp.x - 420f, lamp.y + 300f, 840f, 150f);
        groundGlow.color = new Color(1f, 0.66f, 0.26f, 0f);
        Image lampGlow = Img(near.transform, "LampGlow", Spr("title_glow"), lamp.x - 260f, lamp.y - 260f, 520f, 520f);
        lampGlow.color = new Color(1f, 0.74f, 0.3f, 0f);
        // 번개 줄기: 건물에 가려지지 않게 도시 층 앞(안개와 눈 뒤)에 둔다. 위쪽 가운데가 기준이고 쳐질 때마다 코드가 위치와 방향을 바꾼다.
        Image boltImage = Img(root, "LightningBolt", Spr("title_bolt"), 0f, 0f, 250f, 820f);
        boltImage.rectTransform.pivot = new Vector2(0.5f, 1f);
        boltImage.rectTransform.anchoredPosition = new Vector2(960f, 0f);
        boltImage.color = new Color(1f, 1f, 1f, 0f);

        // 빨간 눈
        RectTransform eyeRoot = Rect("RedEyes", root, 0f, 0f, 1920f, 1080f);
        UI_RedEyes eyes = eyeRoot.gameObject.AddComponent<UI_RedEyes>();

        RawImage fogFront = Fog(root, "FogFront", "title_fog_b", 0.24f);
        RawImage fogWisp = Fog(root, "FogWisp", "title_fog_a", 0.15f);

        Image lightning = Img(root, "Lightning", null, 0f, 0f, 1920f, 1080f);
        lightning.color = new Color(0.62f, 0.76f, 1f, 0f);

        RectTransform rainRoot = Rect("Rain", root, 0f, 0f, 1920f, 1080f);
        UI_Rain rain = rainRoot.gameObject.AddComponent<UI_Rain>();

        RectTransform emberRoot = Rect("Embers", root, 0f, 0f, 1920f, 1080f);
        RectTransform lampOrigin = Rect("LampOrigin", emberRoot, 550f, 598f, 4f, 4f);
        UI_TitleEmbers embers = emberRoot.gameObject.AddComponent<UI_TitleEmbers>();

        Img(root, "Vignette", Spr("title_vignette"), 0f, 0f, 1920f, 1080f);

        // ── 제목 ──
        CanvasGroup titleGroup = Group(root, "Title", 0f, 0f, 1920f, 1080f);
        RectTransform titleBase = Rect("TitleBase", titleGroup.transform, 0f, 0f, 1920f, 1080f);
        Image titleGlow = Img(titleBase, "TitleGlow", Spr("title_glow"), 960f - 760f, 242f - 220f, 1520f, 440f);
        titleGlow.color = new Color(1f, 0.62f, 0.22f, 0f);

        // 제목 "VESPER": 도트 하나가 캔버스 4칸인 픽셀 글자 그림(318x78도트 → 1272x312)
        Img(titleBase, "TitleText", Spr("title_vesper"), 960f - 636f, 86f, 1272f, 312f);

        Img(titleBase, "Ornament", Spr("title_ornament"), 960f - 360f, 372f, 720f, 36f);
        TMP_Text tagline = Text(titleBase, "Tagline", bodyFont, "어둠이 내리면, 불을 밝혀라", 28f, new Color(0.55f, 0.66f, 0.88f, 0.85f), TextAlignmentOptions.Center);
        tagline.rectTransform.anchorMin = tagline.rectTransform.anchorMax = tagline.rectTransform.pivot = new Vector2(0f, 1f);
        tagline.rectTransform.anchoredPosition = new Vector2(560f, -420f);
        tagline.rectTransform.sizeDelta = new Vector2(800f, 44f);
        tagline.characterSpacing = 10f;

        // ── 로그인 창 ──
        CanvasGroup loginGroup = Group(root, "Login", 640f, 478f, 640f, 400f);
        Image panel = Img(loginGroup.transform, "Panel", Spr("ui_panel"), 0f, 0f, 640f, 400f);
        panel.type = Image.Type.Sliced;
        panel.pixelsPerUnitMultiplier = CanvasPpu;

        TMP_InputField idField = InputField(loginGroup.transform, "IdField", bodyFont, "아이디", 80f, 56f, false);
        TMP_InputField pwField = InputField(loginGroup.transform, "PasswordField", bodyFont, "비밀번호", 80f, 148f, true);
        Button loginButton = TitleButton(loginGroup.transform, "LoginButton", bodyFont, "로그인", 80f, 264f);
        Button signUpButton = TitleButton(loginGroup.transform, "SignUpButton", bodyFont, "회원가입", 332f, 264f);

        // ── 섬광과 페이드(맨 위) ──
        Image flash = Img(root, "Flash", null, 0f, 0f, 1920f, 1080f);
        flash.color = new Color(1f, 0.86f, 0.5f, 0f);
        flash.raycastTarget = false;

        GameObject fadeGo = new GameObject("Fader", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(UI_ScreenFader));
        fadeGo.transform.SetParent(root, false);
        Stretch(fadeGo.GetComponent<RectTransform>());
        fadeGo.GetComponent<Image>().color = Color.black;
        fadeGo.GetComponent<CanvasGroup>().alpha = 1f;
        UI_ScreenFader fader = fadeGo.GetComponent<UI_ScreenFader>();

        // ── 연결 ──
        GameObject controller = new GameObject("TitleController", typeof(UI_Title), typeof(UI_Login));
        UI_Title titleCtrl = controller.GetComponent<UI_Title>();
        UI_Login login = controller.GetComponent<UI_Login>();

        SerializedObject st = new SerializedObject(titleCtrl);
        Ref(st, "_fader", fader);
        Ref(st, "_lampGlow", lampGlow);
        Ref(st, "_lampCone", cone);
        Ref(st, "_groundGlow", groundGlow);
        Ref(st, "_titleGroup", titleGroup);
        Ref(st, "_titleRect", titleBase);
        Ref(st, "_titleGlow", titleGlow);
        Ref(st, "_loginGroup", loginGroup);
        Ref(st, "_loginRect", loginGroup.GetComponent<RectTransform>());
        Ref(st, "_lightning", lightning);
        Ref(st, "_bolt", boltImage.rectTransform);

        // 마우스 시차: 멀수록 조금, 가까울수록 많이 움직인다.
        RectTransform[] parallaxRects = { sky.rectTransform, far.rectTransform, mid.rectTransform, near.rectTransform, eyeRoot };
        Vector2[] parallaxStrength = { new Vector2(6f, 3f), new Vector2(12f, 6f), new Vector2(20f, 10f), new Vector2(34f, 16f), new Vector2(34f, 16f) };
        SerializedProperty layers = st.FindProperty("_parallaxLayers");
        layers.arraySize = parallaxRects.Length;
        for (int i = 0; i < parallaxRects.Length; i++)
        {
            SerializedProperty e = layers.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("Rect").objectReferenceValue = parallaxRects[i];
            e.FindPropertyRelative("Strength").vector2Value = parallaxStrength[i];
        }

        // 안개: 속도, 일렁이는 폭, 일렁이는 속도, 짙기 변화
        RawImage[] fogs = { fogFar, fogMid, fogFront, fogWisp };
        float[][] fogParams =
        {
            new[] { 0.03f, 0.5f, 14f, 0.30f, 0.12f },
            new[] { 0.055f, 0.55f, 18f, 0.38f, 0.15f },
            new[] { 0.10f, 0.6f, 22f, 0.45f, 0.2f },
            new[] { 0.17f, 0.7f, 26f, 0.55f, 0.25f },
        };
        SerializedProperty fogProp = st.FindProperty("_fogLayers");
        fogProp.arraySize = fogs.Length;
        for (int i = 0; i < fogs.Length; i++)
        {
            SerializedProperty e = fogProp.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("Image").objectReferenceValue = fogs[i];
            e.FindPropertyRelative("Speed").floatValue = fogParams[i][0];
            e.FindPropertyRelative("Gust").floatValue = fogParams[i][1];
            e.FindPropertyRelative("BobAmplitude").floatValue = fogParams[i][2];
            e.FindPropertyRelative("BobSpeed").floatValue = fogParams[i][3];
            e.FindPropertyRelative("AlphaPulse").floatValue = fogParams[i][4];
        }
        st.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject sl = new SerializedObject(login);
        Ref(sl, "_title", titleCtrl);
        Ref(sl, "_fader", fader);
        Ref(sl, "_loginGroup", loginGroup);
        Ref(sl, "_idField", idField);
        Ref(sl, "_passwordField", pwField);
        Ref(sl, "_loginButton", loginButton);
        Ref(sl, "_signUpButton", signUpButton);
        Ref(sl, "_flash", flash);
        sl.ApplyModifiedPropertiesWithoutUndo();

        // 빨간 눈이 숨어 있는 자리: 큰 건물의 창, 낮은 폐허, 차 사이 골목, 먼 빌딩의 창
        Rect[] zones =
        {
            new Rect(40f, 200f, 200f, 700f),    // 왼쪽 거대 빌딩
            new Rect(220f, 420f, 200f, 460f),
            new Rect(1620f, 200f, 260f, 700f),  // 오른쪽 거대 빌딩
            new Rect(1470f, 520f, 150f, 380f),
            new Rect(440f, 760f, 1000f, 160f),  // 낮은 폐허
            new Rect(300f, 885f, 1300f, 45f),   // 차 사이 골목
            new Rect(700f, 520f, 700f, 180f),   // 먼 빌딩(제목, 부제와 겹치지 않게 아래쪽만)
        };
        SerializedObject sr = new SerializedObject(eyes);
        Ref(sr, "_eyeSprite", Spr("title_eye"));
        SerializedProperty zoneProp = sr.FindProperty("_zones");
        zoneProp.arraySize = zones.Length;
        for (int i = 0; i < zones.Length; i++) zoneProp.GetArrayElementAtIndex(i).rectValue = zones[i];
        sr.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject se = new SerializedObject(embers);
        Ref(se, "_lampOrigin", lampOrigin);
        Ref(se, "_sprite", Spr("title_glow"));
        se.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true),
        };
        AssetDatabase.SaveAssets();

        Debug.Log("[TitleSceneBuilder] " + ScenePath + "를 만들고 빌드 설정에 0번(TitleScene), 1번(CityScene)으로 넣었습니다. 비 컴포넌트: " + (rain != null));
    }

    // ───────────────────────────── 글꼴 ─────────────────────────────

    private static TMP_FontAsset GetOrCreateFont(string ttfName, string assetName, int samplingSize, int padding, string preload)
    {
        string assetPath = FontDir + assetName;
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null) return existing;

        Font source = AssetDatabase.LoadAssetAtPath<Font>(FontDir + ttfName);
        TMP_FontAsset fa = TMP_FontAsset.CreateFontAsset(source, samplingSize, padding, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        AssetDatabase.CreateAsset(fa, assetPath);
        fa.material.name = assetName + " Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);
        for (int i = 0; i < fa.atlasTextures.Length; i++)
        {
            fa.atlasTextures[i].name = assetName + " Atlas " + i;
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[i], fa);
        }
        fa.TryAddCharacters(preload);
        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        return fa;
    }

    // ───────────────────────────── UI 도우미 ─────────────────────────────

    private static Sprite Spr(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(TitleAssetGenerator.Dir + "/" + name + ".png");
    }

    private static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
        r.anchoredPosition = new Vector2(x, -y);
        r.sizeDelta = new Vector2(w, h);
        return r;
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    private static Image Img(Transform parent, string name, Sprite sprite, float x, float y, float w, float h)
    {
        RectTransform r = Rect(name, parent, x, y, w, h);
        Image image = r.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    private static Image Layer(Transform parent, string name, string sprite)
    {
        return Img(parent, name, Spr(sprite), -32f, -32f, 1984f, 1144f);
    }

    private static CanvasGroup Group(Transform parent, string name, float x, float y, float w, float h)
    {
        RectTransform r = Rect(name, parent, x, y, w, h);
        return r.gameObject.AddComponent<CanvasGroup>();
    }

    // 화면 전체를 덮는 안개 한 겹(타일 한 장이 화면 한 장 크기이고 좌우로 이어 붙는다)
    private static RawImage Fog(Transform parent, string name, string texture, float alpha)
    {
        RectTransform r = Rect(name, parent, 0f, 0f, 1920f, 1080f);
        RawImage raw = r.gameObject.AddComponent<RawImage>();
        raw.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TitleAssetGenerator.Dir + "/" + texture + ".png");
        raw.uvRect = new Rect(0f, 0f, 1f, 1f);
        raw.color = new Color(1f, 1f, 1f, alpha);
        raw.raycastTarget = false;
        return raw;
    }

    private static TMP_Text Text(Transform parent, string name, TMP_FontAsset font, string content, float size, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.font = font;
        t.text = content;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        return t;
    }

    private static TMP_InputField InputField(Transform parent, string name, TMP_FontAsset font, string placeholder, float x, float y, bool password)
    {
        Image bg = Img(parent, name, Spr("ui_field"), x, y, 480f, 72f);
        bg.type = Image.Type.Sliced;
        bg.pixelsPerUnitMultiplier = CanvasPpu;
        bg.raycastTarget = true;

        GameObject area = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        area.transform.SetParent(bg.transform, false);
        RectTransform areaRect = area.GetComponent<RectTransform>();
        Stretch(areaRect);
        areaRect.offsetMin = new Vector2(22f, 8f);
        areaRect.offsetMax = new Vector2(-22f, -8f);

        TMP_Text ph = Text(area.transform, "Placeholder", font, placeholder, 34f, new Color(0.36f, 0.43f, 0.6f, 1f), TextAlignmentOptions.MidlineLeft);
        TMP_Text txt = Text(area.transform, "Text", font, "", 34f, new Color(0.86f, 0.92f, 1f, 1f), TextAlignmentOptions.MidlineLeft);
        Stretch(ph.rectTransform);
        Stretch(txt.rectTransform);

        TMP_InputField field = bg.gameObject.AddComponent<TMP_InputField>();
        field.textViewport = areaRect;
        field.textComponent = txt;
        field.placeholder = ph;
        field.targetGraphic = bg;
        field.customCaretColor = true;
        field.caretColor = Amber;
        field.caretWidth = 2;
        field.selectionColor = new Color(1f, 0.7f, 0.25f, 0.35f);
        field.contentType = password ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
        field.lineType = TMP_InputField.LineType.SingleLine;
        if (password) field.asteriskChar = '*';
        return field;
    }

    private static Button TitleButton(Transform parent, string name, TMP_FontAsset font, string label, float x, float y)
    {
        Image bg = Img(parent, name, Spr("ui_button"), x, y, 228f, 72f);
        bg.type = Image.Type.Sliced;
        bg.pixelsPerUnitMultiplier = CanvasPpu;
        bg.raycastTarget = true;

        TMP_Text text = Text(bg.transform, "Label", font, label, 36f, new Color(0.78f, 0.84f, 0.96f, 1f), TextAlignmentOptions.Center);
        Stretch(text.rectTransform);

        Button button = bg.gameObject.AddComponent<Button>();
        button.targetGraphic = bg;
        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = new SpriteState
        {
            highlightedSprite = Spr("ui_button_hover"),
            pressedSprite = Spr("ui_button_pressed"),
            selectedSprite = Spr("ui_button"),
            disabledSprite = Spr("ui_button_disabled"),
        };
        Navigation nav = button.navigation;
        nav.mode = Navigation.Mode.None;
        button.navigation = nav;

        UI_TitleButton titleButton = bg.gameObject.AddComponent<UI_TitleButton>();
        SerializedObject so = new SerializedObject(titleButton);
        Ref(so, "_label", text);
        so.ApplyModifiedPropertiesWithoutUndo();
        return button;
    }

    private static void Ref(SerializedObject so, string field, Object value)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p == null) { Debug.LogError("[TitleSceneBuilder] 필드를 찾을 수 없습니다: " + field); return; }
        p.objectReferenceValue = value;
    }
}
#endif
