# 訳したテクスチャ

[English](TRANSLATED_TEXTURES.md)

2026-09-18に `experimental/translated-textures` ブランチで作成。
**そのブランチで実装済み（フレームワークは `experimental/language-textures` の Assets 1.2.0）。**
**2026-09-19 にゲームで確認しました**
**（Direct3D 12。メニューのボタンをテスト用の絵に差し替え・表示される、設定でオフにできる、**
**ほかの言語ではゲームの絵に戻る、落ちない）。**
きっかけは [#4](https://github.com/TomXV/dragnwash-localization/issues/4) です。
ゲームの文字の一部は絵になっています（メニューのボタン、ロード画面の扉の札、壁の看板）。

## 変わったこと

Drag'n Wash ModFrameworkの[コンテンツポリシー](https://github.com/TomXV/dragnwash-modframework/blob/main/docs/CONTENT_POLICY.ja.md)により、
このリポジトリに、**手で描いたテクスチャや、ゲームのテクスチャに手を加えたもの**
（別の言語に描き直した看板など）を入れられるようになりました。
ゲームの絵をそのまま入れることはしません。

## プレイヤーから見て

- 文字の入った絵が、選んだ言語で表示されます（その言語の絵があるものだけ。ないものはゲームの絵のまま）。
- **Options → Mods → Drag'n Wash Localization → Settings → Translate pictures**（既定はオン）で、絵の翻訳だけをオフにできます。
- 言語を変えると、絵もすぐに変わります。
  ただし **Direct3D 12** では、ゲームを動かしたまま絵を読み込むと落ちることがあるので（Unity UUM-140564）、
  絵が変わるのは次にゲームを起動したときです。設定の説明文にそう書き、切り替えのたびにログにも出します。

## リポジトリの中

```text
Translations/
  ja/
    strings.csv
    textures/
      MenuButtons0001.png      ← 差し替えるゲームのテクスチャの名前
      Reception.png
      credits.csv              ← それぞれの絵を作った人
  zh-Hant/
    textures/
      fallback.txt             ← 任意：zh-Hans
```

- **名前**:
  フレームワークのAssetsタブに出る、ゲームのテクスチャの名前にします。
  大きさは元と同じにします（スプライトは元の範囲を保つため）。
- **credits.csv**:
  `file,author,note` の形で、PNG 1つにつき1行。
  `note` には何をしたかを書きます（`一から描いた`、`ゲームのテクスチャを描き直した`）。
  すべてのPNGに行が必要です。
- **形式**:
  PNG形式。
  1枚につき4096×4096以内、8 MB以内。
- **フォールバック**:
  その言語に絵がないテクスチャは、次の順で代わりを探します。
  `textures/fallback.txt` に書いた言語（1行に1つ。例：`zh-Hant` の代わりに `zh-Hans`）、
  別のModの普通の差し替え、ゲームの元の絵。
  読み込めないPNGファイルも同じ順で代わりになります。
  `fallback.txt` がなければ、絵のないものはゲームの元の絵のままです。
- **入れてはいけないもの**:
  ゲームのテクスチャに手を加えていないPNGファイル。
- **CI の確認**（`tools/check-translations.py`）:
  - `textures/` には、PNGファイルとcredits.csvだけ。
  - PNGファイルは、本物のPNGファイルで、大きさの上限以内。
  - すべてのPNGファイルに、作った人と説明のあるcredits.csvの行があり、すべての行にPNGファイルがあること。
  - フレームワークのアセットの指紋ができたら、手を加えていない書き出しと同じPNGファイルも拒否する。
  - 名前がゲームのテクスチャと合っているかは、ここでは確かめません。どこにも当たらなかった絵はAssetsタブに出ます。
- **リリース**:
  `tools/pack.ps1` が、言語ごとに `textures/` もコピーします。
- **クレジット**:
  READMEのクレジットに、翻訳者と同じく言語ごとに絵を作った人を載せます。

## フレームワーク（Assets ライブラリ）の側

今のフレームワークは、`<Mod>/assets/textures/` の絵で、言語に関係なくテクスチャを名前で差し替えます。
訳した絵には、言語ごとのまとまりが要ります。

新しく足すもの：

- `AssetReplacements.AddLanguageFolder(string guid, string root, string subfolder)`:
  `root/<言語>/<subfolder>/*.png` を、`GameFonts.Language` がその言語のときだけ効く差し替えにします。
  Localizationは `AddLanguageFolder(guid, <プラグイン>/Translations, "textures")` を呼びます。
- 設定用に `AssetReplacements.SetLanguageFoldersEnabled(string guid, bool on)`。
- **読み込むのは、使っている言語の分だけ**:
  起動時には今の言語のファイルを読みます。
  言語が変わったとき：
  - 実行中のアップロードが安全な環境では、新しい言語のファイルを読み、前の言語の差し替えを元に戻してから、新しい差し替えを当てます。
  - Direct3D 12では何も読みません。
    `AssetReplacements.PendingLanguage` に、再起動後に効く言語を入れ、Modがそれを表示できるようにします。
- **元に戻す**:
  今の差し替えは、元に戻す必要がありませんでした。
  言語の切り替えと設定のオフでは戻す必要があるので、
  ライブラリは、変えたマテリアルのプロパティとスプライトの使い手ごとに、元が何だったかを覚えておき戻します。
  差し替えている間も元のテクスチャへの参照を持ち続け、Unityに解放されないようにします。
- **ぶつかったとき**:
  言語の絵と、別のModの普通の差し替えが同じテクスチャなら、言語の絵を当てます。
  どちらの名前も、ほかの衝突と同じくlogとAssetsタブに出します。
- **Assets タブ**:
  差し替えに言語を表示します。
  **Reload files** は今の言語の絵も読み直します（今と同じくDirect3D 12では動きません）。
- **ゲームの更新**:
  起動してから一度もどこにも当たらなかった差し替えを「未使用」として並べ、テクスチャの名前が変わったことに気づけるようにします。

## 翻訳する人の手順

1. 開発者ツールをオンにし、Assetsタブでテクスチャを探します（絞り込みの欄、またはInspectorの **Inspect**）。
2. 下絵を手に入れて（フレームワークの書き出しができるまでは、自分のPCでゲームのテクスチャを読めるツールで）、訳した絵を描きます。
3. `BepInEx/plugins/DragNWashLocalization/Translations/<locale>/textures/<名前>.png` に保存し、
   **Reload files** を押してゲームで確認します（Direct3D 11かVulkanで。Windowsでは起動オプションに `-force-d3d11`）。
4. `credits.csv` に行を足し、PNGファイルと一緒にプルリクエストを出します。

## 作る順番

1. フレームワーク：言語フォルダー、元に戻す処理、再起動待ちの言語、Assetsタブの言語の列（Assetsライブラリのマイナーバージョン）。
2. Localization：設定（`[General] TranslatePictures`）、`AddLanguageFolder`、Direct3D 12での案内、pack.ps1、CIの確認、
   CONTRIBUTINGの説明。
   Assetsライブラリが1.2.0より古いときは、絵なしで動きます。
3. 1枚の絵で、Direct3D 11、Vulkan（Steam Deck）、Direct3D 12（再起動の流れ）を試す。
4. 最初の絵は、協力してくれる人から。
