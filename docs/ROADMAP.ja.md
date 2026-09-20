# ロードマップ

[English](ROADMAP.md)

Drag'n Wash Localizationのこれからの予定です。
予定は変わることがあり、日付は近いものだけ書いています。
更新日：2026-09-20。

## リリース済み：v1.4.0（2026-09-20）

- 他のModが同梱する訳（[#28](https://github.com/TomXV/dragnwash-localization/issues/28)）。
  β版の実験的機能で、既定はオフです。
- 言語ごとの絵の翻訳（[#4](https://github.com/TomXV/dragnwash-localization/issues/4)）。
  仕組みだけで、絵はまだ描かれていません。
- Drag'n Wash ModFramework 1.4.0の上で動きます。

それぞれの中身は、下の2つの項目のとおりです。

### 他の Mod のテキストの翻訳（[#28](https://github.com/TomXV/dragnwash-localization/issues/28)）

新しい仕組み・UI・会話を足すModのテキストも、翻訳できるようにします。

- **今できること**:
  ModがTextMeshProで出す文字は、ゲームの文字と同じ仕組みで訳せます。
  未翻訳の行は書き出しにも出ます。
  IMGUIや古いuGUIの `Text` で出す文字は対象外です。
- **どう動くか**:
  各Modが `<Mod のフォルダー>/Translations/<locale>/strings.csv` に自分の訳を同梱し、
  このModが自分のパックのあとにそれを読み込みます。
  Modのファイルがゲーム本体の行を上書きすることはありません。
  同じ行の訳が食い違うときは、先に読んだほう（まずこのMod自身のパック、次に他のModをGUID順に毎回同じ順）を使い、
  あとのほうを衝突として記録します。
  ログとF1 → Translationに、両方の名前とどちらを使ったかが出ます。
  書き出しにはModの列が付くので、その行がどこのものか分かります。
- **β版の実験的機能で、既定はオフ**です。
  他のModとの組み合わせは未知の領域なので、承知のうえでオンにしてもらいます。
- 機械翻訳は組み込みません。
- 他のModは、その作者から頼まれたときに試します。
- 他のModの訳は、このリポジトリでは持ちません。
- 設計: [docs/MOD_TRANSLATIONS.ja.md](MOD_TRANSLATIONS.ja.md)。
  状況: **テスト用の Mod でゲーム内まで確認済み。次のリリースから入ります。**

### 絵の翻訳（[#4](https://github.com/TomXV/dragnwash-localization/issues/4)）

メニューのボタン、ロード画面の扉の札、壁の看板は絵です。
フレームワークのコンテンツポリシーに基づき、手で描いた絵やゲームの絵に手を加えたものを、言語ごとにこのリポジトリに入れます。
描いた人はクレジットに載せます。
置き場所は `Translations/<locale>/textures/` で、`fallback.txt` にたどる言語を書けます。
**Translate pictures** の設定でオフにでき、Direct3D 12では言語を変えたあと再起動で切り替わります。
土台は、フレームワークのAssets 1.2.0の言語ごとのテクスチャ差し替えです。

- 設計: [docs/TRANSLATED_TEXTURES.ja.md](TRANSLATED_TEXTURES.ja.md)。
  状況: **ゲーム内（Direct3D 12）まで確認済み。次のリリースから入ります。絵はまだ一枚もありません。**
  —— 仕組みだけ先にあり、どの言語からでも足せます。

## リリース済み：v1.3.0（2026-09-19）

- Drag'n Wash ModFramework 1.3.0の上で動きます。
  Direct3D 12でのクラッシュが減り、落ちたときはクラッシュレポートのウィンドウが出ます。

## リリース済み：v1.2.1（2026-09-19）

- 2026-09-14のゲームのアップデート以降に作ったセーブを、Savesタブがまた見つけられるように（Drag'n Wash ModFramework 1.2.1）。
- 書き出したときに開いていなかった画面の英語も、
  作業用ファイルに入るように（[#31](https://github.com/TomXV/dragnwash-localization/issues/31)）。

## リリース済み：v1.2.0（2026-09-17）

- ウクライナ語・タイ語・ベトナム語（仮翻訳）を追加し、16言語に。
- 韓国語を母語話者が校正（Hotcakeさん、ありがとうございます）。
- ゲームの更新で台詞の文面が変わっても、翻訳が消えないように。
- Mister ERIOさんによるロゴ。
- Drag'n Wash ModFramework 1.2.0の上で動きます。

## 予定していること

### 母語話者による確認

ドイツ語、フランス語、スペイン語、ブラジルポルトガル語、ロシア語、ポーランド語、ヘブライ語、ウクライナ語、タイ語、
ベトナム語、繁体字中国語は、まだ仮翻訳です。
修正のプルリクエストはいつでも歓迎します。確認してくださった方はクレジットに載せます（[CONTRIBUTING.ja.md](../CONTRIBUTING.ja.md)）。

### ほかの環境での確認

- Steam Deck: F1の窓で、画面キーボードで文字を入力できるか。
- macOS: BepInExのDoorstopがUnity 6.3にフックできるようになるのを待っています（
  [UnityDoorstop#108](https://github.com/NeighTools/UnityDoorstop/issues/108)）。

## 予定していないこと

- Mod内での機械翻訳。
- ゲームのファイルや英語の台本を、手を加えずにそのまま配布すること（
  [コンテンツポリシー](https://github.com/TomXV/dragnwash-modframework/blob/main/docs/CONTENT_POLICY.ja.md)）。
- 他のModの訳をこのリポジトリで持つこと。
