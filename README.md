# Core Overclock (코어 오버클럭)

탑다운 아레나 오토 슈터 / 로그라이크 (Brotato-like). Unity 6.3 LTS (6000.3.11f1), 2D URP.

> "제한 시간 45초, 몰려오는 고철 군단을 부수고 코어를 오버클럭하여 궁극의 살인 병기를 조립하라."

## 열기 / 실행
1. Unity Hub → **Add** → `CoreOverclock` 폴더 (에디터 6000.3.11f1)
2. `Assets/_Project/Scenes/Arena.unity` 열고 Play
3. 데이터/씬을 다시 생성하려면 메뉴 **Core Overclock → Setup Project**
4. 빌드: **Core Overclock → Build Windows** (정식, `Builds/Windows`) / **Build Demo (Windows)** (웨이브 1~10 데모, `Builds/Demo`)

## 조작
| 동작 | 키보드/마우스 | 게임패드 |
|---|---|---|
| 이동 | WASD / 방향키 | 좌스틱 |
| 공격 | 자동 | 자동 |
| 긴급 방열 (Vent Out) | Space / 우클릭 | A / RB |
| 일시정지 | ESC | Start |

## 개발 현황
- [x] **Phase 1** 그레이박스: 이동, 자동 조준 무기, 추적형 적, 웨이브 타이머 → 정지·소멸, 결산(스크랩 자동 회수)
- [x] **Phase 2** 상점 & 데이터: 코어 작업실(4칸 진열·리롤·잠금·판매 70%), 무기 6슬롯, 패시브 칩셋, 태그 시너지, 열(Heat) 게이지 · 오버클럭 · 과열 · 긴급 방열
- [x] **Phase 3** 콘텐츠: 적 5종(스크랩 비트·차지 러너·터렛 드론·마그마 버스트·코어 골렘) + 보스 3종(저거너트·오버시어·스크랩 레기온 코어), 무기 15종, 칩셋 25종, 웨이브 1~20 밸런싱
- [x] **Phase 4** 폴리싱 & 데모: 코드 합성 효과음 22종·BGM 4곡, 블룸/비네트/색수차 포스트 프로세싱, 타이틀·설정 화면, 업적 7종 + Steam 연동 레이어, Next Fest용 데모 빌드

- [x] **중간 점검 (v0.4.1)**: 대미지 숫자 정리, 처치·피격 파편 이펙트, 엘리트 HP 바, 저체력 경고, 무기 슬롯 표시, 첫 플레이 튜토리얼 힌트, 포커스 잃으면 자동 일시정지, 저거너트 난이도 완화
- [x] **Phase 5 (v0.5.0)** 메타 성장 & 퓨전
  - **코어 파편**: 판이 끝날 때(사망/클리어) 도달 웨이브·보스 처치·처치 수로 획득
  - **연구소** (타이틀): 영구 강화 6종(최대 HP, 시작 스크랩, 냉각, 방열 쿨타임, 리롤 할인, 상점 진열 +1칸), 무기 7종·칩셋 10종 해금, 시작 무기 선택, 퓨전 도감
  - **퓨전 8종**: 특정 무기 2개 조합 시 특수 효과 (열충격, EMP 탄, 탄막 지옥, 절대영도, 용융 파편, 서리 산탄, 굴절 포격, 트윈 링크)
  - 조합식은 처음엔 숨겨져 있고, 플레이 중 조합을 완성하면 "새 퓨전 발견!" 연출과 함께 도감에 공개. 미발견 조합은 상점에서 "☆ 미지의 반응 감지…" 신호만 표시
  - 업적 2종 추가(첫 퓨전, 퓨전 도감 완성), 설정에 진행 초기화
  - 데모 빌드: 강화 3단계·티어 2 부품까지만 해금 가능
- [x] **UI 가시성 개선**: 화면 흔들림 완화(적중·폭발 흔들림 제거/상한, 설정에서 끔/약하게/보통), 모든 화면비에서 UI가 잘리지 않도록 스케일 조정, 시너지·퓨전 상세 창(상점 Tab · 일시정지 메뉴), 상점 레이아웃 정리
- [x] **Phase 6 (v0.6.0)** 시작 코어 · 무기 등급 · 암시장
  - **시작 코어 6종** (게임 시작 → 코어 선택): 표준 / 용광로(오버클럭 60%부터·오버클럭 공격력↑·냉각↓) / 수집가(스크랩↑·상점 할인·무기 슬롯 5칸) / 빙하(냉각↑·발열↓·공격력↓) / 쌍둥이(시작 무기 2개·강화 할인·HP↓) / 유리(공격력·치명타↑·HP↓). 표준 외에는 코어 파편으로 해금 (데모는 쌍둥이·유리 제외)
  - **무기 등급 I~IV**: 같은 무기·같은 등급 2개를 **합치기**하면 다음 등급 (슬롯이 꽉 찼을 때 같은 무기를 사면 자동 합치기), 스크랩으로 **강화**도 가능. 등급마다 피해 x1.4/x1.9/x2.6, 연사 +8/16/25%, 발열 감소. 후반 웨이브 상점에는 II~IV 등급 무기도 등장
  - **암시장**: 웨이브 4 이후 정비 시간에 가끔(30%, 4번 연속 안 나오면 확정) 등장. 강력하지만 단점이 있는 금지 칩셋 8종과, 최대 HP를 대가로 무기 등급을 싸게 올리는 불법 개조를 판매
  - 합치기로 트윈 링크가 풀리면 상점에 알림 표시, 업적 2종 추가(최고 등급, 뒷거래)

## 구조
- `Assets/_Project/Scripts/Core` — GameManager(웨이브 흐름), HeatSystem, Loadout(스탯·시너지), Fusion(퓨전 정의), MetaProgress(코어 파편·영구 성장), Cores(시작 코어), WeaponGrades(무기 등급), BlackMarket(암시장), Shop, 풀링/입력/유틸
- `Assets/_Project/Scripts/Gameplay` — Player, Weapon, Projectile, Enemy, Spawner, Scrap, DataTower, Arena, FX
- `Assets/_Project/Scripts/UI` — HUD, ShopUI, TitleScreen/SettingsPanel, LabScreen(연구소), CoreSelectScreen(코어 선택), BlackMarketPanel(암시장 창), DamagePopups
- `Assets/_Project/Scripts/Audio` — SoundSynth(효과음·음악 합성), AudioManager
- `Assets/_Project/Scripts/Platform` — SteamService(업적), BuildFlavor(데모 여부)
- `Assets/_Project/Data` — ScriptableObject 데이터(무기/칩셋/적/웨이브/상점)
- `Assets/_Project/Editor` — 프로젝트 셋업 & 기본 콘텐츠 정의, 빌드

## Steam 연동
기본 빌드는 Steam SDK 없이 동작하며 업적은 로컬(PlayerPrefs)에 기록됩니다. 실제 Steam에 연결하려면:
1. [Steamworks.NET](https://github.com/rlabrecque/Steamworks.NET) 패키지를 설치
2. **Project Settings → Player → Scripting Define Symbols**에 `STEAMWORKS_NET` 추가
3. 빌드 폴더의 `steam_appid.txt`(현재 `480` = Valve 테스트 앱)를 발급받은 App ID로 교체
4. Steamworks 파트너 사이트에 업적 등록 — API 이름은 `Scripts/Platform/SteamService.cs`의 `Achievements` 참고

## 테스트용 커맨드라인
`CoreOverclock.exe -screen-fullscreen 0 -autopilot -autobuy -god -timescale 3 -quitafter 300` — 자동 조종으로 전체 웨이브를 돌며 웨이브별 밸런스 로그를 남깁니다 (`-shots <폴더>`, `-startwave N`, `-title` 등은 `DevCommandLine.cs` 참고).
- `-meta N`: 모든 영구 강화를 N단계로 가정하고 전부 해금한 상태로 실행
- `-loadout laser,cryo_shard`: 지정한 무기를 들고 시작 (퓨전 테스트)
- `-title -lab N`: 연구소 N번 탭을 연 상태로 시작 (스크린샷용)
- `-core Furnace`: 지정한 코어로 시작 (Standard, Furnace, Collector, Glacier, Twin, Glass)
- `-loadout minigun:3`: `무기id:등급` 형식으로 등급 지정
- `-blackmarket`: 매 상점마다 암시장 등장, `-openmarket`: 암시장 창을 바로 열기
- `-title -cores`: 코어 선택 화면을 연 상태로 시작 (스크린샷용)

테스트 실행 중에는 업적·코어 파편·해금 등 진행 상황이 저장되지 않습니다.
