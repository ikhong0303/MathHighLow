# MathHighLow

간단한 수식 조합 하이로우 게임 프로토타입을 위한 핵심 스크립트와 UI 자동 생성 로직이 추가되었습니다.

## 주요 구성 요소

- **MathHighLowGameManager** (`Assets/Scripts/Core/MathHighLowGameManager.cs`)
  - 덱 구성, 카드 드로우, 수식 제출, 승패 판정을 모두 관리합니다.
  - 씬에 빈 GameObject를 만들고 컴포넌트를 추가하기만 하면 필요한 Canvas/UI가 자동 생성됩니다.
- **DeckService / CardDefinitions**
  - 숫자 및 연산자 카드를 생성하고 셔플·드로우하는 유틸리티입니다.
- **MathExpressionEvaluator**
  - 선택된 카드 순서대로 수식을 검증/계산하고 문자열로 반환합니다.
- **SimpleAiPlayer**
  - 2~3개의 숫자와 연산자 조합을 탐색하여 목표값(=20 / =1)에 가장 근접한 수식을 만듭니다.
- **GameUIController & CardButtonView**
  - 흰색 Unity UI 버튼 기반 카드 프리팹을 런타임에 생성하고 상호작용 이벤트를 제공합니다.
  - `Resources/UI` 폴더에 있는 숫자/연산자 카드 프리팹을 자동으로 불러와 적용합니다. (프리팹이 없으면 기본 버튼을 즉시 생성합니다.)
- **UI 프리팹 샘플** (`Assets/Resources/UI/NumberCardButton.prefab`, `Assets/Resources/UI/OperatorCardButton.prefab`)
  - 숫자 카드와 연산자 카드 각각에 색상과 폰트가 지정된 UI 버튼 예시입니다.
  - 디자이너가 쉽게 색상을 조정할 수 있도록 독립된 프리팹으로 분리했습니다.

## 사용 방법

1. `Assets/Scenes/SampleScene.unity`에는 이미 `MathHighLowGame` 오브젝트가 배치되어 있으며 `MathHighLowGameManager`가 연결되어 있습니다.
2. 별도의 UI 세팅 없이 Play를 누르면, 0.2초 간격으로 AI와 플레이어에게 카드가 배분됩니다. (필요 시 `MathHighLowGameManager`의 Inspector에서 라운드 설정 값을 조절하세요.)
3. 플레이어 카드를 차례로 클릭하여 수식을 구성하고, 목표 버튼(=20 또는 =1)을 선택한 뒤 **수식 제출** 버튼을 누릅니다.
4. AI가 자동으로 만든 수식과 비교하여 목표값에 더 가까운 쪽이 승리하며 결과 메시지가 표시됩니다.
5. **선택 초기화** 버튼으로 현재 라운드에서 사용한 카드를 다시 선택할 수 있습니다.

## 개발 로드맵(v2.0 대응)

1. **UI 및 라운드 안정화**
   - 카드 프리팹 라벨을 Canvas 기반 `TextMeshProUGUI`로 강제 전환해 텍스트가 보이지 않는 문제를 해결합니다.
   - 라운드가 끝나기 전에 덱을 다시 셔플하지 않도록 메인 루프를 수정해 "무한 셔플" 상태를 방지합니다.

2. **카드 풀·분배 규칙 확장**
   - 숫자 0~10, ×, √를 포함하는 덱을 만들고, 특수 카드 획득 시 보너스(추가 숫자 지급, 기본 기호 비활성화)를 처리하는 로직을 추가합니다.
   - 특수 카드를 반드시 사용하도록 핸드/수식 상태를 추적하고 UI 배지를 제공합니다.

3. **검증·UI 고도화**
   - "모든 숫자/특수 카드 사용", "이항 연산자 수 = 숫자 수 − 1", "0으로 나누기 금지" 등의 제출 검증기를 구현하고 원인별 안내 메시지를 노출합니다.
   - 베팅 UI, 3분 타이머(30초 이후 제출 가능), 결과창 차이 표시 등 기획서에 명시된 UX 요구사항을 순차적으로 반영합니다.

4. **AI 및 추가 시스템**
   - 특수 카드 사용을 포함한 완전 탐색 기반 AI를 구현하고, 베팅/자산 시스템과 연동합니다.
   - 연출(애니메이션·사운드), 난이도 옵션 등을 확장합니다.

---

## UI 리팩터링 Read Me

다음 내용은 현재 코드 기반 UI 생성 방식을 **Canvas/Prefab + Inspector 연결 방식**으로 전환하기 위한 정리입니다. 강의용 프로젝트에서 디자이너가 Unity 편집기만으로 UI를 다룰 수 있도록 하는 것이 목표입니다.

### 1. 현재 구조 요약

- `GameUIController`가 `BuildLayout()` 이하 일련의 `Build*`, `Create*` 메서드를 통해 런타임에 모든 UI GameObject를 생성합니다.
- 숫자/연산자 카드 버튼은 `CardButtonView` 프리팹을 인스턴스화하지만, 나머지 버튼과 텍스트는 코드로만 존재해 편집이 어렵습니다.
- `MathHighLowGameManager`는 `EnsureUi()` 실행 시점에 UI를 지연 초기화하며, 이때 `BuildLayout()`이 호출됩니다.

### 2. 목표 상태

- Canvas, 섹션 컨테이너, 텍스트, 버튼 등을 **씬 또는 프리팹에서 미리 구성**하고 `GameUIController`에는 **참조만 연결**합니다.
- 카드 버튼 프리팹(`CardButtonView`)은 계속 사용하되, Inspector 슬롯으로 지정해 디자이너가 다른 프리팹으로 교체할 수 있게 합니다.
- 연산자/타겟/베팅/제출 버튼 등의 이벤트 연결은 `GameUIController`의 `Awake/Start/EnsureUi` 단계에서만 수행하고, Inspector에 노출된 필드가 null인지 확인하는 방식을 채택합니다.

### 3. 구현해야 할 코드 작업

1. **SerializeField 전환**
   - `GameUIController`의 UI 참조 필드(컨테이너, 텍스트, 버튼 등)에 `[SerializeField]`를 적용해 Inspector에서 할당할 수 있게 합니다.
   - 연산자 버튼은 구조체/Dictionary 등을 활용해 Inspector에 노출하거나, 각 버튼을 개별 필드(`addButton`, `subtractButton` 등)로 선언한 뒤 런타임에 Dictionary에 등록합니다.

2. **동적 생성 코드 제거**
   - `BuildLayout()`과 모든 `Build*`, `Create*`, `CreatePanel()` 등 GameObject를 만드는 함수를 삭제하거나 비활성화합니다.
   - `EnsureCanvasComponents()`는 최소한의 검증만 남기고, Canvas 세팅은 씬에서 처리한다고 명시합니다.

3. **이벤트 연결 정리**
   - `EnsureUi()` 또는 새로운 초기화 메서드에서 Inspector로 받은 버튼들의 `onClick`을 정리(`RemoveAllListeners`) 후, 기존 델리게이트(`OnSubmitRequested` 등)를 연결합니다.
   - 타겟 버튼, 연산자 버튼도 동일한 방식으로 연결하며, 필요 시 `List<Button>` 혹은 `SerializedField` 배열을 사용해 간편하게 순회합니다.

4. **유틸리티 수정**
   - `SetTargetOptions`는 이제 버튼을 생성하지 않고, Inspector로 받은 버튼 리스트를 순회하며 라벨/상태만 업데이트합니다.
   - `ShowDisableOperatorPrompt` 등 팝업 관련 함수도 프리팹 또는 씬 객체를 참조하도록 변경합니다.

### 4. Unity 에디터에서 준비할 항목 (사용자 담당)

1. **씬 구조 만들기**
   - Canvas 하위에 다음과 같은 섹션을 빈 GameObject + Layout Group 조합으로 미리 구성합니다.
     - Header, Target Buttons, Badge Row, AI 카드 영역, 플레이어 카드 영역, 수식 표시, 연산자 영역, 베팅/타이머/제출 영역, 결과 영역, 비활성화 선택 패널 등.
   - 각 섹션에 TextMeshProUGUI, Button, Image 등을 배치하고 필요한 스타일을 적용합니다.

2. **프리팹 연결**
   - 숫자/연산자 카드 버튼 프리팹을 준비하고, `GameUIController`의 `numberCardPrefab` 슬롯에 드래그해 연결합니다.
   - 타겟 버튼(=1, =20)과 연산자 버튼(+, -, ÷, ×, √), 베팅/제출/초기화 버튼 등을 모두 씬에 배치한 뒤 `GameUIController` Inspector 슬롯에 연결합니다.

3. **보조 텍스트/패널 연결**
   - 상태 메시지, 플레이어/AI 수식, 잔액, 배지, 타이머, 제출 툴팁, 결과 요약/상세 텍스트 필드를 모두 Inspector에 연결합니다.
   - × 카드 사용 시 노출되는 비활성화 패널과 그 안의 버튼 컨테이너도 참조로 연결합니다.

### 5. 권장 개발 순서

1. **UI 씬 설계 (사용자)**: Canvas와 섹션 구조를 배치하고 필요한 컴포넌트를 모두 준비합니다.
2. **`GameUIController` 직렬화 작업 (개발자)**: 필드에 `[SerializeField]`를 선언하고, 기존 동적 생성 코드를 제거합니다.
3. **이벤트 연결 리팩터링 (개발자)**: Inspector에서 받은 버튼을 기반으로 `EnsureUi()` 로직을 재작성합니다.
4. **기능 테스트**: 플레이 모드에서 카드 배분, 버튼 클릭, 목표 선택, 제출/초기화 등이 정상 동작하는지 확인합니다.
5. **디자인 조정 (사용자)**: 필요 시 색상·폰트·레이아웃 등을 Inspector에서 자유롭게 수정합니다.

위 순서를 반복 적용하면, 앞으로도 새로운 UI 요구사항이 생길 때 Scene 편집만으로 대응할 수 있습니다.
