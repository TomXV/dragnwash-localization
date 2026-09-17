# ロードマップ

[English](ROADMAP.md)

Drag'n Wash Localization のこれからの予定です。予定は変わることがあり、日付は近いものだけ書いています。更新日：2026-09-17。

## リリース済み：v1.3.0（2026-09-19）

- Drag'n Wash ModFramework 1.3.0 の上で動きます。Direct3D 12 でのクラッシュが減り、落ちたときはクラッシュレポートのウィンドウが出ます。

## リリース済み：v1.2.1（2026-09-19）

- 2026-09-14 のゲームのアップデート以降に作ったセーブを、Saves タブがまた見つけられるように（Drag'n Wash ModFramework 1.2.1）。
- 書き出したときに開いていなかった画面の英語も、作業用ファイルに入るように（[#31](https://github.com/TomXV/dragnwash-localization/issues/31)）。

## リリース済み：v1.2.0（2026-09-17）

- ウクライナ語・タイ語・ベトナム語（仮翻訳）を追加し、16 言語に。
- 韓国語を母語話者が校正（Hotcake さん、ありがとうございます）。
- ゲームの更新で台詞の文面が変わっても、翻訳が消えないように。
- Mister ERIO さんによるロゴ。
- Drag'n Wash ModFramework 1.2.0 の上で動きます。

## 予定していること

### 他の Mod のテキストの翻訳（[#28](https://github.com/TomXV/dragnwash-localization/issues/28)）

新しい仕組み・UI・会話を足す Mod のテキストも、翻訳できるようにします。

- **今できること：** Mod が TextMeshPro で出す文字は、ゲームの文字と同じ仕組みで訳せます。未翻訳の行は書き出しにも出ます。IMGUI や古い uGUI の `Text` で出す文字は対象外です。
- **やること：** 各 Mod が `<Mod のフォルダー>/Translations/<locale>/strings.csv` に自分の訳を同梱し、この Mod が自分のパックのあとにそれを読み込みます。Mod のファイルがゲーム本体の行を上書きすることはありません。他の Mod の訳は、このリポジトリでは持ちません。
- **β版の実験的機能として、既定はオフ**で入れます。他の Mod との組み合わせは未知の領域なので、承知のうえでオンにしてもらいます。
- 機械翻訳は組み込みません。
- 他の Mod は、その作者から頼まれたときに試します。
- 設計：[docs/MOD_TRANSLATIONS.ja.md](MOD_TRANSLATIONS.ja.md)。状況：**設計済み、未実装。**

### 絵の翻訳（[#4](https://github.com/TomXV/dragnwash-localization/issues/4)）

メニューのボタン、ロード画面の扉の札、壁の看板は絵です。フレームワークのコンテンツポリシーに基づき、手で描いた絵やゲームの絵に手を加えたものを、言語ごとにこのリポジトリに入れます。描いた人はクレジットに載せます。設定でオフにでき、Direct3D 12 では再起動後に切り替わります。そのために、フレームワークに言語ごとのテクスチャの差し替えを足します。

- 設計：[docs/TRANSLATED_TEXTURES.ja.md](TRANSLATED_TEXTURES.ja.md)。状況：**設計済み、未実装。**

### 母語話者による確認

ドイツ語、フランス語、スペイン語、ブラジルポルトガル語、ロシア語、ポーランド語、ヘブライ語、ウクライナ語、タイ語、ベトナム語、繁体字中国語は、まだ仮翻訳です。修正のプルリクエストはいつでも歓迎します。確認してくださった方はクレジットに載せます（[CONTRIBUTING.ja.md](../CONTRIBUTING.ja.md)）。

### ほかの環境での確認

- Steam Deck：F1 の窓で、画面キーボードで文字を入力できるか。
- macOS：BepInEx の Doorstop が Unity 6.3 にフックできるようになるのを待っています（[UnityDoorstop#108](https://github.com/NeighTools/UnityDoorstop/issues/108)）。

## 予定していないこと

- Mod 内での機械翻訳。
- ゲームのファイルや英語の台本を、手を加えずにそのまま配布すること（[コンテンツポリシー](https://github.com/TomXV/dragnwash-modframework/blob/main/docs/CONTENT_POLICY.ja.md)）。
- 他の Mod の訳をこのリポジトリで持つこと。
