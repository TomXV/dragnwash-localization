# 検査とハッシュ化をコンテナーで行う

[English](DOCKER.md)

翻訳にプログラミングは要りませんが、これまでPowerShellは要りました。
コミット前の手順 `tools/hash-strings.ps1` が、手元のファイルを公開用のハッシュ化された形に書き直すからです。
翻訳を守る検査のほうはPython 3.12で動きます。[`docker/Dockerfile`](../docker/Dockerfile) の
イメージにはその両方が入っているので、WindowsでもmacOSでもLinuxでも、同じ1コマンドで済みます。

```bash
docker compose run --rm hash ja      # コミット前のハッシュ化
docker compose run --rm checks       # CI が回す検査すべて
docker compose run --rm shell        # 同じイメージのシェル
```

最初の1回だけイメージをダウンロードします（1〜2分）。
作業ツリーは `/work` にマウントしているので、ハッシュ化が書き直すのは手元のそのファイルです。
結果はいつもどおり `git diff` で確かめてください。

GitHub Actionsも同じイメージでジョブを回すので、手元で通った検査は向こうでも通ります。

## できること・できないこと

|              |                                                                                                                                                                                                         |
|--------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **できる**   | `hash-strings.ps1`（`hash` から）、`check-translations.py`、`check-game-files.py`、`linekeys.py --check`、`check-commits.py`                                                                            |
| **できない** | ゲームの実行、ゲーム内の書き出し（F6/F7 と作業用コピーは遊んで作るものです）、リリース zip（Build ワークフローが作ります）、プラグインのビルド（Git が無視する `libs/` にゲームのアセンブリが要ります） |

ゲーム由来のものはイメージに入りません。
手元の環境も変わりません。
イメージを消せば（`docker image rm dragnwash-localization-ci:local`）跡形もなくなります。

## 言語をハッシュ化する

```bash
docker compose run --rm hash          # 全部の言語（作業用コピーも拾います）
docker compose run --rm hash ja       # その言語の公開用ファイル
docker compose run --rm hash ja ko    # 複数まとめて
```

言語を指定しないときは `pwsh tools/hash-strings.ps1` と同じで、`Translations/` の全言語が対象になり、
作業用コピー（`Translations/_discovered/<locale>.working.csv`）があればそれを入力にします。
言語を指定したときは、その公開用ファイル自体を変換します。

ゲーム内の **Hash for commit** ボタンと同じ変換なので、やりやすいほうを使ってください。

## GitHub Actions では

イメージは [ci-image.yml](../.github/workflows/ci-image.yml) が `ghcr.io/tomxv/dragnwash-localization-ci` に作って置きます
（`docker/` に変更があったとき）。
[check-translations.yml](../.github/workflows/check-translations.yml) と
[commit-checker.yml](../.github/workflows/commit-checker.yml) のジョブは、その中で検査を回します。
[docker/ci-image.sh](../docker/ci-image.sh) は、イメージを取得できないときにDockerfileから作り直します。
フォークからのプルリクエストはこの道を通るので、翻訳者のプルリクエストも、権限なしで、メンテナーのものと同じように検査されます。

**Build** だけは外に置いています。
非公開の参照アセンブリを使ってWindowsでリリースを作るワークフローだからです。

ジョブの名前（`check`、`commits`）は変えていません。ブランチのルールセットがその名前を要求しているからです。

## うまくいかないとき

- **`docker compose` がデーモンに繋がらない**:
  Docker Desktopが起動していないか、Linuxエンジンが動いていません。
- **手元で通るのに Actions で落ちる**:
  pullしたあと `docker compose build` をすれば、最新のDockerfileから作り直せます。
- **ハッシュ化で思ったより差分が出た**:
  それが公開用の形です。
  行はゲームの進行順に並び、`#` の見出しが付き、キーは英文のハッシュになります。
  [CONTRIBUTING.ja.md](../CONTRIBUTING.ja.md) に説明があり、何が動いたかは `git diff` で分かります。
