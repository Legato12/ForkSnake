#if UNITY_EDITOR && SNAKE_MENUS
using UnityEditor;
using UnityEngine;
using TMPro;

public static class QSnakeAutoSetup
{
    [MenuItem("Tools/QSnake/Setup ▸ Forest (current scene)")]
    public static void SetupForest()
    {
        var board = Object.FindObjectOfType<Board>();
        if (!board)
        {
            var go = new GameObject("Board");
            board = go.AddComponent<Board>();
            board.borderX = 9; board.borderY = 5; board.tileWorldSize = 1f; board.wrap = false; board.safeTopRows = 2;
        }

        var registry = Object.FindObjectOfType<ObstacleRegistry>();
        if (!registry) new GameObject("ObstacleRegistry").AddComponent<ObstacleRegistry>();

        SnakeController snake = Object.FindObjectOfType<SnakeController>();
        if (!snake)
        {
            var snakeGO = new GameObject("Snake");
            var headSR = snakeGO.AddComponent<SpriteRenderer>();
            headSR.sprite = MakeSprite(20, 20, Color.white);
            snake = snakeGO.AddComponent<SnakeController>();
            var segmentsParent = new GameObject("Segments").transform;
            segmentsParent.SetParent(snakeGO.transform, false);
            var segPrefab = new GameObject("SegmentTemplate");
            var sr = segPrefab.AddComponent<SpriteRenderer>();
            sr.sprite = MakeSprite(18, 18, new Color(0.9f,0.9f,0.9f,1f));
            snakeGO.transform.position = board.CellToWorld(new Vector2Int(0,0));
            snake.GetType().GetField("segmentPrefab", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(snake, segPrefab.transform);
            snake.GetType().GetField("segmentsParent", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(snake, segmentsParent);
        }

        // Canvas + HUD texts
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (!canvas)
        {
            var cgo = new GameObject("Canvas");
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cgo.AddComponent<CanvasScaler>();
            cgo.AddComponent<GraphicRaycaster>();
            cgo.AddComponent<HUDSingleton>();
            CreateHudTexts(canvas, out var score, out var coins, out var chain);
            var snakeCtrl = Object.FindObjectOfType<SnakeController>();
            var t = snakeCtrl.GetType();
            t.GetField("scoreText", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(snakeCtrl, score);
            t.GetField("coinsText", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(snakeCtrl, coins);
            t.GetField("chainText", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(snakeCtrl, chain);
        }

        Selection.activeObject = board.gameObject;
        Debug.Log("QSnake Forest setup complete.");
    }

    [MenuItem("Tools/QSnake/Setup ▸ HUD Singleton (fix double text)")]
    public static void AddHudSingleton()
    {
        var canvas = Object.FindObjectOfType<Canvas>();
        if (!canvas) { Debug.LogWarning("Canvas not found."); return; }
        if (!canvas.GetComponent<HUDSingleton>()) canvas.gameObject.AddComponent<HUDSingleton>();
        Debug.Log("HUDSingleton added to Canvas.");
    }

    private static void CreateHudTexts(Canvas canvas, out TextMeshProUGUI score, out TextMeshProUGUI coins, out TextMeshProUGUI chain)
    {
        GameObject Make(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(canvas.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
            return go;
        }

        score = Make("ScoreText", new Vector2(0,1), new Vector2(0,1), new Vector2(12,-36), new Vector2(260,-12)).AddComponent<TextMeshProUGUI>();
        score.enableAutoSizing = true; score.fontSizeMin = 14; score.fontSizeMax = 36; score.text = "Score: 0";

        coins = Make("CoinsText", new Vector2(0.5f,1), new Vector2(0.5f,1), new Vector2(-120,-36), new Vector2(120,-12)).AddComponent<TextMeshProUGUI>();
        coins.enableAutoSizing = true; coins.fontSizeMin = 14; coins.fontSizeMax = 36; coins.alignment = TextAlignmentOptions.Center; coins.text = "Coins: 0";

        chain = Make("ChainText", new Vector2(1,1), new Vector2(1,1), new Vector2(-220,-36), new Vector2(-12,-12)).AddComponent<TextMeshProUGUI>();
        chain.enableAutoSizing = true; chain.fontSizeMin = 14; chain.fontSizeMax = 36; chain.alignment = TextAlignmentOptions.Right; chain.text = "";
    }

    private static Sprite MakeSprite(int w, int h, Color color)
    {
        var tex = new Texture2D(w,h);
        var pixels = new Color[w*h];
        for (int i=0;i<pixels.Length;i++) pixels[i]=color;
        tex.SetPixels(pixels); tex.Apply();
        var sp = Sprite.Create(tex, new Rect(0,0,w,h), new Vector2(0.5f,0.5f), 100f);
        sp.name = "QSnakeRuntimeSprite";
        return sp;
    }
}
#endif
