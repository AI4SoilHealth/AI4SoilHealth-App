using PublicAPI.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.ML;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using PublicAPI.Common.Controllers;

namespace PublicAPI.Specific.Controllers {
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    /**
        * Predictions controller
        @module PredictionsController
    */
    public class PredictionsController : Controller {
        private readonly AppOptions _options;
        private readonly Db _db;
        private readonly IDistributedCache _cache;
        private readonly ILogger<PredictionsController> _logger;
        private readonly AuthService _authService;
        private readonly CurrentUserInfo _currentUserInfo;
        private readonly MLContext _mlContext = new();

        public PredictionsController(IOptions<AppOptions> options, Db db, IDistributedCache cache, ILogger<PredictionsController> logger, AuthService authService, CurrentUserInfo currentUserInfo) {
            _options = options.Value;
            _db = db;
            _cache = cache;
            _logger = logger;
            _authService = authService;
            _currentUserInfo = currentUserInfo;
        }

        /**
        * Get Onnx model session asynchronously 
        * @param {string} modelFile - model filepath
        * @return {Task<OnnxScoringEstimator>} - session with loaded model
        */
        [NonAction]
        public async Task<InferenceSession> GetOnnxSessionAsync(byte[] modelData) {
            return await Task.Run(() =>
            {
                InferenceSession session = new(modelData);
                return session;
            });
        }

        /**
        * Get features as Tensor
        * @param {NDList} features - features
        * @return {DataView} - features Tensor
        */
        [NonAction]
        public Tensor<float> GetFeaturesTensor(PredictingData data) {
            float[] flattenedFeatures = data.features.SelectMany(row => row).ToArray();
            int[] dimensions = [data.features.Length, data.features[0].Length];
            var tensor = new DenseTensor<float>(flattenedFeatures, dimensions);
            return tensor;
        }


        /**
        * Get predictions for a session with loaded model and features Tensor asynchronously
        * @param {ITransformer} session - session with loaded model
        * @param {DataView} featuresDataView - features Tensor
        * @return {Task<float[]>} - predictions
        */
        [NonAction]
        public async Task<float[]> PredictAsync(InferenceSession session, Tensor<float> featuresTensor) {
            return await Task.Run(() =>
            {
                using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(
                    [NamedOnnxValue.CreateFromTensor("input", featuresTensor)]);
                float[] predictions = results
                    .FirstOrDefault(result => result.Name == "output")
                    .AsTensor<float>()
                    .ToArray();
                return predictions;
            });
        }

        /**
            * Get routes for the current user
            * @return {ActionResult} - List of routes
        */
        [HttpGet("GetModelsForDataSource")]
        public async Task<ActionResult> GetModelsForDataSource(int dataSourceId) {
            var ret = await _db.QueryJsonAsyncCached("data.get_prediction_models_for_data_source", new { DataSourceId = dataSourceId, _currentUserInfo.LangId }, _options.ConnectionString, "long");
            return Ok(ret);
        }

        /**
            * Get predicted values
            * @param {int} modelId - Model id
            * @return {ActionResult} - Predicted values
        */
        [HttpPost("GetPredictedValues/{modelId}")]
        public async Task<ActionResult> GetPredictedValues(int modelId, [FromBody] PredictingData data) {
            if (data == null || data.features == null || data.features.Length == 0) {
                return BadRequest();
            }
            // Get model from database by modelID
            var modelData = await _db.QuerySingleAsync<byte[]>("data.get_prediction_model_data", new { PredictionModelId = modelId }, _options.ConnectionString);
            try {
                using InferenceSession session = await GetOnnxSessionAsync(modelData);
                Tensor<float> featuresTensor = GetFeaturesTensor(data);
                float[] predictions = await PredictAsync(session, featuresTensor);
                return Ok(predictions);
            } catch (Exception ex) {
                return BadRequest($"Failed to load the model: {ex.Message}");
            }
        }
    }

    /**
    * Predicting data
    * @typedef {Object} PredictingData
    * @property {float[][]} features - features 2D array, each feature is an array of floats
    */
    public class PredictingData {
        public float[][] features { get; set; }
    }

}
