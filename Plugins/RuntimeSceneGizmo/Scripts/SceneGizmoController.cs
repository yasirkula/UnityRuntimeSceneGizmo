using UnityEngine;

namespace RuntimeSceneGizmo
{
	public enum GizmoComponent { None = -1, Center = 0, XNegative = 1, XPositive = 2, YNegative = 3, YPositive = 4, ZNegative = 5, ZPositive = 6 };

	public class SceneGizmoController : MonoBehaviour
	{
		private const int GIZMOS_LAYER = 24;

		[SerializeField]
		private Camera gizmoCamera;
		private Transform gizmoCamParent;

		[SerializeField]
		private Renderer[] gizmoComponents;

		[SerializeField]
		private TextMesh[] labels;
		private Transform[] labelsTR;

		private Camera mainCamera;
		private Transform mainCamTR;

		private Vector3 prevForward;

		private Material gizmoNormalMaterial, gizmoFadeMaterial, gizmoHighlightMaterial;
		private int gizmoMaterialFadeProperty;

		private GizmoComponent highlightedComponent = GizmoComponent.None;
		private GizmoComponent fadingComponent = GizmoComponent.None;
		private bool isFadingToZero;
		private float fadeT = 1f;

		public RenderTexture TargetTexture { get; private set; }

		private void Awake()
		{
			mainCamera = Camera.main;
			mainCamTR = mainCamera.transform;
			gizmoCamParent = gizmoCamera.transform.parent;
			labelsTR = new Transform[labels.Length];

			int gizmoResolution = Mathf.Min( Mathf.NextPowerOfTwo( Mathf.Max( Screen.width, Screen.height ) / 6 ), 512 );
			TargetTexture = new RenderTexture( gizmoResolution, gizmoResolution, 16 );
			gizmoCamera.aspect = 1f;
			gizmoCamera.targetTexture = TargetTexture;
			gizmoCamera.cullingMask = 1 << GIZMOS_LAYER;
			gizmoCamera.eventMask = 0;
			mainCamera.cullingMask = mainCamera.cullingMask & ~( 1 << GIZMOS_LAYER );
			if( mainCamera.clearFlags == CameraClearFlags.Color )
				gizmoCamera.backgroundColor = mainCamera.backgroundColor;

			gizmoCamera.enabled = true;

			gizmoNormalMaterial = gizmoComponents[0].sharedMaterial;
			gizmoFadeMaterial = new Material( gizmoNormalMaterial );
			gizmoMaterialFadeProperty = Shader.PropertyToID( "_AlphaVal" );

			gizmoHighlightMaterial = new Material( gizmoNormalMaterial );
			gizmoHighlightMaterial.EnableKeyword( "HIGHLIGHT_ON" );
			gizmoHighlightMaterial.color = Color.yellow;

			for( int i = 0; i < gizmoComponents.Length; i++ )
				gizmoComponents[i].gameObject.layer = GIZMOS_LAYER;

			for( int i = 0; i < labelsTR.Length; i++ )
			{
				labels[i].gameObject.layer = GIZMOS_LAYER;
				labelsTR[i] = labels[i].transform;
			}
		}

		private void OnEnable()
		{
			if( highlightedComponent != GizmoComponent.None )
			{
				gizmoComponents[(int) highlightedComponent].sharedMaterial = gizmoNormalMaterial;
				highlightedComponent = GizmoComponent.None;
			}

			SetHiddenComponent( GizmoComponent.None );
		}

		private void OnDestroy()
		{
			if( TargetTexture != null )
			{
				TargetTexture.Release();
				Destroy( TargetTexture );
			}
		}

		private void LateUpdate()
		{
			Vector3 forward = mainCamTR.forward;
			if( prevForward != forward )
			{
				float xAbs = forward.x < 0 ? -forward.x : forward.x;
				float yAbs = forward.y < 0 ? -forward.y : forward.y;
				float zAbs = forward.z < 0 ? -forward.z : forward.z;

				GizmoComponent facingDirection;
				float facingDirectionCosine;
				if( xAbs > yAbs )
				{
					if( xAbs > zAbs )
					{
						facingDirection = forward.x > 0 ? GizmoComponent.XPositive : GizmoComponent.XNegative;
						facingDirectionCosine = Vector3.Dot( forward, new Vector3( 1f, 0f, 0f ) );
					}
					else
					{
						facingDirection = forward.z > 0 ? GizmoComponent.ZPositive : GizmoComponent.ZNegative;
						facingDirectionCosine = Vector3.Dot( forward, new Vector3( 0f, 0f, 1f ) );
					}
				}
				else if( yAbs > zAbs )
				{
					facingDirection = forward.y > 0 ? GizmoComponent.YPositive : GizmoComponent.YNegative;
					facingDirectionCosine = Vector3.Dot( forward, new Vector3( 0f, 1f, 0f ) );
				}
				else
				{
					facingDirection = forward.z > 0 ? GizmoComponent.ZPositive : GizmoComponent.ZNegative;
					facingDirectionCosine = Vector3.Dot( forward, new Vector3( 0f, 0f, 1f ) );
				}

				if( facingDirectionCosine < 0 )
					facingDirectionCosine = -facingDirectionCosine;

				if( facingDirectionCosine >= 0.92f ) // cos(20)
					SetHiddenComponent( GetOppositeComponent( facingDirection ) );
				else
					SetHiddenComponent( GizmoComponent.None );

				Quaternion rotation = mainCamTR.rotation;
				gizmoCamParent.localRotation = rotation;

				// Adjust the labels
				float xLabelPos = ( xAbs - 0.15f ) * 0.65f;
				float yLabelPos = ( yAbs - 0.15f ) * 0.65f;
				float zLabelPos = ( zAbs - 0.15f ) * 0.65f;

				if( xLabelPos < 0f )
					xLabelPos = 0f;
				if( yLabelPos < 0f )
					yLabelPos = 0f;
				if( zLabelPos < 0f )
					zLabelPos = 0f;

				labelsTR[0].localPosition = new Vector3( 0f, 0f, xLabelPos );
				labelsTR[1].localPosition = new Vector3( 0f, 0f, yLabelPos );
				labelsTR[2].localPosition = new Vector3( 0f, 0f, zLabelPos );

				labelsTR[0].rotation = rotation;
				labelsTR[1].rotation = rotation;
				labelsTR[2].rotation = rotation;

				prevForward = forward;
			}

			if( fadeT < 1f )
			{
				fadeT += Time.unscaledDeltaTime * 4f;
				if( fadeT >= 1f )
					fadeT = 1f;

				SetAlphaOf( fadingComponent, isFadingToZero ? 1f - fadeT : fadeT );
				if( fadeT >= 1f )
				{
					if( !isFadingToZero )
					{
						SetMaterialOf( fadingComponent, gizmoNormalMaterial );
						fadingComponent = GizmoComponent.None;
					}
					else
					{
						gizmoComponents[(int) fadingComponent].gameObject.SetActive( false );
						gizmoComponents[(int) GetOppositeComponent( fadingComponent )].gameObject.SetActive( false );
					}
				}
			}
		}

		public GizmoComponent Raycast( Vector3 normalizedPosition )
		{
			RaycastHit hit;
			if( Physics.Raycast( gizmoCamera.ViewportPointToRay( normalizedPosition ), out hit, gizmoCamera.farClipPlane, 1 << GIZMOS_LAYER, QueryTriggerInteraction.Collide ) )
			{
				GameObject hitObject = hit.collider.transform.gameObject;
				for( int i = 0; i < gizmoComponents.Length; i++ )
				{
					if( gizmoComponents[i].gameObject == hitObject )
						return (GizmoComponent) i;
				}
			}

			return GizmoComponent.None;
		}

		public void OnPointerHover( Vector3 normalizedPosition )
		{
			// Set highlighted component
			GizmoComponent hitComponent = Raycast( normalizedPosition );
			if( hitComponent != GizmoComponent.None )
			{
				if( hitComponent != highlightedComponent )
				{
					if( highlightedComponent != GizmoComponent.None )
						gizmoComponents[(int) highlightedComponent].sharedMaterial = gizmoNormalMaterial;

					if( hitComponent != fadingComponent )
					{
						highlightedComponent = hitComponent;
						gizmoComponents[(int) highlightedComponent].sharedMaterial = gizmoHighlightMaterial;
					}
					else
						highlightedComponent = GizmoComponent.None;
				}
			}
			else if( highlightedComponent != GizmoComponent.None )
			{
				gizmoComponents[(int) highlightedComponent].sharedMaterial = gizmoNormalMaterial;
				highlightedComponent = GizmoComponent.None;
			}
		}

		private void SetHiddenComponent( GizmoComponent component )
		{
			if( component != GizmoComponent.None )
			{
				if( component != fadingComponent )
				{
					if( fadingComponent != GizmoComponent.None )
					{
						SetMaterialOf( fadingComponent, gizmoNormalMaterial );
						SetAlphaOf( fadingComponent, 1f );

						gizmoComponents[(int) fadingComponent].gameObject.SetActive( true );
						gizmoComponents[(int) GetOppositeComponent( fadingComponent )].gameObject.SetActive( true );
					}

					fadingComponent = component;
					SetMaterialOf( fadingComponent, gizmoFadeMaterial );
					isFadingToZero = true;
					fadeT = 0f;
				}
			}
			else if( fadingComponent != GizmoComponent.None && fadeT >= 1f )
			{
				gizmoComponents[(int) fadingComponent].gameObject.SetActive( true );
				gizmoComponents[(int) GetOppositeComponent( fadingComponent )].gameObject.SetActive( true );

				isFadingToZero = false;
				fadeT = 0f;
			}
		}

		private void SetAlphaOf( GizmoComponent component, float alpha )
		{
			if( component == GizmoComponent.None )
				return;

			gizmoFadeMaterial.SetFloat( gizmoMaterialFadeProperty, alpha );
			if( component == GizmoComponent.XNegative || component == GizmoComponent.XPositive )
				labels[0].color = new Color( 1f, 1f, 1f, alpha );
			else if( component == GizmoComponent.ZNegative || component == GizmoComponent.ZPositive )
				labels[2].color = new Color( 1f, 1f, 1f, alpha );
			else
				labels[1].color = new Color( 1f, 1f, 1f, alpha );
		}

		private void SetMaterialOf( GizmoComponent component, Material material )
		{
			if( component == GizmoComponent.None )
				return;

			GizmoComponent oppositeComponent = GetOppositeComponent( component );
			if( component == highlightedComponent || oppositeComponent == highlightedComponent )
				highlightedComponent = GizmoComponent.None;

			gizmoComponents[(int) component].sharedMaterial = material;
			gizmoComponents[(int) oppositeComponent].sharedMaterial = material;
		}

		private GizmoComponent GetOppositeComponent( GizmoComponent component )
		{
			if( component == GizmoComponent.None || component == GizmoComponent.Center )
				return component;

			int val = (int) component;
			if( val % 2 == 0 )
				return (GizmoComponent) ( val - 1 );

			return (GizmoComponent) ( val + 1 );
		}
	}
}