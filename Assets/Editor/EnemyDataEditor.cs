using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

/// <summary>
/// EnemyData用カスタムエディタ
/// </summary>
[CustomEditor(typeof(EnemyData))]
public class EnemyDataEditor : Editor
{
    private ReorderableList _attackActionsList;
    private ReorderableList _defenseActionsList;

    private SerializedProperty _enemyName;
    private SerializedProperty _enemySprite;
    private SerializedProperty _element;
    private SerializedProperty _elementPhases;
    private SerializedProperty _guardReflectDamage;
    private SerializedProperty _elementAnimatorTriggers;
    private SerializedProperty _flipSpriteInDialogue;
    private SerializedProperty _scale;
    private SerializedProperty _animatorController;
    private SerializedProperty _slashEffectPrefab;
    private SerializedProperty _attackSE;
    private SerializedProperty _maxHealth;
    private SerializedProperty _regenHealthOnTurnEnd;
    private SerializedProperty _useKeyGolemMechanic;
    private SerializedProperty _sortRole;
    private SerializedProperty _assignedNumber;
    private SerializedProperty _isInvincible;
    private SerializedProperty _attackPhaseActions;
    private SerializedProperty _defensePhaseActions;
    private SerializedProperty _showTelegraph;

    private void OnEnable()
    {
        _enemyName = serializedObject.FindProperty("enemyName");
        _enemySprite = serializedObject.FindProperty("enemySprite");
        _element = serializedObject.FindProperty("element");
        _elementPhases = serializedObject.FindProperty("elementPhases");
        _guardReflectDamage = serializedObject.FindProperty("guardReflectDamage");
        _elementAnimatorTriggers = serializedObject.FindProperty("elementAnimatorTriggers");
        _flipSpriteInDialogue = serializedObject.FindProperty("flipSpriteInDialogue");
        _scale = serializedObject.FindProperty("scale");
        _animatorController = serializedObject.FindProperty("animatorController");
        _slashEffectPrefab = serializedObject.FindProperty("slashEffectPrefab");
        _attackSE = serializedObject.FindProperty("attackSE");
        _maxHealth = serializedObject.FindProperty("maxHealth");
        _regenHealthOnTurnEnd = serializedObject.FindProperty("regenHealthOnTurnEnd");
        _useKeyGolemMechanic = serializedObject.FindProperty("useKeyGolemMechanic");
        _sortRole = serializedObject.FindProperty("sortRole");
        _assignedNumber = serializedObject.FindProperty("assignedNumber");
        _isInvincible = serializedObject.FindProperty("isInvincible");
        _attackPhaseActions = serializedObject.FindProperty("attackPhaseActions");
        _defensePhaseActions = serializedObject.FindProperty("defensePhaseActions");
        _showTelegraph = serializedObject.FindProperty("showTelegraph");

        _attackActionsList = CreateActionList(_attackPhaseActions, "攻撃フェーズ行動");
        _defenseActionsList = CreateActionList(_defensePhaseActions, "防御フェーズ行動");
    }

    private ReorderableList CreateActionList(SerializedProperty property, string header)
    {
        var list = new ReorderableList(serializedObject, property, true, true, true, true);

        list.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, header, EditorStyles.boldLabel);
        };

        list.elementHeightCallback = index =>
        {
            return EditorGUIUtility.singleLineHeight * 4 + 12;
        };

        list.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            var element = property.GetArrayElementAtIndex(index);
            var actionProp = element.FindPropertyRelative("action");
            var weightProp = element.FindPropertyRelative("weight");
            var conditionProp = element.FindPropertyRelative("condition");

            rect.y += 2;
            float lineHeight = EditorGUIUtility.singleLineHeight + 2;

            // アクション
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                actionProp, new GUIContent("アクション"));

            // 重み
            rect.y += lineHeight;
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                weightProp, new GUIContent("重み"));

            // 条件
            rect.y += lineHeight;
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                conditionProp, new GUIContent("条件 (任意)"));

            // アクション説明を表示
            rect.y += lineHeight;
            var action = actionProp.objectReferenceValue as EnemyAction;
            if (action != null)
            {
                EditorGUI.LabelField(
                    new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                    "→ " + action.GetDescription(), EditorStyles.miniLabel);
            }
        };

        list.onAddCallback = l =>
        {
            property.arraySize++;
            var newElement = property.GetArrayElementAtIndex(property.arraySize - 1);
            newElement.FindPropertyRelative("action").objectReferenceValue = null;
            newElement.FindPropertyRelative("weight").floatValue = 1f;
            newElement.FindPropertyRelative("condition").objectReferenceValue = null;
        };

        list.onRemoveCallback = l =>
        {
            var element = property.GetArrayElementAtIndex(l.index);
            var actionProp = element.FindPropertyRelative("action");
            var action = actionProp.objectReferenceValue as EnemyAction;

            // サブアセット（同じファイルに埋め込まれたアクション）かどうか確認
            bool isSubAsset = action != null
                && AssetDatabase.GetAssetPath(action) == AssetDatabase.GetAssetPath(target);

            if (isSubAsset)
            {
                // サブアセットごと削除するか確認
                bool delete = EditorUtility.DisplayDialog(
                    "アクションの削除",
                    $"'{action.name}' をリストから削除し、アセットデータも消去しますか？\n（この操作は元に戻せません）",
                    "削除", "キャンセル");
                if (!delete) return;

                ReorderableList.defaultBehaviours.DoRemoveButton(l);
                serializedObject.ApplyModifiedProperties();
                DestroyImmediate(action, true);
                AssetDatabase.SaveAssets();
            }
            else
            {
                // 外部アセット参照またはnullの場合はリストからのみ削除
                ReorderableList.defaultBehaviours.DoRemoveButton(l);
            }
        };

        return list;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 基本情報
        EditorGUILayout.LabelField("基本情報", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_enemyName, new GUIContent("敵の名前"));
        EditorGUILayout.PropertyField(_enemySprite, new GUIContent("スプライト"));
        EditorGUILayout.PropertyField(_element, new GUIContent("属性", "敵本体の属性（None = 無属性）"));

        EditorGUILayout.Space(10);

        // 属性フェーズメカニクス
        EditorGUILayout.LabelField("属性フェーズメカニクス", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "elementPhasesにリストを設定すると、攻撃を受けるたびに属性がランダム変化します。\n" +
            "空の場合はメカニクス無効（常に上記の属性のまま）。",
            MessageType.None);
        EditorGUILayout.PropertyField(_elementPhases, new GUIContent("属性プール"), true);
        EditorGUILayout.PropertyField(_guardReflectDamage, new GUIContent("ガード時反射ダメージ", "属性不一致の攻撃を受けたときプレイヤーに与えるダメージ（0 = なし）"));
        EditorGUILayout.PropertyField(_elementAnimatorTriggers, new GUIContent("属性変化アニメーション", "属性ごとに発火する Animator トリガー名を設定"), true);

        EditorGUILayout.Space(10);

        // 見た目
        EditorGUILayout.LabelField("見た目", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_flipSpriteInDialogue, new GUIContent("ダイアログ時に反転", "バトル前会話でスプライトを左右反転する（右向きスプライトを左向きにしたい場合にチェック）"));
        EditorGUILayout.PropertyField(_scale, new GUIContent("大きさ", "1.0が標準サイズ"));

        EditorGUILayout.Space(10);

        // アニメーション
        EditorGUILayout.LabelField("アニメーション", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_animatorController, new GUIContent("アニメーターコントローラー"));

        EditorGUILayout.Space(10);

        // エフェクト
        EditorGUILayout.LabelField("エフェクト", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_slashEffectPrefab, new GUIContent("攻撃エフェクトプレハブ"));

        EditorGUILayout.Space(10);

        // 効果音
        EditorGUILayout.LabelField("効果音", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_attackSE, new GUIContent("攻撃SE"));

        EditorGUILayout.Space(10);

        // ステータス
        EditorGUILayout.LabelField("ステータス", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_maxHealth, new GUIContent("最大HP"));
        EditorGUILayout.PropertyField(_regenHealthOnTurnEnd, new GUIContent("ターン終了時HP全回復", "プレイヤーの攻撃フェーズ完了後にHPを全回復する"));
        EditorGUILayout.PropertyField(_useKeyGolemMechanic, new GUIContent("KeyGolemメカニクス", "プレイヤー攻撃フェーズ中に指定回数ぴったり攻撃するとダメージが入るメカニクスを有効化"));

        EditorGUILayout.Space(10);

        // ソート / CoreBossメカニクス
        EditorGUILayout.LabelField("ソート / CoreBossメカニクス", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_sortRole, new GUIContent("役割", "None=通常, Guard=バブルソートガード, Boss=バブルソートボス, CoreTotem=Coreトーテム, CoreBoss=Coreボス"));
        EditorGUILayout.PropertyField(_assignedNumber, new GUIContent("割り当て番号", "CoreTotem用: 昇順に並べるとCoreに30ダメージ"));
        EditorGUILayout.PropertyField(_isInvincible, new GUIContent("無敵（HPバーなし）", "攻撃を受けてもダメージ0・HPバー非表示（CoreTotem用）"));

        EditorGUILayout.Space(10);

        // 予告表示
        EditorGUILayout.PropertyField(_showTelegraph, new GUIContent("攻撃予告を表示"));

        EditorGUILayout.Space(15);

        // 攻撃フェーズ行動
        _attackActionsList.DoLayoutList();

        EditorGUILayout.Space(10);

        // 防御フェーズ行動
        _defenseActionsList.DoLayoutList();

        EditorGUILayout.Space(15);

        // クイック作成ボタン
        EditorGUILayout.LabelField("クイック作成", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("攻撃アクション追加"))
        {
            CreateAndAddAction<AttackAction>("Attack", _attackPhaseActions);
        }
        if (GUILayout.Button("移動アクション追加"))
        {
            CreateAndAddAction<MoveAction>("Move", _defensePhaseActions);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("複数攻撃追加"))
        {
            CreateAndAddAction<MultiAttackAction>("MultiAttack", _attackPhaseActions);
        }
        if (GUILayout.Button("待機アクション追加"))
        {
            CreateAndAddAction<WaitAction>("Wait", _defensePhaseActions);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("連続行動(攻撃)追加"))
        {
            CreateAndAddAction<SequenceAction>("Sequence", _attackPhaseActions);
        }
        if (GUILayout.Button("連続行動(防御)追加"))
        {
            CreateAndAddAction<SequenceAction>("Sequence", _defensePhaseActions);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("魔法攻撃追加"))
        {
            CreateAndAddAction<MagicAttackAction>("MagicAttack", _attackPhaseActions);
        }
        if (GUILayout.Button("チャージ追加"))
        {
            CreateAndAddAction<ChargeAction>("Charge", _attackPhaseActions);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 条件作成ボタン
        EditorGUILayout.LabelField("条件作成", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("作成した条件は、上の行動リストの「条件(任意)」欄にドラッグして使用します", MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("HP条件を作成"))
        {
            CreateCondition<HealthCondition>("HealthCond");
        }
        if (GUILayout.Button("位置条件を作成"))
        {
            CreateCondition<PositionCondition>("PositionCond");
        }
        EditorGUILayout.EndHorizontal();

        // 作成済み条件一覧
        DrawExistingConditions();

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("プレファブ作成", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "EnemyDataをもとに敵プレファブを生成します。\nScout.prefabをテンプレートにコピーし、EnemyDataを差し替えます。",
            MessageType.Info);
        if (GUILayout.Button("プレファブを作成", GUILayout.Height(32)))
        {
            CreateEnemyPrefab();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void CreateCondition<T>(string namePrefix) where T : ActionCondition
    {
        var enemyData = (EnemyData)target;

        var condition = ScriptableObject.CreateInstance<T>();
        int count = CountSubAssetsOfType<T>(enemyData);
        condition.name = $"{enemyData.enemyName}_{namePrefix}_{count}";

        AssetDatabase.AddObjectToAsset(condition, enemyData);
        AssetDatabase.SaveAssets();

        EditorUtility.SetDirty(enemyData);

        // 作成した条件を選択状態にする
        Selection.activeObject = condition;
        EditorGUIUtility.PingObject(condition);
    }

    private int CountSubAssetsOfType<T>(Object mainAsset) where T : Object
    {
        string path = AssetDatabase.GetAssetPath(mainAsset);
        var subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
        int count = 0;
        foreach (var asset in subAssets)
        {
            if (asset is T) count++;
        }
        return count;
    }

    private void DrawExistingConditions()
    {
        var enemyData = (EnemyData)target;
        string path = AssetDatabase.GetAssetPath(enemyData);
        var subAssets = AssetDatabase.LoadAllAssetsAtPath(path);

        var conditions = new System.Collections.Generic.List<ActionCondition>();
        foreach (var asset in subAssets)
        {
            if (asset is ActionCondition cond)
                conditions.Add(cond);
        }

        if (conditions.Count == 0) return;

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField($"作成済み条件 ({conditions.Count}件)", EditorStyles.miniLabel);

        EditorGUI.indentLevel++;
        foreach (var cond in conditions)
        {
            EditorGUILayout.BeginHorizontal();

            // 条件名と説明
            string description = GetConditionDescription(cond);
            EditorGUILayout.LabelField($"• {cond.name}", GUILayout.Width(150));
            EditorGUILayout.LabelField(description, EditorStyles.miniLabel);

            // 選択ボタン
            if (GUILayout.Button("選択", GUILayout.Width(40)))
            {
                Selection.activeObject = cond;
            }

            // 削除ボタン
            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                if (EditorUtility.DisplayDialog("条件の削除", $"'{cond.name}' を削除しますか？", "削除", "キャンセル"))
                {
                    DestroyImmediate(cond, true);
                    AssetDatabase.SaveAssets();
                }
            }

            EditorGUILayout.EndHorizontal();
        }
        EditorGUI.indentLevel--;
    }

    private string GetConditionDescription(ActionCondition condition)
    {
        if (condition is HealthCondition hc)
        {
            string target = hc.target == HealthCondition.Target.Enemy ? "敵" : "プレイヤー";
            string comp = hc.comparison switch
            {
                HealthCondition.ComparisonType.LessThan => "<",
                HealthCondition.ComparisonType.LessOrEqual => "≤",
                HealthCondition.ComparisonType.Equal => "=",
                HealthCondition.ComparisonType.GreaterOrEqual => "≥",
                HealthCondition.ComparisonType.GreaterThan => ">",
                _ => "?"
            };
            return $"{target}HP {comp} {hc.thresholdPercent}%";
        }
        else if (condition is PositionCondition pc)
        {
            return pc.conditionType switch
            {
                PositionCondition.ConditionType.EnemyAtPosition => $"敵がエリア{pc.targetPosition}にいる",
                PositionCondition.ConditionType.PlayerAtPosition => $"プレイヤーがエリア{pc.targetPosition}にいる",
                PositionCondition.ConditionType.SamePosition => "同じエリアにいる",
                PositionCondition.ConditionType.DifferentPosition => "違うエリアにいる",
                _ => "不明"
            };
        }
        return "";
    }

    // ====================================================
    // プレファブ生成
    // ====================================================

    /// <summary>
    /// EnemyDataをもとに敵プレファブを生成する。
    /// Scout.prefabをテンプレートとしてコピーし、EnemyDataのみ差し替える。
    /// テンプレートがない場合はスクラッチで作成。
    /// </summary>
    private void CreateEnemyPrefab()
    {
        var enemyData = (EnemyData)target;
        string prefabName = target.name;
        string savePath = $"Assets/Objects/Prefab/Enemies/{prefabName}.prefab";

        // 既存チェック
        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(savePath) != null;
        if (exists)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "プレファブが既に存在します",
                $"'{prefabName}.prefab' を上書きしますか？",
                "上書き", "キャンセル");
            if (!overwrite) return;
        }

        // Enemies フォルダを確保（段階的に作成）
        if (!AssetDatabase.IsValidFolder("Assets/Objects"))
            AssetDatabase.CreateFolder("Assets", "Objects");
        if (!AssetDatabase.IsValidFolder("Assets/Objects/Prefab"))
            AssetDatabase.CreateFolder("Assets/Objects", "Prefab");
        if (!AssetDatabase.IsValidFolder("Assets/Objects/Prefab/Enemies"))
            AssetDatabase.CreateFolder("Assets/Objects/Prefab", "Enemies");

        // Scout.prefab をテンプレートとして使用（自身が Scout の場合は除く）
        const string templatePath = "Assets/Objects/Prefab/Enemies/Scout.prefab";
        bool useTemplate = savePath != templatePath
                        && AssetDatabase.LoadAssetAtPath<GameObject>(templatePath) != null;

        if (useTemplate)
        {
            if (exists) AssetDatabase.DeleteAsset(savePath);
            AssetDatabase.CopyAsset(templatePath, savePath);
            AssetDatabase.Refresh();
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(savePath) != null)
        {
            // コピー済みまたは既存プレファブを編集
            var contents = PrefabUtility.LoadPrefabContents(savePath);
            contents.name = prefabName;
            ApplyEnemyDataToPrefab(contents, enemyData);
            PrefabUtility.SaveAsPrefabAsset(contents, savePath);
            PrefabUtility.UnloadPrefabContents(contents);
        }
        else
        {
            // テンプレートなし: スクラッチで作成
            var go = BuildEnemyGameObject(prefabName, enemyData);
            PrefabUtility.SaveAsPrefabAsset(go, savePath);
            Object.DestroyImmediate(go);
        }

        AssetDatabase.Refresh();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(savePath);
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log($"[EnemyDataEditor] プレファブを作成しました: {savePath}");
    }

    /// <summary>
    /// EnemyData の必須フィールドをプレファブ内の EnemyController に適用する
    /// </summary>
    private static void ApplyEnemyDataToPrefab(GameObject go, EnemyData enemyData)
    {
        var ctrl = go.GetComponent<EnemyController>();
        if (ctrl == null) return;
        ctrl.enemyData = enemyData;
        // EnemyData に slashEffectPrefab が設定されていれば enemySlash を上書き
        if (enemyData.slashEffectPrefab != null)
            ctrl.enemySlash = enemyData.slashEffectPrefab;
    }

    /// <summary>
    /// テンプレートなし時のフォールバック: 最低限の構成で GameObject を生成
    /// </summary>
    private static GameObject BuildEnemyGameObject(string name, EnemyData enemyData)
    {
        var go = new GameObject(name);
        go.AddComponent<SpriteRenderer>();
        go.AddComponent<Animator>();
        var ctrl = go.AddComponent<EnemyController>();
        ctrl.enemyData = enemyData;
        if (enemyData.slashEffectPrefab != null)
            ctrl.enemySlash = enemyData.slashEffectPrefab;
        // デフォルトエリア位置（StageConfig が未設定の場合のフォールバック）
        ctrl.areaPos = new List<Vector2>
        {
            new Vector2(-4.5f, 0.5f),
            new Vector2(1.4f, 0.5f),
            new Vector2(7.4f, 0.5f),
        };
        return go;
    }

    private void CreateAndAddAction<T>(string namePrefix, SerializedProperty listProperty) where T : EnemyAction
    {
        var enemyData = (EnemyData)target;
        string path = AssetDatabase.GetAssetPath(enemyData);
        string directory = System.IO.Path.GetDirectoryName(path);

        // サブアセットとして作成
        var action = ScriptableObject.CreateInstance<T>();
        action.name = $"{enemyData.enemyName}_{namePrefix}_{listProperty.arraySize}";
        action.actionName = action.name;

        AssetDatabase.AddObjectToAsset(action, enemyData);
        AssetDatabase.SaveAssets();

        // リストに追加
        listProperty.arraySize++;
        var newElement = listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1);
        newElement.FindPropertyRelative("action").objectReferenceValue = action;
        newElement.FindPropertyRelative("weight").floatValue = 1f;
        newElement.FindPropertyRelative("condition").objectReferenceValue = null;

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(enemyData);
    }
}
