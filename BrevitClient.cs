/*
 * =================================================================================
 * BREVIT.NET
 *
 * A high-performance, type-safe .NET library for semantically compressing
 * and optimizing data before sending it to a Large Language Model (LLM).
 *
 * Project: Brevit
 * Author: Javian
 * Version: 0.1.0
 *
 * This library is designed to be lightweight, fast, and extensible,
 * using modern .NET features to dramatically reduce LLM token costs.
 * =================================================================================
 */

#nullable enable
namespace Brevit.NET
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Threading.Tasks;

    #region == Public Interfaces & Enums ==

    /// <summary>
    /// Specifies the optimization strategy for JSON data.
    /// </summary>
    public enum JsonOptimizationMode
    {
        /// <summary>
        /// No optimization. The JSON is passed as-is.
        /// </summary>
        None,

        /// <summary>
        /// Flattens the JSON into a 'key.path: value' format.
        /// E.g., {"user": {"name": "J" }} -> "user.name: J"
        /// This is the default and most powerful mode.
        /// </summary>
        Flatten,

        /// <summary>
        /// Converts the JSON to YAML, which is more token-efficient.
        /// </summary>
        ToYaml,

        /// <summary>
        /// Filters the JSON to only include fields specified in BrevitConfig.
        /// </summary>
        Filter
    }

    /// <summary>
    /// Specifies the optimization strategy for long unstructured text.
    /// </summary>
    public enum TextOptimizationMode
    {
        /// <summary>
        /// No optimization. The text is passed as-is.
        /// </summary>
        None,

        /// <summary>
        /// Removes common boilerplate, excessive whitespace, and signatures.
        /// </summary>
        Clean,

        /// <summary>
        /// Uses a fast, cheap model to create a summary.
        /// (Requires an ITextOptimizer implementation).
        /// </summary>
        SummarizeFast,

        /// <summary>
        /// Uses a high-quality model to create a summary.
        /// (Requires an ITextOptimizer implementation).
        /// </summary>
        SummarizeHighQuality
    }

    /// <summary>
    /// Specifies the optimization strategy for image data.
    /// </summary>
    public enum ImageOptimizationMode
    {
        /// <summary>
        /// Do not process the image. The byte array will be ignored.
        /// </summary>
        None,

        /// <summary>
        /// Performs Optical Character Recognition (OCR) on the image.
        /// (Requires an IImageOptimizer implementation).
        /// </summary>
        Ocr,

        /// <summary>
        /// Extracts simple metadata (filename, size).
        /// </summary>
        Metadata
    }

    /// <summary>
    /// The core configuration object for the BrevitClient.
    /// This defines the rules for the optimization pipeline.
    /// </summary>
    public record BrevitConfig(
        JsonOptimizationMode JsonMode = JsonOptimizationMode.Flatten,
        TextOptimizationMode TextMode = TextOptimizationMode.Clean,
        ImageOptimizationMode ImageMode = ImageOptimizationMode.Ocr
    )
    {
        /// <summary>
        /// A list of JSON property paths to keep when using Filter mode.
        /// E.g., "user.name", "order.details.item_id"
        /// </summary>
        public List<string> JsonPathsToKeep { get; init; } = new();

        /// <summary>
        /// The maximum number of text characters to consider "short text".
        /// Anything over this will be processed by the TextOptimizer.
        /// </summary>
        public int LongTextThreshold { get; init; } = 500;
    }

    #endregion

    #region == Extensibility: Service Interfaces ==

    /// <summary>
    /// Interface for a service that optimizes JSON strings.
    /// </summary>
    public interface IJsonOptimizer
    {
        /// <summary>
        /// Optimizes a JSON string based on the configuration.
        /// </summary>
        /// <returns>An optimized string representation.</returns>
        Task<string> OptimizeJsonAsync(string jsonString, BrevitConfig config);
    }

    /// <summary>
    /// Interface for a service that optimizes unstructured text.
    /// This is where you would plug in Microsoft.SemanticKernel.
    /// </summary>
    public interface ITextOptimizer
    {
        /// <summary>
        /// Optimizes a block of long text.
        /// </summary>
        /// <returns>An optimized string (e.g., a summary).</returns>
        Task<string> OptimizeTextAsync(string longText, BrevitConfig config);
    }

    /// <summary>
    /// Interface for a service that optimizes image data.
    /// This is where you would plug in Azure.AI.Vision.
    /// </summary>
    public interface IImageOptimizer
    {
        /// <summary>
        /// Optimizes image data (e.g., by performing OCR).
        /// </summary>
        /// <param name="imageData">The raw bytes of the image.</param>
        /// <returns>A string representation of the image content.</returns>
        Task<string> OptimizeImageAsync(byte[] imageData, BrevitConfig config);
    }

    /// <summary>
    /// Data structure analysis result for automatic strategy selection.
    /// </summary>
    public class DataAnalysis
    {
        public string Type { get; set; } = "unknown";
        public int Depth { get; set; }
        public bool HasUniformArrays { get; set; }
        public bool HasPrimitiveArrays { get; set; }
        public bool HasNestedObjects { get; set; }
        public int TextLength { get; set; }
        public int ArrayCount { get; set; }
        public int ObjectCount { get; set; }
        public string Complexity { get; set; } = "simple";
    }

    /// <summary>
    /// Interface for extensible optimization strategies.
    /// </summary>
    public interface IOptimizationStrategy
    {
        /// <summary>
        /// Analyzes data and returns a score (0-100) indicating how well this strategy fits.
        /// </summary>
        int Analyze(object data, DataAnalysis analysis);
        
        /// <summary>
        /// Optimizes the data using this strategy.
        /// </summary>
        Task<string> OptimizeAsync(object data, BrevitConfig config);
    }

    #endregion

    #region == Core Class: BrevitClient ==

    /// <summary>
    /// The main client for the Brevit.NET library.
    /// This class orchestrates the optimization pipeline.
    ///
    /// Use this class by Dependency Injection (DI) by registering
    /// BrevitClient and its required I...Optimizer services.
    /// </summary>
    public class BrevitClient
    {
        private readonly BrevitConfig _config;
        private readonly IJsonOptimizer _jsonOptimizer;
        private readonly ITextOptimizer _textOptimizer;
        private readonly IImageOptimizer _imageOptimizer;
        private readonly Dictionary<string, IOptimizationStrategy> _strategies;

        /// <summary>
        /// Creates a new instance of the BrevitClient.
        /// </summary>
        public BrevitClient(
            BrevitConfig config,
            IJsonOptimizer jsonOptimizer,
            ITextOptimizer textOptimizer,
            IImageOptimizer imageOptimizer)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _jsonOptimizer = jsonOptimizer ?? throw new ArgumentNullException(nameof(jsonOptimizer));
            _textOptimizer = textOptimizer ?? throw new ArgumentNullException(nameof(textOptimizer));
            _imageOptimizer = imageOptimizer ?? throw new ArgumentNullException(nameof(imageOptimizer));
            _strategies = new Dictionary<string, IOptimizationStrategy>();
        }

        /// <summary>
        /// The primary method. Optimizes any raw C# object, JSON string,
        /// or data (text, image bytes) into a token-efficient string.
        /// </summary>
        /// <param name="rawData">The data to optimize. Can be a POCO,
        /// JSON string, text string, or byte[] for an image.</param>
        /// <param name="intent">
        /// (Optional) A hint about the user's goal, which can
        /// help the optimizers make better decisions.
        /// </param>
        /// <returns>A single string optimized for an LLM prompt.</returns>
        public async Task<string> OptimizeAsync(object rawData, string? intent = null)
        {
            return rawData switch
            {
                // Case 1: Raw JSON string
                string s when IsJson(s) =>
                    await _jsonOptimizer.OptimizeJsonAsync(s, _config),

                // Case 2: Long unstructured text
                string s when s.Length > _config.LongTextThreshold =>
                    await _textOptimizer.OptimizeTextAsync(s, _config),

                // Case 3: Short text (no optimization needed)
                string s => s,

                // Case 4: Image data
                byte[] imgBytes =>
                    await _imageOptimizer.OptimizeImageAsync(imgBytes, _config),

                // Case 5: A Plain Old C# Object (POCO)
                // This is a powerful feature! We serialize it and run it
                // through the JSON optimizer.
                object poco =>
                    await HandlePocoAsync(poco, _config),

                // Default case
                _ => rawData?.ToString() ?? string.Empty
            };
        }

        /// <summary>
        /// Handles the serialization and optimization of a C# object.
        /// </summary>
        private async Task<string> HandlePocoAsync(object poco, BrevitConfig config)
        {
            try
            {
                // Serialize the object to JSON.
                // We use System.Text.Json for performance.
                string jsonString = JsonSerializer.Serialize(poco, new JsonSerializerOptions
                {
                    WriteIndented = false // Save tokens
                });

                // Now, optimize the JSON we just created.
                return await _jsonOptimizer.OptimizeJsonAsync(jsonString, config);
            }
            catch (Exception ex)
            {
                // Log the serialization error
                Console.Error.WriteLine($"[Brevit] Failed to serialize POCO: {ex.Message}");
                return $"[Error: Could not process object {poco.GetType().Name}]";
            }
        }

        /// <summary>
        /// Analyzes data structure to determine the best optimization strategy.
        /// </summary>
        private DataAnalysis AnalyzeDataStructure(object data)
        {
            var analysis = new DataAnalysis
            {
                Type = "unknown",
                Depth = 0,
                HasUniformArrays = false,
                HasPrimitiveArrays = false,
                HasNestedObjects = false,
                TextLength = 0,
                ArrayCount = 0,
                ObjectCount = 0,
                Complexity = "simple"
            };

            void AnalyzeNode(JsonNode? node, int depth = 0)
            {
                if (node == null) return;
                
                analysis.Depth = Math.Max(analysis.Depth, depth);

                if (node is JsonObject obj)
                {
                    analysis.ObjectCount++;
                    if (depth > 0) analysis.HasNestedObjects = true;

                    foreach (var property in obj)
                    {
                        if (property.Value != null)
                        {
                            AnalyzeNode(property.Value, depth + 1);
                        }
                    }
                }
                else if (node is JsonArray arr)
                {
                    analysis.ArrayCount++;

                    // Check for uniform object arrays
                    var (keys, isUniform) = AnalyzeArrayUniformity(arr);
                    if (isUniform) analysis.HasUniformArrays = true;

                    // Check for primitive arrays
                    if (AnalyzeArrayPrimitives(arr)) analysis.HasPrimitiveArrays = true;

                    // Analyze each element
                    for (int i = 0; i < arr.Count; i++)
                    {
                        AnalyzeNode(arr[i], depth + 1);
                    }
                }
                else if (node is JsonValue val)
                {
                    var str = val.ToString();
                    analysis.TextLength += str.Length;
                }
            }

            // Convert object to JsonNode for analysis
            try
            {
                string jsonString = data is string s ? s : JsonSerializer.Serialize(data);
                var node = JsonNode.Parse(jsonString);
                if (node != null)
                {
                    AnalyzeNode(node);
                }
            }
            catch
            {
                // If serialization fails, use basic type detection
                analysis.Type = data.GetType().Name;
            }

            // Determine complexity
            if (analysis.Depth > 3 || analysis.ArrayCount > 5 || analysis.ObjectCount > 10)
            {
                analysis.Complexity = "complex";
            }
            else if (analysis.Depth > 1 || analysis.ArrayCount > 0 || analysis.ObjectCount > 3)
            {
                analysis.Complexity = "moderate";
            }

            // Determine type
            string type = data switch
            {
                string s when s.Length > _config.LongTextThreshold => "longText",
                string => "text",
                byte[] => "image",
                JsonArray => "array",
                JsonObject => "object",
                _ => "object"
            };

            analysis.Type = type;
            return analysis;
        }

        /// <summary>
        /// Selects the best optimization strategy based on data analysis.
        /// </summary>
        private (string name, JsonOptimizationMode? jsonMode, TextOptimizationMode? textMode, ImageOptimizationMode? imageMode, int score, string reason) SelectOptimalStrategy(DataAnalysis analysis)
        {
            var strategies = new List<(string name, JsonOptimizationMode? jsonMode, TextOptimizationMode? textMode, ImageOptimizationMode? imageMode, int score, string reason)>();

            // Strategy 1: Flatten with tabular optimization
            if (analysis.HasUniformArrays || analysis.HasPrimitiveArrays)
            {
                strategies.Add((
                    "Flatten",
                    JsonOptimizationMode.Flatten,
                    null,
                    null,
                    analysis.HasUniformArrays ? 100 : 80,
                    analysis.HasUniformArrays
                        ? "Uniform object arrays detected - tabular format optimal"
                        : "Primitive arrays detected - comma-separated format optimal"
                ));
            }

            // Strategy 2: Standard flatten
            if (analysis.HasNestedObjects || analysis.Complexity == "moderate")
            {
                strategies.Add((
                    "Flatten",
                    JsonOptimizationMode.Flatten,
                    null,
                    null,
                    70,
                    "Nested objects detected - flatten format optimal"
                ));
            }

            // Strategy 3: Text optimization
            if (analysis.Type == "longText")
            {
                strategies.Add((
                    "TextOptimization",
                    null,
                    _config.TextMode,
                    null,
                    90,
                    "Long text detected - summarization recommended"
                ));
            }

            // Strategy 4: Image optimization
            if (analysis.Type == "image")
            {
                strategies.Add((
                    "ImageOptimization",
                    null,
                    null,
                    _config.ImageMode,
                    100,
                    "Image data detected - OCR recommended"
                ));
            }

            // Select highest scoring strategy
            if (strategies.Count == 0)
            {
                return ("Flatten", JsonOptimizationMode.Flatten, null, null, 50, "Default flatten strategy");
            }

            return strategies.OrderByDescending(s => s.score).First();
        }

        /// <summary>
        /// Intelligently optimizes data by automatically selecting the best strategy.
        /// This method analyzes the input data structure and applies the most
        /// appropriate optimization methods automatically.
        /// </summary>
        /// <param name="rawData">The data to optimize. Can be a POCO, JSON string, text string, or byte[] for an image.</param>
        /// <param name="intent">(Optional) A hint about the user's goal.</param>
        /// <returns>A single string optimized for an LLM prompt.</returns>
        public async Task<string> BrevityAsync(object rawData, string? intent = null)
        {
            // Handle image data immediately
            if (rawData is byte[] imgBytes)
            {
                return await _imageOptimizer.OptimizeImageAsync(imgBytes, _config);
            }

            // Handle text
            if (rawData is string str)
            {
                if (IsJson(str))
                {
                    // Parse JSON for analysis
                    var node = JsonNode.Parse(str);
                    if (node != null)
                    {
                        var analysis = AnalyzeDataStructure(node);
                        var strategy = SelectOptimalStrategy(analysis);
                        
                        var tempConfig = _config with
                        {
                            JsonMode = strategy.jsonMode ?? _config.JsonMode,
                            TextMode = strategy.textMode ?? _config.TextMode,
                            ImageMode = strategy.imageMode ?? _config.ImageMode
                        };
                        
                        return await _jsonOptimizer.OptimizeJsonAsync(str, tempConfig);
                    }
                }
                else if (str.Length > _config.LongTextThreshold)
                {
                    return await _textOptimizer.OptimizeTextAsync(str, _config);
                }
                return str;
            }

            // Handle POCO
            try
            {
                string jsonString = JsonSerializer.Serialize(rawData, new JsonSerializerOptions { WriteIndented = false });
                var node = JsonNode.Parse(jsonString);
                if (node != null)
                {
                    var analysis = AnalyzeDataStructure(node);
                    var strategy = SelectOptimalStrategy(analysis);
                    
                    var tempConfig = _config with
                    {
                        JsonMode = strategy.jsonMode ?? _config.JsonMode,
                        TextMode = strategy.textMode ?? _config.TextMode,
                        ImageMode = strategy.imageMode ?? _config.ImageMode
                    };
                    
                    return await _jsonOptimizer.OptimizeJsonAsync(jsonString, tempConfig);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Brevit] Failed to analyze POCO: {ex.Message}");
            }

            // Fallback to standard optimization
            return await OptimizeAsync(rawData, intent);
        }

        /// <summary>
        /// Registers a custom optimization strategy for the brevity method.
        /// This allows extending Brevit with new optimization strategies.
        /// </summary>
        /// <param name="name">Strategy name</param>
        /// <param name="strategy">The optimization strategy implementation</param>
        public void RegisterStrategy(string name, IOptimizationStrategy strategy)
        {
            _strategies[name] = strategy;
        }

        /// <summary>
        /// Analyzes if an array contains uniform objects (all have same keys).
        /// </summary>
        private (List<string>? keys, bool isUniform) AnalyzeArrayUniformity(JsonArray arr)
        {
            if (arr == null || arr.Count == 0)
                return (null, false);

            var firstItem = arr[0] as JsonObject;
            if (firstItem == null)
                return (null, false);

            // Preserve original field order instead of sorting
            var firstKeys = firstItem.Select(p => p.Key).ToList();
            var firstKeySet = new HashSet<string>(firstKeys);

            // Check if all items have the same keys (order-independent)
            for (int i = 1; i < arr.Count; i++)
            {
                var item = arr[i] as JsonObject;
                if (item == null)
                    return (null, false);

                var itemKeys = item.Select(p => p.Key).ToList();
                if (firstKeys.Count != itemKeys.Count)
                    return (null, false);

                // Check if all keys exist (order doesn't matter for uniformity)
                if (!itemKeys.All(key => firstKeySet.Contains(key)))
                    return (null, false);
            }

            return (firstKeys, true);
        }

        /// <summary>
        /// Analyzes if an array contains only primitives.
        /// </summary>
        private bool AnalyzeArrayPrimitives(JsonArray arr)
        {
            if (arr == null || arr.Count == 0)
                return false;

            foreach (var item in arr)
            {
                if (item is JsonObject || item is JsonArray)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Utility to check if a string is likely JSON.
        /// </summary>
        private static bool IsJson(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return false;
            var trimmed = str.Trim();
            return (trimmed.StartsWith("{") && trimmed.EndsWith("}")) ||
                   (trimmed.StartsWith("[") && trimmed.EndsWith("]"));
        }
    }

    #endregion

    #region == Default Service Implementations ==

    /// <summary>
    /// The default, high-performance JSON optimizer.
    /// This implementation is the core of the library.
    /// </summary>
    public class DefaultJsonOptimizer : IJsonOptimizer
    {
        public Task<string> OptimizeJsonAsync(string jsonString, BrevitConfig config)
        {
            return config.JsonMode switch
            {
                JsonOptimizationMode.Flatten => Task.FromResult(FlattenJson(jsonString)),
                JsonOptimizationMode.ToYaml => Task.FromResult(ConvertJsonToYaml(jsonString)),
                JsonOptimizationMode.Filter => Task.FromResult(FilterJson(jsonString, config.JsonPathsToKeep)),
                _ => Task.FromResult(jsonString) // None
            };
        }

        /// <summary>
        /// Checks if an array contains uniform objects (all have same keys).
        /// </summary>
        private (List<string>? keys, bool isUniform) IsUniformObjectArray(JsonArray arr)
        {
            if (arr == null || arr.Count == 0)
                return (null, false);

            var firstItem = arr[0] as JsonObject;
            if (firstItem == null)
                return (null, false);

            // Preserve original field order instead of sorting
            var firstKeys = firstItem.Select(p => p.Key).ToList();
            var firstKeySet = new HashSet<string>(firstKeys);

            // Check if all items have the same keys (order-independent)
            for (int i = 1; i < arr.Count; i++)
            {
                var item = arr[i] as JsonObject;
                if (item == null)
                    return (null, false);

                var itemKeys = item.Select(p => p.Key).ToList();
                if (firstKeys.Count != itemKeys.Count)
                    return (null, false);

                // Check if all keys exist (order doesn't matter for uniformity)
                if (!itemKeys.All(key => firstKeySet.Contains(key)))
                    return (null, false);
            }

            return (firstKeys, true);
        }

        /// <summary>
        /// Checks if an array contains only primitives.
        /// </summary>
        private bool IsPrimitiveArray(JsonArray arr)
        {
            if (arr == null || arr.Count == 0)
                return false;

            foreach (var item in arr)
            {
                if (item is JsonObject || item is JsonArray)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Escapes a value for comma-separated format.
        /// </summary>
        private string EscapeValue(JsonNode? node)
        {
            if (node == null)
                return "null";

            var str = node.ToString();
            
            // Quote if contains comma, newline, or quotes
            if (str.Contains(',') || str.Contains('\n') || str.Contains('"'))
            {
                return $"\"{str.Replace("\"", "\\\"")}\"";
            }

            return str;
        }

        /// <summary>
        /// Formats a uniform object array in tabular format.
        /// </summary>
        private string FormatTabularArray(JsonArray arr, string prefix, List<string> keys)
        {
            var header = $"{prefix}[{arr.Count}]{{{string.Join(",", keys)}}}:";
            var rows = new List<string>();
            
            foreach (var item in arr.Cast<JsonObject>())
            {
                var values = keys.Select(key =>
                {
                    var value = item[key];
                    return EscapeValue(value);
                });
                
                rows.Add(string.Join(",", values));
            }
            
            return $"{header}\n{string.Join("\n", rows)}";
        }

        /// <summary>
        /// Formats a primitive array in comma-separated format.
        /// </summary>
        private string FormatPrimitiveArray(JsonArray arr, string prefix)
        {
            var values = arr.Select(item => EscapeValue(item));
            return $"{prefix}[{arr.Count}]:{string.Join(",", values)}";
        }

        /// <summary>
        /// This is the "magic." Flattens a JSON object into a
        /// token-efficient key-value format with tabular optimization.
        /// </summary>
        private string FlattenJson(string jsonString)
        {
            try
            {
                var node = JsonNode.Parse(jsonString);
                if (node == null) return "[]";

                var output = new List<string>();
                Flatten(node, "", output);

                return string.Join("\n", output);
            }
            catch (JsonException ex)
            {
                return $"[Error: Invalid JSON - {ex.Message}]";
            }
        }

        /// <summary>
        /// Recursive helper for FlattenJson with tabular optimization.
        /// </summary>
        private void Flatten(JsonNode node, string prefix, List<string> output)
        {
            if (node is JsonObject obj)
            {
                foreach (var property in obj)
                {
                    string newPrefix = string.IsNullOrEmpty(prefix) ? property.Key : $"{prefix}.{property.Key}";
                    if (property.Value != null)
                    {
                        Flatten(property.Value, newPrefix, output);
                    }
                }
            }
            else if (node is JsonArray arr)
            {
                // Check for uniform object array (tabular format)
                var (keys, isUniform) = IsUniformObjectArray(arr);
                if (isUniform && keys != null)
                {
                    output.Add(FormatTabularArray(arr, prefix, keys));
                    return;
                }
                
                // Check for primitive array (comma-separated format)
                if (IsPrimitiveArray(arr))
                {
                    output.Add(FormatPrimitiveArray(arr, prefix));
                    return;
                }
                
                // Fall back to current format for mixed/non-uniform arrays
                for (int i = 0; i < arr.Count; i++)
                {
                    string newPrefix = $"{prefix}[{i}]";
                    if (arr[i] != null)
                    {
                        Flatten(arr[i]!, newPrefix, output);
                    }
                }
            }
            else if (node is JsonValue val)
            {
                // We've reached a leaf node. Add its path and value.
                if (string.IsNullOrEmpty(prefix))
                {
                    prefix = "value"; // Handle root-level value
                }
                output.Add($"{prefix}:{val.ToString()}");
            }
        }

        /// <summary>
        /// Stub for YAML conversion.
        /// A real implementation would use the YamlDotNet NuGet package.
        /// </summary>
        private string ConvertJsonToYaml(string jsonString)
        {
            // TODO: Implement with YamlDotNet
            // var jsonObject = JsonSerializer.Deserialize<object>(jsonString);
            // var serializer = new YamlDotNet.Serialization.Serializer();
            // return serializer.Serialize(jsonObject);
            return $"--- # YAML Conversion Stub\n# (Install YamlDotNet to implement)\n{jsonString}\n";
        }

        /// <summary>
        /// Stub for JSON filtering.
        /// A real implementation would use System.Text.Json or Newtonsoft.Json
        /// to selectively extract the paths.
        /// </summary>
        private string FilterJson(string jsonString, List<string> pathsToKeep)
        {
            // TODO: Implement JSON path filtering
            // This is complex and would use JObject.SelectTokens from Newtonsoft
            // or a custom parser for System.Text.Json.
            if (pathsToKeep.Any())
            {
                return $"[Filtered JSON, keeping: {string.Join(", ", pathsToKeep)}]\n{jsonString}";
            }
            return jsonString;
        }
    }

    /// <summary>
    /// Default text optimizer.
    /// This implementation is a STUB.
    /// Replace this by registering your own service that calls an LLM
    /// (like via Microsoft.SemanticKernel).
    /// </summary>
    public class DefaultTextOptimizer : ITextOptimizer
    {
        public Task<string> OptimizeTextAsync(string longText, BrevitConfig config)
        {
            if (config.TextMode == TextOptimizationMode.None)
            {
                return Task.FromResult(longText);
            }

            // TODO: Implement with Semantic Kernel or another LLM call.
            // var summary = await llmClient.SummarizeAsync(longText, config.TextMode);
            // return summary;

            // Stub implementation:
            string mode = config.TextMode.ToString();
            string stubSummary = longText.Length > 150
                ? longText.Substring(0, 150)
                : longText;
            string result = $"[{mode} Stub: Summary of text follows...]\n{stubSummary}...\n[End of summary]";
            return Task.FromResult(result);
        }
    }

    /// <summary>
    /// Default image optimizer.
    /// This implementation is a STUB.
    /// Replace this by registering your own service that calls Azure AI Vision.
    /// </summary>
    public class DefaultImageOptimizer : IImageOptimizer
    {
        public Task<string> OptimizeImageAsync(byte[] imageData, BrevitConfig config)
        {
            if (config.ImageMode == ImageOptimizationMode.None)
            {
                return Task.FromResult(string.Empty);
            }

            // TODO: Implement with Azure.AI.Vision
            // var visionClient = new ImageAnalysisClient(new Uri(endpoint), new AzureKeyCredential(key));
            // var ocrResult = await visionClient.AnalyzeAsync(imageData, VisualFeatures.Read);
            // return ocrResult.Read.GetText();

            // Stub implementation:
            string result = $"[OCR Stub: Extracted text from image ({imageData.Length} bytes)]\n" +
                            "Sample OCR Text: INVOICE #1234\n" +
                            "Total: $499.99\n" +
                            "[End of extracted text]";
            return Task.FromResult(result);
        }
    }

    #endregion
}

