using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.ObjectModel;

namespace CubeGameWorld
{
    /// 
    /// <summary>
    /// This class wraps lists of render data to ensure that no 1 list becomes out of sync with the other lists.  This
    /// structure of lists rather than lists of structures format should help with cache coherency
    /// </summary>
    /// 
    class RenderData
    {
        #region Private Member Variables

        /// <summary>
        /// A list of already generated vertex buffers ready to be used for rendering
        /// </summary>
        private List<VertexBuffer> _vertexBuffers = new List<VertexBuffer>();

        /// <summary>
        /// A list of already generated index buffers (1 to 1 correspondence to _vertexBuffers) ready to be used for rendering
        /// </summary>
        private List<IndexBuffer> _indexBuffers = new List<IndexBuffer>();

        /// <summary>
        /// A list of already generated models (1 to 1 correspondence to _vertexBuffers) ready to be used for rendering
        /// </summary>
        private List<Model> _models = new List<Model>();

        #endregion

        #region Public Interface 

        /// 
        /// <summary>
        /// Add data to the list of render data
        /// </summary>
        /// 
        /// <param name="vertexBuffer">
        /// The vertex buffer to add
        /// </param>
        /// 
        /// <param name="indexBuffer">
        /// The index buffer corresponding to the input vertex buffer
        /// </param>
        /// 
        /// <param name="model">
        /// The model corresponding to the input vertex buffer
        /// </param>
        /// 
        public void AddData(VertexBuffer vertexBuffer, IndexBuffer indexBuffer, Model model)
        {
            _vertexBuffers.Add(vertexBuffer);
            _indexBuffers.Add(indexBuffer);
            _models.Add(model);
        }



        /// 
        /// <summary>
        /// Get an immutable copy of the current render data.  The three output lists are guaranteed to have the same .Count
        /// </summary>
        /// 
        /// <param name="vertexBuffers">
        /// The curent vertex buffers
        /// </param>
        /// 
        /// <param name="indexBuffers">
        /// The current index buffers corresponding to the vertex buffers
        /// </param>
        /// 
        /// <param name="models">
        /// The current models corresponding to the vertex buffers
        /// </param>
        /// 
        public void GetData(out ReadOnlyCollection<VertexBuffer> vertexBuffers, out ReadOnlyCollection<IndexBuffer> indexBuffers, out ReadOnlyCollection<Model> models)
        {
            vertexBuffers = new ReadOnlyCollection<VertexBuffer>(_vertexBuffers);
            indexBuffers = new ReadOnlyCollection<IndexBuffer>(_indexBuffers);
            models = new ReadOnlyCollection<Model>(_models);
        }

        #endregion
    }
}
