# リリース手順

[English](RELEASING.md)

この Mod の配布手順です。プラグイン本体をビルドするにはゲームの参照アセンブリ
（`libs/`）が必要で、これは著作権のためリポジトリにコミットしていません。したがって
**CI ではビルドできず、リリースはゲームを導入済みの環境でローカルに作成**して
GitHub Releases へアップロードします。

翻訳の追加だけであればビルド不要です。翻訳者は [CONTRIBUTING.ja.md](../CONTRIBUTING.ja.md)
を参照してください。

## 前提

- Drag'n Wash（Steam版）がインストール済み
- `src/DragNWashLocalization/libs/` にゲーム由来の参照アセンブリが揃っている
  （`.csproj` のコメントに一覧あり）
- .NET SDK と PowerShell 7（`pwsh`）
- GitHub Releases をコマンドラインで作る場合は [`gh`](https://cli.github.com/)

## 手順

### 1. バージョンを更新する

`src/DragNWashLocalization/Plugin.cs` の `PluginVersion` を更新します。 `.csproj` の `<Version>` / `<FileVersion>` も同じ値にします（インストーラーがファイルバージョンを表示します）。
BepInEx のプラグインID文字列に使われるため、形式は `x.y.z`（例: `0.2.0`）です。

必要なら `docs/PLAN.md` と `README.md` のステータスも更新します。

### 2. ビルドして zip を作る

バージョン更新のコミットとタグ付けを**ビルドより先に**行います。DLL の内部バージョン（ファイルのプロパティで `0.1.1+<ハッシュ>` と見える値）にはビルド時のコミットハッシュが埋め込まれるため、先にビルドすると1つ前のコミットが刻まれます。ハッシュを確実に更新するためクリーンビルドにします。

```powershell
git commit -am "Release 0.2.0"
git tag -a v0.2.0 -m "v0.2.0"
Remove-Item -Recurse -Force src/DragNWashLocalization/obj, src/DragNWashLocalization/bin
pwsh tools/pack.ps1
```

`release/DragNWashLocalization-<version>.zip` が生成されます。中身は次のとおりです。

```
BepInEx/plugins/DragNWashLocalization/DragNWashLocalization.dll
BepInEx/plugins/DragNWashLocalization/Translations/<locale>/strings.csv
BepInEx/plugins/DragNWashLocalization/Translations/ignore.txt
BepInEx/plugins/DragNWashLocalization/Translations/<locale>/name.txt
BepInEx/plugins/DragNWashLocalization/FlagCatalog.csv
BepInEx/plugins/DragNWashLocalization/dragnwash-menufont.bundle
BepInEx/plugins/DragNWashLocalization/dragnwash-menufont-LICENSE.txt
BepInEx/plugins/DragNWashLocalization/data/script_order.csv
BepInEx/plugins/DragNWashLocalization/data/level_flow.csv
Install.exe
Install.cmd
install-steamdeck.sh
installer/Installer.ps1
README.md
README.ja.md
```

`Install.exe` は `pack.ps1` が .NET Framework 4 付属の C# コンパイラ（`%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe`）で生成する、コンソールを持たない小さな起動用プログラムです。追加のインストールは不要です。利用者はこれをダブルクリックしてインストール・更新・アンインストールを行います。従来どおり `BepInEx/` を手動でゲームフォルダに重ねる方法も使えます。`Install.cmd` は、SmartScreen やポリシーで署名なしの `Install.exe` が止められる環境向けに、同じインストーラー画面を開きます。`install-steamdeck.sh` は Steam Deck / Linux 用のインストーラーで改行コードは LF、`Install.cmd` は CRLF のままにする必要があります（どちらも `.gitattributes` で固定）。`installer/experimental/` の実験的な macOS 用スクリプトは zip に入れません。

### 3. 検証する

- Release ビルドの警告・エラーが0であること。
- zip を展開して、DLL と `Translations/` が正しい位置にあること。
- 可能ならクリーンな BepInEx 導入で一度起動し、F1 メニュー・言語切り替え・
  **Options →「言語（Mod）」**（選択・Save・Back）が動くことを確認します。
- インストーラーを変更した場合は、Windows で `Install.exe`（インストールとアンインストール）、
  Steam Deck で `install-steamdeck.sh` を実行して確認します。

### 4. GitHub Release を作る

```powershell
gh release create v0.2.0 release/DragNWashLocalization-0.2.0.zip `
  --title "v0.2.0" `
  --notes "変更点をここに記載"
```

タグ名は `v` 付き（`v0.2.0`）で統一します。Web UI からでも構いません
（Releases → Draft a new release → タグ作成 → zip をアップロード）。

リリースノートには、`Install.exe` と `install-steamdeck.sh` は BepInEx を自動で導入すること、
手動で導入する場合は BepInEx 5 が別途必要なこと、対応環境を書いてください
（[README](../README.ja.md) 参照）。

## 公開済みのリリースを作り直す

公開済み（push 済み）のタグは付け替えません。公開済みのリリースを作り直す場合（zip にファイルを追加するなど）は、先に GitHub のリリースとタグを削除してから、新しいコミットにタグを付け、上の手順どおりにビルドしてリリースを作り直します。リリースを削除するとダウンロード数はリセットされます。元がプレリリースだった場合は、作り直すリリースを最新版（Latest）にするかどうかを公開前に決めてください。

## なぜ CI で自動ビルドしないのか

ビルドに必要なゲームの DLL（`UnityEngine.CoreModule.dll` や `YarnSpinner.dll` など）を
リポジトリに含めることができないため、GitHub Actions 上でコンパイルできません。
そのためビルドはローカルで行い、成果物（zip）だけをリリースへ添付します。
