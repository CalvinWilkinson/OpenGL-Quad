using System;
using System.IO;
using System.Reflection;
using Silk.NET.OpenGL;
// ReSharper disable InconsistentNaming

namespace OpenGLQuad
{
    public class ShaderProgram : IDisposable
    {
        private static readonly string BaseDirPath = @$"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\";
        private readonly uint _vertexShaderId;
        private readonly uint _fragmentShaderId;
        private bool _isDisposed;
        private readonly GL _gl;
        private readonly uint _batchSize;

        /// <summary>
        /// Creates a new instance of <see cref="ShaderProgram"/>.
        /// </summary>
        /// <param name="openGL">The OpenGL object to call OpenGL functions.</param>
        /// <param name="vertexShaderName">The name of the vertex shader.</param>
        /// <param name="fragmentShaderName">The name of the fragment shader.</param>
        /// <exception cref="Exception">Thrown if there is an issue create the shader program.</exception>
        public ShaderProgram(GL openGL, string vertexShaderName, string fragmentShaderName, uint batchSize)
        {
            _gl = openGL;
            _batchSize = batchSize;

            _gl.BeginGroup("Create Shader");

            _vertexShaderId = LoadVertShader(vertexShaderName);
            _fragmentShaderId = LoadFragShader(fragmentShaderName);

            Id = _gl.CreateProgram();
            _gl.LabelShaderProgram(Id, "GPUBuffer Shader");

            _gl.AttachShader(Id, _vertexShaderId);
            _gl.AttachShader(Id, _fragmentShaderId);
            _gl.LinkProgram(Id);
            _gl.GetProgram(Id, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                throw new Exception($"Error linking shader {_gl.GetProgramInfoLog(Id)}");
            }

            _gl.ValidateProgram(Id);

            // Check for linking errors
            _gl.GetProgram(Id, ProgramPropertyARB.LinkStatus, out var progParams);

            _gl.EndGroup();

            if (progParams > 0) return;

            // We can use `this.gl.GetProgramInfoLog(program)` to get information about the error.
            var programInfoLog = _gl.GetProgramInfoLog(Id);

            throw new Exception($"Error occurred while linking program with ID '{Id}'\n{programInfoLog}");
        }

        public uint Id { get; }

        ~ShaderProgram() => Dispose();

        public void Use() => _gl.UseProgram(Id);

        public void StopUsing() => _gl.UseProgram(0);

        private uint LoadVertShader(string name)
        {
            _gl.BeginGroup("Create Vertex Shader");

            name = Path.HasExtension(name)
                ? Path.GetFileNameWithoutExtension(name)
                : name;

            var fullFilepath = $"{BaseDirPath}{name}.vert";

            var shaderSrc = File.ReadAllText(fullFilepath);

            // Find and replace the batch size
            shaderSrc = shaderSrc.Replace("${{ BATCH_SIZE }}", _batchSize.ToString());

            var shaderId = _gl.CreateShader(ShaderType.VertexShader);
            _gl.LabelShader(shaderId, "Vertex Shader");

            _gl.ShaderSource(shaderId, shaderSrc);
            _gl.CompileShader(shaderId);

            _gl.GetShader(shaderId, GLEnum.CompileStatus, out var status);

            if (status == 0)
            {
                throw new Exception("Error compiling vertex shader");
            }

            //Checking the shader for compilation errors.
            var infoLog = _gl.GetShaderInfoLog(shaderId);
            if (!string.IsNullOrWhiteSpace(infoLog))
            {
                throw new Exception($"Error compiling vertex shader '{name}'\n{infoLog}");
            }

            _gl.EndGroup();

            return shaderId;
        }

        private uint LoadFragShader(string name)
        {
            _gl.BeginGroup("Create Fragment Shader");

            name = Path.HasExtension(name)
                ? Path.GetFileNameWithoutExtension(name)
                : name;

            var fullFilepath = $"{BaseDirPath}{name}.frag";
            var shaderSrc = File.ReadAllText(fullFilepath);

            var shaderId = _gl.CreateShader(ShaderType.FragmentShader);
            _gl.LabelShader(shaderId, "Fragment Shader");

            _gl.ShaderSource(shaderId, shaderSrc);
            _gl.CompileShader(shaderId);

            _gl.GetShader(shaderId, GLEnum.CompileStatus, out var status);

            if (status == 0)
            {
                throw new Exception("Error compiling fragment shader");
            }

            //Checking the shader for compilation errors.
            var infoLog = _gl.GetShaderInfoLog(shaderId);
            if (!string.IsNullOrWhiteSpace(infoLog))
            {
                throw new Exception($"Error compiling fragment shader '{name}'\n{infoLog}");
            }

            _gl.EndGroup();

            return shaderId;
        }

        /// <summary>
        /// <inheritdoc cref="IDisposable.Dispose"/>.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed is false)
            {
                return;
            }

            StopUsing();
            _gl.DetachShader(Id, _vertexShaderId);
            _gl.DetachShader(Id, _fragmentShaderId);
            _gl.DeleteShader(_vertexShaderId);
            _gl.DeleteShader(_fragmentShaderId);
            _gl.DeleteProgram(Id);

            _isDisposed = true;

            GC.SuppressFinalize(this);
        }
    }
}
