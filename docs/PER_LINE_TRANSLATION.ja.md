# 台詞 ID ごとの訳

作成日: 2026-09-14。
調査の記録と、実装した仕組みの説明。
翻訳者向けの使い方は [CONTRIBUTING.ja.md](../CONTRIBUTING.ja.md#複数のキャラが話す台詞) を参照。

## 背景

翻訳は「画面に表示される英文のハッシュ」で引くため、同じ英文は全場面で同じ訳になる。
会話1839行のうち、同じ英文を別のキャラが話す例が29種類（142回）ある。例:

| 台詞 ID         | 場面                           | 話者           | 英文       |
|-----------------|--------------------------------|----------------|------------|
| `line:6046bedf` | `Ryan_1_intro` 9               | ライアン       | Wonderful! |
| `line:ab423ac7` | `Alexander_5_required` 19      | アレクサンダー | Wonderful! |
| `line:36d49875` | `Alexander_5_romance_route` 21 | アレクサンダー | Wonderful! |

短い相づちほどキャラの口調が出るので、韓国語のように敬語の区別が強い言語では1つの訳で両立させにくい。

## 実機で確認したこと（Windows、v0.5.0 相当のビルド）

- 会話は `GDG Dialogue System` の1つの `DialogueRunner` が扱う。
  行の取得役はYarn Spinner標準の`BuiltinLocalisedLineProvider`、
  表示は標準の `LinePresenter`（1文字ずつのタイプライター、キャラ名は別欄）と`OptionsPresenter`。
  ゲーム独自の表示クラスはない。
- `LinePresenter.RunLineAsync` と `OptionItem.Option` のsetterにPrefixを入れ、
  **「このTMP部品は今この台詞IDを表示する」**と覚えておき、既存のTMPフック（`TmpTextHook.Rewrite`）が、
  入ってきた文字列がその台詞の文字列と一致するときだけ台詞ID専用の訳を使うという方式（案B）で次を確認した。
  - ライアンの `line:6046bedf` は専用の訳、アレクサンダーの `line:ab423ac7` は共有の訳が吹き出しに出る
  - 表示中に言語を切り替えると（`RefreshAll`）、専用の訳も新しい言語のものに切り替わる
  - 選択肢（`OptionItem`）でも専用の訳が出る
- Yarnの会話（ノード）を実行せずに `RunLineAsync` / `RunOptionsAsync` を直接呼んで確認したので、
  フラグやセーブには触れていない。
- タイプライターは1行につき3回 `text` を設定する（覚えた文字列との一致判定で問題なく処理される）。
- 会話データ: `{0}` の置き換えを使う行は0、書式タグを含む行は77、選択肢は578、台詞と選択肢の両方で使われる英文は15種類。

案A（`Localization.GetLocalizedString` の戻り値を差し替える）も動くが、訳をYarnの書式（`Name:` や書式タグ）
どおりに書く必要があり、TMPフックが訳済みの文字列を未翻訳として記録してしまうので採らない。

## 実装

### 翻訳ファイル

`strings.csv` の `key` 列に、16桁のハッシュの代わりに **台詞 ID（`line:` + 16 進 8 桁）** を書ける行を追加する。
ほかの列は同じ。台詞IDの行があればそれが優先され、なければ今までどおりハッシュの行が使われる。
既存のパックはそのまま動く。

```csv
key,section,node,order,speaker,translation
84f325bca745e504,L01 Ryan,Ryan_1_intro,9,Ryan/Alexander,素晴らしい！
line:6046bedf,L01 Ryan,Ryan_1_intro,9,Ryan,すごいね！
```

- `speaker` 列は、共有されている行では話者を全員 `Ryan/Alexander` のように並べる（今は1人だけで、
  `hash-strings.ps1` が入力ファイルの値を優先するため、一度書かれた話者が固定されている）。
- 作業コピー（`Export working copy`）は、共有行の各出現位置に `line:` の行を空の訳で並べる。
  訳を入れた行だけが `Hash for commit` で公開ファイルに残る。
- `speaker` 列は `data/script_order.csv` から全員分を付ける（パックに書かれた値より優先）。

### 変更した場所

| 場所                                        | 変更                                                                                                         |
|---------------------------------------------|--------------------------------------------------------------------------------------------------------------|
| `ScriptOrder.Generate`                      | 今はキーごとに初出だけを出している（`emittedKeys`）。全出現（台詞 ID ごと）を `data/script_order.csv` に出す |
| `TranslationStore`                          | `line:` の行を台詞 ID 辞書に読む。`ResolveKey` / `TranslationKey.LooksLikeKey` が台詞 ID を受け付ける        |
| `LineIdContext`（新規）・`TmpTextHook`      | RunLineAsync / OptionItem.Option の Prefix で台詞 ID を覚え、`Rewrite` と `RefreshAll` で台詞 ID の行を優先  |
| `WorkingCopy`                               | 共有行の各出現に `line:` 行を出す。話者を全員分にする                                                        |
| `hash-strings.ps1`                          | `line:` の行を落とさない。話者は `script_order.csv` を正とする                                               |
| `tools/check-translations.py` / CI コメント | `line:` の key を許可し、説明文を追加                                                                        |
| `DialogueDumper`（F6）                      | すでに `line_id` 列があるので変更なし                                                                        |
| ハッシュ化（両ツール）                      | パック先頭のコメントを残す。空の訳は出さない                                                                 |
| ドキュメント                                | CONTRIBUTING / スタイルガイドに「共有行と台詞 ID 行」の説明                                                  |

### 注意点

- 台詞IDはゲームのアップデートで変わりうる（Yarnは本文のハッシュからIDを作る）。
  変わった行は台詞IDの行が効かなくなるだけで、ハッシュの行に戻る。
- UI文字列には台詞IDがないので対象外。

## 実装後の確認（Windows）

- `Export game flow` で `data/script_order.csv` を作り直し（1839行、共有行29種類142か所）、
  作業コピーの `line:6046bedf` に訳を入れると、ホットリロードでライアンの台詞だけがその訳になり、
  アレクサンダーの同じ英文は共有の訳のままだった。
- ゲーム内の *Hash for commit* と `tools/hash-strings.ps1` は、未訳の行を除いて同じ出力になり、`line:` の行は台本の位置に書き出された。
- 13言語のパックを作り直した差分は、話者欄（共有行27行）と `order` の番号だけで、訳文と先頭コメントは変わっていない。
