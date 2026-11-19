# MagiCode — Python サーバーのセットアップ

MagiCode では、プレイヤーが書いた Python コードを **ローカルの Python サーバー** で実行し、その結果（実行ログ）をゲームが読み取ってキャラクターを動かします。
ゲームを遊ぶ・テストする前に、このサーバーを起動しておく必要があります。

---

## 1. 必要なもの

| 項目 | 内容 |
|---|---|
| Python | 3.10 以上（動作確認は 3.14.6） |
| パッケージ | `fastapi` / `uvicorn` / `websockets` |

> `websockets` が入っていないと、サーバーは起動していてもゲームからの接続がすべて失敗します（404 になります）。必ず3つともインストールしてください。

### インストール

```bash
python -m pip install fastapi uvicorn websockets
```

動作確認したバージョン：fastapi 0.138.1 / uvicorn 0.49.0 / websockets 17.4

---

## 2. サーバーのファイル

| ファイル | 役割 |
|---|---|
| `Assets/PythonServer/local_server.py` | **プロジェクトに入っているサーバー本体**（これを起動する） |
| `C:\Users\ys-03\OneDrive\Desktop\myStuff\CodeHunter\Python\CustomLogger\server.py` | 普段起動している作業用のコピー。`local_server.py` と**中身を同じに保つ** |
| `Assets/PythonServer/main.py` | 以前の API キー付きの版。今のゲームは使っていない |

片方を直したら、もう片方にも同じ変更を入れてください（ファイルごとコピーすれば OK）。

---

## 3. 起動方法

どちらか一方を起動すれば OK です。

### A. 作業用コピー（OneDrive）を起動する（普段の起動方法）

```bash
cd "C:/Users/ys-03/OneDrive/Desktop/myStuff/CodeHunter/Python/CustomLogger"
```

```bash
python -m uvicorn server:app --host 0.0.0.0 --port 8000 --reload
```

### B. プロジェクト内のサーバーを起動する

```bash
cd "C:/Users/ys-03/Unity_Workplaces/[reboot]MagiCode/Assets/PythonServer"
```

```bash
python -m uvicorn local_server:app --host 127.0.0.1 --port 8000 --reload
```

`python local_server.py` でも起動できます（この場合 `--reload` は効きません）。

> **PowerShell で移動するときの注意**：フォルダ名の `[reboot]` の `[ ]` が特別な記号として扱われ、普通の `cd` ではうまく移動できないことがあります。PowerShell では次のように `-LiteralPath` を付けてください。
> `Set-Location -LiteralPath "C:\Users\ys-03\Unity_Workplaces\[reboot]MagiCode\Assets\PythonServer"`

### 起動できたかの確認

- ターミナルに `Uvicorn running on http://...:8000` と出れば起動成功です。
- ゲームでコードを実行すると、ターミナルに `接続確立` → `実行完了: ○ステップ, エラー0件` → `切断` と出ます。
- `--reload` を付けて起動しておくと、`server.py` を保存し直したときに自動で再起動します。

止めるときはターミナルで **Ctrl + C** を押します。

---

## 4. ゲーム側の設定

| 項目 | 場所 | 値 |
|---|---|---|
| 接続先 URL | `LogsManager` の `Server Url`（Battle シーン） | `ws://localhost:8000/ws` |

サーバーのポート番号を変えた場合は、ここも合わせて変更してください。
（`PythonExecutor.cs` は PythonTest シーン用の古いテスト用スクリプトです）

---

## 5. ゲームとサーバーの約束ごと（変更するときの注意）

サーバーを書き換えるときは、次の決まりを崩さないでください。崩すとゲームがコードの結果を読めなくなります。

### 送受信の形式

- ゲーム → サーバー：`{"code": "<Python コード>"}`
- サーバー → ゲーム：`executionLogs` / `securityLogs` / `errors` / `finalResult` を持つ JSON
- ゲームは **実行のたびに接続し、結果を受け取ったら切断** します。

### 行動ログの書式

ゲームは実行ログの1行**全体**が次の形と一致したときだけ、行動として扱います。

| Python の関数 | サーバーが返すログ |
|---|---|
| `Attack()` | `Attacking` |
| `Cast(魔法名, エリア番号)` | `Casting: <ライトニング / フレイム / フリーズ>, <数字>` |
| `MoveTo(エリア番号)` | `Moving to: <数字>` |
| `MoveForward()` | `Moving forward` |
| `MoveBackward()` | `Moving backward` |
| `Swap(j, k)` | `Swap: <数字>, <数字>` |

対応している書式は `PlayerController.cs` の `CastLogPattern` などで決まっています。魔法を増やすときは、サーバーの `VALID_SPELLS` とゲーム側の両方を直してください。

### ゲームがコードの先頭に自動で差し込むもの

プレイヤーのコードを送る前に、ゲームが次のような変数をコードの先頭に足しています（プレイヤーには見えません）。

| 変数 | 使われるステージ |
|---|---|
| `enemies` / `enemy` | すべて（`EnemyInfo(element='フレイム', hp=10)` の形。`enemy.element` で読める） |
| `EnemiesHP` | ソートボス |
| `numbers` | コアボス |
| `count` | キーゴーレム |

`EnemyInfo` はサーバー側（`server.py`）で定義しています。サーバーが古いと `NameError` になります。
差し込んだ行の分だけ、エラーの行番号はゲーム側でずらして表示しています。

### 実行の制限（サーバー側）

| 制限 | 値 |
|---|---|
| コードの長さ | 10KB まで |
| 処理ステップ数 | 3000 まで（`MAX_EXECUTION_STEPS`） |
| `for` の回数 | 1000 回まで |
| `while` の回数 | 100 回まで |

ゲームは、エラーが1つでも返ってきたコードは実行しません。

---

## 6. うまく動かないとき

| 症状 | 確認すること |
|---|---|
| ゲームからつながらない／ターミナルに `404` が出る | `websockets` がインストールされているか（手順 1） |
| ポートが使用中と言われる | すでに別のターミナルでサーバーが起動していないか |
| `EnemyInfo` が見つからない（NameError） | 古いサーバーを起動していないか。`local_server.py` と `server.py` が同じ中身か |
| サーバーを直したのに反映されない | `--reload` 付きで起動しているか。付けていなければ Ctrl + C で止めて起動し直す |
