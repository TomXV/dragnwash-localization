# Drag'n Wash Localization

[English](README.md) | [日本語](README.ja.md)

> [!NOTE]
> 이 문서는 영어판 [README.md](README.md)의 번역입니다. 내용이 다를 때는 영어판이 최신입니다.

[Drag'n Wash](https://store.steampowered.com/app/4739660/)용 비공식 BepInEx 기반 다국어 로컬라이제이션 모드입니다.

이 모드는 게임을 여러 언어로 플레이할 수 있게 해 주며([Language packs](#language-packs) 참고), 누구나 코드를 작성하지 않고 CSV 파일 편집만으로 언어를 추가하거나 개선할 수 있습니다.

기술 조사 및 구현 계획은 [docs/PLAN.md](docs/PLAN.md)를 참고하세요.

> [!WARNING]
> ## ⚠️ 스포일러 경고 ⚠️
> **`Translations/` 아래의 CSV 파일에는 게임의 모든 대사가 스토리 순서대로 들어 있습니다.**
> 파일을 열면 스포일러를 보게 됩니다. 먼저 게임을 몇 회차 플레이한 뒤 보세요!

## 설치

### 빠른 설치 (권장)

설치는 아주 쉽습니다.

1. [Releases 페이지](https://github.com/TomXV/dragnwash-localization/releases)에서 zip을 내려받아 원하는 위치에 압축을 풉니다.
2. **`Install.exe`**를 더블클릭합니다.
3. 언어를 고르고 **Install**을 클릭합니다(이미 설치되어 있으면 **Update**).

> [!TIP]
> 동일한 절차가 Steam 가이드에도 있습니다: [English](https://steamcommunity.com/sharedfiles/filedetails/?id=3801420947) / [日本語](https://steamcommunity.com/sharedfiles/filedetails/?id=3801418794). Drag'n Wash는 Steam Workshop을 지원하지 않으므로, 모드 자체는 GitHub Releases에서 내려받습니다.

## 봤죠? 쉽습니다. ( ･´ｰ･｀) HEH! YIP!

설치 프로그램은 Steam에서 게임 경로를 자동으로 찾습니다(또는 폴더를 직접 선택 가능). BepInEx가 아직 설치되지 않았다면 공식 5.4.23.5 릴리스를 내려받아 SHA-256을 검증한 뒤 자동으로 압축 해제합니다. 이후 Steam에서 게임을 실행하면 됩니다.

언어: 日本語 / 简体中文 / English(번역 없음), 원어민이 교정한 한국어, 그리고 번체 중국어, 독일어, 프랑스어, 스페인어, 브라질 포르투갈어, 러시아어, 폴란드어, 히브리어, 우크라이나어, 태국어, 베트남어의 임시 팩, 그리고 재미용으로 에스페란토와 토키 포나([Language packs](#language-packs) 참고). 같은 창에 **Uninstall** 버튼도 있습니다. 기본값으로 세이브 히스토리 스냅샷은 유지되며, BepInEx는 사용자가 요청하고 다른 모드가 사용하지 않을 때에만 모드와 함께 제거됩니다. 게임 안에서도 제거할 수 있습니다: **Options → Mods → Drag'n Wash Localization → Uninstall**을 누르면 다음에 게임을 시작할 때 모드가 제거됩니다.

직접 수동으로 설치하고 싶다면 아래 수동 절차를 따르세요.


> [!NOTE]
> **`Install.exe` 실행 시 아무 반응이 없거나 Windows에서 "Windows에서 PC를 보호했습니다"라고 나올 때**
> `Install.exe`는 서명되지 않은 작은 프로그램이므로, Windows SmartScreen이 처음 실행 시 차단할 수 있습니다.
> - 경고가 뜨면 **추가 정보 → 실행**을 클릭하세요.
> - 창이 아예 뜨지 않으면 `Install.exe` 우클릭 → 속성 → **차단 해제** 체크 → 확인 후 다시 더블클릭하세요.

> [!WARNING]
> **Windows 보안(Microsoft Defender)에서 `Install.exe`를 "Trojan:Script/Wacatac.B!ml"로 탐지하거나, 압축 해제한 폴더에서 `Install.exe`가 사라진 경우**
> 이것은 오탐지(false positive)입니다. `!ml` 접미사는 머신러닝 모델이 파일을 수상하다고 추정했다는 뜻이지, 알려진 악성코드와 일치했다는 뜻이 아닙니다. v1.0.0까지의 설치 프로그램은 콘솔 창 없이 PowerShell 스크립트를 실행했고, 이런 방식이 악성코드 동작과 비슷해 보였습니다. v1.1.0부터 `Install.exe`는 Drag'n Wash ModFramework의 공용 설치 프로그램으로, 스크립트를 실행하지 않는 평범한 무서명 프로그램이지만, 새로 나온 무서명 파일은 여전히 탐지될 수 있습니다. 소스는 공개되어 있습니다: 프레임워크 저장소의 [`installer/`](https://github.com/TomXV/dragnwash-modframework/tree/main/installer).
>
> 파일이 자동 격리되면 위협 이름이 표시되지 않는 경우가 있고, 압축 해제 폴더에서 `Install.exe`가 그냥 없는 것처럼 보일 수 있습니다. 무엇이 제거됐는지는 Windows 보안 → **보호 기록**에서 확인하세요.
> - 먼저 내려받은 zip이 진짜인지 확인하세요. PowerShell에서 `(Get-FileHash "<zip 경로>").Hash -eq ("<Releases의 sha256>" -replace '^sha256:')`를 실행하세요. 결과가 `True`면 여기서 배포한 파일과 동일합니다. `False`면 파일을 삭제하고 사용하지 마세요.
> - `sha256`은 [Releases](https://github.com/TomXV/dragnwash-localization/releases) 페이지의 `DragNWashLocalization-<version>.zip` 항목 아래에 표시됩니다. 자동 생성되는 "Source code" 2개 항목에는 해시가 없으므로 사용하지 마세요.
> - 해시가 일치하면 여기서 배포한 파일이라는 사실은 확인됩니다. 그 자체로 안전성을 완전히 증명하는 것은 아니며, 이를 확인하려면 위에 링크한 소스를 참고하세요.
> - 일치한다면 Windows 보안 → **보호 기록**에서 해당 탐지 항목을 열고 **동작 → 디바이스에서 허용**을 선택하세요. 그 파일 하나만 허용하면 됩니다. 폴더 제외를 추가하거나 Windows 보안을 끌 필요는 없습니다.
> - 어떤 것도 허용하고 싶지 않다면 아래 수동 설치 절차를 사용하세요.
> - 이 저장소 Releases 페이지 이외의 출처에서 받은 복사본은 사용하지 마세요.

### Windows on ARM (검증됨)

Snapdragon X 노트북 같은 ARM Windows PC에서도 동일하게 `Install.exe` 절차로 설치하면 동작합니다(게임 자체는 에뮬레이션으로 x64 실행).

> [!IMPORTANT]
> **해당 환경에서는 게임이 DirectX 12에서 정상 렌더링되지 않으므로, Steam 실행 옵션에 `-force-d3d11`을 추가하세요.** 모드 유무와 무관하게 발생하는 게임 측 문제입니다.
> - 구형 GPU 드라이버에서는 스플래시 화면 직후 게임이 크래시합니다.
> - 최신 드라이버에서는 크래시는 줄었지만 3D가 렌더링되지 않습니다.
>
> Steam에서 게임 우클릭 → **속성** → **실행 옵션**에 `-force-d3d11`을 입력하면 DirectX 11로 실행되어 정상 플레이됩니다.
> ASUS ProArt PZ13 (Snapdragon X Plus / Adreno X1-45)에서 검증했습니다.

### Steam Deck / Linux (검증됨)

> [!IMPORTANT]
> Steam Deck 지원은 **v0.3.0 이상**이 필요합니다. 이전 버전도 Deck에서 실행되지만 F1 메뉴 조작이 불가능합니다.

게임의 네이티브 Linux 빌드 + BepInEx Linux 빌드 조합에서 동작합니다. `Install.exe`는 Windows용이므로 Deck에서는 설치 스크립트를 사용하세요.

**설치 스크립트(권장)**, Desktop Mode에서:

1. [Releases 페이지](https://github.com/TomXV/dragnwash-localization/releases)에서 zip을 내려받아 압축 해제합니다(우클릭 → Extract).
2. 압축 해제 폴더를 열고 빈 공간 우클릭 후 **Open Terminal Here**를 선택합니다.
3. 아래 명령을 입력하고 Enter를 누릅니다:

   ```bash
   bash install-steamdeck.sh
   ```

4. **Install / Update**를 고른 뒤 언어를 선택합니다. 실행 옵션 설정을 위해 Steam이 잠시 종료되어야 하며, 스크립트가 먼저 확인 후 Steam을 다시 실행합니다.
5. Gaming Mode로 돌아가 게임을 실행합니다. 언어는 이후 **Options → Language (Mod)**에서 변경할 수 있습니다.

스크립트는 Steam 라이브러리(SD 카드 포함)에서 게임을 찾아 공식 Linux BepInEx 5.4.23.5를 내려받고 SHA-256을 검증합니다. 이어서 `run_bepinex.sh`의 `executable_name="DragNWash"`를 설정하고, 모드를 복사하며, 기존 옵션을 보존한 채 게임 실행 옵션에 `./run_bepinex.sh %command%`를 추가합니다. 업데이트/삭제도 같은 명령을 다시 실행해 **Install / Update** 또는 **Uninstall**을 선택하면 됩니다. 제거 시 세이브 히스토리는 유지되며, 다른 BepInEx 모드가 필요로 하지 않을 경우 실행 옵션에서 `./run_bepinex.sh`를 제거하고 BepInEx 제거도 제안합니다. `--install`과 `--uninstall`은 확인 질문을 생략합니다.

Steam은 실행 중에 실행 옵션을 다시 쓰기 때문에, 실행 옵션 변경이 필요할 때 스크립트가 Steam을 종료하고 수정 후 재시작합니다(기본은 확인 질문, `--close-steam`은 질문 생략). 어떤 단계가 수행되지 못하면 마지막 대화상자에서 그 사실과 수동으로 바꿔야 할 항목을 알려줍니다. 각 실행 로그는 `~/.local/state/dragnwash-installer/installer.log`에 남습니다.

<details>
<summary>Deck 수동 설치</summary>

1. [BepInEx_linux_x64_5.4.23.5.zip](https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.5/BepInEx_linux_x64_5.4.23.5.zip)을 게임 폴더(`~/.local/share/Steam/steamapps/common/Drag'n Wash/`)에 압축 해제합니다.
2. 이 모드의 `BepInEx/` 폴더를 같은 위치에 병합합니다.
3. `run_bepinex.sh`를 열어 `executable_name="DragNWash"`로 설정하고 저장한 뒤 `chmod +x run_bepinex.sh`를 실행합니다.
4. Steam 게임 속성 → 실행 옵션에 `./run_bepinex.sh %command%`를 설정합니다.
5. 게임을 실행하고 **Options → Language (Mod)**에서 언어를 변경합니다.

</details>

폰트는 추가 설정이 필요 없습니다. 게임 텍스트는 SteamOS의 Noto Sans CJK(일본어/중국어/한국어)를 폰트 파일에서 직접 사용하고, F1 메뉴는 번들된 Noto Sans JP(`dragnwash-menufont.bundle`)로 렌더링합니다. 이유는 Steam의 Linux 런타임이 Unity 메뉴 시스템에 CJK 폰트를 제공하지 않기 때문입니다. 이 메뉴 폰트에는 한글/히브리어 글리프가 없어서 Deck에서는 해당 언어의 F1 언어 버튼에 로케일 코드가 표시됩니다. 게임 본문은 정상 표시됩니다.

Deck에서 언어 변경은 컨트롤러로 **Options → Language (Mod)**를 사용하면 되며 F1 키가 필요 없습니다. F1 메뉴의 번역 도구를 사용하려면(Gaming Mode):

- Steam Input으로 버튼 하나에 **F1**을 바인딩해 메뉴를 엽니다.
- 오른쪽 트랙패드나 터치스크린으로 포인터를 이동합니다. **A**, **R2**, 또는 트랙패드 클릭으로 포인터 아래 버튼을 누를 수 있습니다(Steam Input은 트랙패드 클릭을 마우스 클릭이 아닌 스틱 입력으로 보내므로, 모드에서 이를 처리합니다).
- 창 이동은 제목 표시줄 위에서 해당 버튼을 누른 채 드래그, 크기 조절은 우하단 모서리에서 같은 방식으로 드래그합니다.
- 스틱과 D-pad는 포인터가 올라가 있는 목록을 스크롤합니다.

> [!WARNING]
> **현재 macOS는 동작하지 않습니다.** Drag'n Wash는 Unity 6.3으로 빌드되었고, macOS에서 BepInEx 5.4.23.5가 사용하는 Doorstop 로더는 아직 Unity 6.3 게임에 훅을 걸지 못합니다([NeighTools/UnityDoorstop#108](https://github.com/NeighTools/UnityDoorstop/issues/108)). Doorstop은 게임에 로드되지만 BepInEx는 시작되지 않습니다: `BepInEx/LogOutput.log`와 `BepInEx/config`가 생성되지 않고 게임은 영어로 실행됩니다. Apple M3 Pro + macOS 26.6 환경에서 네이티브와 Rosetta 모두 확인했습니다. 이는 BepInEx 측 이슈이므로 이 모드에서 우회할 수 없습니다. BepInEx 릴리스에 수정이 포함되면 macOS를 다시 테스트할 예정입니다.
>
> 그 시점을 대비해 **실험적(experimental)** macOS 설치 스크립트가 저장소에 포함되어 있습니다: [`installer/experimental/install-macos.sh`](installer/experimental/install-macos.sh). Steam Deck 스크립트와 동일하게(BepInEx for macOS, 모드, 언어, Steam 실행 옵션 설정) 동작하며, 게임 실행 후 모드 로드 여부를 확인하는 **Check** 모드도 포함합니다. 아직 실제 Mac에서 실행되진 않았고, 설치 전 위 이슈를 경고하며, 릴리스 zip에는 포함되어 있지 않습니다.

### 수동 설치

### 준비물

- Drag'n Wash의 Windows Steam 버전
- [BepInEx 5 for 64-bit Windows (Mono)](https://github.com/BepInEx/BepInEx/releases)
- 이 저장소 [Releases 페이지](https://github.com/TomXV/dragnwash-localization/releases)의 최신 `DragNWashLocalization-<version>.zip`

> [!IMPORTANT]
> 릴리스 자산에서 `DragNWashLocalization-<version>.zip` 파일을 내려받으세요. GitHub가 자동 생성하는 **Source code** 아카이브는 설치 가능한 모드 패키지가 아닙니다. Releases 페이지에 모드 ZIP이 없다면, 설치 가능한 빌드가 아직 배포되지 않은 것입니다.

### 1. 게임 폴더 열기

Steam에서 **Drag'n Wash**를 우클릭하고 **관리 → 로컬 파일 보기**를 선택하세요. 게임 `.exe`가 있는 루트 폴더가 열립니다.

### 2. BepInEx 설치

**Windows x64 (Mono)**용 BepInEx 5 압축 파일을 내려받아 게임 루트에 직접 압축 해제합니다.

압축 해제 후 `winhttp.dll`, `doorstop_config.ini`, `BepInEx` 폴더가 게임 실행 파일 옆에 있어야 합니다. 다른 중첩 폴더 안에 들어갔다면 게임 루트로 옮기세요.

게임을 한 번 실행해 타이틀 화면까지 진입한 후 종료합니다. BepInEx가 설정과 로그 파일을 생성합니다. 계속하기 전에 `BepInEx/LogOutput.log`가 생성되었는지 확인하세요.

### 3. Drag'n Wash Localization 설치

[Releases](https://github.com/TomXV/dragnwash-localization/releases)에서 `DragNWashLocalization-<version>.zip`을 내려받아 **같은 게임 루트**에 압축 해제합니다. 압축 도구에서 포함된 `BepInEx` 폴더 병합을 허용하세요.

플러그인 DLL 경로는 다음과 같아야 합니다:

```text
<Drag'n Wash folder>/BepInEx/plugins/DragNWashLocalization/DragNWashLocalization.dll
```

zip에는 모드가 의존하는 Drag'n Wash ModFramework도 함께 포함됩니다: 플러그인별 폴더 `BepInEx/plugins/DragNWash.ModFramework`, `DragNWash.ModFramework.Text`, `.Dialogue`, `.ToolWindow`, `.Assets`, `.Saves`, 그리고 `BepInEx/patchers/DragNWash.ModFramework.Preloader.dll`입니다. 모두 유지하세요. 다른 모드가 이미 더 최신 ModFramework를 설치했다면 최신 파일을 유지하세요.

ZIP 파일 자체나 `DragNWashLocalization-<version>` 같은 추가 폴더가 `plugins`와 DLL 사이에 끼어 있지 않도록 하세요.

### 4. 실행 및 확인

Drag'n Wash를 실행합니다. 수동 설치 시 기본 시작 언어는 일본어입니다(설치 프로그램 사용 시에는 선택한 언어로 시작).

언어를 바꾸려면 **Options**를 열고 Gameplay 섹션 맨 아래의 **Language (Mod)**를 사용하세요. 언어를 고르면 즉시 적용되며, 유지하려면 **Save**, 저장된 언어로 되돌아가려면 **Back**을 누르세요. 마우스/게임패드 모두 지원하며 Steam Deck에서도 동일합니다. **F1** 창의 **Translation** 탭에서도 언어 변경이 가능하며 선택 즉시 저장됩니다.

설치가 성공하면 `BepInEx/LogOutput.log`에 `DragNWashLocalization` 시작 항목이 기록됩니다.

기본 언어를 수동으로 바꾸려면 게임을 종료하고 아래 파일을 편집하세요:

```text
BepInEx/config/com.tomxv.dragnwash.localization.cfg
```

`[General]` 아래 `TargetLocale` 값을 `ja` 또는 `zh-Hans` 같은 설치된 로케일로 설정한 뒤 게임을 다시 실행하세요. `en`은 게임 원본 영어를 유지합니다(모드는 설치 상태지만 번역은 하지 않음). 설치 프로그램, Options → Language (Mod), F1 메뉴에서도 같은 선택을 제공합니다.

### 모드가 로드되지 않을 때

- BepInEx와 모드가 모두 게임 실행 파일이 있는 폴더에 압축 해제되었는지 확인하세요.
- 위에 표시한 정확한 DLL 경로를 확인하세요.
- `BepInEx/LogOutput.log`를 여세요. 파일이 없으면 BepInEx 자체가 로드되지 않은 것입니다. 파일이 있다면 `DragNWashLocalization`를 검색하고 주변 에러를 확인하세요.
- Options를 열 때 Direct3D 12 크래시가 난다면 [Windows에서 Options 열 때 크래시](#crash-when-opening-options-on-windows) 절의 해결책을 적용하세요.
- 과거에 게임 파일을 덮어쓰는 번역(예: `DragNWash_Data`에 파일 복사)을 설치했다면, 게임의 영어 원문이 이미 사라져 모드가 번역할 대상을 찾지 못합니다. 먼저 원본 파일을 복구하세요: Steam에서 게임 우클릭 → **속성** → **설치된 파일** → **게임 파일 무결성 확인**, 이후 모드를 다시 설치하세요. 이런 덮어쓰기 방식 번역은 게임 업데이트 시 깨지거나 작동 중단되지만, 이 모드는 게임 파일을 변경하지 않습니다.

## Language packs

번역 파일은 TomXV가 작성해 같은 zip에 포함되어 있으며, 패키지 개선에 기여한 분들은 아래 행에 크레딧이 표시됩니다([기여자 크레딧](CONTRIBUTING.md#credits-for-contributors) 참고). 설치 프로그램과 F1 메뉴는 `Translations/` 아래의 모든 폴더를 나열합니다.

| Locale | Language | Status |
| --- | --- | --- |
| `ja` | 日本語 | 제작자 감수 |
| `zh-Hans` | 简体中文 | 제작자 감수 |
| `zh-Hant` | 繁體中文 | 임시, 감수된 간체 중국어를 대만식 표현으로 변환 |
| `de` | Deutsch | 임시 |
| `fr` | Français | 임시 |
| `es` | Español | 임시 |
| `pt-BR` | Português (Brasil) | 임시 |
| `ko` | 한국어 | 원어민 교정 완료, Hotcake (게임에서 쓰이지 않는 줄은 그대로 둠) |
| `ru` | Русский | 임시 |
| `pl` | Polski | 임시 |
| `he` | עברית | 임시, 우->좌 표시 |
| `uk` | Українська | 임시 |
| `th` | ไทย | 임시 (단어 사이의 폭 없는 공백으로 줄바꿈) |
| `vi` | Tiếng Việt | 임시 |
| `eo` | Esperanto | 임시, 재미용 |
| `tok` | toki pona | 임시, 재미용 (137개 단어 언어라 의역이 많을 수 있음) |
| `en` | English | 게임 원본 텍스트 (번역 없음) |

> [!NOTE]
> **임시(Provisional)** 팩은 원어민 검수를 거치지 않았습니다. 전 구간 플레이 가능하지만 일부 문장이 어색하거나 농담이 빠질 수 있습니다. 제작자가 감수한 것은 일본어와 간체 중국어뿐입니다. 원어민이시라면 PR로 수정 기여를 부탁드립니다([CONTRIBUTING.md](CONTRIBUTING.md) 참고). 모든 `strings.csv` 상단에도 같은 안내가 들어 있습니다.

## 번역 기여자를 위한 안내

`Translations/<locale>/strings.csv`를 편집해 번역을 추가할 수 있습니다. 배포 파일의 컬럼은 `key,section,node,order,speaker,translation`입니다. `key`는 영어 원문의 해시, `section`/`node`/`order`는 게임 내 재생 위치(레벨/대화/순서), `speaker`는 화자를 나타냅니다. `#`로 시작하는 줄은 `# ===== Level 1: Ryan (Sunny) =====` 같은 섹션 헤더이므로, 파일을 위에서 아래로 읽으면 대본처럼 볼 수 있습니다. 권장 작업 흐름은 다음과 같습니다.

1. 게임에서 **F1 → Translation → Export working copy**를 실행합니다. 그러면 `Translations/_discovered/<locale>.working.csv`가 생성되며, 각 줄 옆에 영어 원문이 포함됩니다(`key,section,node,order,speaker,source_en,translation`). 줄 순서와 섹션 헤더도 동일합니다.
2. `translation` 컬럼을 편집합니다. 저장하면 실행 중인 게임에 핫리로드됩니다.
3. 커밋 전 **F1 → Translation → Hash for commit**(또는 `tools/hash-strings.ps1`)을 실행합니다. 영어 원문이 제거된 `strings.csv`가 다시 생성됩니다.

각 언어 폴더에는 언어 표시 이름(예: `日本語`) 한 줄만 담긴 `name.txt`도 있으며, 설치 프로그램과 인게임 메뉴에 표시됩니다.

게임 영어 스크립트 원문은 의도적으로 이 저장소에 포함하지 않습니다. 이렇게 해야 **게임 정식 소유자만 번역을 만들 수 있기** 때문입니다. Unity 내부 키를 알 필요도, 코드를 작성할 필요도 없습니다. 자세한 내용은 [CONTRIBUTING.md](CONTRIBUTING.md)를 참고하세요.

원문에 `<size=70%>` 같은 서식 태그가 있을 경우 태그 구조는 유지하고 내부 텍스트만 번역하세요.

### 맥락 확인용 전체 대사 내보내기

이 기능들은 개발자 도구입니다. 먼저 **Options → Mods → Drag'n Wash ModFramework → Developer tools**를 켜세요(플레이어에게는 꺼져 있으므로 아래 기능은 동작하지 않습니다). 그런 다음 세이브를 불러오고 게임에서 **F6**을 누르면 모든 대사가 다음 위치로 내보내집니다:

`BepInEx/plugins/DragNWashLocalization/Translations/_discovered/dialogue_lines.csv`

실제 게임 설치본에서 1,839줄로 검증했습니다.

줄은 게임에서 재생되는 순서대로 정렬됩니다. `node` 컬럼은 각 대화를 식별하며 `Alexander_2_intro` 같은 이름을 사용합니다(패턴: "character name_occurrence_scene"). `order` 컬럼은 해당 대화 내 줄 위치를 의미합니다. `kind` 컬럼은 캐릭터 대사(`line`)와 플레이어 선택지(`option`)를 구분합니다. 이 정보 덕분에 누가 말하는지, 답변이 무엇을 가리키는지 파악하기 쉬워집니다. 게임의 세 드래곤은 Conrad, Ryan, Alexander입니다.

번역할 줄을 `Translations/<locale>/strings.csv`로 복사해 `translation` 컬럼을 채운 뒤 게임에서 확인하세요. `node`, `key` 같은 추가 컬럼이 남아 있어도 플러그인은 파일을 올바르게 로드합니다.

**PR을 열기 전에 공개용 파일을 다시 만드세요.** 게임 안의 **Hash for commit** 버튼(또는 `tools/hash-strings.ps1`)을 사용합니다. 자동 검사는 공개용 헤더만 받습니다: *Hash for commit*이 쓰는 `key,section,node,order,speaker,translation`, 또는 더 짧은 `key,speaker,translation`과 `key,translation`. `source_en` 등 내보내기용 컬럼이 남은 파일은 거부되며, 영어 원문이 담긴 PR은 이 저장소가 가장 피하려는 것입니다. [CONTRIBUTING.md](CONTRIBUTING.md#hash-before-committing)를 참고하세요.

이미 번역된 줄은 재내보내기 시 번역이 채워진 상태로 나오므로, 다시 내보내도 기존 작업이 사라지지 않습니다.

### 게임 재시작 없이 수정 미리보기

게임 실행 중 `Translations/<current-language>/strings.csv` 또는 작업본 `_discovered/<locale>.working.csv`를 저장하면 약 2초 후 플러그인이 자동 리로드하고 현재 화면에 보이는 텍스트를 즉시 갱신합니다. 변경된 각 줄은 F1 Activity 로그에 기록됩니다.

즉, 게임을 재시작하지 않고 번역을 반복 수정/확인할 수 있습니다. 이 동작은 `[Debug] HotReloadTranslations`로 끌 수 있습니다. 리로드 성공 시 F1 Activity 로그에 `[reload]` 항목이 나타납니다.

### 모든 UI 텍스트 내보내기

**F7**을 눌러 로드된 모든 UI 텍스트를 다음으로 내보냅니다:

`Translations/_discovered/ui_texts.csv`

내보내기에는 **숨겨진 메뉴**도 포함되어, 일시정지 메뉴나 확인 대화상자를 열지 않고도 현재 씬의 UI 문자열을 거의 모두 수집할 수 있습니다. 컬럼은 `key`, `source_en`, `translation`(기존 번역이 있을 경우), `object_path`(UI 내 위치)입니다.

타이틀 화면에서 한 번, 게임플레이 중 한 번 F7을 누르면 UI 텍스트 대부분을 수집할 수 있습니다.

미번역 UI 텍스트는 게임플레이 중 `Translations/_discovered/strings.csv`에도 자동 기록됩니다. 이 파일은 게임 시작 때마다 정리되어 번역 완료 항목과 중복이 제거되며, 남은 작업 목록만 최신 상태로 유지됩니다.

### 번역이 필요 없는 문자열

슬라이더 값, `1920 x 1080 @ 164.995Hz` 같은 해상도 표기, 빌드 번호 등은 기본적으로 수집 대상에서 제외됩니다.

제외 패턴은 [Translations/ignore.txt](Translations/ignore.txt)에 추가할 수 있습니다. 파일은 정규식을 사용하며 예시가 포함되어 있습니다.

제외는 "수집(discovery)"에만 적용됩니다. 번역 조회가 먼저 수행되므로, `strings.csv`에 존재하는 항목은 제외 패턴과 일치해도 항상 번역됩니다.

### 인게임 디버그 메뉴

**F1**을 눌러 도구 창을 토글합니다. 이 창은 Drag'n Wash ModFramework 기반 다른 모드와 공유됩니다. 키 설정은 `BepInEx/config/com.tomxv.dragnwash.modframework.toolwindow.cfg`의 `[General] ToggleKey`입니다. 제목 표시줄 드래그로 이동, 우하단 드래그로 크기 조절이 가능합니다. 이 모드는 다음 4개 탭을 추가합니다.

- **Activity log:** 번역 결과와 처리 로그를 표시합니다. `Follow: ON/OFF`는 최신 항목 자동 스크롤을 제어하며, 수동 스크롤 시 follow가 꺼집니다. `Clear log`는 표시를 지우고 중복 억제를 초기화합니다. 로그는 최신 100개 항목을 유지합니다.
- **Translation:** 재시작 없이 언어 전환(버튼에는 `name.txt`의 언어명이 표시, **English**는 번역 비활성화). 대사 내보내기(`Export loaded dialogue`), UI 텍스트 내보내기(`Export UI text`), 영어 병기 작업본 내보내기(`Export working copy`), 배포 파일 재생성(`Hash for commit`), 레이아웃 점검 실행이 가능합니다.
- **Saves:** 이전 세이브 복원, 레벨 인덱스 증감, 세이브 플래그 토글. 아래 설명 참고.
- **About:** 현재 실행 중인 모드 버전/빌드, 제작자, 라이선스, 이번 세션의 로드 정보가 표시됩니다. 버그 리포트 시 인용하기 좋습니다.

**Check translation layout** 버튼은 레이아웃 범위를 넘길 위험이 있는 문자열을 `Translations/_discovered/layout_risks.csv`로 내보냅니다. 임계값은 `BepInEx/config/.../LayoutOverflowThreshold`로 설정하며 기본값은 정확히 맞춤을 뜻하는 `1.0`입니다.

### 번역 테스트용 이전 세이브 복원

게임이 세이브를 저장할 때마다 플러그인은 버전별 복사본을 다음에 저장합니다:

`BepInEx/SaveHistory/<slot>/`

기본으로 슬롯당 30개 버전을 유지합니다. 이는 `BepInEx/config/com.tomxv.dragnwash.modframework.saves.cfg`의 `[History] Keep` 또는 Mods 화면에서 변경할 수 있습니다. 예전 버전 모드가 `BepInEx/plugins/DragNWashLocalization/SaveHistory`에 보관하던 복사본은 첫 실행 시 이 위치로 이동됩니다.

**F1 → Saves**를 열어 슬롯을 선택하고 원하는 버전의 **Restore**를 클릭하세요. 그다음 타이틀 화면으로 돌아가 해당 슬롯을 불러오면 복원된 세이브가 반영됩니다. 이후 게임 중 저장하면 평소처럼 활성 세이브를 덮어씁니다.

복원 직전 상태는 자동으로 별도 보존되므로 너무 많이 되돌렸더라도 복구할 수 있습니다.

이 기능을 사용하면 같은 장면을 반복 방문하며 대사 번역 리비전을 비교하기 쉽습니다. Restore는 게임 세이브 파일 자체를 바꾸지만 플래그/변수를 직접 편집하진 않습니다.

같은 탭에는 **PROGRESS** 편집기도 있습니다. **-** / **+**로 레벨 인덱스를 이동한 뒤 **Apply**를 누르세요. 앞으로 진행할 때는 아직 보지 않은 콘텐츠 스포일러 가능성이 있어 확인을 요청합니다. **Flags...**는 게임이 사용하는 것으로 알려진 모든 이벤트 플래그를 그룹별(레벨 진행, 스토리, 연애, 씬 트리거, 씬 감상 여부, 세척 세션, 아이템, 디버그)로 보여 주며, 각 항목의 간단한 설명과 현재 세이브에서 설정 여부를 표시합니다. 값을 클릭하면 unset → true → false 순환, 검색창 입력으로 필터, **Reset all to false...**로 모든 플래그를 false로 초기화할 수 있습니다(레벨 인덱스는 유지). 이 목록은 플러그인 DLL 옆의 `FlagCatalog.csv`에서 읽으므로, 나중에 발견한 플래그를 행 추가로 반영할 수 있습니다. 모든 편집 전 세이브 스냅샷이 먼저 생성됩니다.

## Windows에서 Options 열 때 크래시

Unity 6000.3.14f1 + DirectX 12 환경에서 Options를 열 때 `D3D12ScratchAllocator::DestroyScratch` 크래시가 관측되었습니다. Unity 공식 이슈 트래커에도 동일 스택 트레이스가 보고되어 있습니다: [UUM-140564](https://issuetracker.unity.com/issues/10698). 이는 네이티브 렌더링 버그이므로 번역 훅에서 예외 처리를 해도 방지할 수 없습니다.

이 버그는 런타임 텍스처 할당/업로드 시 촉발됩니다. 그래서 Direct3D 12에서는 플러그인이 시작 시 설치된 모든 언어 폰트를 미리 준비하여, 게임플레이 중이나 Options/F1에서 언어 전환 시 폰트 아틀라스에 새 항목이 추가되지 않게 합니다. 각 문자는 해당 언어가 사용하는 폰트 하나에만 래스터라이즈되므로 시작 시 작업량도 과도하지 않습니다. 실제 환경에서 반복 언어 전환까지 검증했습니다. 별도 설정은 필요 없습니다.

다른 그래픽 API(Direct3D 11, Steam Deck의 Vulkan)에서는 런타임 업로드가 가능하므로, 현재 언어만 먼저 준비하고 나머지는 선택 시 로드됩니다. 여기서도 시작 시 전부 준비하고 싶다면 `[Font] PreloadAllLocales = true`를 설정하세요.

그래도 게임이 크래시한다면 Steam의 **Drag'n Wash → 속성 → 일반 → 실행 옵션**에 `-force-d3d11`을 추가하고 재시작하세요. 그래픽 API를 바꿔 이슈를 우회합니다. 해당 옵션은 [Unity 표준 명령행 인자](https://docs.unity3d.com/6000.3/Documentation/Manual/PlayerCommandLineArguments.html)의 일부이며, 게임 DLL이나 세이브 데이터는 수정하지 않습니다.

`BepInEx/LogOutput.log`의 플러그인 시작 항목에는 현재 그래픽 API가 `graphics=...` 형태로 기록됩니다.

`BepInEx/config/com.tomxv.dragnwash.modframework.assets.cfg`의 `[Fonts] AtlasPointSize`를 낮추면 폰트 아틀라스 수가 줄고, 높이면 글자가 더 선명해집니다. 기본값은 80입니다.

## Exclusive 전체 화면에서 창 전환 후 멈춤 (Windows)

DirectX 12에서 **Window Mode**를 **Exclusive**로 두고 다른 창으로 전환했다가(Alt+Tab, 또는 다른 창 클릭) 돌아오면 게임이 멈춘 뒤 크래시할 수 있습니다. 크래시 보고서를 보면 Windows가 게임을 전용 전체 화면에서 빼거나 다시 넣는 동안 Unity의 DirectX 12 스왑 체인이 멈춰 있습니다(`D3D12SwapChain::Present`가 `887a0001`로 실패하며, 로그에서는 그 전에 `D3D12Fence::Wait ... May cause crash`가 자주 보입니다). 그 순간에는 모드 코드가 실행되지 않으며, 이 모드가 아니라 게임의 그래픽 코드 문제입니다.

피하려면 다음 중 하나를 하세요.

- 실행 옵션에 `-force-d3d11`을 추가합니다: Steam에서 **Drag'n Wash → 속성 → 일반 → 실행 옵션**. 검증됨: 이 옵션을 쓰면 Exclusive 전체 화면에서 창을 전환해도 멈추지 않습니다.
- **Window Mode**를 **Exclusive** 대신 **Fullscreen**으로 둡니다. 창을 전환할 때 디스플레이 모드를 바꾸는 것은 전용 전체 화면뿐이므로 이것으로도 피할 수 있을 것입니다(아직 미검증).

## 현재 상태

앞으로의 계획은 [docs/ROADMAP.md](docs/ROADMAP.md)(영어)를 참고하세요.

최신 릴리스는 v1.3.0입니다(Direct3D 12에서의 크래시가 줄고, 게임이 멈추면 무슨 일이 있었는지 창으로 알려 줍니다. Drag'n Wash ModFramework 1.3.0 포함). v1.2.1에서는 2026년 9월 14일 게임 업데이트 이후 만든 세이브가 Saves 탭에 다시 표시되고, 번역자 작업 사본에 열려 있지 않던 화면의 영어도 채워지게 했습니다. v1.2.0으로 릴리스되었습니다(우크라이나어·태국어·베트남어로 16개 언어, 한국어 원어민 교정, 게임 업데이트에도 사라지지 않는 번역, 로고, Drag'n Wash ModFramework 1.2.0). v1.1.2는 직접 그린 Mods 화면 아이콘이 들어간 Drag'n Wash ModFramework 1.1.2를 포함합니다. v1.1.1은 프레임워크의 첫 아이콘이 들어간 ModFramework 1.1.1을 포함했습니다. v1.1.0부터 이 모드의 새 릴리스가 나오면 Mods 화면과 타이틀 화면에서 알려 줍니다(ModFramework 1.1.0과 함께). v1.0.0은 [Drag'n Wash ModFramework](https://github.com/TomXV/dragnwash-modframework) 위에서 동작하도록 바뀌고 Mods 화면을 추가했습니다. v0.6.2는 게임 업데이트 이전 작업본에서 "Hash for commit" 실행 시 행이 누락되던 문제를 막고 "Really Delete Save?"를 번역했습니다. v0.6.1은 히브리어에서 이름 및 일부 미번역 텍스트가 역순으로 표시되던 문제를 수정했습니다. v0.6.0은 줄 단위 번역(같은 영어 문장도 화자별로 다르게 번역 가능)을 추가했으며 2026-09-14 게임 업데이트 기준으로 검증했습니다. v0.5.0은 게임 자체 Options 화면에서 언어 변경을 추가했고, v0.4.0은 13개 언어, 언어별 폰트, About 탭, 영/일/중 설치 프로그램을 도입했습니다. v0.3.0은 Steam Deck 지원을 추가했습니다. Windows on ARM도 검증되었으며(해당 환경에서 게임 자체는 `-force-d3d11` 필요), macOS는 현재 BepInEx 측 알려진 이슈로 동작하지 않습니다([Steam Deck / Linux](#steam-deck--linux-검증됨) 항목의 주석 참고). BepInEx 플러그인 스켈레톤, UI/대사의 일중 번역 치환, CJK 폰트 렌더링, 대량 대사/UI 내보내기, 인게임 디버그 메뉴, 레이아웃 오버플로 감지, 번역자 문서화, 릴리스 워크플로까지 모두 구현 및 실게임 테스트를 완료했습니다.

자세한 내용은 [docs/PLAN.md](docs/PLAN.md)를 참고하세요.

## Drag'n Wash ModFramework

v1.0.0부터 이 모드는 **Drag'n Wash ModFramework** 위에서 동작합니다. 다른 Drag'n Wash 모드도 이를 기반으로 개발할 수 있습니다.

- **기능.** 게임 훅킹 과정에서 이 모드가 구현했던 기능 중 다른 모드에도 유용한 부분을 프레임워크로 분리했습니다: 설치된 모든 모드의 설정/켜기끄기를 보여 주는 **Mods** 화면(Options → Mods), 게임 Options의 언어 행, 텍스트 표시 전 재작성, 대사/선택지 이벤트, 공유 F1 도구 창, Direct3D 12 안전 폰트, 세이브 히스토리.
- **이유.** 게임 업데이트 시 프레임워크만 변경을 따라가면 되고, 그 위의 모드들은 계속 동작할 수 있습니다. 2026-09-14 업데이트가 바로 이런 종류의 변경입니다.
- **플레이어용.** 릴리스 zip과 설치 프로그램에 프레임워크가 포함됩니다. v1.1.0부터 설치 프로그램은 모든 Drag'n Wash 모드가 함께 쓸 수 있는 프레임워크의 공용 설치 프로그램입니다. 이 모드를 제거해도 다른 모드가 설치되어 있으면 프레임워크는 유지됩니다.
- **업데이트 알림.** v1.1.0부터 이 모드나 프레임워크의 새 릴리스가 나오면 타이틀 화면에 **1 update available in Mods**가 표시되고, **Options → Mods**에 릴리스 페이지로 가는 버튼이 생깁니다. 프레임워크는 하루에 한 번 GitHub에 최신 릴리스를 물어보며, 사용자나 게임에 관한 정보는 보내지 않고 아무것도 내려받지 않습니다. **Mods → Drag'n Wash ModFramework → Settings → Check for updates**에서 끌 수 있습니다.
- **번역자용.** CSV 형식과 번역 도구는 바뀌지 않으므로 기존 팩과 기여 내역을 그대로 이어갈 수 있습니다.

Drag'n Wash 모드를 개발 중이고 프레임워크에 있었으면 하는 기능 아이디어가 있다면 이슈를 열어 주세요.

## 번역 기여

코드는 필요 없습니다. `Translations/<locale>/strings.csv`를 편집해 번역에 기여할 수 있습니다.

워크플로, 파일 형식, 미번역 문자열 찾는 방법은 [CONTRIBUTING.md](CONTRIBUTING.md)를 참고하세요.

참여하는 모든 분은 [행동 강령](CODE_OF_CONDUCT.md)을 따릅니다. 보안 문제를 발견하면 이슈가 아니라 비공개로 알려 주세요([SECURITY.md](SECURITY.md)). 원하시고 여유가 있다면 [GitHub Sponsors](https://github.com/sponsors/TomXV)도 있습니다. 어느 쪽이든 모드는 계속 무료이며, 번역이 훨씬 더 큰 도움이 됩니다.

## 배포와 릴리스

릴리스 ZIP 빌드/배포 절차는 [docs/RELEASING.md](docs/RELEASING.md)를 참고하세요.

게임 유래 레퍼런스 어셈블리는 커밋할 수 없기 때문에, 릴리스는 로컬 빌드 후 GitHub Releases에 업로드합니다.

## 개발팀에 드리는 안내

이 프로젝트는 비공식 팬 프로젝트이며 Gator Dragon Games와 제휴되어 있지 않습니다. Drag'n Wash ModFramework의 [콘텐츠 정책](https://github.com/TomXV/dragnwash-modframework/blob/main/docs/CONTENT_POLICY.md)을 따르며, 게임 에셋이나 스크립트 원문을 그대로 포함하지 않고, 영어 문장은 SHA-256 해시로만 저장하고 게임 파일 자체는 수정하지 않습니다(BepInEx가 런타임에 플러그인을 로드). 개발팀 구성원이 우려 사항이 있다면 이 저장소에 이슈를 열거나 유지보수자에게 연락해 주세요. 요청에 맞춰 프로젝트를 조정하거나 내리겠습니다.

## 크레딧

- 이 모드의 **로고**(모드 화면의 아이콘, `icon.png`)는 **Mister ERIO**([@mistererio](https://github.com/mistererio))가 그렸으며, 허락을 받아 사용하고 있습니다.
- Drag'n Wash ModFramework에 포함된 Options 화면의 **Mods 버튼**도 Mister ERIO의 작품입니다.
- Drag'n Wash ModFramework의 **로고와 아이콘**(아이콘은 이 zip에도 포함)은 **NotaGames**([@NotaGames](https://github.com/NotaGames))의 작품입니다.
- 한국어 팩은 **Hotcake**가 교정해 주었습니다.
- 언어 팩을 개선해 주신 번역자는 [Language packs](#language-packs) 표에 표기되어 있습니다.

## 라이선스

플러그인 코드 라이선스는 [LICENSE](LICENSE)를 참고하세요. 크레딧에 적힌 그림은 작가의 것이며, 이 라이선스의 대상이 아닙니다. 이 저장소에는 게임의 에셋이나 코드가 포함되어 있지 않습니다. 번역은 각 번역자의 기여물로 취급됩니다.
