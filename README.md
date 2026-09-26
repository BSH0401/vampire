# Core Overclock (코어 오버클럭)

탑다운 아레나 오토 슈터 / 로그라이크 (Brotato-like). Unity 6.3 LTS (6000.3.11f1), 2D URP.

> "제한 시간 45초, 몰려오는 고철 군단을 부수고 코어를 오버클럭하여 궁극의 살인 병기를 조립하라."

## 열기 / 실행
1. Unity Hub → **Add** → `CoreOverclock` 폴더 (에디터 6000.3.11f1)
2. `Assets/_Project/Scenes/Arena.unity` 열고 Play
3. 데이터/씬을 다시 생성하려면 메뉴 **Core Overclock → Setup Project**, 빌드는 **Core Overclock → Build Windows**

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
- [ ] **Phase 4** 폴리싱: 사운드, 블룸, Steam SDK, 데모 빌드

## 구조
- `Assets/_Project/Scripts/Core` — GameManager(웨이브 흐름), HeatSystem, Loadout(스탯·시너지), Shop, 풀링/입력/유틸
- `Assets/_Project/Scripts/Gameplay` — Player, Weapon, Projectile, Enemy, Spawner, Scrap, DataTower, Arena, FX
- `Assets/_Project/Scripts/UI` — HUD, ShopUI, DamagePopups
- `Assets/_Project/Data` — ScriptableObject 데이터(무기/칩셋/적/웨이브/상점)
- `Assets/_Project/Editor` — 프로젝트 셋업 & 기본 콘텐츠 정의, 빌드
