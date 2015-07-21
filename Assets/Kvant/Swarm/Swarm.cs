//
// Swarm - gleaming trail lines
//
using UnityEngine;

namespace Kvant
{
    [ExecuteInEditMode, AddComponentMenu("Kvant/Swarm")]
    public class Swarm : MonoBehaviour
    {
        #region Parameters Exposed To Editor

        [SerializeField]
        int _lineCount = 32;

        [SerializeField]
        int _historyLength = 32;

        [SerializeField]
        Vector3 _attractorPosition = Vector3.zero;

        [SerializeField]
        float _spread = 0.2f;

        [SerializeField]
        float _minAcceleration = 0.5f;

        [SerializeField]
        float _maxAcceleration = 1.0f;

        [SerializeField]
        float _damp = 0.5f;

        [SerializeField]
        float _noiseAmplitude = 0.1f;

        [SerializeField]
        float _noiseFrequency = 0.2f;

        [SerializeField]
        float _noiseSpeed = 1.0f;

        [SerializeField]
        float _noiseVariance = 1.0f;

        [SerializeField]
        Vector3 _flow = Vector3.zero;

        public enum ColorMode { Random, Smooth }

        [SerializeField]
        ColorMode _colorMode = ColorMode.Random;

        [SerializeField, ColorUsage(true, true, 0, 8, 0.125f, 3)]
        Color _color1 = Color.white;

        [SerializeField, ColorUsage(true, true, 0, 8, 0.125f, 3)]
        Color _color2 = Color.white;

        [SerializeField]
        float _gradientSteepness = 2.0f;

        [SerializeField]
        int _randomSeed = 0;

        #endregion

        #region Shader And Materials

        [SerializeField] Shader _kernelShader;
        [SerializeField] Shader _lineShader;

        Material _kernelMaterial;
        Material _lineMaterial;

        #endregion

        #region Private Variables And Objects

        RenderTexture _positionBuffer1;
        RenderTexture _positionBuffer2;
        RenderTexture _velocityBuffer1;
        RenderTexture _velocityBuffer2;
        Mesh _mesh;
        bool _needsReset = true;

        int DrawCount {
            get {
                var total = _historyLength * _lineCount;
                if (total < 65000) return _lineCount;
                return total / 65000 + 1;
            }
        }

        int LinesPerDraw {
            get {
                return _lineCount / DrawCount;
            }
        }

        int TotalLineCount {
            get {
                return _lineCount - _lineCount % DrawCount;
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

        void UpdateKernelShader()
        {
            var m = _kernelMaterial;
            m.SetVector("_AttractPos", _attractorPosition);
            m.SetFloat("_Spread", _spread);
            m.SetFloat("_Damp", _damp);
            m.SetVector("_Acceleration", new Vector2(_minAcceleration, _maxAcceleration));
            m.SetVector("_NoiseParams", new Vector4(_noiseFrequency, _noiseAmplitude, _noiseSpeed, _noiseVariance));
            m.SetVector("_Flow", _flow);
            m.SetFloat("_RandomSeed", _randomSeed);
            m.SetTexture("_PositionTex", _positionBuffer1);
            m.SetTexture("_VelocityTex", _velocityBuffer1);
        }

        void SwapGpgpuBuffers()
        {
            var pb = _positionBuffer1;
            var vb = _velocityBuffer1;
            _positionBuffer1 = _positionBuffer2;
            _velocityBuffer1 = _velocityBuffer2;
            _positionBuffer2 = pb;
            _velocityBuffer2 = vb;
        }

        void ResetResources()
        {
            // mesh object
            if (_mesh == null) _mesh = CreateMesh();

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

            // warming up
            UpdateKernelShader();
            InitializeAndPrewarmBuffers();

            _needsReset = false;
        }

        void InitializeAndPrewarmBuffers()
        {
            Graphics.Blit(null, _positionBuffer2, _kernelMaterial, 0);
            Graphics.Blit(null, _velocityBuffer2, _kernelMaterial, 1);

            // Execute the kernel shader repeatedly.
            for (var i = 0; i < 32; i++) {
                SwapGpgpuBuffers();
                UpdateKernelShader();
                Graphics.Blit(_positionBuffer1, _positionBuffer2, _kernelMaterial, 2);
                Graphics.Blit(_velocityBuffer1, _velocityBuffer2, _kernelMaterial, 3);
            }
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
                // Execute the kernel shaders.
                SwapGpgpuBuffers();
                UpdateKernelShader();
                Graphics.Blit(_positionBuffer1, _positionBuffer2, _kernelMaterial, 2);
                Graphics.Blit(_velocityBuffer1, _velocityBuffer2, _kernelMaterial, 3);
            }
            else
            {
                InitializeAndPrewarmBuffers();
            }

            // Draw lines.
            _lineMaterial.SetTexture("_PositionTex", _positionBuffer2);
            _lineMaterial.SetTexture("_VelocityTex", _velocityBuffer2);
            _lineMaterial.SetColor("_Color1", _color1);
            _lineMaterial.SetColor("_Color2", _color2);
            _lineMaterial.SetFloat("_GradExp", _gradientSteepness);

            if (_colorMode == ColorMode.Smooth)
                _lineMaterial.EnableKeyword("COLOR_SMOOTH");
            else
                _lineMaterial.DisableKeyword("COLOR_SMOOTH");

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
