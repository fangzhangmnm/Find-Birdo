using UnityEngine;
using UnityEngine.XR;
/* Credit
 * https://github.com/wacki/Unity-VRInputModule
 * MIT License
 */
namespace Wacki {
    public class IUILaserPointer : MonoBehaviour {

        public float laserThickness = 0.002f;
        public float laserHitScale = 0.02f;
        public Color color;
        public XRNode whichHand;

        public LayerMask UILayer = 1<<5;

        private GameObject hitPoint;
        private GameObject pointer;
        


        private float _distanceLimit;

        // Use this for initialization
        void Start()
        {
            // todo:    let the user choose a mesh for laser pointer ray and hit point
            //          or maybe abstract the whole menu control some more and make the 
            //          laser pointer a module.
            pointer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pointer.transform.SetParent(transform, false);
            pointer.transform.localScale = new Vector3(laserThickness, laserThickness, 100.0f);
            pointer.transform.localPosition = new Vector3(0.0f, 0.0f, 50.0f);

            hitPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hitPoint.transform.SetParent(transform, false);
            hitPoint.transform.localScale = new Vector3(laserHitScale, laserHitScale, laserHitScale);
            hitPoint.transform.localPosition = new Vector3(0.0f, 0.0f, 100.0f);

            pointer.GetComponent<MeshRenderer>().material.color = color;
            hitPoint.GetComponent<MeshRenderer>().material.color = color;

            // remove the colliders on our primitives
            DestroyImmediate(hitPoint.GetComponent<SphereCollider>());
            DestroyImmediate(pointer.GetComponent<BoxCollider>());

            pointer.SetActive(false);
            hitPoint.SetActive(false);

            
            // initialize concrete class
            Initialize();
            
            // register with the LaserPointerInputModule
            if(LaserPointerInputModule.instance == null) {
                new GameObject("[Event System]").AddComponent<LaserPointerInputModule>();
            }
            

            LaserPointerInputModule.instance.AddController(this);
        }

        void OnDestroy()
        {
            if(LaserPointerInputModule.instance != null)
                LaserPointerInputModule.instance.RemoveController(this);
        }

        protected virtual void Initialize() { }

        bool inControl = false;
        public virtual void OnEnterControl(GameObject control) {
            inControl = true;
        }

        public virtual void OnExitControl(GameObject control)
        {
            inControl = false;
            hitPoint.SetActive(false);
            pointer.SetActive(false);
        }

        public void OnDisable()
        {
            hitPoint.SetActive(false);
            pointer.SetActive(false);
        }

        public bool ButtonDown()
        {
            return trigger > .5f;
        }

        public bool ButtonUp()
        {
            return !ButtonDown();
        }

        public bool _hasButtonDown=false;
        float distance = 100f;
        public void SetLaserDistance(float value) { distance = value; }
        float trigger = 0;

        protected virtual void Update()
        {
            if (isActiveAndEnabled)
            {
                trigger = 0;
                InputDevice device = InputDevices.GetDeviceAtXRNode(whichHand);
                if (device != null)
                {
                    if (!device.TryGetFeatureValue(CommonUsages.trigger, out trigger)) trigger = 0;
                }
                if (whichHand == XRNode.RightHand && Input.GetButton("Fire1"))
                    trigger =1;
            }


            if (inControl && isActiveAndEnabled)
            {
                bool bHit = distance > 0;

                pointer.transform.localScale = new Vector3(laserThickness, laserThickness, distance)/transform.lossyScale.x;
                pointer.transform.localPosition = new Vector3(0.0f, 0.0f, distance * 0.5f) / transform.lossyScale.x;
                hitPoint.SetActive(true);
                pointer.SetActive(true);

                if (bHit)
                {
                    hitPoint.SetActive(true);
                    hitPoint.transform.localPosition = new Vector3(0.0f, 0.0f, distance) / transform.lossyScale.x;
                }
                else
                {
                    hitPoint.SetActive(false);
                }

            }
        }
    }

}