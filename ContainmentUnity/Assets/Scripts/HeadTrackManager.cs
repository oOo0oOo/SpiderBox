using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class HeadTrackManager : MonoBehaviour {
	private GameObject mainCamera;

	private ARFaceManager _faceManager;

    void Start(){
		mainCamera = GameObject.Find("MainCamera");

        // Check if AR is ready (available, installed and started)
        ARSession.stateChanged += OnStateChanged;
    }
	
    void OnStateChanged(ARSessionStateChangedEventArgs evt){
        if (ARSession.state == ARSessionState.Ready){
            var obj = GameObject.Find("AR Session Origin");
			if (obj != null){
            	Debug.Log("AR enabled! Using face tracking for depth illusion.");
				_faceManager = obj.GetComponent<ARFaceManager>();
            	_faceManager.facesChanged += OnFacesChanged;
			}
        }
    }

    public void OnFacesChanged(ARFacesChangedEventArgs args){
        ARFace face = args.updated[0];
        if (face == null){
            face = args.added[0];
        }
        if (face == null){
            return;
        }

		var pos = face.transform.position;

		// Third eye position
		// pos += (face.rightEye.transform.position + face.leftEye.transform.position) / 2;

		mainCamera.transform.position = pos;
    }
}
