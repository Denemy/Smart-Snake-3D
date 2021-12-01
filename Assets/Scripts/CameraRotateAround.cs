using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraRotateAround : MonoBehaviour, IBeginDragHandler, IDragHandler
{

	public Transform cam;								// Ссылка на камеру.
	public Transform target;							// Трансформ аквариума.
	
	private Vector3 camPos;                             // Позиция камеры.
	private Quaternion camOrient;                       // Ориентация камеры.
	
	private float dist = 230;						    // Растояние от камеры до центра аквариума.
	private float angleHor = 0;                         // Поворот по горизонтали (0..360).
	private float angleVert = 0;                        // Поворот по вертикали (-90..90).
	private float sensRotate = 0.1f;                    // Чувствительность вращения.
	private float sensZoom = 0.02f;                     // Чувствительность зума.

	private Vector3 startPos;                           // Координаты начала перемещения.
	private Vector3 curPos;                             // Текущие координаты пальцв.


	// =================== Начало перемещения =======================
	public void OnBeginDrag(PointerEventData data)
	{
		startPos = Input.mousePosition;								// Координаты начала перемещения.
	}
	// --------------------------------------------------------------


	// =================== Перемещение пальца ========================
	public void OnDrag(PointerEventData data)
	{
		Vector3 drag;
		curPos = Input.mousePosition;                               // Текущие координаты пальца.
																	//drag = curPos - startPos;                                   // Вектор перемещения пальца.
		drag = data.delta;
		angleHor += (-drag.x * sensRotate) % 360;
		angleVert += (-drag.y * sensRotate);
		angleVert = Mathf.Clamp(angleVert, -89, 89);                // Ограничиваем по вертикали.
	}
	// --------------------------------------------------------------


	// =================== Конец перемещение ========================
	public void OnDragEnd(PointerEventData data)
	{

	}
	// --------------------------------------------------------------

	// ========================== Старт =============================
	void Start () 
	{
		Camera.main.fieldOfView = 70f;
		//Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, -100f);

	}
	// --------------------------------------------------------------


	// ======================= Каждый кадр ==========================
	void Update ()
	{
		float ah;
		float x, y, z, h;
		float distTouch, currentDistTouch, difference;
		Vector3 tV;
		Vector2 touch0StartPos, touch1StartPos;
		Touch touch0, touch1;

		if (Input.touchCount == 2)												// Только если два касания.
        {
			touch0 = Input.GetTouch(0);											// Прикосномение первого пальца.
			touch1 = Input.GetTouch(1);											// Прикосновение второго пальца.

			touch0StartPos = touch0.position - touch0.deltaPosition;			// Позиция начала прикосновения первого пальца.
			touch1StartPos = touch1.position - touch1.deltaPosition;            // Позиция начала прикосновения второго пальца.

			distTouch = (touch0StartPos - touch1StartPos).magnitude;			// Растояние между пальцами в начале.
			currentDistTouch = (touch0.position - touch1.position).magnitude;   // Текущее растояние между пальцами.

			difference = currentDistTouch - distTouch;

			Zoom(difference * sensZoom);
		}



		y = (float)Math.Sin(Math.PI * angleVert / 180);
		h = (float)Math.Cos(Math.PI * angleVert / 180);
		ah = (270 + angleHor) % 360;                                    // Угол позиции камеры, относительно центра куба.
		x = h * (float)Math.Cos(Math.PI * ah / 180);
		z = h * (float)Math.Sin(Math.PI * ah / 180);
		tV = new Vector3(x, y, z);
		tV = dist * tV.normalized;
		camPos = tV + target.position;

		camOrient = Quaternion.LookRotation(target.position - camPos);
		cam.position = camPos;
		cam.rotation = camOrient;                                       // Поворачиваем камеру на аквариум.

		Zoom(Input.GetAxis("Mouse ScrollWheel"));
	}
	// --------------------------------------------------------------


	// ============== Приближение и отдаление камеры ================
	private void Zoom(float increment)
    {
		Camera.main.fieldOfView = Mathf.Clamp(Camera.main.fieldOfView - increment, 20, 60);

	}
	// --------------------------------------------------------------
}