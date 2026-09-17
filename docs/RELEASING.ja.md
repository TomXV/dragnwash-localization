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
- [Drag'n Wash ModFramework](https://github.com/TomXV/dragnwash-modframework) をこのリポジトリの隣にチェックアウトしてある（または `pack.ps1` に `-FrameworkPath` を渡す）。フレームワーク側の `libs/` はその `tools/copy-libs.ps1` でコピーしておく。`pack.ps1` がフレームワークもビルドし、中核・ライブラリ・プリローダーパッチャーを zip に入れます
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
BepInEx/patchers/DragNWash.ModFramework.Preloader.dll
BepInEx/plugins/DragNWash.ModFramework/DragNWash.ModFramework.dll
BepInEx/plugins/DragNWash.ModFramework/LICENSE.txt
BepInEx/plugins/DragNWash.ModFramework/icon.png
BepInEx/plugins/DragNWash.ModFramework/ModsButton0.png
BepInEx/plugins/DragNWash.ModFramework/ModsButton1.png
BepInEx/plugins/DragNWash.ModFramework.Text/DragNWash.ModFramework.Text.dll
BepInEx/plugins/DragNWash.ModFramework.Dialogue/DragNWash.ModFramework.Dialogue.dll
BepInEx/plugins/DragNWash.ModFramework.ToolWindow/DragNWash.ModFramework.ToolWindow.dll
BepInEx/plugins/DragNWash.ModFramework.Assets/DragNWash.ModFramework.Assets.dll
BepInEx/plugins/DragNWash.ModFramework.Saves/DragNWash.ModFramework.Saves.dll
BepInEx/plugins/DragNWashLocalization/DragNWashLocalization.dll
BepInEx/plugins/DragNWashLocalization/icon.png
BepInEx/plugins/DragNWashLocalization/Translations/<locale>/strings.csv
BepInEx/plugins/DragNWashLocalization/Translations/ignore.txt
BepInEx/plugins/DragNWashLocalization/Translations/<locale>/name.txt
BepInEx/plugins/DragNWashLocalization/FlagCatalog.csv
BepInEx/plugins/DragNWashLocalization/dragnwash-menufont.bundle
BepInEx/plugins/DragNWashLocalization/dragnwash-menufont-LICENSE.txt
BepInEx/plugins/DragNWashLocalization/data/script_order.csv
BepInEx/plugins/DragNWashLocalization/data/level_flow.csv
Install.exe
install-steamdeck.sh
mod-install.json
README.md
README.ja.md
```

うち 4 つは条件付きです。`pack.ps1` は、フレームワークのチェックアウト直下に `LICENSE` があるときだけ `BepInEx/plugins/DragNWash.ModFramework/LICENSE.txt` を、そのチェックアウトの `src/DragNWash.ModFramework/` にあるときだけ `icon.png`・`ModsButton0.png`・`ModsButton1.png` をコピーします。他のファイルは常に書き出されます。

`Install.exe` と `install-steamdeck.sh` は Drag'n Wash ModFramework の共通インストーラーで、`pack.ps1` がフレームワークのチェックアウトからビルド・コピーします（説明はフレームワークの [docs/INSTALLER.ja.md](https://github.com/TomXV/dragnwash-modframework/blob/main/docs/INSTALLER.ja.md)）。`pack.ps1` は `mod-install.json` も書き出します。この Mod のフォルダー、残すプレイヤーのデータ、設定ファイル、同梱するすべての言語パックを選べる言語の質問が入ります。`Install.exe` のビルドは決定的で、ウイルス対策ソフトの評価がリリースのたびにリセットされません。`pack.ps1` が SHA-256 を表示するので、フレームワークの `installer/` が変わっていなければ前のリリースと同じになっているか確認してください。利用者はこれをダブルクリックしてインストール・更新・アンインストールを行います。従来どおり `BepInEx/` を手動でゲームフォルダに重ねる方法も使えます。`installer/experimental/` の実験的な macOS 用スクリプトは zip に入れません。

### 2b. GitHub にビルドさせる

Actions の **Build** ワークフローが、手順 2 を Windows の runner で行います。`main` への push、`v*` のタグ、手動実行（同梱するフレームワークのブランチかタグを指定）のときに動きます。Drag'n Wash ModFramework をチェックアウトし、参照アセンブリを非公開リポジトリ `TomXV/dragnwash-libs` から `LIBS_TOKEN` シークレットで取り、`tools/pack.ps1 -FrameworkPath` を回して、zip を成果物として残します。タグのときは zip を添えた **下書き** のリリースも作るので、流れは「バージョンを上げる → コミット → タグを push → ワークフローを待つ → 下書きにノートを書いて公開」です。PR では動きません。ゲームが更新されたら、`tools/copy-libs.ps1` で非公開リポジトリを更新してください。手元の `pack.ps1` は、そのまま予備として使えます。

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

> 2026-09-16 からはできます。[2b](#2b-github-にビルドさせる) を参照。参照アセンブリは Build ワークフローだけが読む非公開リポジトリにあり、この公開リポジトリには引き続き含まれません。

ビルドに必要なゲームの DLL（`UnityEngine.CoreModule.dll` や `YarnSpinner.dll` など）を
リポジトリに含めることができないため、GitHub Actions 上でコンパイルできません。
そのためビルドはローカルで行い、成果物（zip）だけをリリースへ添付します。

- ゲームがアップデートされたら、`tools/game-fingerprints.py` で新しいビルドのファイルを `ci/game-fingerprints.json` に追加し（Windows と Steam Deck の両方）、Drag'n Wash ModFramework にもコピーしてください。CI はリポジトリのすべてのファイルをこれと照合し、ゲームのファイルの混入を拒否します。
- これもゲームのアップデート後（実験的）: ゲーム内で F6 と F7 を押して `_discovered/` を更新し、`python tools/rekey.py replay --discovered <そのフォルダー>` でアップデートが変えた台詞を確かめます。そのあと新しい `script_order.csv` を `data/` へコピーし、`python tools/rekey.py augment --discovered <そのフォルダー>` を実行してリゾルバーのキーを持たせます。`tools/linekeys.py` はフレームワーク側の同名ファイルと常に同一に保ちます。CI が `ci/linekey-vectors.json` と照合します。
