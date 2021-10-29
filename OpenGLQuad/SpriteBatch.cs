using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Silk.NET.OpenGL;

namespace OpenGLQuad
{
    public class SpriteBatch
    {
        private readonly GPUBuffer _gpuBuffer;
        private bool _batchHasBegun;
        private readonly Dictionary<uint, BatchItem> _batchItems = new ();
        private readonly uint _batchSize;
        private uint _currentBatchIndex;
        private readonly GL _gl;
        private readonly ShaderProgram _shader;

        public SpriteBatch(GL gl, GPUBuffer gpuBuffer, ShaderProgram shader, uint batchSize)
        {
            _gl = gl;
            _gpuBuffer = gpuBuffer;
            _shader = shader;
            _batchSize = batchSize;

            for (var i = 0u; i < _batchSize; i++)
            {
                _batchItems.Add(i, BatchItem.Create());
            }
        }

        public void Begin()
        {
            _batchHasBegun = true;
        }

        public void RenderRectangle(Rectangle rect)
        {
            if (_batchHasBegun is false)
            {
                throw new Exception("The batch begin() method must be invoked first.");
            }

            // Is the batch full
            if (_currentBatchIndex >= _batchSize)
            {
                RenderBatch();
            }

            BatchItem newBatchItem = default;
            newBatchItem.Position = rect.Position;
            newBatchItem.Width = rect.Width;
            newBatchItem.Height = rect.Height;
            newBatchItem.Color = rect.Color;

            _batchItems[_currentBatchIndex] = newBatchItem;

            _currentBatchIndex += 1;
        }

        private void RenderBatch()
        {
            if (_batchItems.All(i => i.Value.IsEmpty()))
            {
                return;
            }

            _shader.Use();
            _gpuBuffer.BindVAO();

            // Update all of the data
            for (var i = 0u; i < _batchItems.Count; i++)
            {
                var item = _batchItems[i];

                if (item.IsEmpty() is false)
                {
                    _gpuBuffer.UpdateTransformData(item.Position, item.Width, item.Height, i);
                    _gpuBuffer.UpdateVertexData(item.Color, i);
                }
            }

            unsafe
            {
                // Draw the triangle using the GL_TRIANGLES primitive
                _gl.DrawElements(PrimitiveType.Triangles, _gpuBuffer.TotalIndices, DrawElementsType.UnsignedInt, (void*)0);
            }

            // Reset/clear all of the items in the batch
            foreach (var batchItem in _batchItems.Values)
            {
                batchItem.Empty();
            }

            _currentBatchIndex = 0;
        }

        public void End()
        {
            RenderBatch();
            _batchHasBegun = false;
        }
    }
}
