using UnityEngine;
using UnityEngine.Rendering;
using Unity.Cinemachine;

namespace UnityEssentials
{
    [RequireComponent(typeof(Camera))]
    public class CameraPhysicalPropertiesController : MonoBehaviour
    {
        [SerializeField] private CameraPresetData _presetData;

        [Space]
        [Range(0, 1)] public float EffectsStrength = 1;
        [Range(0, 1)] public float ZoomMultiplier = 0.5f;
        [Range(0, 1)] public float IsoMultiplier = 0.5f;
        [Range(0, 1)] public float ShutterSpeedMultiplier = 0.5f;

        private Camera _camera;
        private CinemachineCamera _cinemachineCamera;

        private Volume _isoVolume;
        private Volume _zoomVolume;
        private Volume _fovVolume;

        public float FStop { get; private set; }
        public float FocalLength { get; private set; }
        public float ShutterSpeed { get; private set; }
        public int ISO { get; private set; }

        public Vector2 SensorSize => _presetData.SensorSize;
        public float ShutterSpeedUnscaled => 1 - _presetData.ShutterSpeed1OverXRange.Slerp(ShutterSpeedMultiplier, 0.5f);

        public CameraPresetData Preset
        {
            get => _presetData;
            set
            {
                _presetData = value;
                UpdatePhysicalValues();
            }
        }

        public void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.usePhysicalProperties = true;
            _cinemachineCamera = GetComponent<CinemachineCamera>();

            var prefab = ResourceLoader.InstantiatePrefab("UnityEssentials_Prefab_CameraNoiseVolumes", "Noise Volumes", this.gameObject.transform);
            if (prefab != null)
            {
                _isoVolume = prefab.transform.Find("Iso Volume")?.GetComponent<Volume>();
                _zoomVolume = prefab.transform.Find("Zoom Volume")?.GetComponent<Volume>();
                _fovVolume = prefab.transform.Find("Fov Volume")?.GetComponent<Volume>();
            }
        }

        public void OnEnable() =>
            UpdatePhysicalValues();

        public void Update()
        {
            if (_presetData == null)
                return;

            if (_camera == null && _cinemachineCamera == null)
                return;

            // Convert normalized values to physical values
            UpdatePhysicalValues();

            if (_cinemachineCamera != null)
            {
                _cinemachineCamera.Lens.FieldOfView = Camera.FocalLengthToFieldOfView(FocalLength, SensorSize.y);
                _cinemachineCamera.Lens.PhysicalProperties.SensorSize = SensorSize;
                _cinemachineCamera.Lens.PhysicalProperties.Aperture = FStop;
                _cinemachineCamera.Lens.PhysicalProperties.Iso = ISO;
                _cinemachineCamera.Lens.PhysicalProperties.ShutterSpeed = ShutterSpeed;
            }
            else
            {
                _camera.fieldOfView = Camera.FocalLengthToFieldOfView(FocalLength, SensorSize.y);
                _camera.sensorSize = SensorSize;
                _camera.focalLength = FocalLength;
                _camera.aperture = FStop;
                _camera.iso = ISO;
                _camera.shutterSpeed = ShutterSpeedUnscaled;
            }

            _isoVolume.weight = IsoMultiplier * EffectsStrength * GetIsoNoiseWeight();
            _zoomVolume.weight = ZoomMultiplier * EffectsStrength;

            var focalLengthDistortionMultiplier = FocalLength;
            focalLengthDistortionMultiplier = Mathf.Clamp01(focalLengthDistortionMultiplier.Remap(35, 1, 0, 1));

            _fovVolume.weight = _presetData.LensDistortion ? 1 - ZoomMultiplier : 0;
            _fovVolume.weight *= focalLengthDistortionMultiplier * EffectsStrength;
        }

        private void UpdatePhysicalValues()
        {
            if (_presetData == null)
                return;

            // Convert normalized values to physical ranges
            FStop = _presetData.FStopRange.Lerp(ZoomMultiplier);
            ShutterSpeed = ToShutterSpeed(1 - _presetData.ShutterSpeed1OverXRange.Slerp(ShutterSpeedMultiplier, 0.5f));
            ISO = Mathf.RoundToInt(_presetData.ISORange.Lerp(IsoMultiplier));
            FocalLength = Mathf.Max(1, _presetData.FocalLengthRange.Lerp(ZoomMultiplier));
        }

        private float ToShutterSpeed(double timesPerSecond) =>
            (float)(1 / timesPerSecond);

        private float GetIsoNoiseWeight()
        {
            const int minIso = 400;
            const int maxIso = 6400;

            if (ISO <= minIso) return 0;
            if (ISO >= maxIso) return 1;

            return (ISO - minIso) / (float)(maxIso - minIso);
        }
    }
}