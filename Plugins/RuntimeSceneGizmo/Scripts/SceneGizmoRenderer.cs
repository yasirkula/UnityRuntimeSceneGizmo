using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RuntimeSceneGizmo
{
	[System.Serializable]
	public class ComponentClickedEvent : UnityEvent<GizmoComponent>
	{
	}

	public class SceneGizmoRenderer : MonoBehaviour, IPointerClickHandler, IDragHandler
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL || UNITY_FACEBOOK || UNITY_WSA || UNITY_WSA_10_0
		, IPointerEnterHandler, IPointerExitHandler
#else
		, IPointerDownHandler, IPointerUpHandler
#endif
	{
		[SerializeField]
		private RawImage imageHolder;
		private RectTransform imageHolderTR;

		[SerializeField]
		private SceneGizmoController controllerPrefab;
		private SceneGizmoController controller;

		[SerializeField]
		private bool highlightHoveredComponents = true;
		private PointerEventData hoveringPointer;

		[SerializeField]
		private ComponentClickedEvent m_onComponentClicked;
		public ComponentClickedEvent OnComponentClicked { get { return m_onComponentClicked; } }

		private void Awake()
		{
			imageHolderTR = (RectTransform) imageHolder.transform;
			controller = (SceneGizmoController) Instantiate( controllerPrefab );

			imageHolder.texture = controller.TargetTexture;
		}

		private void OnEnable()
		{
			if( controller != null && !controller.Equals( null ) )
				controller.gameObject.SetActive( true );
		}

		private void OnDisable()
		{
			if( controller != null && !controller.Equals( null ) )
				controller.gameObject.SetActive( false );
		}

		private void Update()
		{
			if( hoveringPointer != null )
				controller.OnPointerHover( GetNormalizedPointerPosition( hoveringPointer ) );
		}

		public void OnPointerClick( PointerEventData eventData )
		{
			if( eventData.dragging )
				return;

			GizmoComponent hitComponent = controller.Raycast( GetNormalizedPointerPosition( eventData ) );
			if( hitComponent != GizmoComponent.None )
				m_onComponentClicked.Invoke( hitComponent );
		}

		public void OnDrag( PointerEventData eventData )
		{
		}

		private Vector3 GetNormalizedPointerPosition( PointerEventData eventData )
		{
			Vector2 localPos;
			Vector2 size = imageHolderTR.rect.size;
			RectTransformUtility.ScreenPointToLocalPointInRectangle( imageHolderTR, eventData.position, null, out localPos );

			return new Vector3( 1f + localPos.x / size.x, 1f + localPos.y / size.y, 0f );
		}

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL || UNITY_FACEBOOK || UNITY_WSA || UNITY_WSA_10_0
		public void OnPointerEnter( PointerEventData eventData )
		{
			if( highlightHoveredComponents )
				hoveringPointer = eventData;
		}

		public void OnPointerExit( PointerEventData eventData )
		{
			if( hoveringPointer != null )
			{
				controller.OnPointerHover( new Vector3( -10f, -10f, 0f ) );
				hoveringPointer = null;
			}
		}
#else
		public void OnPointerDown( PointerEventData eventData )
		{
			if( highlightHoveredComponents )
				hoveringPointer = eventData;
		}

		public void OnPointerUp( PointerEventData eventData )
		{
			if( hoveringPointer != null )
			{
				controller.OnPointerHover( new Vector3( -10f, -10f, 0f ) );
				hoveringPointer = null;
			}
		}
#endif
	}
}