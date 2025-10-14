using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace MathHighLow.UI
{
    /// <summary>
    /// 데모 씬에서도 UI 상호작용이 가능하도록 이벤트 시스템을 자동으로 구성합니다.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class EventSystemBootstrapper : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("씬 전환 시에도 이벤트 시스템을 유지하려면 체크하세요.")]
        private bool dontDestroyOnLoad;

        private void Awake()
        {
            if (EventSystem.current != null)
            {
                if (dontDestroyOnLoad)
                {
                    DontDestroyOnLoad(EventSystem.current.gameObject);
                }

                return;
            }

            var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            var inputModule = eventSystemGo.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
#else
            eventSystemGo.AddComponent<StandaloneInputModule>();
#endif

            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(eventSystemGo);
            }
        }
    }
}
