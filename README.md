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

## 다음 단계 제안

- 카드 배분 및 제출 결과에 맞춘 연출(애니메이션, 사운드) 추가
- 베팅·자산 시스템, 타이머 등 기획서의 추가 기능 확장
- 난이도에 따른 AI 전략 다양화 및 UI 시각적 개선
