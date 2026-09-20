# Drag'n Wash Localization

[English](README.md) | [한국어](README.ko.md)

[Drag'n Wash](https://store.steampowered.com/app/4739660/) 用の BepInEx ベースの非公式・多言語ローカライズ Mod です。
多くの言語でゲームを遊べます（[言語パック](#言語パック)参照）。どの言語も、コードを書かずに CSV を編集するだけで
追加・改善できます。

技術的な調査結果と実装計画は [docs/PLAN.ja.md](docs/PLAN.ja.md) を参照してください。

> [!WARNING]
> ## ⚠️ ネタバレ注意 ⚠️
> **`Translations/` の CSV には、ゲームの全会話がストーリーの進行順で入っています。**
> 中身を見るのはネタバレ注意です！ まずはゲームを何度か周回してから開くことをおすすめします！

## 導入方法

### かんたん導入（推奨）

導入方法はすっごく簡単です。

1. [Releases](https://github.com/TomXV/dragnwash-localization/releases) から zip をダウンロードして展開する
2. **`Install.exe` をダブルクリック**
3. 言語を選んで、**「インストール」をポチッ**（入っていれば「更新」）

> [!TIP]
> 同じ手順を Steam のガイドにも載せています: [日本語](https://steamcommunity.com/sharedfiles/filedetails/?id=3801418794) / [English](https://steamcommunity.com/sharedfiles/filedetails/?id=3801420947)。Drag'n Wash には Steam ワークショップがないため、Mod 本体は GitHub の Releases からダウンロードします。

## ね？ 簡単でしょ？ ( ･´ｰ･｀)ドヤッ

ゲームフォルダは Steam から勝手に見つけます（見つからなければ選ぶだけ）。BepInEx が入っていなければ、公式の 5.4.23.5 を自動でダウンロードして入れます（SHA-256 で検証済み）。あとは Steam からゲームを起動するだけです。

言語は 日本語 / 简体中文 / English（翻訳しない）に加えて、ネイティブが校正した韓国語、仮翻訳の繁体字中国語・ドイツ語・フランス語・スペイン語・ブラジルポルトガル語・ロシア語・ポーランド語・ヘブライ語・ウクライナ語・タイ語・ベトナム語、面白枠のエスペラント・トキポナから選べます（[言語パック](#言語パック)参照）。同じ画面に「アンインストール」ボタンもあり、セーブ履歴は既定で残します。BepInEx は、選んだときに、他の Mod がなければ一緒に消せます。ゲームの中の **Options → Mods → Drag'n Wash Localization → Uninstall** からもアンインストールでき、次にゲームを起動したときに削除されます。

手動で導入したい場合は、以下の手順に従ってください。


> [!NOTE]
> **`Install.exe` を押しても何も起きない、または「Windows によって PC が保護されました」と出る場合**
> `Install.exe` は署名のない小さなプログラムなので、初回だけ Windows SmartScreen が止めることがあります。
> - 警告画面が出たら **「詳細情報」→「実行」** を押してください
> - 何も出ないときは、`Install.exe` を右クリック → プロパティ → 一番下の **「許可する」** にチェック → OK のあと、もう一度ダブルクリック

> [!WARNING]
> **Windows セキュリティ（Microsoft Defender）が `Install.exe` を「Trojan:Script/Wacatac.B!ml」と検出する場合、または展開したフォルダに `Install.exe` が見当たらない場合**
> 誤検知です。末尾の `!ml` は、既知のウイルスと一致したのではなく、機械学習で「怪しいパターンに似ている」と推定されたことを表します。v1.0.0 までのインストーラーは PowerShell のスクリプトを黒い窓を出さずに起動しており、その起動のしかたがマルウェアの手口に似ていました。v1.1.0 からの `Install.exe` は Drag'n Wash ModFramework の共通インストーラーで、スクリプトを実行しない普通の署名なしプログラムですが、新しい署名なしのファイルは検出されることがあります。中身はフレームワークのリポジトリの [`installer/`](https://github.com/TomXV/dragnwash-modframework/tree/main/installer) で公開しています。
>
> 自動で隔離されたときは検出名が表示されず、展開したフォルダから `Install.exe` が消えているように見えるだけです。何が消されたかは、Windows セキュリティの「保護の履歴」で確認できます。
> - まず、ダウンロードした zip が本物か確かめてください。PowerShell で `(Get-FileHash "<zip のパス>").Hash -eq ("<Releases の sha256>" -replace '^sha256:')` を実行し、`True` と出れば、このリポジトリで配布しているファイルです。`False` なら使わずに削除してください
> - `sha256` は [Releases](https://github.com/TomXV/dragnwash-localization/releases) のページで `DragNWashLocalization-<version>.zip` の下に表示されます。自動で付く「Source code」の2行にはハッシュがないので、そちらは使わないでください
> - 一致は「配布しているファイルと同じもの」の確認であって、安全性そのものの証明ではありません。中身は上記のソースで確認できます
> - 一致した場合は、Windows セキュリティの「保護の履歴」でその検出を開き、「操作」→「デバイスで許可」を選ぶと使えます。許可するのはこのファイルだけにしてください。フォルダの除外を追加したり、Windows セキュリティを無効にしたりする必要はありません
> - 何も許可したくない場合は、下の「手動で導入する」の手順でも導入できます
> - GitHub の Releases 以外から入手したファイルは使わないでください

### Windows on ARM（動作確認済み）

Snapdragon X などの ARM 版 Windows でも、上と同じ `Install.exe` の手順で導入でき、Mod も動きます（ゲームは x64 版がエミュレーションで動きます）。

> [!IMPORTANT]
> **ゲーム本体が DirectX 12 では正しく動かないため、Steam の起動オプションに `-force-d3d11` を入れてください。** Mod を入れていなくても起きる、ゲーム側の問題です。
> - GPU ドライバが古いと、スプラッシュ画面のあとにゲームが落ちます
> - 最新のドライバでは落ちなくなりますが、3D が描画されません
>
> Steam でゲームを右クリック → **プロパティ** → **起動オプション** に `-force-d3d11` と入力すると、DirectX 11 で起動して正常に遊べます。
> 確認環境: ASUS ProArt PZ13（Snapdragon X Plus / Adreno X1-45）

### Steam Deck / Linux（動作確認済み）

> [!IMPORTANT]
> Steam Deck での動作は **v0.3.0 以降**です。それより前のバージョンでも起動はしますが、Deck では F1 メニューを操作できません。

Linux ネイティブ版のゲームと Linux 版 BepInEx で動きます。`Install.exe` は Windows 用なので、Deck ではインストールスクリプトを使います。

**インストールスクリプト（推奨）**。デスクトップモードで作業します。

1. [Releases ページ](https://github.com/TomXV/dragnwash-localization/releases)から zip をダウンロードして展開する（右クリック → 展開）
2. 展開したフォルダを開き、何もない所を右クリックして **ここでターミナルを開く** を選ぶ
3. 次の 1 行を入力して Enter

   ```bash
   bash install-steamdeck.sh
   ```

4. **インストール / 更新** を選び、言語を選ぶ。起動オプションを設定するため Steam を一度終了する必要があり、その前に確認が出ます（設定後に Steam を起動し直します）
5. ゲームモードに戻ってゲームを起動する。言語はあとから **Options → 言語（Mod）** で変えられます

スクリプトは、Steam のライブラリ（SD カードも含む）からゲームを探し、公式の Linux 版 BepInEx 5.4.23.5 をダウンロードして SHA-256 で検証し、`run_bepinex.sh` の `executable_name="DragNWash"` を設定し、Mod をコピーし、ゲームの起動オプションに `./run_bepinex.sh %command%` を追加します（すでに設定しているオプションは残します）。更新や削除も同じコマンドで実行し、**インストール / 更新** か **アンインストール** を選びます。アンインストールではセーブ履歴を残し、BepInEx を使う Mod がほかになければ起動オプションから `./run_bepinex.sh` を外し、BepInEx も消すか確認します。`--install` や `--uninstall` を付けると、この質問を飛ばせます。

Steam は起動中に起動オプションを上書きするため、起動オプションを変更するときは Steam を一度終了して書き換え、Steam を起動し直します（事前に確認します。`--close-steam` を付けると確認を飛ばせます）。できなかった手順があれば、最後のダイアログに表示し、手動で変更する内容を案内します。実行内容は `~/.local/state/dragnwash-installer/installer.log` に記録されます。

<details>
<summary>Deck に手動で導入する場合</summary>

1. [BepInEx_linux_x64_5.4.23.5.zip](https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.5/BepInEx_linux_x64_5.4.23.5.zip) をゲームフォルダ（`~/.local/share/Steam/steamapps/common/Drag'n Wash/`）に展開する
2. この Mod の zip の `BepInEx/` を同じ場所に重ねる
3. `run_bepinex.sh` を開き、`executable_name="DragNWash"` にして保存。`chmod +x run_bepinex.sh` で実行権限を付ける
4. Steam のゲームのプロパティ → 起動オプションに `./run_bepinex.sh %command%`
5. 起動する。言語は **Options → 言語（Mod）** で変えられます

</details>

フォントの準備は不要です。ゲーム本編の文字は SteamOS 標準の Noto Sans CJK をファイルから直接読み（日本語・中国語・韓国語の各書体）、F1 メニューは同梱の Noto Sans JP（`dragnwash-menufont.bundle`）で描きます（Steam の Linux ランタイム内では Unity のメニュー描画から CJK フォントが見えないため）。このメニュー用フォントにはハングルとヘブライ文字がないので、Deck の F1 の言語ボタンではこの 2 言語はロケールコードで表示されます。ゲーム本編は通常どおり表示されます。

Deck で言語を変えるときは、コントローラーで **Options → 言語（Mod）** を使えば F1 キーは要りません。翻訳者向けの F1 メニューを使う場合の操作（ゲーミングモード）:

- **F1** を Steam Input でボタンに割り当てて開く
- 右トラックパッドかタッチ画面でポインタを合わせ、**A**・**R2**・トラックパッド押し込みのどれかで押す（Steam Input はトラックパッド押し込みをマウスクリックではなくスティック押し込みとして送るので、Mod 側で対応しています）
- タイトルバーでボタンを押したまま動かすと移動、右下の角なら大きさ変更
- スティックと十字キーで、ポインタが乗っている一覧をスクロール

> [!WARNING]
> **macOS では現在動作しません。** Drag'n Wash は Unity 6.3 で作られていて、BepInEx 5.4.23.5 が macOS で使う読み込み役（Doorstop）が、まだ Unity 6.3 のゲームに割り込めません（[NeighTools/UnityDoorstop#108](https://github.com/NeighTools/UnityDoorstop/issues/108)）。Doorstop 自体はゲームに読み込まれますが BepInEx が起動せず、`BepInEx/LogOutput.log` も `BepInEx/config` も作られないまま、ゲームは英語で始まります。Apple M3 Pro / macOS 26.6 で、Apple シリコンのままでも Rosetta でも同じ結果になることを確認しました。BepInEx 側の問題なので、この Mod からは回避できません。修正の入った BepInEx が出たら、あらためて macOS で試します。
>
> その日のために、**実験的な** macOS 用インストールスクリプトをリポジトリに置いてあります: [`installer/experimental/install-macos.sh`](installer/experimental/install-macos.sh)。Steam Deck 用スクリプトと同じく、macOS 版 BepInEx・Mod・言語・Steam の起動オプションを設定し、ゲームを起動したあとで Mod が読み込まれたかを確かめる **動作確認** もできます。Mac ではまだ一度も実行しておらず、導入前に上の不具合について確認を出します。リリースの zip には入っていません。

### 手動で導入する

### 必要なもの

- Steam版（Windows）の Drag'n Wash
- [BepInEx 5 Windows x64（Mono）版](https://github.com/BepInEx/BepInEx/releases)
- このリポジトリの [Releasesページ](https://github.com/TomXV/dragnwash-localization/releases) で配布される最新版の `DragNWashLocalization-<version>.zip`

> [!IMPORTANT]
> ReleasesのAssetsにある `DragNWashLocalization-<version>.zip` を使用してください。GitHubが自動生成する **Source code** のZIPはMod導入用ではありません。ReleasesページにModのZIPがまだない場合は、導入可能なビルドが未公開です。

### 1. ゲームフォルダを開く

Steamライブラリで **Drag'n Washを右クリック → 管理 → ローカルファイルを閲覧** を選びます。ゲームの `.exe` が置かれているフォルダがゲームルートです。

### 2. BepInExを導入する

BepInEx 5の **Windows x64（Mono）版**をダウンロードし、アーカイブの中身をゲームルートへ直接展開します。

展開後、ゲームの実行ファイルと同じ場所に `winhttp.dll`、`doorstop_config.ini`、`BepInEx` フォルダが並んでいることを確認してください。これらがもう1段内側のフォルダに入っている場合は、ゲームルートへ移します。

ゲームを一度起動し、タイトル画面まで進んだら終了します。BepInExの設定ファイルとログが生成されるので、次へ進む前に `BepInEx/LogOutput.log` が存在することを確認します。

### 3. Drag'n Wash Localizationを導入する

[Releases](https://github.com/TomXV/dragnwash-localization/releases) から `DragNWashLocalization-<version>.zip` をダウンロードし、BepInExと同じ**ゲームルート**へ展開します。`BepInEx` フォルダの統合を確認された場合は許可してください。

プラグインのDLLが次の場所にあれば正しく展開されています。

```text
<Drag'n Washのフォルダ>/BepInEx/plugins/DragNWashLocalization/DragNWashLocalization.dll
```

zip には、この Mod が動く土台の Drag'n Wash ModFramework も入っています。プラグインごとのフォルダ `BepInEx/plugins/DragNWash.ModFramework`、`DragNWash.ModFramework.Text`、`.Dialogue`、`.ToolWindow`、`.Assets`、`.Saves` と、`BepInEx/patchers/DragNWash.ModFramework.Preloader.dll` です。すべて残してください。ほかの Mod がもっと新しい ModFramework を入れている場合は、新しい方を残してください。

ZIPファイルや `DragNWashLocalization-<version>` フォルダが `plugins` とDLLの間に入らないようにしてください。

### 4. 起動して確認する

Drag'n Washを起動します。手動導入では日本語で始まります（インストーラーを使った場合は選んだ言語）。

言語を変えるときは **Options** を開き、ゲームプレイの項目の最後にある **言語（Mod）** を使います。選んだ時点でその言語に切り替わり、**Save** で確定、**Back** で保存済みの言語に戻ります。マウスでもゲームパッドでも操作でき、Steam Deck でも使えます。**F1** ウィンドウの **Translation** タブからも切り替えられ、こちらはその場で確定します。

`BepInEx/LogOutput.log` に `DragNWashLocalization` の起動行が記録されていれば、プラグインは読み込まれています。

起動時の言語を手動で変更する場合は、ゲームを終了して次のファイルを開きます。

```text
BepInEx/config/com.tomxv.dragnwash.localization.cfg
```

`[General]` の `TargetLocale` を `ja` や `zh-Hans` などの導入済みロケールへ変更し、ゲームを起動し直します。`en` にすると Mod を入れたまま英語の原文で遊べます（インストーラー、Options の 言語（Mod）、F1 メニューでも同じ選択ができます）。

### Modが読み込まれない場合

- BepInExとModの両方を、ゲームの実行ファイルがあるフォルダへ展開したか確認します。
- DLLが上記のパスにあるか確認します。
- `BepInEx/LogOutput.log` を開きます。ファイル自体がない場合はBepInExが起動していません。ファイルがある場合は `DragNWashLocalization` を検索し、周辺のエラーを確認します。
- Optionsを開いたときにDirect3D 12でクラッシュする場合は、[設定画面を開くとクラッシュする場合（Windows）](#設定画面を開くとクラッシュする場合windows) の回避策を試してください。
- ゲームのファイルを上書きするタイプの翻訳（`DragNWash_Data` にファイルをコピーするものなど）を入れていた場合、ゲームの英文が置き換わっているため、この Mod は訳す対象を見つけられません。先に Steam でゲームを右クリック → **プロパティ** → **インストール済みファイル** → **ゲームファイルの整合性を確認** で元のファイルに戻してから、この Mod を入れ直してください。そうした翻訳はゲームのアップデートで消えたり壊れたりしますが、この Mod はゲームのファイルを一切書き換えません。

## 言語パック

翻訳ファイルは TomXV が作成し、同じ zip に同梱しています。言語パックを改善してくれた協力者は、下の表のその言語の行に記載します（[協力者のクレジット](CONTRIBUTING.ja.md#協力者のクレジット)参照）。インストーラーと F1 メニューは `Translations/` 配下のフォルダを自動で一覧にします。

| ロケール | 言語 | 状態 |
| --- | --- | --- |
| `ja` | 日本語 | 作者が監修 |
| `zh-Hans` | 简体中文 | 作者が監修 |
| `zh-Hant` | 繁體中文 | 仮翻訳（監修済みの簡体字版を台湾の言い回しで繁体字化） |
| `de` | Deutsch | 仮翻訳 |
| `fr` | Français | 仮翻訳 |
| `es` | Español | 仮翻訳 |
| `pt-BR` | Português (Brasil) | 仮翻訳 |
| `ko` | 한국어 | ネイティブ校正済み、Hotcake（ゲームで使われない行はそのまま） |
| `ru` | Русский | 仮翻訳 |
| `pl` | Polski | 仮翻訳 |
| `he` | עברית | 仮翻訳（右から左に表示） |
| `uk` | Українська | 仮翻訳 |
| `th` | ไทย | 仮翻訳（単語の間にゼロ幅スペースを入れて折り返せるようにしています） |
| `vi` | Tiếng Việt | 仮翻訳 |
| `eo` | Esperanto | 仮翻訳（面白枠） |
| `tok` | toki pona | 仮翻訳（面白枠。単語が 137 個しかない言語なので、かなりざっくり） |
| `en` | English | ゲーム本来の英語（翻訳なし） |

> [!NOTE]
> **仮翻訳**はネイティブスピーカーの監修を受けていません。全行そろっていて通しで遊べますが、不自然な言い回しやジョークの取りこぼしがあり得ます。作者が監修したのは日本語と簡体字中国語のみです。ネイティブの方からの修正 PR を歓迎します（[CONTRIBUTING.ja.md](CONTRIBUTING.ja.md)）。各 `strings.csv` の先頭にも同じ注記をコメントで入れています。

## 翻訳者向け

`Translations/<locale>/strings.csv` を編集するだけで翻訳を追加できます。公開ファイルは
`key,section,node,order,speaker,translation` の列を持ちます。`key` は英語原文のハッシュ、`section` / `node` / `order` はゲーム内のどこ（レベルと会話）で流れるかをプレイ順で示し、`speaker` は誰の台詞かです。`#` で始まる行は `# ===== Level 1: Ryan (Sunny) =====` のような見出しで、ファイルを上から読むと台本のように流れが追えます。
おすすめの作業手順：

1. ゲーム内で **F1 → Translation → Export working copy** を押す。`Translations/_discovered/<locale>.working.csv`
   に、各行の英語原文を並べた作業用ファイル（`key,section,node,order,speaker,source_en,translation`）が、同じ見出しつきでゲーム内の実行順に書き出されます
2. `translation` 列を編集して保存する。起動中のゲームにその場で反映されます
3. コミット前に **F1 → Translation → Hash for commit**（または `tools/hash-strings.ps1`）で、英語原文を含まない `strings.csv` を作り直す

リポジトリにはゲームの英語台本を含めない方針で、**製品版を持っている人だけが翻訳できる**仕組みです。
各言語フォルダには表示名を書いた1行の `name.txt`（例: `日本語`）があり、インストーラーとゲーム内メニューに表示されます。
Unity内部のキー名などを知る必要はありません。手順は [CONTRIBUTING.ja.md](CONTRIBUTING.ja.md) を参照。書式タグ（`<size=70%>`など）が原文に
含まれている場合は、タグ構造をそのまま残して中の文章だけ訳してください。

### 会話文をまとめて確認したい場合

これらは開発者向けの機能です。先に **Options → Mods → Drag'n Wash ModFramework → Developer tools** をオンにしてください（遊ぶだけの人にはオフで、以下は何も動きません）。そのうえで、ゲーム内（セーブをロードした後）で **F6キー** を押すと、全会話文が
`BepInEx/plugins/DragNWashLocalization/Translations/_discovered/dialogue_lines.csv`
に一括で書き出されます（実機で1839行を確認済み）。

行は**ゲーム内で実際に流れる順**に並びます。`node` 列が会話の単位で、
`Alexander_2_intro` のように「キャラクター名＿何回目＿場面」の形になっており、
`order` 列がその会話内での順番です。`kind` 列は `line`（キャラクターの台詞）と
`option`（プレイヤーが選ぶ選択肢）を区別します。誰が誰に何と答えているかが分かるので、
前後を見ながら訳せます。登場するドラゴンは Conrad / Ryan / Alexander の3体です。

訳したい行を `Translations/<locale>/strings.csv` にコピーし、`translation` 列を埋めて
実機で確認してください（`node` や `key` など余分な列が付いたままでもプラグインは問題なく読み込みます）。

**PR を送る前に、公開用ファイルを作り直してください。** ゲーム内の **Hash for commit** ボタン
（または `tools/hash-strings.ps1`）を使います。
自動チェックが受け付けるのは公開形式のヘッダだけです。*Hash for commit* が書く
`key,section,node,order,speaker,translation` のほか、短い `key,speaker,translation` と `key,translation`
も通ります。作業ファイルのヘッダはどれにも当てはまらないため、
`source_en` などダンプ由来の列が残ったファイルは弾かれます。
英語原文を含む PR を作らないことは、このリポジトリの
一番の前提でもあります。詳しくは [CONTRIBUTING.ja.md](CONTRIBUTING.ja.md#コミット前にハッシュ化する) を参照してください。

すでに訳した行は `translation` 列に訳が入った状態で出力されるので、再ダンプしても
作業は失われません。

### 編集した訳を再起動なしで確認する

ゲームを起動したまま `Translations/<現在の言語>/strings.csv` または作業用ファイル
`_discovered/<locale>.working.csv` を保存すると、約2秒後に自動で読み直され、画面に出ている
テキストにもその場で反映されます。変わった行は F1 の Activity log に1行ずつ出ます。
訳を直しては画面で確かめる、を再起動なしで繰り返せます（`[Debug] HotReloadTranslations`
で無効化可能）。反映されたかは F1 の Activity log に `[reload]` で出ます。

### UI文言をまとめて確認したい場合

**F7キー** で、読み込み済みの全UIテキストが
`Translations/_discovered/ui_texts.csv` に書き出されます。**非表示のメニューも対象**なので、
ポーズメニューや確認ダイアログを開かなくても、そのシーンのUI文言が丸ごと手に入ります。
列は `key` / `source_en` / `translation`（訳済みなら既存の訳）/ `object_path`（画面上のどこか）です。

タイトル画面とゲーム中で1回ずつ押せば、ほぼ全てのUIが揃います。

未翻訳のUI文言は、プレイ中に自動的に
`Translations/_discovered/strings.csv` にも記録されます。このファイルは起動のたびに
整理され、すでに訳した行や重複は取り除かれるので、常に「残りの作業リスト」になります。

### 訳す必要のない文字列について

スライダーの数値・解像度（`1920 x 1080 @ 164.995Hz`）・ビルド番号などは、
最初から記録の対象外です。除外パターンは [Translations/ignore.txt](Translations/ignore.txt)
に追記できます（正規表現、ファイル内に記述例あり）。

除外されるのは「記録」だけです。翻訳の検索のほうが先に行われるため、
`strings.csv` に書いた行は除外パターンに一致していても必ず翻訳されます。

### ゲーム内デバッグメニュー

**F1キー** で、Drag'n Wash ModFramework を使うほかの Mod と共通のツールウィンドウを開閉できます
（キーは `BepInEx/config/com.tomxv.dragnwash.modframework.toolwindow.cfg` の `[General] ToggleKey`）。
タイトル部分をドラッグして移動、右下の角をドラッグしてサイズを変更できます。この Mod は次の 4 つのタブを足します。

- **Activity log**: 翻訳結果と処理ログを表示します。`Follow: ON/OFF` で末尾への
  自動追従を切り替えられ、手動スクロールすると追従が止まります。`Clear log` は
  表示と重複抑制をリセットします。ログは直近100件を保持します。
- **Translation**: 再起動なしで言語を切り替えます（ボタン名は各言語の `name.txt`、**English** は翻訳オフ）。
  会話の書き出し（`Export loaded dialogue`）、UI文言の書き出し（`Export UI text`）、
  原文つき作業ファイルの書き出し（`Export working copy`）、公開ファイルの作り直し（`Hash for commit`）、
  レイアウトチェックもここから行います。
- **Saves**: セーブの巻き戻し、レベル番号の変更、フラグの反転。後述。
- **About**: 動いている版とビルド、制作者、ライセンス、今回の起動で読み込んだ内容。不具合報告のときにそのまま引用できます。

**Check translation layout** ボタンで、レイアウト崩れリスクのある文字列を
`Translations/_discovered/layout_risks.csv` に書き出せます（しきい値は
`BepInEx/config/.../LayoutOverflowThreshold`、既定 `1.0` ＝ ちょうど収まる）。

### セーブを1つ前に戻す（翻訳確認用）

ゲームがセーブを書き込むたびに、プラグインが
`BepInEx/SaveHistory/<スロット>/` に世代コピーを残します
（スロットごとに既定30世代、`BepInEx/config/com.tomxv.dragnwash.modframework.saves.cfg` の `[History] Keep` か Mods 画面で変更可）。
以前の版が `BepInEx/plugins/DragNWashLocalization/SaveHistory` に残したコピーは、初回起動時にそこへ移ります。
**F1 → Saves** タブでスロットを選び、戻したい世代の **Restore** を押すと、その世代が
ゲームのセーブファイルに書き戻されます。**その後タイトル画面に戻ってスロットをロード**すると
反映されます（ゲーム内でセーブすると再び上書きされます）。復元前の状態も自動で
世代に残るので、戻しすぎても元に戻せます。

同じ場面の会話を訳し直して見比べたいときに使ってください。Restore はフラグや変数を
直接いじるわけではなく、ゲーム自身のセーブファイルを差し替えるだけです。

同じタブの **PROGRESS** 欄では、レベル番号を **-** / **+** で変えて **Apply** できます。
先に進める方向はネタバレの可能性があるので確認が出ます。**Flags...** を押すと、ゲームが使う
イベントフラグが分類（レベル進行 / ストーリー / 恋愛 / シーン発生条件 / シーン視聴済み / 洗浄セッション / アイテム / デバッグ）と
説明つきで全部並びます。セーブにまだ存在しないフラグも「unset」として表示され、値をクリックすると
unset → true → false の順に切り替わります。検索欄で絞り込み、**Reset all to false...** で全フラグを
一括で false にできます（レベル番号は保持）。一覧は DLL の隣の `FlagCatalog.csv` から読むので、
後から見つけたフラグを行として追加できます。どの編集も直前の
状態をスナップショットに残してから行われます。

## 設定画面を開くとクラッシュする場合（Windows）

Unity 6000.3.14f1 / DirectX 12 環境で、Options を開いた際に
`D3D12ScratchAllocator::DestroyScratch` で落ちる問題がありました。同じスタックの
[Unity公式の不具合報告（UUM-140564）](https://issuetracker.unity.com/issues/10698)
があり、ネイティブ描画側のバグなので翻訳フックで例外を捕捉しても防げません。

このバグは**実行中のテクスチャ確保・アップロード**で踏みます。そのため Direct3D 12 では、
導入済みの全言語のフォントを起動時に準備し、ゲーム中も、Options や F1 メニューでの言語切り替え時も
フォントアトラスに何も追加しないようにしています。各文字はその言語が使うフォント 1 つにだけ
焼き込むので、起動時の処理は小さく済みます。言語を何度も切り替えても落ちないことを
実機で確認済みです。設定は不要です。

Direct3D 11 や Steam Deck の Vulkan など他の描画 API は実行中のアップロードに耐えるので、
使用中の言語だけを準備し、ほかの言語は選んだ時に読み込みます。そちらでも起動時に全部準備したい
場合は `[Font] PreloadAllLocales = true` にしてください。

それでも落ちる場合は、Steamライブラリで
**Drag'n Wash → プロパティ → 一般 → 起動オプション** に `-force-d3d11` を追加して
再起動すると、描画APIごとバグを回避できます
（[Unity標準の起動オプション](https://docs.unity3d.com/6000.3/Documentation/Manual/PlayerCommandLineArguments.html)。
ゲーム本体のDLL・セーブデータは変更されません）。
`BepInEx/LogOutput.log` のプラグイン起動行で、実際に使われている描画APIを
`graphics=...` として確認できます。

`BepInEx/config/com.tomxv.dragnwash.modframework.assets.cfg` の `[Fonts] AtlasPointSize`
を下げると、フォントアトラスの枚数が減り、上げると文字が鮮明になります（既定80）。

## 排他的フルスクリーンで画面を切り替えると固まる場合（Windows）

DirectX 12 で **ウィンドウモード** を **排他的フルスクリーン** にしていると、別のウィンドウに切り替えて（Alt+Tab や、ほかのウィンドウをクリック）戻ったときにゲームが固まり、そのまま落ちることがあります。
クラッシュの記録では、Windows が排他的フルスクリーンを解除・再開する間に Unity の DirectX 12 の画面表示が止まっています（`D3D12SwapChain::Present` がエラー `887a0001` で失敗し、ログにはその前に `D3D12Fence::Wait ... May cause crash` が出ることが多い）。
このとき Mod のコードは動いておらず、この Mod ではなくゲームの描画処理の問題です。

次のどちらかで避けられます。

- Steam ライブラリで **Drag'n Wash → プロパティ → 一般 → 起動オプション** に `-force-d3d11` を追加する。排他的フルスクリーンのまま切り替えても固まらなくなることを確認済みです。
- **ウィンドウモード** を **排他的フルスクリーン** ではなく **フルスクリーン** にする。画面を切り替えたときに表示モードが変わるのは排他的フルスクリーンだけなので、これでも避けられるはずです（まだ試していません）。

## 現在のステータス

最新のリリースは v1.3.0 です（Direct3D 12 でのクラッシュが減り、落ちたときは何が起きたかをウィンドウで知らせるように。Drag'n Wash ModFramework 1.3.0 を同梱）。v1.2.1 では、2026 年 9 月 14 日のゲームのアップデート以降に作ったセーブが Saves タブにまた出るように、翻訳者の作業用ファイルに開いていなかった画面の英語も入るようにしました。v1.2.0 をリリース済み（ウクライナ語・タイ語・ベトナム語で 16 言語、韓国語のネイティブ校正、ゲームの更新で消えにくい翻訳、ロゴ、Drag'n Wash ModFramework 1.2.0）。v1.1.2 で（Drag'n Wash ModFramework 1.1.2 を同梱し、Mods 画面のアイコンを手作りのロゴに差し替え）。v1.1.1 で（ModFramework 1.1.1 を同梱し、Mods 画面にフレームワークのアイコンを表示）。v1.1.0 で（ModFramework 1.1.0 と合わせて、この Mod の新しいリリースが出ると Mods 画面とタイトル画面で知らせるように）。v1.0.0 で [Drag'n Wash ModFramework](https://github.com/TomXV/dragnwash-modframework) の上で動くようにし、Mods 画面を追加。v0.6.2 で、古い作業用ファイルで「Hash for commit」をしても行が消えないように修正し、「Really Delete Save?」を翻訳。v0.6.1 でヘブライ語の、訳のない名前などが反転して表示される不具合を修正。v0.6.0 で台詞 ID ごとの訳（複数のキャラが話す同じ英文を、キャラごとに訳し分けられるように。2026 年 9 月 14 日のゲームのアップデートで動作確認済み）。v0.5.0 でゲームの Options 画面から言語を変更できるように、v0.4.0 で 13 言語、言語ごとのフォント準備、About タブ、日英中に切り替えられるインストーラーを、v0.3.0 で Steam Deck 対応を入れました。Windows on ARM でも動作を確認済みです（ゲーム本体の都合で `-force-d3d11` が必要）。macOS は、BepInEx 側の既知の不具合で現在は動作しません（[Steam Deck / Linux](#steam-deck--linux動作確認済み) の下の注意書き参照）。BepInExプラグインの骨格、UI文字列・会話文の日本語/中国語差し替え、
CJKフォント表示、会話・UIの一括抽出、ゲーム内デバッグメニュー、レイアウト崩れ検出、
翻訳者向けドキュメント、リリース手順を実装・実機確認済みです。
詳細は [docs/PLAN.ja.md](docs/PLAN.ja.md) を参照してください。

これからの予定（他の Mod のテキストの翻訳を含む）は [docs/ROADMAP.ja.md](docs/ROADMAP.ja.md) にあります。

## Drag'n Wash ModFramework

v1.0.0 から、この Mod は前提 Mod **Drag'n Wash ModFramework** の上で動きます。ほかの Drag'n Wash の Mod も、この上に作れます。

- **役割。** この Mod がゲームに入り込むために作ってきた仕組みの多くは、ほかの Mod にも役立つので、フレームワークに移しました。入っている Mod を設定やオン・オフと一緒に一覧できる **Mods** 画面（Options → Mods）、ゲームの Options 画面の言語の行、表示前のテキストの書き換え、台詞や選択肢のイベント、共通の F1 ツールウィンドウ、Direct3D 12 で安全なフォント、セーブ履歴です
- **目的。** ゲームがアップデートされたとき、追従が必要なのはフレームワークだけになり、その上に乗る Mod は動き続けられます。2026 年 9 月 14 日のアップデートのような変更を、1 か所で吸収します
- **更新のお知らせ。** v1.1.0 から、この Mod やフレームワークの新しいリリースが出ると、タイトル画面に **1 update available in Mods** と出て、**Options → Mods** からリリースページを開けます。フレームワークが 1 日 1 回 GitHub に最新リリースを問い合わせるだけで、あなたやゲームについての情報は送らず、ダウンロードもしません。止めるには **Mods → Drag'n Wash ModFramework → 設定 → 更新を確認する** をオフにしてください。
- **プレイヤーの方へ。** リリースの zip とインストーラーにフレームワークも入っています。v1.1.0 からのインストーラーは、どの Drag'n Wash の Mod も同梱できるフレームワークの共通インストーラーです。この Mod をアンインストールしても、ほかの Mod が入っていればフレームワークは残します
- **翻訳者の方へ。** CSV の形式と翻訳用ツールは変わりません。今までの翻訳パックや協力はそのまま使えます
- **説明書** は、フレームワークの [Wiki](https://github.com/TomXV/dragnwash-modframework/wiki/Home-ja) にあります。Mods 画面、クラッシュレポート、F1 の開発者ツール、そしてこの Mod には入っていない [Inspector](https://github.com/TomXV/dragnwash-modframework/wiki/Inspector-ja)（入れ方も Wiki にあります）です

Drag'n Wash の Mod を作っていて、フレームワークに欲しい機能があれば、Issue で教えてください。

## 翻訳に参加する

コード不要で `Translations/<locale>/strings.csv` を編集するだけで参加できます。
手順・書式・未翻訳の見つけ方は [CONTRIBUTING.ja.md](CONTRIBUTING.ja.md) を参照してください。

参加するみなさんには[行動規範](CODE_OF_CONDUCT.ja.md)に沿ってもらいます。セキュリティの問題を見つけたときは、Issue ではなく非公開で報告してください（[SECURITY.ja.md](SECURITY.ja.md)）。そうしたくて、できるならば [GitHub Sponsors](https://github.com/sponsors/TomXV) もあります。いずれにせよ Mod は無料のままですし、翻訳のほうがずっと価値があります。

## 配布・リリース

リリース zip のビルドと配布手順は [docs/RELEASING.ja.md](docs/RELEASING.ja.md) を
参照してください（ゲーム由来の参照アセンブリをコミットできないため、リリースは
ローカルでビルドして GitHub Releases にアップロードします）。

## 開発者の方へ

本プロジェクトは非公式のファン制作物で、Gator Dragon Games とは無関係です。Drag'n Wash ModFramework の[コンテンツポリシー](https://github.com/TomXV/dragnwash-modframework/blob/main/docs/CONTENT_POLICY.ja.md)に従い、ゲームのアセットや台本をそのままの形では含まず、英語原文は SHA-256 ハッシュとしてのみ保持し、ゲームのファイルを書き換えることもありません（BepInEx が実行時にプラグインを読み込みます）。開発チームの方で懸念がある場合は、このリポジトリの Issue かメンテナーへの連絡でお知らせください。ご希望に応じて修正または公開停止します。

## クレジット

- この Mod の**ロゴ**（Mods 画面のアイコン、`icon.png`）は **Mister ERIO** さん（[@mistererio](https://github.com/mistererio)）が描き、許可を得て使っています。
- Drag'n Wash ModFramework に同梱されている Options 画面の **Mods ボタン**も、Mister ERIO さんの作品です。
- Drag'n Wash ModFramework の**ロゴとアイコン**（アイコンはこの zip にも入っています）は、**NotaGames** さん（[@NotaGames](https://github.com/NotaGames)）の作品です。
- 韓国語パックは **Hotcake** さんに校正していただきました。
- 言語パックを改善してくださった翻訳者の方は、[言語パック](#言語パック)の表に記載しています。

## ライセンス

プラグインのコードは [LICENSE](LICENSE) を参照してください。「クレジット」に挙げた絵は作者のもので、このライセンスの対象外です。ゲーム本体の資産・コードは
含んでおらず、翻訳文はそれぞれの翻訳者の貢献として扱われます。
