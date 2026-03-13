#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class LevelDataCreator : Editor
{
    // Load màu trực tiếp từ sprite assets
    static Color GetLevelColor(int levelIndex)
    {
        string[] colorPaths = new string[]
        {
            "Assets/Art/Ingame/Block_Blue_Style1.png",
            "Assets/Art/Ingame/Block_Green_Style1.png",
            "Assets/Art/Ingame/Block_Pink_Style1.png",
            "Assets/Art/Ingame/Block_Yellow_Style1.png",
        };

        string path = colorPaths[levelIndex % colorPaths.Length];
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        if (tex != null)
        {
            // Lấy pixel ở giữa sprite để lấy màu đại diện
            int cx = tex.width / 2;
            int cy = tex.height / 2;
            return tex.GetPixel(cx, cy);
        }

        // Fallback nếu không load được sprite
        Color[] fallback = new Color[]
        {
            Hex("#3498DB"), // Blue
            Hex("#2ECC71"), // Green
            Hex("#E91E8C"), // Pink
            Hex("#F39C12"), // Yellow
        };
        Debug.LogWarning($"[LevelDataCreator] Không load được sprite tại: {path}, dùng màu fallback");
        return fallback[levelIndex % fallback.Length];
    }

    [MenuItem("TapAway/Create Sample Levels")]
    static void CreateSampleLevels()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");

        // Xóa assets cũ nếu có
        string[] oldFiles = { "Assets/Data/Level_01.asset","Assets/Data/Level_02.asset",
                               "Assets/Data/Level_03.asset","Assets/Data/Level_04.asset",
                               "Assets/Data/LevelDatabase.asset" };
        foreach (var f in oldFiles)
            AssetDatabase.DeleteAsset(f);

        var database = ScriptableObject.CreateInstance<LevelDatabase>();
        database.Levels = new List<LevelData>();

        var levels = new LevelData[]
        {
            CreateLevel1(),
            CreateLevel2(),
            CreateLevel3(),
            CreateLevel4(),
        };

        for (int i = 0; i < levels.Length; i++)
        {
            AssetDatabase.CreateAsset(levels[i], $"Assets/Data/Level_0{i + 1}.asset");
            database.Levels.Add(levels[i]);
        }

        AssetDatabase.CreateAsset(database, "Assets/Data/LevelDatabase.asset");
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = database;
        Debug.Log("[LevelDataCreator] Tạo xong 4 levels!");
    }

    static LevelData MakeLevel(string name, int w, int h, int colorIdx, int difficulty, List<BlockData> blocks)
    {
        var data = ScriptableObject.CreateInstance<LevelData>();
        data.levelName = name;
        data.gridWidth = w;
        data.gridHeight = h;
        data.cellSize = 1.6f;
        data.difficulty = difficulty;

        Color c = GetLevelColor(colorIdx);
        foreach (var b in blocks) b.color = c;
        data.blocks = blocks;
        return data;
    }

    // Level 1 - Blue - 4 blocks
    static LevelData CreateLevel1() => MakeLevel("Level 1", 4, 4, 0, 1, new List<BlockData>
    {
        new BlockData { gridPosition = new Vector2Int(0, 3), size = Vector2Int.one, direction = SlideDirection.Left  },
        new BlockData { gridPosition = new Vector2Int(3, 3), size = Vector2Int.one, direction = SlideDirection.Right },
        new BlockData { gridPosition = new Vector2Int(0, 0), size = Vector2Int.one, direction = SlideDirection.Down  },
        new BlockData { gridPosition = new Vector2Int(3, 0), size = Vector2Int.one, direction = SlideDirection.Right },
    });

    // Level 2 - Green - 6 blocks
    static LevelData CreateLevel2() => MakeLevel("Level 2", 5, 5, 1, 2, new List<BlockData>
    {
        new BlockData { gridPosition = new Vector2Int(0, 4), size = Vector2Int.one, direction = SlideDirection.Left  },
        new BlockData { gridPosition = new Vector2Int(4, 4), size = Vector2Int.one, direction = SlideDirection.Right },
        new BlockData { gridPosition = new Vector2Int(0, 2), size = Vector2Int.one, direction = SlideDirection.Left  },
        new BlockData { gridPosition = new Vector2Int(4, 2), size = Vector2Int.one, direction = SlideDirection.Right },
        new BlockData { gridPosition = new Vector2Int(2, 0), size = Vector2Int.one, direction = SlideDirection.Down  },
        new BlockData { gridPosition = new Vector2Int(2, 4), size = Vector2Int.one, direction = SlideDirection.Up   },
    });

    // Level 3 - Pink - gear tại (2,3), có block chỉ vào gear
    // Layout:
    //  row5: [←]  .  .  .  [→]
    //  row4:  .   .  .  .   .
    //  row3: [→]  . [G] .  [←]   ← 2 block hai bên chỉ VÀO gear
    //  row2:  .   .  .  .   .
    //  row1: [↓]  .  .  .  [→]
    static LevelData CreateLevel3()
    {
        var data = MakeLevel("Level 3", 5, 6, 2, 3, new List<BlockData>
        {
            new BlockData { gridPosition = new Vector2Int(0, 5), size = Vector2Int.one, direction = SlideDirection.Left  },
            new BlockData { gridPosition = new Vector2Int(4, 5), size = Vector2Int.one, direction = SlideDirection.Right },
            // 2 block chỉ VÀO gear tại (2,3)
            new BlockData { gridPosition = new Vector2Int(0, 3), size = Vector2Int.one, direction = SlideDirection.Right }, // → hướng vào gear
            new BlockData { gridPosition = new Vector2Int(4, 3), size = Vector2Int.one, direction = SlideDirection.Left  }, // ← hướng vào gear
            new BlockData { gridPosition = new Vector2Int(0, 1), size = Vector2Int.one, direction = SlideDirection.Down  },
            new BlockData { gridPosition = new Vector2Int(4, 1), size = Vector2Int.one, direction = SlideDirection.Right },
        });
        data.gearPositions = new List<Vector2Int> { new Vector2Int(2, 3) };
        return data;
    }

    // Level 4 - Yellow - 2 gear tại (2,4) và (2,1)
    // Tất cả blocks đều có đường thoát (ra ngoài hoặc vào gear)
    static LevelData CreateLevel4()
    {
        var data = MakeLevel("Level 4", 5, 6, 3, 4, new List<BlockData>
        {
            // Thoát ra ngoài
            new BlockData { gridPosition = new Vector2Int(0, 5), size = Vector2Int.one, direction = SlideDirection.Left  },
            new BlockData { gridPosition = new Vector2Int(4, 5), size = Vector2Int.one, direction = SlideDirection.Right },
            new BlockData { gridPosition = new Vector2Int(2, 0), size = Vector2Int.one, direction = SlideDirection.Down  },
            // Vào gear tại (2,4)
            new BlockData { gridPosition = new Vector2Int(0, 4), size = Vector2Int.one, direction = SlideDirection.Right },
            new BlockData { gridPosition = new Vector2Int(4, 4), size = Vector2Int.one, direction = SlideDirection.Left  },
            // Vào gear tại (2,1)
            new BlockData { gridPosition = new Vector2Int(0, 1), size = Vector2Int.one, direction = SlideDirection.Right },
            // Thoát ra ngoài (không bị gear chặn vì gear ở cột 2, block này ở hàng khác)
            new BlockData { gridPosition = new Vector2Int(4, 2), size = Vector2Int.one, direction = SlideDirection.Right },
        });
        data.gearPositions = new List<Vector2Int>
        {
            new Vector2Int(2, 4),
            new Vector2Int(2, 1),
        };
        return data;
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
#endif