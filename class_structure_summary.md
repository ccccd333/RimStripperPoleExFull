# RimStripperPoleEx クラス・構成ファイル概要

RimStripperPoleEx Mod のソースコードを構成する C# クラスおよび関連する主要な Def の定義と役割のまとめです。

---

## 1. 建築物と定義クラス

### [Building_StripperPole.cs](file:///c:/Users/daichi/source/repos/RimStripperPoleEx/1.6/Source/Stripper/Building_StripperPole.cs)

ストリッパーポール（およびダンススポット）のゲーム内オブジェクトとその Def を表すクラスです。

#### `Building_StripperPole_Def` (派生元: `ThingDef`)
XMLで設定可能なパラメータを保持します。
*   `inspectOwnerDisplayCount`: 調査パネル（画面下部）に表示するオーナーの最大数（デフォルト 3）。
*   `jobName`: ダンスをする際に呼び出す JobDef の名前（デフォルト `"UseStripperPole"`）。
*   `watchRadius`: 観客が観賞可能な半径（デフォルト 5マス / スポットは3マス）。
*   `joyGainFactor`: 娯楽の獲得倍率（デフォルト 1.0 / スポットは0.6）。

#### `Building_StripperPole` (派生元: `Building`)
オブジェクト本体のロジックを制御します。
*   `owners`: ベッドのようにポールに割り当てられたポーンのリストを取得します (`CompAssignableToPawn` を利用)。
*   `CanUse(Pawn)`: 指定したポーンがこのポールを使用できるか判定します（オーナーであるか、あるいは誰もオーナーに割り当てられていない場合に `true`）。
*   `GetInspectString()`: 画面下部の調査パネルに表示するテキスト（オーナー一覧、現在/前回のダンス情報など）を構築します。
*   `GetFloatMenuOptions(Pawn)`: ポールを右クリックした際のメニューを処理します。大人のプレイヤーポーンに対して「**Do a dance (ダンスをする)**」コマンドを提供します（予約状況やオーナー権限、年齢のバリデーションを含む）。

---

## 2. ジョブとドライバー（動作ロジック）

### [JobDriver_UseStripperPole.cs](file:///c:/Users/daichi/source/repos/RimStripperPoleEx/1.6/Source/Stripper/JobDriver_UseStripperPole.cs)

ダンサーがポールで踊る一連のアクションを制御するドライバーです。

#### `Job_UseStripperPole_Def` (派生元: `JobDef`)
ダンスジョブ用の設定値。
*   `turnMin` / `turnMax`: 体の向き（Rotation）を変える最小/最大の間隔（ticks）。
*   `undressMin` / `undressMax`: 衣服を脱ぐ最小/最大の間隔（ticks）。
*   `recordCountName`: カウントする統計レコードの名称。
*   `logDef`: プレイログの定義。

#### `JobDriver_UseStripperPole`
実際のダンス中の処理を担当します。
*   `Init()`: ダンス開始前にポーンの現在の衣服情報を取得し、脱ぐ順序（外側のレイヤー優先）にソートして保存。強制着用フラグも控えます。
*   `MakeNewToils()`: 「ポールへ移動する」「踊る」という Toil を生成します。
*   `StripperPoleTick()`: 毎フレーム実行されるダンス中の挙動。
    *   `TickFacing()`: 一定間隔でランダムな方向を向きます。
    *   `TickClothes()`: 一定間隔で衣服を1枚ずつ脱いでインベントリに移し、ハートのエフェクトを表示します。
    *   `TickStats()`: 娯楽値の加算チェックと、ポールに表示するリアルタイムのダンス情報を更新します。
*   **ダンス終了時 (FinishAction)**:
    *   インベントリに移した衣服をすべて元通り着用させ、強制着用フラグを復元。
    *   娯楽室で過ごしたことによる思考（Rec Room Thought）を付与。
    *   プレイログ (`LogEntry_UseStripperPole`) の追加と、統計レコード（ダンス回数と時間）の加算。

---

### [JobDriver_WatchStripperPole.cs](file:///c:/Users/daichi/source/repos/RimStripperPoleEx/1.6/Source/Stripper/JobDriver_WatchStripperPole.cs)

観客がダンスを観賞するアクションを制御するドライバーです。

#### `Job_WatchStripperPole_Def` (派生元: `JobDef`)
観賞ジョブ用の設定値。
*   `recordCountName`: カウントする統計レコードの名称。
*   `logDef`: プレイログの定義。
*   `thoughtDef`: 観賞後にダンサーに対して抱く社会的思考（好感度）の定義。

#### `JobDriver_WatchStripperPole`
*   `MakeNewToils()`: 「指定した観賞位置（椅子）へ移動する」「観る」 Toil を生成。
    *   ダンス中のポーン（`currentDancer`）が居なくなった場合、観賞ジョブは中断（失敗終了）します。
*   `WatchTickAction()`: 毎フレームダンサーの方向を向き、椅子の快適さ（Comfort）と娯楽値（Joy）を得ます。
*   **観賞終了時 (FinishAction)**:
    *   娯楽室で過ごしたことによる思考を付与。
    *   プレイログの追加と、統計レコード（観賞回数と時間）の加算。
    *   **好感度変化の付与 (`AddThought`)**: ダンサーの「美しさ（`PawnBeauty`）」に比例したオピニオン修正（Social Memory）を付与します。

---

## 3. ジョブ・娯楽の割り当て (Giver)

### [JoyGiver_UseStripperPole.cs](file:///c:/Users/daichi/source/repos/RimStripperPoleEx/1.6/Source/Stripper/JoyGiver_UseStripperPole.cs)
ポーンが自発的に「ダンスをする」娯楽を選択するためのクラス。
*   ポーンが大人（Adult）であること、および対象のポールが使用可能（オーナー設定が適合）であることを確認し、ダンスジョブを割り当てます。

### [JobGiver_UseStripperPole.cs](file:///c:/Users/daichi/source/repos/RimStripperPoleEx/1.6/Source/Stripper/JobGiver_UseStripperPole.cs)
AIの思考ツリー（ThinkTree）から、ポーンの通常行動の合間に自発的にダンスジョブを割り込むためのクラス。
*   ポーンが非徴兵かつ無作業（または睡眠中）でプレイヤー派閥の場合、マップ上の利用可能なポールを探してダンスジョブを生成します。

### [JoyGiver_WatchStripperPole.cs](file:///c:/Users/daichi/source/repos/RimStripperPoleEx/1.6/Source/Stripper/JoyGiver_WatchStripperPole.cs)
ポーンが自発的に「ダンスを観賞する」娯楽を選択するためのクラス。
*   ポールで現在誰かがダンス中であり、自分自身がダンサーではない場合に、最適な観賞用セル（椅子）を探して観賞ジョブを割り当てます。

### [WorkGiver_UseStripperPole.cs](file:///c:/Users/daichi/source/repos/RimStripperPoleEx/1.6/Source/Stripper/WorkGiver_UseStripperPole.cs)
「仕事」としてダンスを行うための WorkGiver です。

---

## 4. ヘルパー・便利クラス

### [StripperPoleHelper.cs](file:///c:/Users/daichi/source/repos/RimStripperPoleEx/1.6/Source/Stripper/StripperPoleHelper.cs)

ポールと観客の位置計算や検索を一元管理するユーティリティクラスです。

*   `Register(Building_StripperPole)`: 配置されたポールの ThingDef を登録します。
*   `GetStripperPoleForPawn(Pawn)`: 指定ポーンから一番近く、かつ予約・到達・使用可能なポールを検索して返します（オーナーに割り当てられている場合はそのポールを最優先）。
*   `WatchCells(...)`: ポールの周囲かつ、同一の部屋（Room）で、かつポールへの視線（Line of Sight）が通る「立てる」セルの一覧を算出します。
*   `TryFindBestWatchCell(...)`: 観客用のセルを探します。前述の `WatchCells` の中から、**座れる家具（椅子など）が設置されており、ポーンが予約可能なセル**をランダムにシャッフルして決定します。

### [PlaceWorker_StripperPoleRadius.cs](file:///c:/Users/daichi/source/repos/RimStripperPoleEx/1.6/Source/Stripper/PlaceWorker_StripperPoleRadius.cs)
建築予定地（Ghost）を配置する際、および選択時に、観賞可能なセルの範囲（`WatchCells`）の輪郭を白枠でハイライト表示します。

---

## 5. プレイログ (LogEntry)

### [LogEntry_UseStripperPole.cs](file:///c:/Users/daichi/source/repos/RimStripperPoleEx/1.6/Source/Stripper/LogEntry_UseStripperPole.cs)
ダンスを踊った際のプレイログイベント。
*   文法ルール（Grammar Rules）に基づき、「[DANCER] がダンスを踊った」というテキストを作成します。

### [LogEntry_WatchStripperPole.cs](file:///c:/Users/daichi/source/repos/RimStripperPoleEx/1.6/Source/Stripper/LogEntry_WatchStripperPole.cs)
ダンスを観賞した際のプレイログイベント。
*   「[WATCHER] が [DANCER] のダンスを観た」というテキストを作成します。
*   ログエントリーをダブルクリックした際に、ダンサーのいる場所へカメラをジャンプする処理が含まれています。

---

## 6. 思考ノード (ThinkNode)

### [ThinkNode_ChancePerHour_UsingStripperPole.cs](file:///c:/Users/daichi/source/repos/RimStripperPoleEx/1.6/Source/Stripper/ThinkNode_ChancePerHour_UsingStripperPole.cs)
AIの意思決定において、1時間あたりにポーンがダンスを踊りたくなる確率の基準値（MTB時間 = 0.1時間）を定義します。

### [ThinkNode_ConditionalCanUseStripperPole.cs](file:///c:/Users/daichi/source/repos/RimStripperPoleEx/1.6/Source/Stripper/ThinkNode_ConditionalCanUseStripperPole.cs)
ポーンがポールを使用可能な精神・環境状態（プレイヤー派閥に非敵対的、マップが存在、非徴兵など）であるかを判定する ThinkNode 条件です。
