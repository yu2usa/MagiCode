using UnityEngine;
using UnityEditor;

/// <summary>
/// サンプル敵データを作成するエディタユーティリティ
/// </summary>
public class EnemyDataCreator
{
    private const string BASE_PATH = "Assets/Data/Enemies/";

    [MenuItem("MagiCode/Create Sample Enemy Data/Basic Slime")]
    public static void CreateBasicSlime()
    {
        var enemyData = ScriptableObject.CreateInstance<EnemyData>();
        enemyData.enemyName = "スライム";
        enemyData.maxHealth = 10;
        enemyData.showTelegraph = true;

        // 攻撃アクション：現在位置に攻撃
        var attackAction = ScriptableObject.CreateInstance<AttackAction>();
        attackAction.name = "Slime_Attack";
        attackAction.actionName = "スライムアタック";
        attackAction.damage = 3;
        attackAction.targetArea = -1;
        attackAction.telegraphMessage = "スライムが攻撃態勢に入った！";

        // 移動アクション：ランダム移動
        var moveAction = ScriptableObject.CreateInstance<MoveAction>();
        moveAction.name = "Slime_RandomMove";
        moveAction.actionName = "ランダム移動";
        moveAction.moveType = MoveAction.MoveType.Random;
        moveAction.moveDuration = 0.8f;
        moveAction.preMoveDelay = 1.0f;

        // アセット作成
        string path = BASE_PATH + "BasicSlime.asset";
        EnsureDirectoryExists();
        AssetDatabase.CreateAsset(enemyData, path);

        // サブアセットとして行動を追加
        AssetDatabase.AddObjectToAsset(attackAction, enemyData);
        AssetDatabase.AddObjectToAsset(moveAction, enemyData);

        // 行動をリストに追加
        enemyData.attackPhaseActions.Add(new WeightedAction { action = attackAction, weight = 1f });
        enemyData.defensePhaseActions.Add(new WeightedAction { action = moveAction, weight = 1f });

        EditorUtility.SetDirty(enemyData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created: {path}");
        Selection.activeObject = enemyData;
    }

    [MenuItem("MagiCode/Create Sample Enemy Data/Sniper Golem")]
    public static void CreateSniperGolem()
    {
        var enemyData = ScriptableObject.CreateInstance<EnemyData>();
        enemyData.enemyName = "狙撃ゴーレム";
        enemyData.maxHealth = 15;
        enemyData.showTelegraph = true;

        // 強攻撃：プレイヤー位置を狙う（ダメージ5）
        var sniperAttack = ScriptableObject.CreateInstance<AttackAction>();
        sniperAttack.name = "Golem_SniperAttack";
        sniperAttack.actionName = "精密射撃";
        sniperAttack.damage = 5;
        sniperAttack.targetArea = -1; // 現在位置（プレイヤー追跡は別途実装）
        sniperAttack.preAttackDelay = 0.5f;
        sniperAttack.telegraphMessage = "ゴーレムが照準を合わせている...";

        // 全体攻撃：すべてのエリアを攻撃
        var aoeAttack = ScriptableObject.CreateInstance<MultiAttackAction>();
        aoeAttack.name = "Golem_AOEAttack";
        aoeAttack.actionName = "全体攻撃";
        aoeAttack.damage = 2;
        aoeAttack.targetAreas = new System.Collections.Generic.List<int> { 0, 1, 2 };
        aoeAttack.delayBetweenAttacks = 0.15f;
        aoeAttack.telegraphMessage = "ゴーレムが全体攻撃を準備！";

        // 後退：プレイヤーから離れる
        var retreatMove = ScriptableObject.CreateInstance<MoveAction>();
        retreatMove.name = "Golem_Retreat";
        retreatMove.actionName = "後退";
        retreatMove.moveType = MoveAction.MoveType.AwayFromPlayer;
        retreatMove.moveDuration = 1.0f;
        retreatMove.preMoveDelay = 0.5f;

        // 待機
        var waitAction = ScriptableObject.CreateInstance<WaitAction>();
        waitAction.name = "Golem_Wait";
        waitAction.actionName = "待機";
        waitAction.waitDuration = 0.5f;

        // アセット作成
        string path = BASE_PATH + "SniperGolem.asset";
        EnsureDirectoryExists();
        AssetDatabase.CreateAsset(enemyData, path);

        AssetDatabase.AddObjectToAsset(sniperAttack, enemyData);
        AssetDatabase.AddObjectToAsset(aoeAttack, enemyData);
        AssetDatabase.AddObjectToAsset(retreatMove, enemyData);
        AssetDatabase.AddObjectToAsset(waitAction, enemyData);

        // 行動をリストに追加
        enemyData.attackPhaseActions.Add(new WeightedAction { action = sniperAttack, weight = 0.7f });
        enemyData.attackPhaseActions.Add(new WeightedAction { action = aoeAttack, weight = 0.3f });
        enemyData.defensePhaseActions.Add(new WeightedAction { action = retreatMove, weight = 0.8f });
        enemyData.defensePhaseActions.Add(new WeightedAction { action = waitAction, weight = 0.2f });

        EditorUtility.SetDirty(enemyData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created: {path}");
        Selection.activeObject = enemyData;
    }

    [MenuItem("MagiCode/Create Sample Enemy Data/Charger Orc")]
    public static void CreateChargerOrc()
    {
        var enemyData = ScriptableObject.CreateInstance<EnemyData>();
        enemyData.enemyName = "突撃オーク";
        enemyData.maxHealth = 12;
        enemyData.showTelegraph = true;

        // 通常攻撃
        var normalAttack = ScriptableObject.CreateInstance<AttackAction>();
        normalAttack.name = "Orc_NormalAttack";
        normalAttack.actionName = "通常攻撃";
        normalAttack.damage = 3;
        normalAttack.targetArea = -1;
        normalAttack.telegraphMessage = "オークが武器を振りかぶった！";

        // チャージ攻撃準備
        var chargeAction = ScriptableObject.CreateInstance<ChargeAction>();
        chargeAction.name = "Orc_Charge";
        chargeAction.actionName = "チャージ";
        chargeAction.chargeDuration = 1.0f;
        chargeAction.damageMultiplier = 2.0f;
        chargeAction.chargeColor = new Color(1f, 0.3f, 0.3f);
        chargeAction.telegraphMessage = "オークが力を溜めている...次の攻撃は強力だ！";

        // プレイヤーに接近
        var approachMove = ScriptableObject.CreateInstance<MoveAction>();
        approachMove.name = "Orc_Approach";
        approachMove.actionName = "接近";
        approachMove.moveType = MoveAction.MoveType.TowardPlayer;
        approachMove.moveDuration = 0.6f;
        approachMove.preMoveDelay = 0.5f;

        // アセット作成
        string path = BASE_PATH + "ChargerOrc.asset";
        EnsureDirectoryExists();
        AssetDatabase.CreateAsset(enemyData, path);

        AssetDatabase.AddObjectToAsset(normalAttack, enemyData);
        AssetDatabase.AddObjectToAsset(chargeAction, enemyData);
        AssetDatabase.AddObjectToAsset(approachMove, enemyData);

        // 行動をリストに追加
        enemyData.attackPhaseActions.Add(new WeightedAction { action = normalAttack, weight = 0.7f });
        enemyData.attackPhaseActions.Add(new WeightedAction { action = chargeAction, weight = 0.3f });
        enemyData.defensePhaseActions.Add(new WeightedAction { action = approachMove, weight = 1f });

        EditorUtility.SetDirty(enemyData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created: {path}");
        Selection.activeObject = enemyData;
    }

    [MenuItem("MagiCode/Create Sample Enemy Data/Assassin (Sequence Example)")]
    public static void CreateAssassin()
    {
        var enemyData = ScriptableObject.CreateInstance<EnemyData>();
        enemyData.enemyName = "アサシン";
        enemyData.maxHealth = 8;
        enemyData.showTelegraph = true;

        // 接近アクション
        var approachMove = ScriptableObject.CreateInstance<MoveAction>();
        approachMove.name = "Assassin_Approach";
        approachMove.actionName = "接近";
        approachMove.moveType = MoveAction.MoveType.TowardPlayer;
        approachMove.moveDuration = 0.4f;
        approachMove.preMoveDelay = 0.2f;

        // 攻撃アクション
        var quickAttack = ScriptableObject.CreateInstance<AttackAction>();
        quickAttack.name = "Assassin_QuickAttack";
        quickAttack.actionName = "素早い斬撃";
        quickAttack.damage = 4;
        quickAttack.targetArea = -1;
        quickAttack.animationDuration = 0.5f;

        // 後退アクション
        var retreatMove = ScriptableObject.CreateInstance<MoveAction>();
        retreatMove.name = "Assassin_Retreat";
        retreatMove.actionName = "離脱";
        retreatMove.moveType = MoveAction.MoveType.AwayFromPlayer;
        retreatMove.moveDuration = 0.4f;
        retreatMove.preMoveDelay = 0.1f;

        // 連続行動：接近 → 攻撃 → 後退
        var hitAndRun = ScriptableObject.CreateInstance<SequenceAction>();
        hitAndRun.name = "Assassin_HitAndRun";
        hitAndRun.actionName = "ヒット&ラン";
        hitAndRun.actions = new System.Collections.Generic.List<EnemyAction> { approachMove, quickAttack, retreatMove };
        hitAndRun.delayBetweenActions = 0.1f;
        hitAndRun.telegraphMessage = "アサシンが素早く動き出した！";

        // 待機アクション
        var waitAction = ScriptableObject.CreateInstance<WaitAction>();
        waitAction.name = "Assassin_Wait";
        waitAction.actionName = "様子見";
        waitAction.waitDuration = 0.8f;

        // アセット作成
        string path = BASE_PATH + "Assassin.asset";
        EnsureDirectoryExists();
        AssetDatabase.CreateAsset(enemyData, path);

        AssetDatabase.AddObjectToAsset(approachMove, enemyData);
        AssetDatabase.AddObjectToAsset(quickAttack, enemyData);
        AssetDatabase.AddObjectToAsset(retreatMove, enemyData);
        AssetDatabase.AddObjectToAsset(hitAndRun, enemyData);
        AssetDatabase.AddObjectToAsset(waitAction, enemyData);

        // 攻撃フェーズ：ヒット&ラン（連続行動）
        enemyData.attackPhaseActions.Add(new WeightedAction { action = hitAndRun, weight = 1f });
        // 防御フェーズ：様子見
        enemyData.defensePhaseActions.Add(new WeightedAction { action = waitAction, weight = 1f });

        EditorUtility.SetDirty(enemyData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created: {path}");
        Selection.activeObject = enemyData;
    }

    [MenuItem("MagiCode/Create Sample Enemy Data/Create All")]
    public static void CreateAllSampleEnemies()
    {
        CreateBasicSlime();
        CreateSniperGolem();
        CreateChargerOrc();
        CreateAssassin();
        Debug.Log("All sample enemies created!");
    }

    private static void EnsureDirectoryExists()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
        {
            AssetDatabase.CreateFolder("Assets", "Data");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Data/Enemies"))
        {
            AssetDatabase.CreateFolder("Assets/Data", "Enemies");
        }
    }
}
