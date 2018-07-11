# Runtime Scene Gizmo for Unity
![screenshot](screenshot.png)

This plugin helps you display a runtime scene gizmo as a UI element in your Unity projects. An event is raised when a component of the gizmo is clicked.

## HOW TO
Simply add **Plugins/RuntimeSceneGizmo/Prefabs/GizmoRenderer** to your UI canvas and position it as you like. Gizmo will always have 1:1 aspect ratio and, by default, will be placed at the top-right corner of the SceneGizmo. You can change the **RenderTarget** child object's pivot for a different placement.

To invoke a function when a gizmo component is clicked, use the **On Component Clicked** event of the SceneGizmo object. Functions registered to this event should ideally take a **GizmoComponent** parameter (which is defined in **RuntimeSceneGizmo** namespace).

GizmoRenderer will automatically create an instance of **SceneGizmo** prefab in your scene. SceneGizmo uses a camera to render the gizmo to a *RenderTexture*, which is then used by the GizmoRenderer to display the gizmo on the canvas. By default, SceneGizmo objects use layer 24 but this can be adjusted via *SceneGizmoController.GIZMOS_LAYER* constant.

See **Plugins/RuntimeSceneGizmo/Demo/DemoScene** for an example scene.