//
// Swarm - transparent flowing lines animation
//
using UnityEngine;

namespace Kvant
{
    [ExecuteInEditMode, AddComponentMenu("Kvant/Swarm")]
    public class Swarm : MonoBehaviour
    {
        #region Basic Configuration

        [SerializeField]
        int _lineCount = 32;

        [SerializeField]
        int _historyLength = 32;

        #endregion

        #region Dynamics Parameters

        [SerializeField]
        float _minAcceleration = 0.5f;

        public float minAcceleration {
            get { return _minAcceleration; }
            set { _minAcceleration = value; }
        }

        [SerializeField]
        float _maxAcceleration = 1.0f;

        public float maxAcceleration {
            get { return _maxAcceleration; }
            set { _maxAcceleration = value; }
        }

        [SerializeField]
        float _damp = 0.5f;

        public float damp {
            get { return _damp; }
            set { _damp = value; }
        }

        #endregion

        #region External Forces

        [SerializeField]
        Vector3 _attractor = Vector3.zero;

        public Vector3 attractor {
            get { return _attractor; }
            set { _attractor = value; }
        }

        [SerializeField]
        float _spread = 0.2f;

        public float spread {
            get { return _spread; }
            set { _spread = value; }
        }

        [SerializeField]
        Vector3 _flow = Vector3.zero;

        public Vector3 flow {
            get { return _flow; }
            set { _flow = value; }
        }

        #endregion

        #region Noise Parameters

        [SerializeField]
        float _noiseAmplitude = 0.1f;

        public float noiseAmplitude {
            get { return _noiseAmplitude; }
            set { _noiseAmplitude = value; }
        }

        [SerializeField]
        float _noiseFrequency = 0.2f;

        public float noiseFrequency {
            get { return _noiseFrequency; }
            set { _noiseFrequency = value; }
        }

        [SerializeField]
        float _noiseSpeed = 1.0f;

        public float noiseSpeed {
            get { return _noiseSpeed; }
            set { _noiseSpeed = value; }
        }

        [SerializeField]
        float _noiseVariance = 1.0f;

        public float noiseVariance {
            get { return _noiseVariance; }
            set { _noiseVariance = value; }
        }

        [SerializeField]
        float _swirlStrength = 0.0f;

        public float swirlStrength {
            get { return _swirlStrength; }
            set { _swirlStrength = value; }
        }

        [SerializeField]
        float _swirlDensity = 1.0f;

        public float swirlDensity {
            get { return _swirlDensity; }
            set { _swirlDensity = value; }
        }

        #endregion

        #region Render Settings

        public enum ColorMode { Random, Smooth }

        [SerializeField]
        ColorMode _colorMode = ColorMode.Random;

        public ColorMode colorMode {
            get { return _colorMode; }
            set { _colorMode = value; }
        }

        [SerializeField, ColorUsage(true, true, 0, 8, 0.125f, 3)]
        Color _color1 = Color.white;

        public Color color1 {
            get { return _color1; }
            set { _color1 = value; }
        }

        [SerializeField, ColorUsage(true, true, 0, 8, 0.125f, 3)]
        Color _color2 = Color.white;

        public Color color2 {
            get { return _color2; }
            set { _color2 = value; }
        }

        [SerializeField]
        float _gradientSteepness = 2.0f;

        public float gradientSteepness {
            get { return _gradientSteepness; }
            set { _gradientSteepness = value; }
        }

        #endregion

        #region Misc Settings

        [SerializeField]
        bool _fixTimeStep = false;

        [SerializeField]
        float _stepsPerSecond = 60;

        [SerializeField]
        int _randomSeed = 0;

        #endregion

        #region Custom Shaders

        [SerializeField] Shader _kernelShader;
        [SerializeField] Shader _lineShader;
        Material _kernelMaterial;
        Material _lineMaterial;

        #endregion

        #region Private Objects And Properties

        RenderTexture _positionBuffer1;
        RenderTexture _positionBuffer2;
        RenderTexture _velocityBuffer1;
        RenderTexture _velocityBuffer2;
        Mesh _mesh;
        bool _needsReset = true;
        float _time;

        // Returns how many draw calls are needed to draw all lines.
        int DrawCount {
            get {
                var total = _historyLength * _lineCount;
                if (total < 65000) return _lineCount;
                return total / 65000 + 1;
            }
        }

        // Returns the actual total number of lines.
        public int TotalLineCount {
            get { return _lineCount - _lineCount % DrawCount; }
        }

        // Returns how many lines in one draw call.
        int LinesPerDraw {
            get {
                return _lineCount / DrawCount;
            }
        }

        #endregion

        #region Resource Management

        public void NotifyConfigChange()
        {
            _needsReset = true;
        }

        Material CreateMaterial(Shader shader)
        {
            var material = new Material(shader);
            material.hideFlags = HideFlags.DontSave;
            return material;
        }

        RenderTexture CreateBuffer(bool forVelocity)
        {
            var format = RenderTextureFormat.ARGBFloat;
            var width = forVelocity ? 1 : _historyLength;
            var buffer = new RenderTexture(width, TotalLineCount, 0, format);
            buffer.hideFlags = HideFlags.DontSave;
            buffer.filterMode = FilterMode.Point;
            buffer.wrapMode = TextureWrapMode.Clamp;
            return buffer;
        }

        Mesh CreateMesh()
        {
            var nx = _historyLength;
            var ny = LinesPerDraw;

            var inx = 1.0f / nx;
            var iny = 1.0f / TotalLineCount;

            // vertex and texcoord array
            var va = new Vector3[nx * ny];
            var ta = new Vector2[nx * ny];

            var offs = 0;
            for (var y = 0; y < ny; y++)
            {
                var v = iny * y;
                for (var x = 0; x < nx; x++)
                {
                    va[offs] = Vector3.zero;
                    ta[offs] = new Vector2(inx * x, v);
                    offs++;
                }
            }

            // index array
            var ia = new int[ny * (nx - 1) * 2];
            offs = 0;
            for (var y = 0; y < ny; y++)
            {
                var vi = y * nx;
                for (var x = 0; x < nx - 1; x++)
                {
                    ia[offs++] = vi++;
                    ia[offs++] = vi;
                }
            }

            // create a mesh object
            var mesh = new Mesh();
            mesh.hideFlags = HideFlags.DontSave;
            mesh.vertices = va;
            mesh.uv = ta;
            mesh.SetIndices(ia, MeshTopology.Lines, 0);
            mesh.Optimize();

            // avoid begin culled
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100);

            return mesh;
        }

        void StepKernel(float time, float deltaTime)
        {
            // GPGPU buffer swap
            var pb = _positionBuffer1;
            var vb = _velocityBuffer1;
            _positionBuffer1 = _positionBuffer2;
            _velocityBuffer1 = _velocityBuffer2;
            _positionBuffer2 = pb;
            _velocityBuffer2 = vb;

            // kernel shader parameters
            var m = _kernelMaterial;
            m.SetVector("_Acceleration", new Vector2(_minAcceleration, _maxAcceleration));
            m.SetFloat("_Damp", _damp);
            m.SetVector("_AttractPos", _attractor);
            m.SetFloat("_Spread", _spread);
            m.SetVector("_Flow", _flow);
            m.SetVector("_NoiseParams", new Vector4(_noiseFrequency, _noiseAmplitude, _noiseSpeed, _noiseVariance));
            m.SetVector("_SwirlParams", new Vector2(_swirlStrength, _swirlDensity));
            m.SetFloat("_RandomSeed", _randomSeed);
            m.SetVector("_TimeParams", new Vector2(time, deltaTime));

            if (_swirlStrength > 0.0f)
                m.EnableKeyword("ENABLE_SWIRL");
            else
                m.DisableKeyword("ENABLE_SWIRL");

            // velocity update
            m.SetTexture("_PositionTex", _positionBuffer1);
            m.SetTexture("_VelocityTex", _velocityBuffer1);
            Graphics.Blit(null, _velocityBuffer2, m, 3);

            // position update
            m.SetTexture("_VelocityTex", _velocityBuffer2);
            Graphics.Blit(null, _positionBuffer2, m, 2);
        }

        void UpdateLineShader()
        {
            var m = _lineMaterial;

            m.SetTexture("_PositionTex", _positionBuffer2);

            m.SetColor("_Color1", _color1);
            m.SetColor("_Color2", _color2);
            m.SetFloat("_GradExp", _gradientSteepness);

            if (_colorMode == ColorMode.Smooth)
                m.EnableKeyword("COLOR_SMOOTH");
            else
                m.DisableKeyword("COLOR_SMOOTH");
        }

        void ResetResources()
        {
            // parameter sanitization
            _lineCount = Mathf.Clamp(_lineCount, 1, 8192);
            _historyLength = Mathf.Clamp(_historyLength, 8, 1024);

            // mesh object
            if (_mesh) DestroyImmediate(_mesh);
            _mesh = CreateMesh();

            // GPGPU buffers
            if (_positionBuffer1) DestroyImmediate(_positionBuffer1);
            if (_positionBuffer2) DestroyImmediate(_positionBuffer2);
            if (_velocityBuffer1) DestroyImmediate(_velocityBuffer1);
            if (_velocityBuffer2) DestroyImmediate(_velocityBuffer2);

            _positionBuffer1 = CreateBuffer(false);
            _positionBuffer2 = CreateBuffer(false);
            _velocityBuffer1 = CreateBuffer(true);
            _velocityBuffer2 = CreateBuffer(true);

            // shader materials
            if (!_kernelMaterial) _kernelMaterial = CreateMaterial(_kernelShader);
            if (!_lineMaterial)   _lineMaterial   = CreateMaterial(_lineShader);

            // buffer initialization
            Graphics.Blit(null, _positionBuffer1, _kernelMaterial, 0);
            Graphics.Blit(null, _velocityBuffer1, _kernelMaterial, 1);

            _needsReset = false;
        }

        #endregion

        #region MonoBehaviour Functions

        void Reset()
        {
            _needsReset = true;
        }

        void OnDestroy()
        {
            if (_mesh) DestroyImmediate(_mesh);
            if (_positionBuffer1) DestroyImmediate(_positionBuffer1);
            if (_positionBuffer2) DestroyImmediate(_positionBuffer2);
            if (_velocityBuffer1) DestroyImmediate(_velocityBuffer1);
            if (_velocityBuffer2) DestroyImmediate(_velocityBuffer2);
            if (_kernelMaterial)  DestroyImmediate(_kernelMaterial);
            if (_lineMaterial)    DestroyImmediate(_lineMaterial);
        }

        void Update()
        {
            if (_needsReset) ResetResources();

            if (Application.isPlaying)
            {
                float deltaTime;
                int steps;

                if (_fixTimeStep)
                {
                    // Fixed time step.
                    deltaTime = 1.0f / _stepsPerSecond;
                    steps = Mathf.RoundToInt(Time.deltaTime * _stepsPerSecond);
                }
                else
                {
                    // Variable time step.
                    deltaTime = Time.smoothDeltaTime;
                    steps = 1;
                }

                // Time steps.
                for (var i = 0; i < steps; i++)
                {
                    _time += deltaTime;
                    StepKernel(_time, deltaTime);
                }
            }
            else
            {
                // Reset simulation state.
                Graphics.Blit(null, _positionBuffer2, _kernelMaterial, 0);
                Graphics.Blit(null, _velocityBuffer2, _kernelMaterial, 1);
                _time = 0;

                // Advance for a short period of time.
                for (var i = 0; i < 32; i++)
                {
                    _time += 0.1f;
                    StepKernel(_time, 0.1f);
                }
            }

            // Draw lines.
            UpdateLineShader();

            var matrix = transform.localToWorldMatrix;
            var stride = LinesPerDraw;
            var total = TotalLineCount;

            var props = new MaterialPropertyBlock();
            var uv = new Vector2(0.5f / _historyLength, 0);

            for (var i = 0; i < total; i += stride)
            {
                uv.y = (0.5f + i) / total;
                props.SetVector("_BufferOffset", uv);
                Graphics.DrawMesh(_mesh, matrix, _lineMaterial, 0, null, 0, props);
            }
        }

        #endregion
    }
}
