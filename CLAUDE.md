# MagiCode - Project Guide for Claude

## Project Overview
MagiCode は、コーディングを通じてバトルを行うターン制戦闘ゲームです。プレイヤーは Python コードやビジュアルブロックを使ってキャラクターを操作し、敵と戦います。

## Tech Stack
- **Engine**: Unity (C#)
- **Animation**: DOTween
- **WebSocket**: NativeWebSocket
- **Code Editor**: BlocksEngine2 (ビジュアルブロック), InGameCodeEditor
- **Serialization**: Newtonsoft.Json
- **Save System**: Easy Save 3

## Project Structure

```
Assets/
├── Scripts/
│   ├── BattleScene/     # バトルシステム
│   │   ├── TurnController.cs      # ターン管理・フェーズ制御
│   │   ├── BattleSceneManager.cs  # UIアニメーション
│   │   └── AI_Assistant.cs        # AI支援
│   ├── Player/
│   │   └── PlayerController.cs    # プレイヤー操作・スキル
│   ├── Enemy/
│   │   └── EnemyController.cs     # 敵AI・攻撃パターン
│   └── PythonExecute/
│       ├── PythonExecutor.cs      # Python実行 (WebSocket)
│       └── LogsManager.cs         # ログ解析・コマンド変換
├── Animations/          # キャラクター・スキルアニメーション
├── Scenes/              # Battle, CodeEditor, PythonTest
├── BlocksEngine2/       # ビジュアルプログラミングエンジン
├── Plugins/             # DOTween, NativeWebSocket, Easy Save 3
└── Shaders/             # カスタムシェーダー (GridEffect)
```

## Battle System

### Turn Flow
```
Enemy Defense → Player Attack → Player Defense → Enemy Attack → (loop)
```

### Grid System
- 3つのエリア (0, 1, 2) でプレイヤーと敵が移動・戦闘
- 位置ベースの攻撃判定

### Player Actions (PlayerController.cs)
- `CastLightning(int areaNum)` - ライトニング魔法 (25MP, 3ダメージ)
- `CastFlame(int areaNum)` - フレイム魔法 (25MP, 3ダメージ)
- `MoveTo(int position)` - 指定エリアへ移動
- `MoveForward()` / `MoveBackward()` - 前後移動

### Player Stats
- HP: 10
- MP: 100

### Enemy Stats
- HP: 10
- Attack Damage: 3

## Python Integration

### WebSocket Server
- URL: `ws://localhost:8000/ws`
- JSON形式でコードを送信: `{ "code": "..." }`

### Command Log Format (LogsManager.cs)
```
"Casting: ライトニング, [areaNum]"  → CastLightning()
"Casting: フレイム, [areaNum]"      → CastFlame()
"Moving to: [position]"             → MoveTo()
"Moving forward"                    → MoveForward()
"Moving backward"                   → MoveBackward()
```

## Coding Conventions

### C# Style
- PascalCase: クラス名、メソッド名、プロパティ
- camelCase: ローカル変数、パラメータ
- _camelCase: プライベートフィールド (optional)
- コルーチンには `IEnumerator` を使用
- DOTween でアニメーション制御

### Unity Patterns
- `[SerializeField]` でインスペクター公開
- `StartCoroutine()` で非同期処理
- イベント駆動でターン管理

## Important Notes

- BlocksEngine2 フォルダは外部アセット - 直接編集しない
- WebSocket 通信は非同期 - エラーハンドリング必須
- アニメーション完了を待ってから次のアクションへ
- HP/MP の UI は Screen Space で位置計算

## Build & Test

### Scenes
1. `Battle.unity` - メインバトルシーン
2. `CodeEditor.unity` - コードエディタ
3. `PythonTest.unity` - Python実行テスト

### Python Server
Python WebSocket サーバーを `localhost:8000` で起動してからゲームを実行



## コーディングの心得

- コードコメントはコードブロックの概要やコードで表現できない背景を日本語で簡潔に書くこと
- 関連コードを読んでからコードを書き始めること
  - 編集対象だけでなく、呼び出し元・依存先・類似実装も確認する
  - プロジェクトのコンテキストを理解してから着手する

## コード品質

- **現状優先** — 既存のコード構造・パターンを理解し、必要最低限の変更で実装する
- **YAGNI原則** — 現時点で不要な機能は実装しない
- **最小依存** — クラス間の依存を最小限に保ち、カプセル化を守る

## 攻撃的プログラミング（Offensive Programming）

問題は即座にクラッシュさせて原因を特定する。
- 存在すべき値は直接アクセス（防御的な存在チェック・null許容は不要）
- 問題があれば早期にクラッシュさせ、根本原因を発見する
- フレームワークが保証する値の検証は不要

## 条件分岐の方針

- 条件分岐は読みやすさを損なうため、可能な限り避ける
- フラグによる制御は最終手段（状態を持たせず、設計で解決する）
- 分岐が必要な場合は早期リターン / ガード節で浅く保つ


安全ルール（ファイル・コマンド操作の保護）
既存ファイルの上書き禁止（確認必須）既存ファイルを編集・上書きする前に、必ず「〇〇を上書きしますが、よろしいですか？」と確認する
確認なしに既存ファイルの内容を変更しない
可能であれば変更前のバックアップを作成する（例：filename.bak）

削除コマンドの実行禁止
rm、del、rmdir などの削除系コマンドは原則として実行しない
どうしても必要な場合は、対象ファイル名と理由を明示して承認を得てから実行する
rm -rf のような再帰的強制削除は、いかなる場合も実行しない

パッケージ追加は事前説明と承認が必要
npm install、pip install、brew install などのパッケージ追加コマンドは、実行前に以下を説明する：何をインストールするか（パッケージ名）
なぜ必要か（目的・用途）
影響範囲（グローバルかローカルか）
説明後、承認を得てから実行する

不明なコマンドは実行前に**日本語で**説明
私がエンジニアではないことを常に意識する
実行しようとするコマンドが技術的・専門的な場合は、実行前に日本語で以下を説明する：
このコマンドは何をするか（平易な言葉で）
実行するとどうなるか（結果・影響）
リスクがある場合はその内容
説明後、「実行してよいですか？」と確認する


do you want to proceed?のような許可を求めるときには、英語ではなく、日本語で説明して。


