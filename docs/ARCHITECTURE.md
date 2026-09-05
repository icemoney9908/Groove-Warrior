# 코드 구조

## 게임 진행과 상태

[GameFlowManager](../Assets/02_Scripts/01_YWJ/GameFlowScript/GameFlowManager.cs)는 현재 진행도에 맞는 게임 상태를 선택하고 콘텐츠 실행 흐름을 관리합니다. 준비가 끝나면 `ContentStarted`를 발행하며, [ProgressScenarioManager](../Assets/02_Scripts/01_YWJ/GameFlowScript/ProgressScenarioManager.cs)가 진행도별 컷씬·튜토리얼·스테이지를 연결합니다.

[GameStateController](../Assets/02_Scripts/01_YWJ/GameStateScripts/GameStateController.cs)는 MainMenu, StoryMode, MusicMode, BossMode, GameClear 상태 구현과 UI를 연결합니다. [PlayerProgressManager](../Assets/02_Scripts/01_YWJ/PlayerScripts/Progress/PlayerProgressManager.cs)는 진행도를 변경하고 `Application.persistentDataPath`에 JSON으로 저장합니다.

## 리듬 판정

[PlayerInputHandler](../Assets/02_Scripts/01_YWJ/PlayerScripts/PlayerInputHandler.cs)는 Input System 콜백을 받아 판정과 연출에 전달합니다. [TabSuccessChecker](../Assets/02_Scripts/01_YWJ/GameLogicScripts/TabSuccessChecker.cs)는 외부 Music & Note 모듈의 곡 시간과 노트 데이터를 사용합니다. 음악 시계 자체의 구현은 이 공개본의 범위가 아닙니다.

- TAP: 현재 노트의 판정 가능 상태와 입력 시각 오차를 확인합니다.
- HOLD: 시작 허용 범위, 유지 Tick 간격, 해제 허용 범위를 분리합니다.
- 노트 상태: Waiting, Judgeable, Ignored 상태에 따라 대기·판정·소비를 구분합니다.
- 재시작: 세대 번호를 증가시키며, `ConsumeCurrentNote()`에서 세대 변경을 확인하여 기존 이벤트 흐름이 초기화된 노트를 바로 소비하는 상황에 대응합니다. 모든 실행 경로의 안전성을 검증했다는 의미는 아닙니다.

## 점수와 이펙트

[GameScoreCalculator](../Assets/02_Scripts/01_YWJ/GameLogicScripts/GameScoreCalculator.cs)는 성공·실패 이벤트로 콤보, 배율, 실패 포인트를 계산하고 연출과 스테이지 실패 처리를 연결합니다. 현재 소스는 초기화 시 Music Tier를 5로 설정하고, 점수에 따른 Tier 변경 호출은 주석 처리되어 있습니다. `SaveScore()`는 로그를 출력하며 영구 점수 저장 구현은 아닙니다.

[AttackEffectPoolManager](../Assets/02_Scripts/01_YWJ/GameLogicScripts/TabFXScripts/AttackEffect/AttackEffectPoolManager.cs)는 Dictionary로 이펙트 ID별 풀을 선택하고 Queue로 인스턴스를 대여·반환합니다. 빈 풀에는 새 인스턴스를 추가합니다. 이 문서는 재사용 구조를 설명하며 성능 측정 결과를 제시하지 않습니다.

## 컷씬 편집과 실행

[CutSceneData](../Assets/06_CustomEditor/YWJ_CutSceneEditor/Runtime/CutSceneData.cs)는 Step 목록을 담는 ScriptableObject입니다. 각 Step에는 Action 목록이 있으며, [CutSceneManager](../Assets/06_CustomEditor/YWJ_CutSceneEditor/Runtime/CutSceneManager.cs)는 Actor·Marker·Object ID와 씬 객체를 매핑합니다.

Step은 순서대로 진행하고, 같은 Step의 Action은 별도 코루틴으로 시작합니다. `WaitForCompletion`이 설정된 Action의 완료만 기다린 뒤 다음 Step으로 진행합니다. [CutSceneEditorWindow](../Assets/06_CustomEditor/YWJ_CutSceneEditor/Editor/CutSceneEditorWindow.cs)는 ReorderableList 편집, 복제, 대상 선택과 플레이 모드 전체·Step 단독 테스트를 제공합니다.

현재 `Stop()`은 상위 `_playCoroutine`을 중지하지만 독립적으로 시작한 자식 Action을 일괄 중단하는 처리는 확인되지 않습니다. 자식 코루틴 핸들 관리와 상태 복구 통합은 향후 개선점이며, 이 업로드에서 수정하거나 동작을 재현하지 않았습니다.
