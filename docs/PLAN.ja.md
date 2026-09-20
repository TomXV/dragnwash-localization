# Drag'n Wash ローカライズ Mod 計画書

[English](PLAN.md)

## 目的

「Drag'n Wash」にBepInExを導入し、日本語・簡体字中国語（および将来的に他言語）へのローカライズを可能にする。
翻訳者はコードを書かずに、決められた形式の翻訳ファイルを`Translations/<locale>/` に置くだけで参加できるようにする。

## 調査結果（2026-09-11 時点、Steam版で確認）

| 項目       | 結果                                                                                                                                                                              |
|------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| エンジン   | Unity **6000.3.14f1**（Unity 6.3）                                                                                                                                                |
| ビルド形式 | **Mono**（IL2CPP ではない）, x64                                                                                                                                                  |
| UI文字列   | **Unity Localization** パッケージ（`Unity.Localization.dll`）＋ Addressables 経由のStringTable（`StreamingAssets/aa/StandaloneWindows64/localization-locales_assets_all.bundle`） |
| 会話文     | **Yarn Spinner**（`YarnSpinner.dll` / `YarnSpinner.Unity.dll` / `Yarn.CsvHelper.dll`）。CSVベースのローカライズ機構を標準搭載                                                     |
| フォント   | Unity 6.3 の TextMeshPro は Dynamic OS Font Fallback に対応。Windows標準搭載のCJKフォント（游ゴシック / Microsoft YaHei 等）で代替表示できる可能性が高い                          |

### 結論

ゲーム本体の `.dll` やアセットを書き換えずに、**BepInEx + Harmony によるランタイムのテキスト差し替え**
で日本語化・中国語化は十分に実現可能と判断。
実機検証（Phase 1）の結果、当初想定していたUnity Localization経由のフックは不採用とし、
**TextMeshProのテキスト設定自体をフックする方式**を採用した
（詳細は下記「Phase 1実機検証で判明したこと」を参照）。

## Phase 1 実機検証で判明したこと（2026-09-11）

- `Unity.Localization.dll` は同梱されているが、メインメニュー・オプション画面・実際の
  会話文のいずれも `LocalizeStringEvent` / `LocalizedString.RefreshString` /
  `LocalizedStringDatabase.GenerateLocalizedString` を一切経由していないことを、
  Harmonyでの呼び出し検知パッチにより実機で確認した（メニュー操作・会話表示中でもヒット数0件）。
  つまりUnity Localizationパッケージは現状ほぼ未使用の依存関係。
- 代わりに、UIボタン・設定項目・会話文はすべて **`TMPro.TMP_Text.text`（プロパティセッター）**
  を経由して画面に反映されることを確認。
  これはUnity Localizationを使っていようがいまいが関係ない、エンジンレベルの共通経路であるため、
  ここをフックすればゲーム内のほぼ全テキストを一括で捕捉・差し替えできる。
- 翻訳データの構造も、当初案の「テーブル名＋キー」方式から、
  **「画面に表示される英語原文そのもの → 訳文」という単純な対応表**に変更した。
  これにより翻訳者はUnityの内部キー名を一切知る必要がなく、
  ゲーム画面に出ている文章をそのままCSVに書き写すだけで参加できる。
- CJKフォント表示: 案の定、ゲーム側のTMPフォントアセットには日本語・中国語のグリフが含まれておらず、
  翻訳を差し込んだ直後は文字が四角（tofu）で表示された。
  Unity 6.3の`TMP_FontAsset.CreateFontAsset(familyName, styleName)` API（`AtlasPopulationMode.DynamicOS`）を使い、
  Windows標準搭載の游ゴシック/メイリオ/MSゴシック（日本語）・Microsoft YaHei（中国語）から動的にTMPフォントアセットを生成し、
  `TMP_Settings.fallbackFontAssets` に登録することで解決。
  ゲーム本体のフォントアセットには一切手を加えていない。実機で日本語表示を確認済み。

## アーキテクチャ（確定版）

1. **BepInEx 5.4.23.5（x64, Mono）** をゲームフォルダーへ導入 — 実機導入・起動確認済み
2. 自作BepInExプラグイン `DragNWashLocalization`
   - `TMPro.TMP_Text` の `text` セッターをHarmonyでフックし（Prefixで`ref string value`を書き換え）、
     外部ファイルに一致する原文があれば訳文に差し替え
   - 一致する訳文がない原文は自動的に `Translations/_discovered/strings.csv` に記録し、
     翻訳者が「まだ訳されていない文章」を把握できるようにする
   - 起動時に `FontFallback.EnsureCjkFallback()` でCJKフォールバックフォントを注入
   - BepInEx configで言語切り替え（`TargetLocale`: `ja` / `zh-Hans` / 手動指定）を提供
3. **翻訳ファイル形式**（誰でも「投げるだけ」で参加できる形式）
   - `Translations/<locale>/strings.csv` — 当初は `source_en,translation` の2列のみ。
     公開形式はのちにハッシュキーへ変更（後述）
   - UIも会話文も同じ1ファイルで扱える（フックが共通のため区別不要）
   - 翻訳者は `Translations/_discovered/strings.csv`（未訳一覧、ゲームをプレイすると
     自動生成される）を参考に、`Translations/<locale>/strings.csv` の該当行を埋めてPRを送るだけ

## リポジトリ構成

```text
dragnwash-localization/
  README.md                    プロジェクト概要・翻訳者向け手順
  LICENSE
  docs/PLAN.md                 この計画書
  src/DragNWashLocalization/   BepInExプラグイン（C#プロジェクト）
    Plugin.cs                  エントリポイント、Harmony初期化、config
    TmpTextPatches.cs          TMP_Text.text のHarmonyフック
    TranslationStore.cs        翻訳データの読み込み・検索・未訳ログ出力
    CsvReader.cs                依存なしの簡易CSVパーサ
    FontFallback.cs             CJKフォールバックフォントの動的生成・登録
  Translations/
    ja/strings.csv
    zh-Hans/strings.csv
  tools/                       （今後）未訳エクスポート等の補助スクリプト
  .gitignore
```

ゲーム本体のアセット・コードはリポジトリに含めない（著作権保護のため）。
配布するのはプラグインのコードと翻訳ファイルのみ。

## Phase 1.5: 会話ログ一括抽出＆デバッグUI（2026-09-11）

翻訳者体験を大きく改善する2機能を追加し、実機で動作確認済み。

- **`DialogueDumper`（F6キー）**:
  `YarnProject.baseLocalization` の内部テーブル（`entries`:行ID→英語原文の辞書）をリフレクションで読み出し、
  ロード済みの全会話行を`Translations/_discovered/dialogue_lines.csv` に一括書き出しする。
  実機で **1839行を一括抽出** できることを確認済み。
  プレイして会話を発生させる必要はなく、該当シーンが一度ロードされていれば（YarnProjectがメモリに乗っていれば）足りる。
  - 抽出結果には `<gradient="gold"><b>...</b></gradient><size=70%>...` のようなTMP書式
    タグを含む行も原文のまま出てくることを確認。**追加のCSV列は不要**で、
    既存の`source_en,translation` 2列方式のまま、タグ構造を保持して中のテキストだけ訳せば書式も再現される。
  - 抽出結果はゲーム本体の著作物（会話文そのもの）を含むため、リポジトリにはコミットせず`.gitignore` 対象とした。
    翻訳者は各自プラグイン導入後にF6を押して手元に再生成する。
- **デバッグメニュー（F1キー）**:
  OnGUIによる簡易オーバーレイ。`Translations/` 配下のロケールフォルダーを自動検出してボタン表示し、
  クリックで即座に言語切り替え（再起動不要）。
  会話ログダンプもボタンから実行可能。ゲーム本体のOptions画面には
  一切手を加えていない（改造対象が壊れにくい／ゲーム更新の影響を受けにくい設計）。

## Phase 2: 実会話での差し替え確認（2026-09-11）

### 発見: `TMP_Text.SetText()` は `text` プロパティを経由しない別経路

実際の会話（NPC頭上の吹き出しUI）で訳文が反映されないケースを実機で確認。
調査の結果、Yarn Spinner標準のタイプライター（`LetterTypewriter`/`WordTypewriter`）は
`Text.text = ...`を使っており問題なかったが、
**このゲーム独自の吹き出しUIコンポーネントは`TMP_Text.SetText(string)` を直接呼んでいた**。
`SetText`を逆コンパイルして確認したところ、
`text` プロパティのセッターを一切経由せず内部フィールド（`m_text`）に直接書き込む実装だったため、
`text` セッターだけへのHarmonyパッチでは捕捉できなかった。

対策として `TMP_Text.SetText(string)` と `SetText(string, bool)` にも同じ書き換えロジック
（`TmpTextHook.Rewrite`）を適用するパッチを追加し、実機で解決を確認した。

**教訓**:
TMPのテキスト設定はプロパティ以外にも複数のAPIがあるため、今後別の未対応経路が見つかる可能性は残る
（デバッグログの `[--]` に該当しそうな文が出ない＝別経路の疑い、という切り分け方が有効）。

### デバッグ体験の改善

ユーザーからのフィードバックを受け、以下を実装：

- ゲーム内ログビューアー（F1メニュー内）:
   `[OK] "原文" -> "訳文"` / `[--] "未対応原文"` をリアルタイム表示。外部ログファイルを都度確認する必要がなくなった。
- ログの自動スクロール、ダークテーマ（デフォルトの白背景OnGUIスキンは可読性が低いため）。
- 「Clear log」ボタンの不具合を修正:
  ログ表示だけでなく、内部の重複排除トラッキングもリセットするようにした（さもないと一度表示した文字列は二度とログに出なくなっていた）。

## Phase 3: 訳文の本格追加とレイアウト確認（2026-09-11）

Phase 3の目的は、(1) 日本語・簡体字中国語の訳を本格的に追加し、(2) CJK文字がゲームのレイアウトを崩さないことを確認すること。

このうち「ツールと訳文の整備」までを実装し、「実機での目視確認」は今後のQA作業として残っている。

### 訳文の追加

- `Translations/zh-Hans/strings.csv` を新規作成し、`ja/strings.csv` にある全原文の
  簡体字中国語訳を追加した（95エントリ、`ja` とキー順が一致）。
  - 中国語フォントはMicrosoft YaHei系を既定で使用。
    実機での表示確認はPhase 3の残作業（下記リスク参照）。
- `Translations/ja/strings.csv` に、ほぼどのUnityゲームにも存在する共通UI文言（
  Play/Continue/Pause/Settings/Language/Volume/Apply/Confirmなど）を追加した。
  - これらは「画面に実際に出るか」を実機で確認する前のベストエフォート追加であり、
    一致しない原文は従来どおり `_discovered/strings.csv` に記録されるだけなので害はない。
- 翻訳キーは完全一致方式のため、追加する原文は「画面に出る英文そのまま」でなければならない。
  `_discovered/strings.csv`（著作物のため未コミット）を各自生成して、本格的な訳の拡充を続ける。

### レイアウト崩れの検出（LayoutChecker）

CJK文字はラテン文字の約2倍の幅で描画されるため、訳文は原文より短くてもボタンやラベルからはみ出すことがある。
目視確認の前に、リスクの高い文字列を絞り込む`LayoutChecker` を追加した。

- 幅の推定は「CJK等の全角グリフ=2、その他=1」というヒューリスティック。
  フォントメトリクスや折り返し・コンテナー幅は見ていない（あくまでトリアージ用）。
- 起動時と、デバッグメニューの **Check layout** ボタンから実行できる。
  しきい値は`[Debug] LayoutRiskThreshold`（既定 `1.4`、訳文の推定幅が原文のこの倍率を超えると記録）。
- 結果は `Translations/_discovered/layout_risks.csv` に書き出される（
  `source_en,translation,source_width,translation_width,ratio`）。

### 残作業

- 実機で `ja` / `zh-Hans` を順に切り替え、`layout_risks.csv` に挙がった文字列を
  中心に目視でレイアウト崩れがないか確認する（とくにOptions画面・会話吹き出し）。
- 中国語フォント（Microsoft YaHei）での実表示確認（Phase 1で登録のみ確認済み）。
- `_discovered/strings.csv` をもとに訳文をさらに拡充する。

## 静的UIテキストの取りこぼし（2026-09-11）

ポーズメニューの `Pause Menu` / `Resume` / `Options` / `Quit` が英語のまま表示され、
ログにも `_discovered` にも記録が一切なかった。フックに到達していなかった。

原因は、プレハブ上で設定され実行時に一度も代入されないTMPテキストが
`m_text` へ直接デシリアライズされること。`text` セッターも `SetText` も呼ばれないため、
傍受する対象が存在しない。`SetText` バイパスに続く3つ目の経路で、今回は
「呼び出し自体がない」という点がこれまでと異なる。メインメニューの
`New Game` / `Credits` / `Quit` / `Back` / `Options` がすべて未適用だったのも同じ理由。

対応:

- `TextMeshProUGUI.OnEnable` と `TextMeshPro.OnEnable` にPostfixを追加し、
  コンポーネントが有効化された時点（＝メニューが表示された時点）で現在のテキストを翻訳する。
  セッター経由で既知のコンポーネントは二重適用を避けるため除外し、訳文の代入時は `SuppressRewrite` で自己再入を止める。
- ただしこれは「表示されれば拾える」だけで、全メニューを開いて回る必要が残る。
  そこで `UiTextDumper` を追加した。`Resources.FindObjectsOfTypeAll<TMP_Text>()` は
  非アクティブなオブジェクトも列挙するため、
  ポーズメニューを開かずにそのシーンの全UI文言を `_discovered/ui_texts.csv` へ書き出せる（F7）。
  翻訳済みコンポーネントは `.text` が訳文になっているので、`TmpTextHook.TryGetTrackedSource` で元の英文を引く。

### 調査時の誤判定について

ゲームアセットを長さ接頭辞付き文字列として検索し、
`Resume` を「アニメーションのボーン名（`Resume.Bone.004`）だから表示テキストではない」と判定したが、
これは誤りだった。
ボーン名の `Resume` と、ポーズメニューのラベルの `Resume` が別々に存在していた。
件数が少なく文脈も紛らわしかったため見落とした。
アセット検索は「存在しないことの証明」には使えるが、
「表示テキストでないことの証明」には弱い。
`ui_texts.csv` が実データを出すので、今後はそちらを根拠にする。

なお画像としてデザインされたボタン（`Back` / `Save` / `Set Default` / `OPTIONS` の見出しなど）は
翻訳対象外とする方針。

## IMGUIの入力がゲームに抜ける問題（2026-09-11）

デバッグウィンドウ上でクリックすると、背後のゲームUIにも当たり判定が入っていた。
IMGUIはゲームの入力読み取りを止めないため、OnGUI側では防げない。uGUIは
EventSystem経由、ゲーム本体はInput System経由で読んでおり、どちらもIMGUIを見ない。

`InputBlocker` を追加し、ポインターがウィンドウ上にある間だけ
`EventSystem.enabled = false` と `PlayerInput.DeactivateInput()` で両方を止める。

「メニューが開いている間」ではなく「ポインターが乗っている間」に限定したのは、
ログをオーバーレイとして出したままプレイできる利点を残すため。

マウスボタンを押している間はポインターが外に出ても遮断を維持する。

ウィンドウやリサイズグリップをドラッグして画面端を越えたときに、
操作の途中でゲーム側へ制御が渡るのを防ぐため。`OnDestroy` で必ず解除する。

## LayoutChecker を実測方式へ（2026-09-11）

初版は原文と訳文の文字幅（全角=2、半角=1）を比較していたが、
コンテナーの幅をまったく見ていないため、余裕のあるラベルまで片端から挙がっていた。
実際`Audio → オーディオ` や `Options → オプション` が「2.0倍」として報告される一方、
スクリーンショットではどちらも余裕をもって収まっていた。
誤検出が大半で、目視確認の絞り込みという本来の役割を果たせていなかった。

TMP自身に問い合わせる方式へ変更した。
`GetPreferredValues` はそのコンポーネント固有のフォント・サイズ・字間で訳文に必要な大きさを返すので、
`rectTransform.rect` と比べれば実際に収まるかがわかる。
折り返しが無効なら幅を、有効なら `rect.width` 制約下での必要高さを比較する。
出力に `axis` / `required_px` / `available_px` / `object_path` を追加し、どこがどれだけはみ出しているかを直接示す。

設定キーは `LayoutRiskThreshold` から `LayoutOverflowThreshold`（既定 `1.0`）へ改名した。
意味が「原文比」から「コンテナー比」に変わったため、既存の設定ファイルに残った `1.4` が
そのまま使われると実際のはみ出しを見逃すことになる。

## UI文言の全量確定（2026-09-11）

F7（`UiTextDumper`）を実機で実行し、読み込み済みの全TMPテキスト40件を取得した。
`object_path` を併記したことで、各文字列の正体が推測なしで判別できた。

- 35件は翻訳済み
- `Option A` はUnityドロップダウンの **Template** 配下、
  `Option Text` はYarn選択肢のプレハブ雛形で、どちらも実行時に本文へ置き換わるため画面には出ない。
  `​` はTMPが空の入力欄に入れるゼロ幅スペース。3件とも `ignore.txt` へ追加した。
- 実質的に未翻訳だったのは入力欄のプレースホルダー `Enter text...` の1件のみ。

見つかった不具合2件:

- `UiTextDumper` が `Trim()` してから翻訳を引いていたため、実際の文字列が`"Version"` であるバージョン表示が、
  `"Version"` として「未翻訳」に見えていた。
  翻訳者が `Version` の行を足しても永久に一致しない。
  トリムは空判定のみに使うよう修正。
- `LayoutChecker.Report` を `Awake` から呼んでいたが、実測方式にした後は
- その時点でシーンが未読み込みのため測定対象が存在しない（`No translated text is on screen yet`）。
  起動時呼び出しを削除し、デバッグメニューのボタン実行のみとした。

これでUI文言の翻訳は完了。
残るはレイアウトの目視確認と会話文の分量。

## レガシー入力APIによる連鎖障害（2026-09-11）

`InputBlocker` 導入後、レイアウトチェックとUIダンプの両方が動かなくなった。原因は1つ。

```text
InvalidOperationException: You are trying to read Input using the UnityEngine.Input
class, but you have switched active Input handling to Input System package.
```

このゲームは新Input Systemのみを使う設定で、レガシー `UnityEngine.Input` は例外を投げる。
ポインター位置の取得に `Input.mousePosition` を使ったのが誤りだった。
BepInExの`KeyboardShortcut` は内部で新旧を判別するため、F1などのホットキーは動いており、
レガシーAPIが使えると誤認した。

被害が入力遮断だけで済まなかったのは、`UpdateInputBlocking()` を `Update()` の前半に置いたため。
毎フレーム例外で `Update()` が中断し、後続のダンプ・レイアウトチェックの処理に到達しなかった。
**1箇所の例外が、無関係な機能を2つ巻き添えにしていた。**

対応:

- ポインター取得を `Mouse.current`（Input System）に変更。
- `UpdateInputBlocking()` を `Update()` の**最後**へ移動し、try-catchで囲んだ。
  さらに一度失敗したらフラグを立てて以後呼ばない（毎フレーム例外を投げ続けないため）。
  順序と例外隔離の両方を直したのは、片方だけでは同種の事故が再発しうるため。
- 未使用になった `UnityEngine.InputLegacyModule` の参照とDLLを削除。このゲームでは
  使ってはいけないAPIなので、参照可能なまま残さない。
- `PlayerInput` を使わずInputActionを直接駆動するゲームでは遮断が空振りしうるため、
  初回遮断時に `EventSystem` の有無とsuspendした `PlayerInput` の数をログに出す。

教訓:

BepInEx側のユーティリティが動くことは、Unity APIが直接使えることを意味しない。
また `Update()` 内の処理順は、例外が起きたときに何を巻き添えにするかを決める。

## 入力遮断のAPI選択ミス（2026-09-11）

`PlayerInput.DeactivateInput()` で遮断する実装にしていたが、
このゲームには`PlayerInput` コンポーネントが存在しない。
`Assembly-CSharp` を調べたところ、参照している入力型は以下のとおりだった。

```text
PlayerInput            0   ← 使っていない
InputActionAsset       1
InputActionReference   1
InputActionMap         1
```

`PlayerInput.all` は空なので、遮断処理は何も止めずに成功したように見えていた。
`InputActionAsset` を `Resources.FindObjectsOfTypeAll` で列挙し、
有効な`InputActionMap` だけを `Disable()` する方式へ変更した。
復帰時は「元々有効だったマップ」だけを `Enable()` する。
`InputActionAsset.Enable()` は全マップを有効にしてしまい、元の状態と異なる結果になるため使わない。

遮断して何も止まらなかった場合はログに明示する
（`No enabled action maps were found, so gameplay input is NOT blocked.`）。
空振りを成功として報告しないため。

## 進め方の反省（2026-09-11）

ユーザーから「2つとも直っていない」との報告。
調査したところ、修正版DLLが配置されていなかった。
「ゲーム終了を監視して配置」する方式にしていたため、ゲームが起動したままだと配置が走らず、
ユーザーは修正前のDLLをテストしていた。

加えて、実機で動かせない変更を続けて投入していたため、
不具合が積み上がってからまとめて発覚する形になっていた。

今後は以下を守る。

- テストを依頼する前に、必ず配置済みDLLと成果物のハッシュ一致を確認して提示する。
- 「動くはず」で依頼せず、事前に検証できることは先に検証する（例: `PlayerInput` の有無はゲームアセットの調査で事前に判明した）。

## Phase 4: 翻訳者向け文書と配布の整備（2026-09-11）

翻訳者とメンテナ双方の導線を整えた。

- **`CONTRIBUTING.md`**:
  翻訳者向けガイド。
  CSVの形式（当時の `source_en,translation` の2列・完全一致）、書式タグの扱い、
  F6/F7/自動記録による未訳の見つけ方、`ignore.txt`、ロケールの追加、PR前のチェック、
  ルール（ゲーム資産・`_discovered/` をコミットしない）をまとめた。
- **`docs/RELEASING.md`**:
  メンテナ向け配布手順。
  ビルドにゲーム由来の参照アセンブリ（`libs/`）が必要でリポジトリにコミットできないため、
  CIではなくローカルでビルドしてGitHub Releasesへアップロードする方針を明文化した。
- **`tools/pack.ps1`**:
  `dotnet build` から `release/DragNWashLocalization-<version>.zip`の作成までを自動化。
  `BepInEx/plugins/DragNWashLocalization/{dll, Translations/}` と`README.md` を、
  ゲームルートに展開すれば導入できる構造で固める。
  ランタイム生成物の`_discovered/`（ゲームの著作物）は同梱しない。
- READMEにコントリビュート／配布の導線を追加し、`.gitignore` に `release/` と `*.zip`を追加した。

## 会話ダンプを実行順に（2026-09-12）

`dialogue_lines.csv` は `line_id` 順で出力していたが、
IDは内容のハッシュなのでゲーム内の流れとは無関係なならびになっていた。
誰が誰に答えているのか、どの返答がどの質問に属するのか、会話がどこから始まるのかが分からず、
実質的に訳せない状態だった。

コンパイル済みの `Yarn.Program` には本来の順序が入っている。
ノードが会話の単位で、その `Instructions` は上から順に実行される。
`RunLine` と `AddOption` を拾えば台本どおりの順序が復元でき、
台詞と選択肢の区別も付く（`Program.LineIDsForNode` と同じ走査だが、種別を残すため自前で歩いている）。

出力列は `yarn_project,node,order,kind,line_id,source_en,translation,tags`。
`translation` には現在の訳を入れて出すので、再ダンプで作業が失われない。
どのノードからも参照されない行は末尾に `(not reached from any node)` として出す
（取りこぼしを翻訳者が疑わなくて済むように）。

実機確認: 1839行すべてが195ノードのいずれかに属し、未参照は0件。
ノード名は `Alexander_2_intro` のように「キャラクター＿回数＿場面」で、
Conrad 547行 / Ryan 507行 / Alexander 472行の3体が主要キャラクターと判明した。
`RyanMuddy` `ConradBeatup` `RyanDate` は同一キャラの状態違い。

注意: YarnProjectはタイトル画面では読み込まれていない。F6はセーブをロードしてから押す。

## 一括翻訳の照合と整理（2026-09-12）

`ja/strings.csv` に会話の一括翻訳が入り1660件になった。

実行順ダンプと照合したところ:

- 会話の一意な原文1601件のうち1566件が一致（97.8%）。
- 「未訳」35件の内訳は、`Test line N` / `title: X_Done` / `==` などの
  開発デバッグ文が33件（すべて `ignore.txt` で除外済み）と、本当の未訳が `Yes!` と `no` の2件。
- 一方で、どのダンプにも一致しない原文が72件あった。
  うち21件はF7実行時に読み込まれていなかったOptions画面などの実在UI（問題なし）。
  残りは**会話の一部だけをキーにした行**だった（例: `I'm in need of a clean...` は
  実際の行`Yes. I'm in need of a clean...` の後半、`HELL YEAH!` は `HELL YEAH!` の一部）。
  完全一致方式なのでこれらは永久に適用されない。

対応:

- 部分キーを実際の行と照合（部分文字列一致→ `difflib` の類似度）。
  実際の行が別途訳済みなら冗長として削除（46件）。
  訳を移す必要があるものは0件だった。
- `Yes!` → `はい！`、`no` → `いいえ。` を追加。
  既訳の `No='いいえ。'` `yes='はい。'` に口調を合わせた。
- ゲーム内に一致行が存在しない5件（`The watermelon!` `Alright.` `Nice to see you.`
  `I am a professor!` `You bipeds tend to savor your meals.`）は残した。
  別ビルド由来か転記ミスか判断できないため、翻訳者の確認待ち。
  害はない（一致しないだけ）。

結果: 1616件、空欄0、重複0。会話原文1601件中1568件が訳済み（97.9%）で、
残り33件は除外対象のデバッグ文。**会話文は実質的に完訳。**
1616件・978文字の事前焼き込みで起動に問題がないことも実機で確認した（`Prewarmed 978/978`、例外なし）。

`zh-Hans` は95件のままで、会話文は未着手。

## 日本語訳の全面やり直し（2026-09-12）

一括翻訳はスラングと口調を取り違えていた。
コンラッドの豪州スラング（"Yeah, nah" は「いや」の意、`G'day mate!` `Good as!` と話す乱暴者）が
直訳されて「うん、いや。相棒」のように意味が反転していた。
アレクサンダーは指針で「〜でございます」の仰々しい教授なのに一人称や語尾が砕けた話し方になっていて、キャラの声が消えていた。

`TRANSLATION_STYLE.md` に従い、会話1561行（一意）をキャラ別・実行順に訳し直した。

- コンラッド:
  **「〜だぜ」「〜だろ」**。
  "Yeah, nah"＝いや、"Grouse as / Good as"＝最高じゃねえか、
  "Damn right"＝あったりめぇだ、"Hell yeah"＝ヘルイェア、"'aight I'm down"＝いいぜ、乗った。
  傷心時（ConradBeatup）は男言葉のまま弱気に。
- ライアン:
  **「〜だよ」「〜なんだ」**の甘えん坊。
  口にバスケットをくわえた行は舌足らずに（舌足らずな表記で）。
  フランチャイズ本部の電話は事務的な敬語で別人格に。
- アレクサンダー:
  **「〜でございます」「〜でありまして」「貴殿」**。
  紋章・大学名・評判への執着。
  縮小サイズが連なる長広舌はタグ位置を保持して訳した。
- コボルド（選択肢491行）:
  **「です・ます」**基調、ときどき**「〜だね」**。
  `Yip!`＝イップ！、`Yip! (Yes)`＝イップ！（うん）。
- ライアン×コンラッドの場面は「ライアン：」「コンラッド：」の全角コロン付きで各自の口調。

作業手順:

実行順ダンプをキャラ別に分割し、140〜150行ずつ読んで訳し、JSONに保存。
組み立て時に (1) キーが実際の行と完全一致するか（打ち間違い検出）、(2) 重複・空欄、
(3) TMPタグ数が原文と一致するか、(4) 除外対象外の全行が訳されているか、を機械検証した。
検出2件は原文が `<i>` を閉じていないのに `</i>` を足したもので、原文どおりに修正。

結果:
UI 48件＋会話1561件＝1609件。実機起動で `Prewarmed 1007/1007`、例外なし。
Yarnのサンプルスクリプト（`Start` ノード）と開発用エラー文は `ignore.txt` へ追加。

## フォント解像度の引き上げ（2026-09-12）

文字をもっと鮮明にしたいとの要望。
鮮明さは `[Font] AtlasPointSize`（SDFのサンプリングポイントサイズ）で決まり、
D3D12クラッシュ対策の過程でアトラス枚数を減らすため40まで下げていた。
しかしその後、グリフ生成を起動時の事前焼き込みに一本化したため、
「実行中のアトラス拡張」という元のリスク要因は消えている。
上げても増えるのは起動時のアトラス生成コストだけ。

40 → 80に変更（設定ファイルとコードの既定値の両方）。
実機で1007文字の事前焼き込みが完了し、例外なし、Options画面の日本語を拡大して輪郭が鮮明なことを確認した。
READMEの記述も更新（旧設定名 `LayoutRiskThreshold` の記載も `LayoutOverflowThreshold` に修正）。

## 簡体字中国語の全訳と、再利用行の中立化（2026-09-12）

日本語と同じ手順で `zh-Hans` の会話1561行を訳した（UI 43件＋会話＝1604件）。
口調: コンラッドは粗野な男言葉（「老兄」「操」は控えめに）、ライアンは柔らかい「呢/呀/啦」、
アレクサンダーは文語調の敬語（「在下」「阁下」「乃是」「甚是」）、コボルドの選択肢は「您/请」。
組み立て時の機械検証（キー完全一致・重複・空欄・TMPタグ数・除外対象外の全行カバー）は問題0で通過。

実プレイ中のユーザーから「`What's up?` はこの場面では『どうしました？』では」との指摘。
調べると原因は構造的だった。完全一致方式では**同じ英文は全場面で同じ訳**になるが、
短い定型句は複数ノードで、しかも**別のキャラの口から**出る。

私は各バッチの文脈だけを見て訳していたため、たとえば:

- `Whats up?`（4箇所）を `Conrad_Outro_4` の「行き先を決めた」への返答だけ見て「どこですか？」に
- `I'm excited.`（6箇所中4箇所がコンラッド）をアレクサンダー調「楽しみでございます。」に
- `I'm glad.`（3箇所中2箇所がコボルド）を「何よりでございます。」に
- `Hey there.` `Me too.` `Thanks.` `Aw, thanks.` `I am too.` も同様の話者混在

対応:
複数ノードで使われる22文字以下の英文76件を、各出現の直前行つきで一覧化し、
話者が混在するものは**全場面に収まる中立的な訳**へ変更（日本語10件、中国語7件）。
`Whats up?` などは全場面に収まる中立的な訳にした。

教訓:
再利用される定型句は、訳す前に全出現箇所と話者を確認する。
この一覧化スクリプトは今後の追加訳でも使う。
ユーザーからは「キャラ感の統一感は問題ない」との評価。

## 翻訳のホットリロード（2026-09-12）

実プレイしながら訳を直し、その場で画面に反映したいとの要望。
言語切替の経路（`TranslationStore.Load` → `TmpTextHook.RefreshAll`）がすでに
「読み直して画面上の全テキストに再適用する」処理そのものなので、
**ファイルの更新を検知してそれを呼ぶ**だけで実現できる。

`HotReload` を追加。`FileSystemWatcher` ではなく `Update` からの1秒ポーリング。
理由は、Watcherはスレッドプールから発火する・1回の保存で複数回発火する・エディターが
まだファイルを掴んでいる最中に発火する、の3点。
更新時刻が2回連続で動かなくなってから読み直すことで、書き込み途中のファイルを読まない。

重要な順序:
読み直し → **差分文字の事前焼き込み** → 再適用。
新しい訳に未生成のグリフが含まれていると、最初の描画で実行時のアトラス拡張（D3D12クラッシュ経路）になるため、
再適用の前に `FontFallback.Prewarm` で差分だけ焼き込む。
プレイ中の1フレームに小さなテクスチャ更新が乗ることになるが、放置すれば必ず起きる実行時拡張よりは安全。

対象は現在の言語の `strings.csv` のみ。
`ignore.txt` は一度しか読まない設計のままなので、除外ルールの変更には再起動が要る。

## 公開準備: 翻訳ファイルから英語台本を外す（2026-09-12）

公開にあたり、`strings.csv` のキーとしてゲームの英語台本1561行がそのまま入っている点が問題になった。
`_discovered/` を `.gitignore` にした理由と同じ著作物が、完全一致方式の構造上、翻訳ファイル側に必ず入る。
ユーザーの要望は「製品版を持っていないと楽に翻訳できない仕組み」。

対応:
行のキーを原文の **SHA-256 先頭16桁**（`TranslationKey`）にした。

- プラグインは画面に出た英文をその場でハッシュして引く（`TranslationStore.KeyFor`、文字列ごとにキャッシュ）。
  `key` 列と `source_en` 列の**両方を同一ファイルで受け付ける**ので、翻訳者は作業中は原文で書き（ホットリロードが効く）、
  コミット前だけ変換する。
- 変換はF1 → Toolsの「Hash strings.csv for commit」と `tools/hash-strings.ps1` の2経路。
  同じ結果になることを1609行で突き合わせて確認した。
- F6 / F7の出力に `key` 列を追加。翻訳者はそこで原文↔キーの対応を見る。
- 両方の列があって食い違う行、16桁の16進数でない `key` は「壊れた行」として読み飛ばし、
  件数をログに出す（黙って何にも一致しない状態を作らない）。
- 原文つきの作業コピーは `_discovered/<locale>.working.csv` に置く（ローカル生成物）。

ユーザーから「Steam認証でハッシュを元に戻し、原文を並べて翻訳したい」との要望。
Steam APIは不要で、**プラグインがゲームの中で動いていることが所有の証明**になる
（BepInExはSteamが起動したゲームの中でしか動かない）。

そこで `WorkingCopy` を追加：
公開用 `strings.csv` を、読み込み済みのYarn台本・シーン内の全TMP_Text・発見済み文字列と突き合わせ、
`key,source_en,translation` の作業ファイル `strings.local.csv` に実行順で展開する。
`TranslationStore` は公開ファイルの上に作業ファイルを重ねて読み、ホットリロードは両方を監視。
「Hash for commit」は作業ファイルがあればそこから公開ファイルを再生成する。
翻訳者の作業は「展開 → 原文を見ながら編集 → 保存で即反映 → ハッシュ化してPR」の一本道になる。

Git履歴には原文つきのCSVが何コミットも入っているため、ハッシュ化だけでは不十分。
公開前に履歴を1コミットに潰す（経緯はこの文書に残っている）。
ドキュメント中の台詞の引用も短い断片に削った。

## セーブの巻き戻し（2026-09-12）

翻訳確認のために「セーブを1つ前に戻したい」との要望。
ゲームの進行は `Flags`（静的レジストリ）＋ Yarn変数で持ち、
`SaveManager` が `savegame.dgn`（JSON）を`<persistentDataPath>/<SteamID>_slot<N>/` に保存、自前のバックアップは1世代のみ。

フラグを個別に編集するタブも検討したが、フラグの意味を知らないと正しく戻せず、
Yarn側との同期（`WalkNWashSceneState._SetFlag` 経由）も必要で危うい。
**ゲーム自身のセーブファイルを世代管理して差し替える**方が、意味を知らずに確実に戻せる。

`SaveHistory` を追加。2秒ごとに各スロットの `savegame.dgn` の更新時刻を見て、
内容が変わっていれば `SaveHistory/<slot>/<timestamp>.dgn` にコピー（既定30世代）。
F1に「Saves」タブを追加し、スロットと世代を選んでRestore。
復元前の状態も世代に残す。
復元はファイルの書き戻しのみで、反映にはタイトルからのロードが必要（メモリ上のフラグは触らない）。
ゲーム内でセーブすれば再び上書きされる、という自然な挙動。
`levelIndex` をJSONから拾って一覧に表示する。

## 公開とインストーラー、v0.1.1（2026-09-12）

- **リポジトリ公開**:
  ハッシュ化以前の英語原文つきCSVが履歴に残っていたため、1コミットに集約（完全な履歴はローカルブランチに保存）してから公開した。
  ルールセットで `main` を保護：PR必須、force push・削除禁止、翻訳チェック必須、管理者はバイパス可。
- **PR チェック**:
  すべてのPRで `check-translations.py` が公開ファイルを検査する。
  別ワークフロー（フォークでも動くよう `workflow_run`）が失敗理由を1件コメントし、修正後は同じコメントを合格文に書き換える。
  ログが公開されるため、レポートには行番号だけを出し、拒否した行の文面は出さない。
- **インストーラー**:
  `Install.exe`（pack時に .NET Framework 4付属のC#コンパイラで生成するコンソールなしの起動役）が
   `installer/Installer.ps1` のWinForms画面を開く。
   Steamからゲームを検出、BepInEx 5.4.23.5が未導入ならSHA-256固定でダウンロード、Modを配置、`TargetLocale` を書き込み、
   アンインストールも行う。
   セーブ履歴は既定で残し、BepInExはインストーラー自身が入れて他に利用者がない場合だけ削除。
   最初は `.cmd` で作ったが、必ず黒い窓が出るためexeに変更。
- **英語のまま**:
  `TargetLocale=en` は何も読み込まないので、Modを入れたまま原文で遊べる。
  インストーラーとF1メニューの両方で選べる。
- **言語名**:
  `Translations/<locale>/name.txt` の内容がF1のボタンとインストーラーの表示名になる。
  言語追加にコード変更は不要。
- **セーブ進捗の編集**:
  Savesタブで `levelIndex` を増減（先に進める場合はネタバレの確認）し、
  セーブ内の真偽フラグ（カットシーン後にゲームが立てる `finished_watching_*` など）を反転できる。
  編集前に必ずスナップショット。セーブ本文への正規表現置換なのでゲーム側の書式はそのまま。
- **タイプライター修正:**
  言語切り替え・ホットリロードで画面の文字を差し替えた際、
  `maxVisibleCharacters` が旧文字数のままで長い文が途中で切れていた（"HEY! This is th"）。直前の文が全文表示済みなら上限を解除する。
- **バージョン**:
  `csproj` がアセンブリ属性（`Version`/`FileVersion`）を出力するようにし、リリースはコミットとタグの後にビルドして、
  詳細バージョンにタグのコミットが入るようにした。

## 進行順の翻訳ファイル、v0.2.0（2026-09-12）

- ゲームの `LevelFlow` アセット（進捗に関係なく15レベル分）と
  Yarnプログラムをゲーム内で書き出し（`FlowDumper`、`ScriptOrder.Generate`）、
  `data/script_order.csv`（ノード名・行ID・ハッシュ・話者。英語なし）を作る。
  `Hash for commit`・作業コピー・`tools/hash-strings.ps1` はこれで並べ、`#` 見出しを出す。
  `CsvReader` とCIはコメント行と空行を読み飛ばす。ゲーム内とPowerShellの出力がバイト単位で一致することを確認した。
- 進行経路の静的監査で到達不能な参照はなし。
  **「未翻訳」**40行は開発者用の残骸（テストノード、Yarnの区切り、エラー文）だった。
  フラグ異常時に出得るエラー・デバッグ行だけ翻訳した。
- レベル14のマウント会話が `Conrad_5_Mount_*` ではなく `Conrad_4_Mount_*` を指している（`Conrad_5_Mount_*` は未使用）。
  ゲーム側のデータミスの可能性として記録、変更はしない。

## フェーズ

- **Phase 0**:
  リポジトリ作成・計画確定 — 完了
- **Phase 1**:
  BepInEx導入、プラグイン骨格作成、UI文字列の差し替え・CJKフォント表示を実機確認 — **完了**（当初計画からフック方式を修正のうえ達成）
- **Phase 1.5**:
  会話ログ一括抽出ツール、ゲーム内デバッグUI — **完了**
- **Phase 2**:
  実際の会話シーンで訳文が正しく差し替わることを実機確認 — **完了**（`SetText`バイパス問題を発見・修正）
- **Phase 3**:
  日本語・中国語の訳を本格的に追加し、UIレイアウト崩れがないか確認 — **完了**（UI文言は全量確定、レイアウト検出はTMP実測方式へ移行。
  会話文の訳文追加は継続）
- **Phase 4**:
  翻訳者向けCONTRIBUTINGの整備、配布方法（GitHub Releases）の確立 — **完了**
- **Phase 5**:
  リポジトリ公開、PRの自動チェック、インストーラー、v0.1.1リリース — **完了**

## リスク・要検証事項

- 動的テキスト（数値・プレースホルダーを含む文章）は完全一致方式だと訳しにくい場合がある。
  → 発生頻度を見て、部分一致/フォーマット文字列対応を検討。
- ゲームのSteamアップデートでTMP_TextやYarnSpinner内部実装のバージョンが変わるリスク
  → バージョンチェックを入れる。
- 中国語フォントはMicrosoft YaHeiの登録は確認できたが、実際の中国語表示は未検証（Phase 3で確認予定）。
- `DialogueDumper` はリフレクションで `Localization` のinternalフィールドを読んでいるため、
  YarnSpinnerのバージョンアップで内部レイアウトが変わると壊れる可能性がある。
- `TMP_Text` には `SetText` 以外にも `SetCharArray` 等のテキスト設定APIが存在する。
  今のところ未遭遇だが、今後同様の「素通り」ケースが見つかる可能性がある。

## Optionsクラッシュの調査（2026-09-11）

- 保存済みの9件のクラッシュログは、すべて描画スレッドの
  `D3D12ScratchAllocator::DestroyScratch → ReleaseExcessScratch → ReclaimMemory`
  で終了していた。
  Unity 6000.3.14f1 / Direct3D 12 / GeForce RTX 3060の環境。
- [Unity UUM-140564](https://issuetracker.unity.com/issues/10698) に同じスタックの報告がある。
  CSV書き込みや文字列セッターの再入が原因という診断は、このログでは裏付けられない。
- 回避策はSteamの起動オプション `-force-d3d11`。描画APIはプラグイン初期化前に決まるため、
- プラグイン内で動的に切り替えるのではなく再起動時に指定する。
- 切り分け用のsetter Postfixを終了し、setter・`SetText(string)`・
  `SetText(string, bool)` の引数をPrefixで差し替える通常の翻訳フックを復元。
  未翻訳文字列のキュー記録と、重複を抑制したデバッグログも復元する。
- 起動ログに描画APIを記録し、確認したUnityバージョンとDirect3D 12の組み合わせでは上記回避策を警告する。
- 動的フォントの構成は今回変更しない。
- 実機確認完了:
  修正版DLL + `-force-d3d11` で設定画面を開閉・スクロールし、クラッシュせず日本語表示されることを確認。
  起動ログでも `graphics=Direct3D11` を確認。

### 根本対応（起動オプション不要化）

`-force-d3d11` に頼らず落ちないようにするため、クラッシュの引き金そのものを潰した。

`TMP_FontAsset.CreateFontAsset(familyName, styleName)` は
`AtlasPopulationMode.DynamicOS` / 1024x1024のアセットを作るが、アトラス実体は
`new Texture2D(1, 1, ...)` のダミーで始まる。デコンパイルで確認した挙動は以下の通り。

1. 最初のグリフ追加で `Reinitialize(1024, 1024)` + `ResetAtlasTexture` → 実行時のテクスチャ再確保
2. 新規グリフのバッチごとに `UpdateAtlasTexture()` → `Apply()` → 実行時のGPUアップロード
3. アトラスが埋まると `SetupNewAtlasTexture()` → `new Texture2D(...)` → 実行時の追加確保

つまり「**はじめて画面に出る文字**」のフレームで毎回テクスチャ確保／アップロードが走る。
Optionsは未表示のテキストが密集した画面なので、確実な再現ポイントになっていた。
`AtlasWidth/Height` のsetterは `internal` でサイズ指定はできないが、
`TryAddCharacters(string, out string, bool)` は `DynamicOS` でも使える公開APIなので、
グリフ生成を起動時へ前倒しできる。

対応:

- 読み込んだ翻訳の全非ASCII文字を起動時に `TryAddCharacters` で焼き込む（`FontFallback.Prewarm`）。
  ゲーム中のアトラス拡張が原理的に発生しなくなる
- フォールバックフォントを言語ごと1本ずつ（計2本）に削減。
  以前は6本登録しており、アトラステクスチャも6面あった
- デバッグメニュー背景の `Texture2D` を `Awake` で生成。
  以前はF1初回押下時に`Apply()` していたため、「設定画面を開いたままF1」で落ちる経路になっていた
- 言語切り替えと会話ログ出力を `OnGUI` から `Update` へ退避（ファイルI/Oとグリフ生成を描画コールバックから追い出す）
- アトラス解像度を `[Font] AtlasPointSize`（既定64）で調整可能にした

実機確認:
`-force-d3d11` を外した状態（起動ログで `graphics=Direct3D12` を確認）で設定画面の開閉・スクロール、
会話シーンの日本語表示は正常になった。
`Prewarmed 175/175 characters` が両フォントで出ており、取りこぼしもなし。
ただしF1のデバッグメニューでは再発した（後述）。

### F1デバッグメニューのクラッシュ（IMGUI側）

取得したスタックトレースが決定的だった。

```text
D3D12ScratchAllocator::DestroyScratch
D3D12ScratchAllocator::ReleaseExcessScratch
D3D12ScratchAllocator::ReclaimMemory
GfxDeviceD3D12::QueueExecute
GfxDeviceD3D12::QueuePresent
GfxDeviceD3D12::PresentFrame
GfxDeviceWorker::RunCommand
```

`PresentFrame` 経由ということは、条件は「1フレーム中のスクラッチメモリ使用量」であり、
そのフレームの提示時に超過分を解放しようとして落ちる。
上のフォント対応は設定画面という経路を塞いだだけで、条件そのものは残っていた。
F1が最悪の再現条件だったのは、TMPとIMGUIの負荷が同一フレームに乗るため。

IMGUI側の対応:

- ログを行ごとの `GUILayout.Label` で描画していた（毎フレーム最大200回、1行ごとに別メッシュ・別ドローコール）。
  ログ全体を1つの `Label` に結合し、内容が変わったときだけ再構築するようにした
- ログには翻訳後の日本語が載るため、IMGUIの動的フォントが実行時にテクスチャを拡張していた。
  `Font.CreateDynamicFontFromOSFont` でメニュー専用フォントを持ち、
  ASCIIと翻訳文の文字を `RequestCharactersInTexture` で起動時に焼き込む
- 表示行数の上限を200から100へ

同時に見つかった別のバグ: 変更検出に `LogBuffer.Count` を使っていたため、
バッファーが上限に達すると件数が変わらずログの表示が止まっていた。
更新のたびに加算するバージョン番号で判定するよう変更。
また会話文中の `<i>` や `<gradient>` をIMGUIがリッチテキストとして解釈していたので`richText = false` にした。

実機確認完了: `-force-d3d11` なしで、設定画面を開いたままのF1を含めクラッシュしない。

### 翻訳対象外の文字列（`IgnoreRules`）

TMPフックは画面に出る全文字列を拾うため、スライダーの値・解像度・ビルド番号
（`9/9/2026_ee944596`）まで「未翻訳」として記録され、訳すべき行が埋もれていた。

組み込みの正規表現（単体の数値、解像度、リフレッシュレート、日付/ビルド番号、時刻）に加え、
`Translations/ignore.txt` で追記できるようにした。

重要な設計判断として、除外が効くのは「記録とログ」だけで、翻訳の検索には影響しない。
`TryGetTranslation` のほうが先に走るので、`strings.csv` に明示した行は除外パターンに
一致していても必ず翻訳される。誤って除外しても取り返しがつく側に倒してある。

注意点: 当初の数値パターン `^[+-]?[\d.,]+\s*%?$` は会話行の `...` にも一致していた。
数字を最低1文字要求する形に修正済み。実データ34件でパターンを検証した。

- 検証: Releaseビルド成功（警告0・エラー0）。
  実際のHarmonyでTMPの代替クラスに本番パッチを適用する一時テストで15項目に合格。
  3つの入口、再代入がないこと、bool引数保持、数値フォーマットの非干渉、
  null/空文字、ログ重複抑制、CSVの遅延出力・改行/カンマの保持、設定フラグ、
  未導入ロケールでの原文維持を確認した。
  このテストはネイティブ描画を検証するものではない。
- ビルドしたDLLをゲームのBepInExプラグインフォルダーへ配置し、SHA-256一致を確認。

## IMGUIデバッグウィンドウの再構成（2026-09-11）

- 描画処理を `Plugin.ImGui.cs` に分離し、ログとツールの2タブに整理。
- タイトル限定の移動、右下のサイズ変更、画面内への位置・サイズ補正を追加。
- ログの自動追従を切り替え可能にし、手動スクロール時には追従を停止する。
  件数が上限に達した後の更新も引き続きバージョン番号で検出する。
- 言語切り替え・会話抽出・レイアウトチェックは従来どおりUpdate側で実行する。
- 全コントロールは事前生成済みの同じフォント・サイズを使う。
  ログは1つのLabelで描画し、結合文字列と折り返し高さを変更時だけ再計算する。
  OnGUI内では新規のTexture2D生成やApply、ファイルI/Oを行わない。
- 検証: Releaseビルド成功。実機での表示・操作・クラッシュ有無はユーザー確認待ち。

## 言語パックと言語ごとのフォント準備（2026-09-13）

- 仮翻訳の言語パックを追加：繁体字中国語、ドイツ語、フランス語、スペイン語、ブラジルポルトガル語、
  韓国語、ロシア語、ポーランド語、ヘブライ語、エスペラント、トキポナ。
  英語原文はゲーム内のF6書き出しから取得し、会話由来でないUI文言は候補の英文をハッシュ照合して復元した。
  各パックは `ja` と同じ進行順で、先頭に仮翻訳である旨のコメントを入れている。
- フォントは各言語が使う文字（かな、漢字、ハングル、ヘブライ文字、その他の非ASCII）から選び、漢字の書体は言語名で決める。
  韓国語・繁体字・ヘブライ語に専用の書体を追加し、ラテン文字用の書体はほかの書体で賄えない文字がある時だけ読む。
- 各文字は、その言語のフォールバック順で最初にグリフを持つ書体に1回だけ焼き込み、TMP全体のフォールバック順も同じ順に揃える。
  これにより、実行中にTMPがグリフを追加しようとする書体に出会うことはない。
- 書体を必要時に読み込む方式では、Direct3D 12で実行中に言語を切り替えると
  クラッシュした（`D3D12ScratchAllocator::DestroyScratch`、UUM-140564）。
  そのため、Direct3D 12では導入済みの全言語を起動時に準備し、切り替えはフォールバック順の入れ替えだけにした。
  VulkanとDirect3D 11では必要時に準備する。Windows（Direct3D 12）とSteam Deck（Vulkan）の両方で、繰り返し切り替えて確認済み。
- ヘブライ語はTMPのコンポーネント単位の右から左表示を、RTL言語の読み込み中に翻訳対象の全コンポーネントで有効にする。
- F1メニューにAboutタブ、インストーラーに言語選択とAboutを追加。
  設定ファイルの説明文を英語化。
- 修正：Steam Deckのパッド対応が実際のマウスクリックもパッド入力として扱っており、
  マウスではF1メニューのボタンが2回ずつ押されていた。

## Options 画面の言語設定（2026-09-13）

- Optionsの各行はUnityScriptableSettingsの `ScriptableSettingSpawner` が `SettingsManager` の一覧から生成している。
  Modは `SettingDropdown` の派生を1件、ゲームプレイの最後の設定の後ろに差し込む（`AddSetting` は
  不安定ソートで既存の行の順番を崩し得るので使わない）。
  行はゲーム自身の部品とパッド操作で作られる。
  ライブラリの構造が想定と違えば何も足さない。
- ゲーム本来のOptionsの流れ（`SaveButtonOnlyAppearOnChanges`）に合わせた。
  言語を選ぶと設定ファイルに書かずにその場で切り替え、Saveで保存し、保存せずにBackで戻ると呼ばれる `Load` で保存済みの言語に戻す。
  F1メニューからの切り替えは従来どおりその場で保存する。
- リストを10件表示できる高さにし、項目名を全言語で翻訳し、言語名を未翻訳一覧から除外し、その文字をDirect3D 12向けに起動時に準備する。
- Windowsのマウス操作と、Steam Deckのコントローラー操作で確認済み。

## Steam Deck 用インストールスクリプト（2026-09-13）

- `install-steamdeck.sh`（zipの直下。改行コードは `.gitattributes` でLFに固定）がDeckの手動手順を行う。
  `libraryfolders.vdf` とアプリのマニフェストからゲームを探し、Linux版BepInEx 5.4.23.5をダウンロードしてSHA-256（`e538560b...`）を検証し、
  `executable_name` を設定し、Modをコピー、`TargetLocale` を書く。
  各プロファイルの `localconfig.vdf` の `LaunchOptions` に `./run_bepinex.sh %command%` を追加する。
  既存のオプションは置き換えずに包む。
- Steamは終了時に `localconfig.vdf` を書き戻すので、起動オプションはSteamを閉じた状態でのみ編集する（`~/.steam/steam.pid` で判定）。
  `steam -shutdown` の前に確認し、終わったらSteamを起動し直す。`--yes` ではSteamを閉じない。
  バックアップを `localconfig.vdf.dragnwash-backup` に残す。
- デスクトップモードではkdialog、ターミナルでは通常の入力で操作する。
  英語・日本語・中国語。`--uninstall` はWindows版と同じ振る舞い（セーブ履歴を残し、
  スクリプトが入れたBepInExかつ他のプラグインがない場合だけBepInExを消し、起動オプションを戻す）。
- Steamは終了時に `localconfig.vdf` を書き戻すため、起動オプションはSteamを閉じた状態でだけ書き換える。
  `steam -shutdown` を送り（事前に確認、既定は「はい」。`--close-steam` で確認を省略）、
  `~/.steam/steam.pid` のプロセスが消えるまで最大90秒待ってから書き換え、Steamを起動し直す。
  できなかった手順は最後のダイアログに一覧表示し、実行内容は `~/.local/state/dragnwash-localization/installer.log` に記録する。
- Deck上の隔離したSteam環境で、新規導入、更新、既存の起動オプション、データを残したアンインストール、ターミナルでの言語選択を確認した。

## macOS（2026-09-13）

- 借りたApple M3 Pro / macOS 26.6.2で、BepInEx 5.4.23.5 `macos_universal`（Doorstop 4.5.0）を試した。
  ゲーム（`DragNWash.app`、Unity 6000.3.14f1、Mono、x86_64/arm64のユニバーサル）はHardened Runtimeなしのアドホック署名なので、
  `DYLD_INSERT_LIBRARIES` は効き、`libdoorstop.dylib` はプロセスに読み込まれる。
- それでもBepInExは起動しない。
  `LogOutput.log` も `config/` もできず、`Player.log` にも何も出ない。
  Appleシリコンのままでも `ARCHPREFERENCE="x86_64,arm64"`（Rosetta）でも同じ。
- NeighTools/UnityDoorstop#108と一致する。
  Unity 6.3の `UnityPlayer.dylib` はchained fixupsのみで、Doorstopの `plthook_osx.c` がそのヘッダーをゼロとして読み、
  `dlsym` をフックできないため `mono_jit_init_version` に割り込めない。
  未マージのPR #110が修正をうたっている。
  Issueに添付された有志ビルドのdylibは使っていない。
- `run_bepinex.sh` は `executable_name` を現在のディレクトリ基準で確認するので、
  手で起動するときはゲームフォルダーから実行する必要がある。
  Steamの外から起動すると `SteamAPI_Init() failed` が出る。
- 方針: 修正の入ったBepInExが出るまでmacOSは非対応と明記し、出たら再検証する。
- 実験的なインストーラー `installer/experimental/install-macos.sh` を用意した。
  Deck用スクリプトのmacOS版で、`executable_name` に `.app` のフルパスを設定し、
  起動オプションはJavaScript for Automationで書き換え、Steamは `steam://exit` で終了し
  UnityDoorstop#107のarch対策と「動作確認」モードを持つ。
  リリースのzipには入れておらず、Macではまだ実行していない。
  修正版のBepInExが出たら `BEPINEX_URL` / `BEPINEX_SHA256` を差し替え、`KNOWN_ISSUE=0` にして試してから同梱する。

## 台詞 ID ごとの訳（2026-09-14）

- TMPフックには英文しか届かないため、複数のキャラが話す同じ英文には訳が1つしか付けられなかった（29種類、会話1839行のうち142行）。
- `LineIdContext` が `LinePresenter.RunLineAsync` と `OptionItem.Option` のsetterにPrefixを入れ、TMP部品がこれから表示する台詞IDを覚える。
  その台詞の文字列がそのまま届いたときは、`strings.csv` の `line:<ID>` の行がハッシュの行より優先される。
  `RefreshAll`（言語切り替え）でも同じ。
- `data/script_order.csv` を全出現（1839行）にし、作業コピーは共有されている台詞の各位置に空の `line:` 行を出す。
  *Hash for commit* と `tools/hash-strings.ps1` は訳の入った行だけを該当位置に書き出す。
  ハッシュの行の話者は、パックの値ではなく台本データから全員分（`Ryan/Alexander`）を付ける。
- 2つのハッシュ化ツールは、パック先頭のコメントを残し、空の訳を出さないようにした。チェックは `line:` のkeyを受け付ける。
- Windows版で、ゲームのLinePresenterとOptionsPresenter、言語切り替えを含めて確認した。詳細は `docs/PER_LINE_TRANSLATION.ja.md`。
