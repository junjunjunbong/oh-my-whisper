# plan.md — Windows Push-to-talk Whisper.cpp 오버레이 전사 앱

## 0. 목표
Windows에서 전역 단축키 **Push-to-talk(누르고 있는 동안만 녹음)**로 마이크를 녹음하고, **작은 오버레이 창**에 **1~2초 주기**로 전사 결과를 갱신 표시한다. 키를 떼면 전사를 확정하고 사용자가 텍스트를 **수정**한 뒤 **복사 버튼**으로 클립보드에 복사한다.

- 플랫폼: Windows
- 입력: 마이크 실시간
- 성능: 1~2초 내 업데이트 체감
- 사용 목적: 본인용
- 엔진: whisper.cpp (CPU-only)

---

## 1. 비범위 (지금은 하지 않음)
- 시스템 오디오(컴퓨터 소리) 캡처
- 다국어 자동 감지/번역/자막 파일 출력 등 확장 기능
- 자동 업데이트/서명/배포 자동화(필요 시 후순위)

---

## 2. 핵심 UX 시나리오
1) 앱은 백그라운드에서 대기(트레이 선택).
2) 사용자가 단축키 조합을 **누르는 순간(Down)** 녹음 시작 + 오버레이 표시.
3) 녹음 중에는 **부분 전사**가 오버레이에 누적/갱신 표시(약 1.2초 주기).
4) 사용자가 단축키를 **떼는 순간(Up)** 녹음 종료 → 최종 전사 확정 → 편집 모드 전환.
5) 사용자가 텍스트 수정 후 **Copy 버튼** 클릭 → 클립보드 복사, **닫기 버튼**(또는 Esc) → 오버레이 숨기기.

---

## 3. 아키텍처 결론
- `whisper.cpp` 자체를 포크하여 UI/훅/오디오를 섞지 않는다.
- 앱(Wrapper)을 별도 리포로 만들고, `whisper.cpp`는 **서브모듈/의존성**으로 포함(특정 커밋 pin).
- 초기에는 “전체 파이프라인 안정화”를 위해 `whisper.cpp` 호출을 단순하게 시작하고, 이후 성능/지연 요구에 맞춰 최적화.

---

## 4. 기술 스택 ✅ 확정
### 4.1 1순위 스택
- **런타임: .NET 8 (LTS) + WPF**
- 전역 키 훅: `WH_KEYBOARD_LL`(P/Invoke) 또는 동등 라이브러리
- 오디오 캡처: WASAPI 기반 (NAudio 등)
- **엔진: whisper.cpp — git submodule + CMake 빌드 + DLL P/Invoke**
  - whisper.cpp를 `/third_party/whisper.cpp`에 서브모듈로 포함, 특정 커밋 pin
  - CMake로 shared library(DLL) 빌드 → C# 에서 P/Invoke 호출
  - 빌드 환경 필요: **Visual Studio 2022 C++ 워크로드 + CMake** (미설치 시 셋업 가이드 제공)
- **전사 언어: 한국어 + 영어** (트레이 메뉴에서 전환 가능, 다국어 모델 사용)

### 4.2 트레이 아이콘(권장)
- 이유: 상시 대기 앱에 적합(창 닫아도 종료되지 않음, 우클릭 메뉴로 종료/설정 가능)

---

## 5. 상태 머신
- `Idle`
  - 오버레이 숨김, 훅 대기
- `Recording`
  - Down 이벤트에서 진입
  - 오디오 캡처 시작, 부분 전사 루프 동작
- `Finalizing`
  - Up 이벤트에서 진입
  - 캡처 중지, 최종 전사 1회 수행(확정 텍스트 생성)
- `Editing`
  - 오버레이 포커스 제공, 사용자 수정
  - Copy 버튼: 클립보드 복사 / 닫기 버튼 또는 Esc: 오버레이 숨기기

---

## 6. 오디오/전사 파이프라인 설계
### 6.1 오디오 포맷
- 16kHz, mono, 16-bit PCM (whisper.cpp 입력 표준에 맞춤)

### 6.2 버퍼링
- 링버퍼(예: 20초) + 추론 큐
- Recording 중 매 `update_interval`마다 “최근 구간”을 추론에 사용

### 6.3 부분 전사(체감 실시간)
- `update_interval`: 기본 1.2초
- `window_sec`: 기본 8초 슬라이딩 윈도우
- 매 주기마다 최근 `window_sec`를 전사해 오버레이에 갱신 표시
- 장점: 진짜 스트리밍 디코딩 없이도 UX가 실시간처럼 보임, 구현 단순

### 6.4 최종 전사
- Up 이벤트 시점에 전체 녹음 구간을 한 번 더 전사(또는 window 누적 결합)
- 오버레이에 최종 텍스트 확정 표시 후 편집 모드

---

## 7. UI 요구사항
### 7.1 오버레이 창 ✅ 확정
- Always-on-top(Topmost)
- **표시 위치: 마우스 커서 근처**에 팝업
- 작은 크기(리사이즈 가능 여부는 옵션)
- 멀티라인 텍스트 박스(편집 가능)
- **Copy 버튼**: 클립보드 복사
- **닫기(X) 버튼**: 오버레이 숨기기
- 단축키:
  - **Enter: 줄바꿈** (일반 텍스트 편집용)
  - **Esc: 닫기**(복사 없이)
- 포커스 정책:
  - Recording 시작 시: 표시하되 포커스 강탈 여부 옵션
  - Recording 종료 후: 편집을 위해 포커스 제공(권장)

### 7.2 트레이 메뉴(선택)
- Enable/Disable hotkey
- **언어 전환 (한국어/영어)**
- 모델 선택(base/small)
- 종료(Quit)

---

## 8. 성능/모델 정책(CPU-only)
- 기본 모델: `base` (다국어 multilingual base — 한국어/영어 지원)
- 정확도 부족 시: `small`로 확장(단 update_interval을 1.5~2.0초로 조정 가능)
- 스레드 설정:
  - 캡처 스레드와 추론 스레드 분리
  - 추론은 큐 기반으로 “중복 실행 방지”(이전 추론이 끝나지 않았으면 스킵/합치기)

---

## 9. 단축키 정책(Push-to-talk)
- 기본 후보: `Ctrl + Alt + Space`
- Down:
  - Recording 시작(이미 Recording이면 무시)
- Up:
  - Recording 종료 + Finalizing 진입
- 예외:
  - 조합키 일부만 떼는 경우 처리(실제 사용성 기준으로 “Space Up”에 맞춰 종료가 단순)

---

## 10. 단계별 구현 계획 (마일스톤)
### M0 — 프로젝트 골격
- .NET 8 WPF 앱 생성
- 오버레이 창 UI 구성(텍스트 박스 + Copy 버튼 + 닫기 버튼 + Esc 처리)
- 마우스 커서 근처에 오버레이 표시 로직
- 클립보드 복사 동작 검증

### M1 — 전역 키 훅(Push-to-talk)
- `WH_KEYBOARD_LL`로 키 Down/Up 감지
- 단축키 조합 상태 추적
- Down/Up에 따라 상태 머신 전환

### M2 — 마이크 캡처
- WASAPI/NAudio로 16kHz mono 캡처
- 링버퍼 구현
- Down에서 캡처 시작, Up에서 캡처 종료

### M3 — 엔진 연동 (DLL P/Invoke)
- whisper.cpp git submodule 추가 + CMake 빌드 스크립트 작성
- DLL 빌드 → C# P/Invoke 바인딩 구현
- 결과를 오버레이에 표시

### M4 — 부분 전사 업데이트(체감 실시간)
- update_interval마다 슬라이딩 윈도우 전사 수행
- 오버레이 텍스트를 “부분 결과”로 갱신(최소한 덮어쓰기 또는 누적)

### M5 — 최종 전사/편집 모드 확정
- Up 시점 최종 전사 1회 수행
- 편집 모드 전환(포커스, 커서 위치 정책)
- Copy 버튼 / 닫기 버튼 / Esc UX 마무리

### M6 — 안정화/튜닝
- CPU 사용률/지연 모니터링
- 모델(base/small) 스위치, update_interval/window_sec 튜닝
- 예외 처리(마이크 장치 변경, 훅 실패, 엔진 실패)

### M7 — 트레이(권장) + 설정(선택)
- 트레이 아이콘 추가
- 우클릭 메뉴(Enable/Disable hotkey, 언어 전환, 모델 선택, Quit)
- 자동 시작(선택)

---

## 11. 리포 구성(제안)
- `/app` : WPF 앱
  - `Hotkey/` : 훅 및 조합키 로직
  - `Audio/` : 캡처, 리샘플, 링버퍼
  - `Transcription/` : whisper 호출(추론 워커, 큐)
  - `UI/` : 오버레이, 트레이, 설정
  - `StateMachine/`
- `/third_party/whisper.cpp` : submodule
- `/scripts/` : 빌드/패키징 스크립트
- `plan.md` (본 문서)

---

## 12. 기본 파라미터(초기값)
- hotkey: `Ctrl + Alt + Space`
- update_interval: `1.2s`
- window_sec: `8s`
- model: `base` (multilingual)
- language: `ko` (트레이에서 `en` 전환 가능)
- audio: `16kHz mono PCM`
- overlay_position: 마우스 커서 근처

---

## 13. 리스크 및 대응
- 전역 훅 충돌(다른 앱과 단축키 겹침)
  - 대응: 단축키 설정 가능하게(후순위)
- CPU-only에서 small 모델 지연 증가
  - 대응: update_interval 증가, window_sec 감소, base로 유지
- 부분 전사 결과 흔들림
  - 대응: Recording 중에는 “최근 결과 표시” 위주, Finalizing에서 최종 확정으로 UX 분리
