using UnityEngine;
using UnityEditor;

/// <summary>
/// StageConfigのカスタムエディタ
/// 設定状態を視覚的に確認しやすくする
/// </summary>
[CustomEditor(typeof(StageConfig))]
public class StageConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // デフォルトのインスペクター描画
        DrawDefaultInspector();

        StageConfig config = (StageConfig)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("設定状態チェック", EditorStyles.boldLabel);

        // エリア設定チェック
        bool hasPlayerAreas = config.playerAreaPositions != null && config.playerAreaPositions.Count > 0;
        bool hasEnemyAreas = config.enemyAreaPositions != null && config.enemyAreaPositions.Count > 0;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("プレイヤーエリア:", GUILayout.Width(120));
        EditorGUILayout.LabelField(hasPlayerAreas ? $"OK ({config.playerAreaPositions.Count}個)" : "未設定",
            hasPlayerAreas ? EditorStyles.label : EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("敵エリア:", GUILayout.Width(120));
        EditorGUILayout.LabelField(hasEnemyAreas ? $"OK ({config.enemyAreaPositions.Count}個)" : "未設定",
            hasEnemyAreas ? EditorStyles.label : EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        // 敵構成チェック
        bool hasEnemySpawns = config.enemySpawns != null && config.enemySpawns.Count > 0;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("敵の構成:", GUILayout.Width(120));
        EditorGUILayout.LabelField(hasEnemySpawns ? $"OK ({config.enemySpawns.Count}体)" : "未設定（シーン配置を使用）",
            EditorStyles.label);
        EditorGUILayout.EndHorizontal();

        // 敵構成の詳細
        if (hasEnemySpawns)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < config.enemySpawns.Count; i++)
            {
                var spawn = config.enemySpawns[i];
                string prefabStatus = spawn.enemyPrefab != null ? "OK" : "未設定!";
                string dataStatus = spawn.enemyData != null ? spawn.enemyData.enemyName : "なし";

                Color oldColor = GUI.color;
                if (spawn.enemyPrefab == null)
                    GUI.color = Color.red;

                EditorGUILayout.LabelField($"[{i}] Prefab:{prefabStatus}, Data:{dataStatus}, Area:{spawn.startArea}");

                GUI.color = oldColor;
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(5);

        // デフォルト設定生成ボタン
        if (GUILayout.Button("デフォルトエリア位置を生成 (3エリア)"))
        {
            Undo.RecordObject(config, "Generate Default Positions");
            config.GenerateDefaultPositions(3, -3f, 3f, -2f, 2f);
            EditorUtility.SetDirty(config);
        }

        // 検証ボタン
        if (GUILayout.Button("設定を検証"))
        {
            if (config.Validate())
            {
                Debug.Log($"[StageConfig] {config.stageName}: 設定は有効です");
            }
        }
    }
}
