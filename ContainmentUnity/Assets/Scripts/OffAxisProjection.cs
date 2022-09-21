using UnityEngine;

// Non-symmetric camera frustum and compute the off-axis projection matrix used for the eye camera
// Based on: https://github.com/algomystic/TheParallaxView/blob/master/Assets/Scripts/OffAxisProjection.cs

[ExecuteInEditMode]
public class OffAxisProjection : MonoBehaviour
{
	private const float METERS_PER_INCH = 0.0254f;
	private Camera mainCamera;

	private float left, right, bottom, top, near, far;
	private float dleft, dright, dbottom, dtop;

	public float border = 0.003f;

	void Start(){
		mainCamera = GameObject.Find("MainCamera").GetComponent<Camera>();
		mainCamera.transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, -1));
		
		dleft = 0f;
		dright = 0.124f;
		dbottom = -0.035f;
		dtop = 0.035f;

		if (Application.isEditor){
		} else {
			// Camera center left in landscape mode
			var widthInMeters = METERS_PER_INCH * Screen.width / Screen.dpi;	
			var heightInMeters = METERS_PER_INCH * Screen.height / Screen.dpi;
			dleft = 0f;
			dright = widthInMeters;
			dbottom = - heightInMeters / 2;
			dtop = heightInMeters / 2;
		}
	}

	void LateUpdate()
	{

		// Vector3 deviceCamPos = mainCamera.transform.worldToLocalMatrix.MultiplyPoint(deviceCameraPosition.transform.position); // find device camera in rendering camera's view space
		Vector3 deviceCamPos = mainCamera.transform.worldToLocalMatrix.MultiplyPoint(Vector3.zero); // find device camera in rendering camera's view space
		Vector3 fwd = mainCamera.transform.worldToLocalMatrix.MultiplyVector (new Vector3(0, 0, 1)); // normal of plane defined by device camera
		Plane device_plane = new Plane( fwd, deviceCamPos);

		Vector3 close = device_plane.ClosestPointOnPlane (Vector3.zero);
		near = close.magnitude;

		left = deviceCamPos.x + dleft - border;
		right = deviceCamPos.x + dright + border;
		top = deviceCamPos.y + dtop + border;
		bottom = deviceCamPos.y + dbottom - border;

		far = 10f; // may need bigger for bigger scenes, max 10 metres for now

		Vector3 topLeft = new Vector3 (left, top, near);
		Vector3 topRight = new Vector3 (right, top, near);
		Vector3 bottomLeft = new Vector3 (left, bottom, near);
		Vector3 bottomRight = new Vector3 (right, bottom, near);

		// move near to 0.01 (1 cm from eye)
		float scale_factor = 0.01f / near;
		near *= scale_factor;
		left *= scale_factor;
		right *= scale_factor;
		top *= scale_factor;
		bottom *= scale_factor;

		Matrix4x4 m = PerspectiveOffCenter(left, right, bottom, top, near, far);
		mainCamera.projectionMatrix = m;
	}

	static Matrix4x4 PerspectiveOffCenter(float left, float right, float bottom, float top, float near, float far)
	{
		float x = 2.0F * near / (right - left);
		float y = 2.0F * near / (top - bottom);
		float a = (right + left) / (right - left);
		float b = (top + bottom) / (top - bottom);
		float c = -(far + near) / (far - near);
		float d = -(2.0F * far * near) / (far - near);
		float e = -1.0F;
		Matrix4x4 m = new Matrix4x4();
		m[0, 0] = x;
		m[0, 1] = 0;
		m[0, 2] = a;
		m[0, 3] = 0;
		m[1, 0] = 0;
		m[1, 1] = y;
		m[1, 2] = b;
		m[1, 3] = 0;
		m[2, 0] = 0;
		m[2, 1] = 0;
		m[2, 2] = c;
		m[2, 3] = d;
		m[3, 0] = 0;
		m[3, 1] = 0;
		m[3, 2] = e;
		m[3, 3] = 0;
		return m;
	}
}