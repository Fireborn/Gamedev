using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace CubeGameWorld
{
    /// 
    /// <summary>
    /// A set of models and model positions wrapped by 
    /// </summary>
    /// 
    class ModelData
    {
        #region Private Member Variables

        /// <summary>
        /// A list of models
        /// </summary>
        private List<Model> _models = new List<Model>();

        /// <summary>
        /// A list of positions corresponding to 
        /// </summary>
        private List<List<Vector3>> _modelPositions = new List<List<Vector3>>();

        #endregion

        #region Public Interface 

        /// 
        /// <summary>
        /// Function for adding data to the object to ensure that Models and ModelPositions always
        /// have the same number of elements.
        /// </summary>
        /// 
        /// <param name="model">
        /// The model to be added
        /// </param>
        /// 
        /// <param name="modelPositions">
        /// A list of positions associated with the input model
        /// </param>
        /// 
        public void Add(Model model, List<Vector3> modelPositions)
        {
            _models.Add(model);
            _modelPositions.Add(modelPositions);
        }



        /// 
        /// <summary>
        /// Get the model and model positions by index
        /// </summary>
        /// 
        /// <param name="index">
        /// The index of the model to request data for
        /// </param>
        /// 
        /// <param name="model">
        /// The model data
        /// </param>
        /// 
        /// <param name="positions">
        /// The model positions
        /// </param>
        /// 
        public void Get(int index, out Model model, out List<Vector3> positions)
        {
            if (index >= _models.Count || index < 0)
            {
                throw new ArgumentOutOfRangeException("The input index was not found");
            }

            model = _models[index];
            positions = _modelPositions[index];
        }


        /// <summary>
        /// The number of models stored in the object
        /// </summary>
        public int Count { get { return _models.Count; } }

        
        /// 
        /// <summary>
        /// Get a model by index
        /// </summary>
        /// 
        /// <param name="index">
        /// The index of the model
        /// </param>
        /// 
        /// <returns>
        /// The model object at the input index
        /// </returns>
        /// 
        public Model Models(int index)
        { 
            return _models[index];
        }


        /// 
        /// <summary>
        /// Get a list of model positions by index
        /// </summary>
        /// 
        /// <param name="index">
        /// The index of the model positions to get
        /// </param>
        /// 
        /// <returns>
        /// A list of model positions corresponding to the model stored at the input index
        /// </returns>
        /// 
        public List<Vector3> ModelPositions(int index)
        {
            return _modelPositions[index];
        }



        /// 
        /// <summary>
        /// Find the index of a model stored within this object
        /// </summary>
        /// 
        /// <param name="model">
        /// The model to search for
        /// </param>
        /// 
        /// <returns>
        /// The index of the model, or -1 if the model is not found
        /// </returns>
        /// 
        public int FindIndex(Model model)
        {
            return _models.FindIndex(x => x == model);
        }

        #endregion 
    }
}
