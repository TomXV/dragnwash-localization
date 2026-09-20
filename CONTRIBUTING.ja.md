# Contributing（翻訳への参加）

[English](CONTRIBUTING.md)

Drag'n Wash Localizationは、コードを書かずに **CSV を編集するだけ**で翻訳に参加できるModです。
この文書は翻訳に参加したい方向けのガイドです。
プラグイン本体の開発者向けのビルド・配布手順は [docs/RELEASING.ja.md](docs/RELEASING.ja.md) を参照してください。

参加するみなさんには[行動規範](docs/CODE_OF_CONDUCT.ja.md)に沿ってもらいます。
セキュリティの問題は、プルリクエストや公開のIssueではなく[SECURITY.ja.md](SECURITY.ja.md) の非公開のフォームへお願いします。

## 必要なもの

- Drag'n Wash（Steam版）と本Modの導入（BepInEx）
- CSVを編集できるテキストエディター（Excel / LibreOffice / VS Codeなど）

Unityの内部キー名やプログラミングの知識は一切不要です。作業中は**画面に表示される
英語原文そのもの**を `source_en` として書き、コミット前にそれをハッシュに変換します
（下記「コミット前にハッシュ化する」）。

### 言語の表示名（`name.txt`）

`Translations/<locale>/name.txt` に書いた文字列が、ゲームの **Options →「言語（Mod）」** の一覧、
F1メニューの言語ボタン、インストーラー（Windows・Steam Deck）の言語選択にそのまま表示されます
（例: `ja/name.txt` → `日本語`、`zh-Hans/name.txt` → `简体中文`）。
1行だけ、UTF-8で保存してください。ファイルがなければフォルダー名が表示されます。
新しい言語を追加するときは、「フォルダー」「`strings.csv`」「`name.txt`」の3つを作れば完了です。

## 基本の流れ

1. このリポジトリをフォークします。
2. `Translations/<locale>/strings.csv` に訳を追加・修正します。
3. コミットしてPull Requestを送ります（タイトルと本文の書き方は「[Pull Request の書き方](#pull-request-の書き方)」を参照）。

## 翻訳ファイルの形式

> [!IMPORTANT]
> **v0.6.0 から CSV の書き方が一部変わりました。**
> 以前のファイルもそのまま読み込めますが、今のファイルを編集するときは次の違いに注意してください。
>
> - **台詞 ID の行:**
>   `key` 列には、16 桁のハッシュのほかに `line:6046bedf` のような Yarn の台詞 ID も入ります。
>   その行は、その 1 つの台詞だけの訳です（「[複数のキャラが話す台詞](#複数のキャラが話す台詞)」参照）。
> - **複数の話者:**
>   複数のキャラが話す台詞の `speaker` 列には、全員が `/` 区切りで並びます（`Ryan/Alexander`）。
> - **作業コピー:**
>   共有されている台詞の位置ごとに、訳が空の台詞 ID の行が追加されています。
>   訳し分けたいとき以外は空のままで構いません。
> - **`order`:**
>   会話の中のすべての行を数えるようになったので、v0.6.0 より前のファイルとは番号が違います。
>   読むための列なので、直す必要はありません。
>
> 翻訳作業には、v0.6.0 以降のプラグインとこのリポジトリのツールを使ってください。
> v0.5.0 以前のツールは台詞 ID の行を知らないため、壊れたハッシュの行に変えてしまいます。
> 古いバージョンで遊んでいる人には影響ありません（台詞 ID の行が無視されるだけです）。

`Translations/<locale>/strings.csv` は公開用のCSVで、**ゲームで流れる順**に並び、`#` の見出しで区切られています。
作業中は原文つきの行を**同じファイルに混在**させても読めます。

```csv
key,section,node,order,speaker,translation

# ===== Level 1: Ryan (Sunny) | sets level_1 | ends level_1_complete =====
# --- intro: Ryan_1_intro ---
5d0a…,L01 Ryan,Ryan_1_intro,1,Ryan,よお。ここが洗い屋か？
# --- phone: Ryan_1_PhoneTutorial | if $has_talked_to_ryan ---
…
# ===== UI and other text (not part of the dialogue script) =====
d0db8b5e364b6989,UI,,,UI,オプション
```

- `section`:
  `L01 Ryan` のようなレベル番号とドラゴン名、または `Cutscene` / `Reaction` / `Unused` / `UI`
- `node` / `order`:
  Yarnの会話ノード名と、その中での順番。分岐先のノードは親の直後に置かれます
- `#` 行:
  見出し。読み込み時は無視されるので自由に残せます。
  `| if $変数` は分岐条件のヒントです
- 並び順は `data/script_order.csv`（ノード名・行ID・ハッシュ・話者だけ。英語なし）で決まり、
  ゲーム内 **Export game flow** で再生成できます（ゲームの更新時にメンテナーが行います）

```csv
source_en,translation
Options,オプション
```

- `key`:
  原文のSHA-256の先頭16桁。**リポジトリにはこの形式だけ**が入ります。
  ゲームの英語台本を再配布しないためで、これにより製品版を持っていない人は
  台本を読むことも、原文なしに訳を書くこともできません。
- `speaker`:
  誰の台詞か（Conrad / Ryan / Alexander / Kobold＝選択肢 / Phone / UI）。
  台本の構造から自動で付きます。複数のキャラが話す英文には、全員が並びます（`Ryan/Alexander`）。
- `key` には、`line:6046bedf` のようなYarnの台詞IDも書けます。
  その行は、その1つの台詞だけの訳になります（「[複数のキャラが話す台詞](#複数のキャラが話す台詞)」参照）。
- `source_en`:
  ゲームに表示される英語原文そのまま（完全一致で照合）。
  **作業中はこちら**で書くと、保存した瞬間にホットリロードで画面に反映されます。
- `translation`:
  訳文。

プラグインは画面に出た英文をその場でハッシュして引くので、どちらの行も同じように動きます。
F6 / F7の出力には `key` 列と `source_en` 列の両方が入っているので、対応はそこで分かります。

### 新しい言語を始める

1. `Translations/<locale>/` フォルダーを作り（例: `ko`）、`name.txt` に表示名（例: `한국어`）を書く。
2. 開発者ツールをオンにする：
   **Options → Mods → Drag'n Wash ModFramework → Developer tools**（初期設定はオフで、
   遊ぶだけの人にはF1の窓も書き出しも`_discovered` フォルダーも出ません）。
   ゲームを起動し、**Options →「言語（Mod）」** かF1 → Translationの言語一覧で新しい言語を選ぶ（まだ訳が0件なので画面は英語のまま）
3. **F1 → Translation → Export working copy** を押す。
   `strings.csv` がなくても、ゲームが持つ全行の英語原文を並べた空の作業コピー `_discovered/<locale>.working.csv` ができる。
4. あとは下記「原文を並べて作業する」と同じ。
   訳した行から順に画面へ反映される。
5. Optionsの項目名 `Language (Mod)`（キー `e3becbaee46cc0df`）も訳す。
   ゲーム本来の設定ではなくModの設定だとわかるように、「(Mod)」かその言語での言い方を残す。
   一度Optionsを開くと、作業コピーとF7の書き出しに出てくる。

### 原文を並べて作業する（推奨）

ゲーム内 **F1 → Translation → Export working copy** を押すと、公開用の `strings.csv` が
`Translations/_discovered/<locale>.working.csv` に展開されます：

```csv
key,section,node,order,speaker,source_en,translation
5d0a…,L01 Ryan,Ryan_1_intro,1,Ryan,Hey. This the cleaning place?,よお。ここが洗い屋か？
```

会話は**ゲーム内の実行順**に並び、`speaker` 列に**誰の台詞か**
（Conrad / Ryan / Alexander、選択肢はKobold、本部からの電話はPhone、UIはUI）が入ります。
口調を合わせるときの目印にしてください。
`source_en` にはゲームが今読み込んでいる台本・UIから原文が埋まります
（セーブをロードしてから押すと会話文が揃います）。
このファイルを編集して保存すれば、ホットリロードでその場で画面に反映されます。
`_discovered/` 配下なのでリポジトリには入りません。

プラグインがゲームの中で動いていること自体が「製品版を持っている」証明なので、別途の認証はありません。

### 複数のキャラが話す台詞

ハッシュの行は、同じ英文の台詞**すべて**に使われます。
短い台詞の中には、別々のキャラが同じ英文を話すものがあります
（`Wonderful!` はレベル1ではライアン、レベル5ではアレクサンダーの台詞）。
そうした行の `speaker` 列には `Ryan/Alexander` のように話者が全員並び、1つの訳が全員に使われます。

1つの訳では収まらないときは、その台詞だけ別の訳にできます。
作業コピーには、共有されている台詞が話される場所ごとに、Yarnの**台詞 ID** をkeyにした空の行が入っています。

```csv
key,section,node,order,speaker,source_en,translation
84f325bca745e504,L01 Ryan,Ryan_1_intro,9,Ryan/Alexander,Wonderful!,全員に使う訳
line:6046bedf,L01 Ryan,Ryan_1_intro,9,Ryan,Wonderful!,ライアン専用の訳
line:ab423ac7,L15 Alexander,Alexander_5_required,19,Alexander,Wonderful!,
```

- 訳し分けたい台詞の行だけ埋めてください。
  訳が入った台詞IDの行はその1か所だけに使われ、ほかの場所はハッシュの行の訳のままです。
- 空の台詞IDの行は残したままで構いません。
  *Hash for commit* は訳が入った行だけを、台本の該当する位置に書き出します。
- 台詞IDはゲームの台本から来ています。
  ゲームのアップデートでIDが変わった台詞は、何も言わずにハッシュの行の訳に戻ります。
- 台詞IDの行が使えるのは会話と選択肢だけで、UIの文字には使えません。

### コミット前にハッシュ化する

PRを送る前に、公開用の `strings.csv` を作り直してください。
作業ファイル（`_discovered/<locale>.working.csv`）があればそこから、
なければ `strings.csv` 自身の `source_en` 行から生成されます。

方法は3つ：

- ゲーム内 **F1 → Translation → Hash for commit**（現在の言語のファイルを書き換えます）
- `tools/hash-strings.ps1` を引数なしで実行（全言語）
- Dockerがあれば、どのOSでも、手元に何も入れずに:
  `docker compose run --rm hash ja`（1言語）、`docker compose run --rm hash`（全部）。
  [docs/DOCKER.ja.md](docs/DOCKER.ja.md) を見てください。

`-Path` はこれとは別の動きをします。
**渡したファイルをその場で変換するだけ**で、作業ファイルを探しません。
公開用の `strings.csv` に対して使ってください。
作業ファイルを渡すと公開形式で上書きされ、`source_en` 列と未翻訳の行がすべて失われます。

**英語原文が残った `strings.csv` は PR で受け付けません。**
PRごとに自動チェックが走り、形式が違う場合は理由を英語でコメントします。
直してプッシュすれば同じコメントが更新されます。
プッシュする前に同じチェックを回すなら、Dockerで `docker compose run --rm checks` です。

カンマ・引用符・改行を含む場合は、フィールドを `"` で囲んでください（引用符は `""` とエスケープ）。
詳細は [RFC 4180](https://datatracker.ietf.org/doc/html/rfc4180) 準拠です。

### 書式タグについて

原文に `<size=70%>` / `<gradient="gold">` / `<i>` のようなTMP書式タグが含まれる場合は、
**タグ構造はそのまま残して、中の文章だけ**訳してください。
タグを壊すと表示が崩れます。

```csv
"<gradient=""gold""><b> ...English... </b></gradient><size=70%> (hint)","<gradient=""gold""><b> ……訳文…… </b></gradient><size=70%>（ヒントの訳）"
```

## 未翻訳の原文を探す

ゲームをプレイしながら、訳すべき原文を手に入れる方法が3つあります。
どれもゲーム内のFキーや自動記録で `Translations/_discovered/` にCSVとして出力されます。

| 方法     | 出力先                           | 内容                                                                                                                                          |
|----------|----------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------|
| **F6**   | `_discovered/dialogue_lines.csv` | 全会話文を**ゲーム内の実行順**で（`yarn_project,node,order,kind,speaker,line_id,key,source_en,translation,tags`）。セーブをロードした後に押す |
| **F7**   | `_discovered/ui_texts.csv`       | 全UIテキスト（非表示メニュー含む。`key,source_en,translation,object_path`）                                                                   |
| 自動記録 | `_discovered/strings.csv`        | プレイ中に見つかった未翻訳の原文（`source_en,translation`）                                                                                   |

`_discovered/` はゲーム本体の著作物（会話文そのもの）を含むため、
**リポジトリにはコミットしないでください**（`.gitignore` 対象）。
各自がローカルで生成します。

- `dialogue_lines.csv` から訳したい行を `strings.csv` へコピーし、
  `translation` 列だけ埋めればOKです（`node` や `line_id` など余分な列が付いたままでも読み込まれます）。
- `node` 列は「キャラクター名＿何回目＿場面」（例: `Conrad_1_intro`）、
  `order` 列はその会話内での順番、`kind` 列は `line`（台詞）か `option`（プレイヤーの選択肢）です。
  会話単位でまとめて訳すと、口調や文脈が揃えやすくなります。
  ドラゴンはConrad / Ryan / Alexanderの3体で、`RyanMuddy` のような接尾辞付きは同じキャラクターの状態違いです。
- 訳済みの行は `translation` 列に訳が入った状態で出力されるので、再ダンプしても作業は失われません。
- `ui_texts.csv` の `object_path` 列で、その文言が画面のどこにあるかが分かります。
  タイトル画面とゲーム中で1回ずつF7を押せば、ほぼ全UIが揃います。

## 訳す必要のない文字列

スライダーの数値・解像度（`1920 x 1080 @ 164.995Hz`）・ビルド番号などは最初から記録の対象外です。
さらに除外したいものは `Translations/ignore.txt` に正規表現で追記できます（ファイル内に記述例あり）。

除外が効くのは「記録」だけです。
翻訳の検索はこれより先に行われるため、`strings.csv`に書いた行は除外パターンに一致していても**必ず翻訳されます**。

## ロケール（言語）の追加

`Translations/<locale>/` にフォルダーを追加して `strings.csv` を置くだけです。
ロケール名は既存に合わせてBCP 47形式にしてください（例: `ja`、`zh-Hans`、`zh-Hant`、`pt-BR`、`ko`）。
プラグインはフォルダー名を自動検出します。

フォントはファイル内の文字から自動で選ぶので、たいていの文字体系は追加作業不要です。
日本語・中国語（簡体字・繁体字）・韓国語・キリル文字・アクセント付きラテン文字・ヘブライ文字は、
WindowsとSteam Deckでフォントが見つかります。
右から左に書く言語（`he`・`ar`・`fa`・`ur`・`yi`）は自動で右から左に表示します。
ファイルは普段の入力順のまま書き、行の中にラテン文字の単語や数字を混ぜないでください（逆順に表示されます）。
対応するフォントがない文字体系は、プラグインDLLの隣の `fonts/` フォルダーに `.ttf` / `.otf` を置いてください。

## 仮翻訳の言語を改善する

日本語と簡体字中国語以外の言語パックはすべて仮翻訳です（全行そろっていますが、ネイティブの確認を受けていません）。
その言語を話せる方のレビューが、いちばんありがたい貢献です。
行の修正はPRでお願いします。パック全体をネイティブが通して確認できたら、
同じPRで `strings.csv` 先頭のコメントと、READMEの言語一覧の状態を書き換えてください。
難しければ、PRに「パック全体を確認した」と書いてもらえれば、メンテナーが両方を更新します。

公開用の `strings.csv` の `translation` 列だけを直し、ほかの列をそのままにしておけば、
ファイルはハッシュ化されたままなので *Hash for commit* は不要です。
英語の原文を横に並べて確認したい場合は、「[原文を並べて作業する](#原文を並べて作業する推奨)」の作業コピーを使ってください。

## 動作確認

- ゲームの **Options →「言語（Mod）」**（Saveで確定、Backで保存済みの言語に戻る）か、
  **F1** のデバッグウィンドウの **Translation** タブで言語を切り替えると、再起動なしで表示が切り替わります。
- Translationタブの **Check translation layout** で、レイアウト崩れリスクのある文字列が
  `_discovered/layout_risks.csv`（`source_en,translation,axis,required_px,available_px,ratio,object_path`）に出力されます。
  `ratio` が大きいものほどはみ出しが大きいので、訳文を短くする等で調整してください。

## Pull Request を送る前に

- `source_en` が実際に画面に表示される英文と**完全一致**しているか（大文字小文字・前後の空白・書式タグまで）。
- **ハッシュ化済みか**（`strings.csv` の列が `key,section,node,order,speaker,translation` で、`source_en` の行が残っていないか）。
- 書式タグの構造が原文と一致しているか。
- 重複行や `translation` が空の行を入れていないか。
- 1つのPRは1言語・まとまりのある範囲に絞ってください。

## Pull Request の書き方

- **タイトル:**
  先頭に言語コードを角かっこで付けて、何を変えたかを書きます。
  例: `[ko] 韓国語訳の修正`、`[ko] レベル1〜3のネイティブチェック`、`[de] Options の項目名を翻訳`。
  英語・日本語・その言語のどれで書いても構いません。
- **本文:**
  PRを作ると、記入用のテンプレートが自動で入ります。
  **「What / 内容」**に言語と直した範囲（レベル、`Conrad_1_intro` のような場面、UIなど）を書き、
  チェックリストは当てはまるものにだけチェックを入れてください。
  **「Credit / クレジット表記」**には、クレジットの表記を希望するかどうかと、
  希望する場合は表示してほしい名前（必要ならリンク）を書いてください。
- **送ったあと:**
  翻訳の自動チェックが走ります。
  問題があれば、理由とファイル・行番号が英語のコメントで届き、修正をpushすると同じコメントが更新されます。
  そのあとメンテナーがレビューします。
  質問はPRに英語か日本語で気軽に書いてください。
  チェックで止められたときは「[自動チェックで止められたとき](#自動チェックで止められたとき)」を参照してください。

## 自動チェックで止められたとき

すべてのPRで `tools/check-translations.py` が実行されます。
問題が見つかると、PRに英語のコメントが付き、
理由の説明と、`ファイル:行: メッセージ` の形で問題を一覧にした **Full report** が表示されます。
行番号は、エディターで開いたときのファイルの行番号です。
同じブランチに修正をpushするとチェックがやり直され、コメントは増えずに同じものが更新されます。

pushする前に、手元で同じチェックを実行することもできます（Python 3.9以降）。

```bash
python tools/check-translations.py
```

問題がなければ `translations OK` と表示されます。

| レポートのメッセージ                                                                 | 意味                                                                                   | 直し方                                                                                                               |
|--------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------|
| `header is [...]; the published file must be ...`                                    | ファイルが作業コピーのまま（`source_en` 列がある）                                     | **F1 → Translation → Hash for commit** か `tools/hash-strings.ps1` を実行し、作り直した `strings.csv` をコミットする |
| `key is not 16 lowercase hex digits or a line ID`                                    | key がハッシュでも台詞 ID でもない。key の列に英文が入っているか、key を書き換えている | *Hash for commit* で作り直す。`key` 列は手で編集しない                                                               |
| `must not be committed (contains source text)`                                       | `Translations/_discovered/` のファイルか `strings.local.csv` が PR に入っている        | `git rm --cached <ファイル>` で PR から外してコミットする（手元のファイルは残せます）                                |
| `duplicate key (see line N)`                                                         | 同じ行が 2 回ある                                                                      | key ごとに 1 行だけ残し、もう一方を消す                                                                              |
| `empty translation`                                                                  | `translation` が空の行がある                                                           | 訳を入れるか、行ごと消す（その行は英語で表示されます）                                                               |
| `expected 6 fields, got N`                                                           | 列の数が合わない行がある                                                               | `,`・改行・`"` を含む値をダブルクォートで囲み、中の `"` は `""` と書く                                               |
| `section does not look like an identifier` / `node does not look like an identifier` | この列を編集したか、列がずれている                                                     | `main` の値に戻し、`translation` 列だけを直す                                                                        |
| `no strings.csv`                                                                     | 言語フォルダーに `strings.csv` がない                                                  | ファイルを追加するか、空のフォルダーを消す                                                                           |
| `empty file`                                                                         | `strings.csv` に見出し行がない                                                         | 先頭に `key,section,node,order,speaker,translation` を書く                                                           |

書式タグが原文と同じ構造かどうかは、このチェックでは調べていません。レビューで確認します。

**表計算ソフトは、保存するときにファイルを壊すことがあります。**
Excelは、数字に見えるkey（例: `12345e6789012345`）を指数表記に変えたり、文字コードやクォートを変えたりすることがあります。
VS Codeなどのテキストエディターか、すべての列を「テキスト」にしたLibreOfficeを使い、UTF-8のCSVで保存してください。

## 絵の翻訳

文字の一部は絵になっています（メニューのボタン、看板）。
訳した絵は `Translations/<locale>/textures/<ゲームのテクスチャ名>.png` に置き、
`textures/credits.csv`（`file,author,note`）に行を足します。
手で描くか、ゲームの絵に手を加えてください。
ゲームの絵をそのままコミットしてはいけません
（[コンテンツポリシー](https://github.com/TomXV/dragnwash-modframework/blob/main/docs/CONTENT_POLICY.ja.md)）。
手順は [docs/TRANSLATED_TEXTURES.ja.md](docs/TRANSLATED_TEXTURES.ja.md#翻訳する人の手順) にあります。

## ルール

- ゲーム本体のアセット・コードを、
  手を加えずにそのままコミットしない（[コンテンツポリシー](https://github.com/TomXV/dragnwash-modframework/blob/main/docs/CONTENT_POLICY.ja.md)）。
  自分で描いた絵や手を加えた絵は歓迎します（上を参照）。
- `Translations/_discovered/` はコミットしない。
- 翻訳文はそれぞれの翻訳者の貢献として扱います（ライセンスは [LICENSE](LICENSE) 参照）。

## 協力者のクレジット

PRの「Credit / クレジット表記」で、クレジットを希望するかどうかと、表示してほしい名前を書いてください。
希望する場合は、PRをマージしたあとにメンテナーが別のコミットでクレジットを追加するので、そのための作業は不要です。
クレジットは次の場所に載ります。

- READMEの言語パックの表の、その言語の行
- その言語パックの `strings.csv` の先頭のコメント
- その変更が入ったリリースのリリースノート
- ゲーム内F1メニューとインストーラーのAbout（次のリリース以降）

パックの「状態」をどう書くか（一部の修正で「仮翻訳」の表記を変えるかなど）は、PRごとに判断します。
クレジットを希望しない場合は何も追加しません。
その場合も、コミットはリポジトリの履歴に残ります。
