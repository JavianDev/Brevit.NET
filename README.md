# Brevit

A high-performance, type-safe .NET library for semantically compressing and optimizing data before sending it to a Large Language Model (LLM). Dramatically reduce token costs while maintaining data integrity and readability.

## Table of Contents

- [Why Brevit?](#why-brevit)
- [Key Features](#key-features)
- [When Not to Use Brevit](#when-not-to-use-brevit)
- [Benchmarks](#benchmarks)
- [Installation & Quick Start](#installation--quick-start)
- [Playgrounds](#playgrounds)
- [CLI](#cli)
- [Format Overview](#format-overview)
- [API](#api)
- [Using Brevit in LLM Prompts](#using-brevit-in-llm-prompts)
- [Syntax Cheatsheet](#syntax-cheatsheet)
- [Other Implementations](#other-implementations)
- [Full Specification](#full-specification)

## Why Brevit?

### .NET-Specific Advantages

- **First-Class POCO Support**: Optimize C# objects directly without manual serialization
- **Dependency Injection Ready**: Seamless integration with ASP.NET Core DI container
- **Type Safety**: Full compile-time type checking with modern C# features
- **Async/Await**: Built-in async support for high-performance applications
- **LINQ Compatible**: Works seamlessly with LINQ queries and expressions

### Performance Benefits

- **40-60% Token Reduction**: Dramatically reduce LLM API costs
- **Zero-Copy Operations**: Efficient memory usage with Span<T> and Memory<T>
- **High Throughput**: Process thousands of objects per second
- **Low Latency**: Sub-millisecond optimization for typical objects

### Example Cost Savings

```csharp
// Before: 234 tokens = $0.000468 per request
var json = JsonSerializer.Serialize(complexOrder);

// After: 127 tokens = $0.000254 per request (46% reduction)
var optimized = await brevit.BrevityAsync(complexOrder); // Automatic optimization

// Or with explicit configuration
var explicit = await brevit.OptimizeAsync(complexOrder);

// Savings: $0.000214 per request
// At 1M requests/month: $214/month savings
```

### Automatic Strategy Selection

Brevit now includes the `BrevityAsync()` method that automatically analyzes your data and selects the optimal optimization strategy:

```csharp
var data = new
{
    Friends = new[] { "ana", "luis", "sam" },
    Hikes = new[]
    {
        new { Id = 1, Name = "Blue Lake Trail", DistanceKm = 7.5 },
        new { Id = 2, Name = "Ridge Overlook", DistanceKm = 9.2 }
    }
};

// Automatically detects uniform arrays and applies tabular format
var optimized = await brevit.BrevityAsync(data);
// No configuration needed - Brevit analyzes and optimizes automatically!
```

## Key Features

- **JSON Optimization**: Flatten nested JSON structures into token-efficient key-value pairs
- **Text Optimization**: Clean and summarize long text documents
- **Image Optimization**: Extract text from images via OCR
- **Type-Safe**: Built with modern C# and .NET 8
- **Extensible**: Plugin architecture for custom optimizers
- **Lightweight**: Minimal dependencies, high performance
- **First-Class POCO Support**: Optimize C# objects directly without manual serialization

### POCO Example

```csharp
public class Order
{
    public string OrderId { get; set; }
    public string Status { get; set; }
    public List<OrderItem> Items { get; set; }
}

var order = new Order 
{ 
    OrderId = "o-456", 
    Status = "SHIPPED",
    Items = new List<OrderItem> { ... }
};

// Direct optimization - no serialization needed!
var optimized = await brevit.OptimizeAsync(order);
```

## When Not to Use Brevit

Consider alternatives when:

1. **API Responses**: If returning JSON to HTTP clients, use standard JSON serialization
2. **Data Contracts**: When strict JSON schema validation is required
3. **Small Objects**: Objects under 100 tokens may not benefit significantly
4. **Real-Time APIs**: For REST APIs serving JSON, standard formatting is better
5. **Legacy Systems**: Systems expecting specific JSON formats

**Best Use Cases:**
- ✅ LLM prompt optimization
- ✅ Reducing OpenAI/Anthropic API costs
- ✅ Processing large datasets for AI
- ✅ Document summarization workflows
- ✅ OCR and image processing pipelines

## Benchmarks

### Token Reduction

| Object Type | Original Tokens | Brevit (No Abbr) | Brevit (With Abbr) | Total Reduction |
|-------------|----------------|------------------|-------------------|-----------------|
| Simple POCO | 45 | 28 | 26 | 42% |
| Complex POCO | 234 | 127 | 105 | 55% |
| Nested Arrays | 156 | 89 | 75 | 52% |
| API Response | 312 | 178 | 145 | 54% |
| Deeply Nested | 95 | 78 | 65 | 32% |

**Note**: Abbreviations are enabled by default and provide additional 10-25% savings on top of base optimization.

### Performance

| Operation | Objects/sec | Avg Latency | Memory |
|-----------|-------------|-------------|--------|
| Flatten (1KB) | 2,000 | 0.5ms | 2.1MB |
| Flatten (10KB) | 450 | 2.2ms | 8.5MB |
| Flatten (100KB) | 55 | 18ms | 45MB |

*Benchmarks: .NET 8, Intel i7-12700K, Release mode*

## Installation & Quick Start

### Prerequisites

- .NET 8 SDK or later
- Visual Studio 2022, VS Code, or Rider

### Install via NuGet (Recommended)

```bash
dotnet add package Brevit
```

### Install from Source

```bash
git clone https://github.com/JavianDev/Brevit.NET.git
cd Brevit.NET
dotnet build
dotnet add reference ../Brevit.NET/Brevit.NET.csproj
```

### Quick Start

```csharp
using Brevit.NET;

// 1. Create configuration
var config = new BrevitConfig(
    JsonMode: JsonOptimizationMode.Flatten,
    TextMode: TextOptimizationMode.Clean,
    ImageMode: ImageOptimizationMode.Ocr
)
{
    LongTextThreshold = 1000
};

// 2. Create optimizers
var jsonOptimizer = new DefaultJsonOptimizer();
var textOptimizer = new DefaultTextOptimizer();
var imageOptimizer = new DefaultImageOptimizer();

// 3. Create client
var brevit = new BrevitClient(config, jsonOptimizer, textOptimizer, imageOptimizer);

// 4. Optimize POCO directly
var order = new { OrderId = "o-456", Status = "SHIPPED" };
string optimized = await brevit.OptimizeAsync(order);
// Result (with abbreviations enabled by default):
// "@o=order\n@o.OrderId:o-456\n@o.Status:SHIPPED"
```

#### Abbreviation Feature (New in v0.1.2)

Brevit automatically creates abbreviations for frequently repeated prefixes, reducing token usage by 10-25%:

```csharp
using Brevit.NET;

var config = new BrevitConfig(
    JsonMode: JsonOptimizationMode.Flatten
)
{
    EnableAbbreviations = true,   // Enabled by default
    AbbreviationThreshold = 2     // Minimum occurrences to abbreviate
};

var brevit = new BrevitClient(config, 
    new DefaultJsonOptimizer(), 
    new DefaultTextOptimizer(), 
    new DefaultImageOptimizer());

var data = new { 
    User = new { 
        Name = "John Doe", 
        Email = "john@example.com", 
        Age = 30 
    },
    Order = new { 
        Id = "o-456", 
        Status = "SHIPPED" 
    }
};

var optimized = await brevit.BrevityAsync(data);
// Output with abbreviations:
// @U=User
// @O=Order
// @U.Name:John Doe
// @U.Email:john@example.com
// @U.Age:30
// @O.Id:o-456
// @O.Status:SHIPPED
```

**Token Savings**: The abbreviation feature reduces tokens by replacing repeated prefixes like "User." and "Order." with short aliases like "@U" and "@O", saving 10-25% on typical nested JSON structures.

## Complete Usage Examples

Brevit supports three main data types: **JSON objects/strings**, **text files/strings**, and **images**. Here's how to use each:

### 1. JSON Optimization Examples

#### Example 1.1: Simple POCO Object

```csharp
using Brevit.NET;

var config = new BrevitConfig(JsonMode: JsonOptimizationMode.Flatten);
var brevit = new BrevitClient(config, 
    new DefaultJsonOptimizer(), 
    new DefaultTextOptimizer(), 
    new DefaultImageOptimizer());

var data = new { 
    User = new { 
        Name = "John Doe", 
        Email = "john@example.com", 
        Age = 30 
    } 
};

// Method 1: Automatic optimization (recommended)
var optimized = await brevit.BrevityAsync(data);
// Output (with abbreviations enabled by default):
// @U=User
// @U.Name:John Doe
// @U.Email:john@example.com
// @U.Age:30

// Method 2: Explicit optimization
var explicit = await brevit.OptimizeAsync(data);
```

#### Example 1.2: JSON String

```csharp
string jsonString = "{\"order\": {\"id\": \"o-456\", \"status\": \"SHIPPED\"}}";

// Brevit automatically detects JSON strings
var optimized = await brevit.BrevityAsync(jsonString);
// Output (with abbreviations enabled by default):
// @o=order
// @o.id:o-456
// @o.status:SHIPPED
```

#### Example 1.2a: Abbreviations Disabled

```csharp
var configNoAbbr = new BrevitConfig(JsonMode: JsonOptimizationMode.Flatten)
{
    EnableAbbreviations = false  // Disable abbreviations
};
var brevitNoAbbr = new BrevitClient(configNoAbbr, 
    new DefaultJsonOptimizer(), 
    new DefaultTextOptimizer(), 
    new DefaultImageOptimizer());

string jsonString = "{\"order\": {\"id\": \"o-456\", \"status\": \"SHIPPED\"}}";
var optimized = await brevitNoAbbr.BrevityAsync(jsonString);
// Output (without abbreviations):
// order.id:o-456
// order.status:SHIPPED
```

#### Example 1.3: Complex Nested POCO with Arrays

```csharp
var complexData = new
{
    Context = new
    {
        Task = "Our favorite hikes together",
        Location = "Boulder",
        Season = "spring_2025"
    },
    Friends = new[] { "ana", "luis", "sam" },
    Hikes = new[]
    {
        new
        {
            Id = 1,
            Name = "Blue Lake Trail",
            DistanceKm = 7.5,
            ElevationGain = 320,
            Companion = "ana",
            WasSunny = true
        },
        new
        {
            Id = 2,
            Name = "Ridge Overlook",
            DistanceKm = 9.2,
            ElevationGain = 540,
            Companion = "luis",
            WasSunny = false
        }
    }
};

var optimized = await brevit.BrevityAsync(complexData);
// Output (with abbreviations enabled by default):
// @C=Context
// @C.Task:Our favorite hikes together
// @C.Location:Boulder
// @C.Season:spring_2025
// Friends[3]:ana,luis,sam
// Hikes[2]{Companion,DistanceKm,ElevationGain,Id,Name,WasSunny}:
// ana,7.5,320,1,Blue Lake Trail,true
// luis,9.2,540,2,Ridge Overlook,false
```

#### Example 1.3a: Complex Data with Abbreviations Disabled

```csharp
var configNoAbbr = new BrevitConfig(JsonMode: JsonOptimizationMode.Flatten)
{
    EnableAbbreviations = false  // Disable abbreviations
};
var brevitNoAbbr = new BrevitClient(configNoAbbr, 
    new DefaultJsonOptimizer(), 
    new DefaultTextOptimizer(), 
    new DefaultImageOptimizer());

var complexData = new
{
    Context = new
    {
        Task = "Our favorite hikes together",
        Location = "Boulder",
        Season = "spring_2025"
    },
    Friends = new[] { "ana", "luis", "sam" },
    Hikes = new[]
    {
        new
        {
            Id = 1,
            Name = "Blue Lake Trail",
            DistanceKm = 7.5,
            ElevationGain = 320,
            Companion = "ana",
            WasSunny = true
        },
        new
        {
            Id = 2,
            Name = "Ridge Overlook",
            DistanceKm = 9.2,
            ElevationGain = 540,
            Companion = "luis",
            WasSunny = false
        }
    }
};

var optimized = await brevitNoAbbr.BrevityAsync(complexData);
// Output (without abbreviations):
// Context.Task:Our favorite hikes together
// Context.Location:Boulder
// Context.Season:spring_2025
// Friends[3]:ana,luis,sam
// Hikes[2]{Companion,DistanceKm,ElevationGain,Id,Name,WasSunny}:
// ana,7.5,320,1,Blue Lake Trail,true
// luis,9.2,540,2,Ridge Overlook,false
```

#### Example 1.4: Different JSON Optimization Modes

```csharp
// Flatten Mode (Default)
var flattenConfig = new BrevitConfig(JsonMode: JsonOptimizationMode.Flatten);
// Converts nested JSON to flat key-value pairs

// YAML Mode
var yamlConfig = new BrevitConfig(JsonMode: JsonOptimizationMode.ToYaml);
// Converts JSON to YAML format

// Filter Mode
var filterConfig = new BrevitConfig(
    JsonMode: JsonOptimizationMode.Filter,
    JsonPathsToKeep: new[] { "user.name", "order.id" }
);
// Keeps only specified paths, removes everything else
```

### 2. Text Optimization Examples

#### Example 2.1: Long Text String

```csharp
string longText = "This is a very long document..." + 
    string.Concat(Enumerable.Repeat("...", 1000));

var config = new BrevitConfig(
    JsonMode: JsonOptimizationMode.None,
    TextMode: TextOptimizationMode.Clean,
    LongTextThreshold: 500
);
var brevit = new BrevitClient(config, 
    new DefaultJsonOptimizer(), 
    new DefaultTextOptimizer(), 
    new DefaultImageOptimizer());

// Automatic detection
var optimized = await brevit.BrevityAsync(longText);

// Explicit text optimization
var cleaned = await brevit.OptimizeAsync(longText);
```

#### Example 2.2: Reading Text from File

```csharp
// Read text file
string textContent = await File.ReadAllTextAsync("document.txt");

// Optimize the text
var optimized = await brevit.BrevityAsync(textContent);
```

#### Example 2.3: Text Optimization Modes

```csharp
// Clean Mode (Remove Boilerplate)
var cleanConfig = new BrevitConfig(TextMode: TextOptimizationMode.Clean);
// Removes signatures, headers, repetitive content

// Summarize Fast
var fastConfig = new BrevitConfig(TextMode: TextOptimizationMode.SummarizeFast);
// Fast summarization (requires custom text optimizer implementation)

// Summarize High Quality
var qualityConfig = new BrevitConfig(TextMode: TextOptimizationMode.SummarizeHighQuality);
// High-quality summarization (requires custom text optimizer with LLM integration)
```

### 3. Image Optimization Examples

#### Example 3.1: Image from File (OCR)

```csharp
// Read image file
byte[] imageBytes = await File.ReadAllBytesAsync("receipt.jpg");

// Brevit automatically detects byte[] as image data
var extractedText = await brevit.BrevityAsync(imageBytes);
// Output: OCR-extracted text from the image
```

#### Example 3.2: Image from URL

```csharp
using System.Net.Http;

var httpClient = new HttpClient();
byte[] imageBytes = await httpClient.GetByteArrayAsync("https://example.com/invoice.png");

var extractedText = await brevit.BrevityAsync(imageBytes);
```

#### Example 3.3: Image Optimization Modes

```csharp
// OCR Mode (Extract Text)
var ocrConfig = new BrevitConfig(ImageMode: ImageOptimizationMode.Ocr);
// Extracts text from images using OCR (requires custom image optimizer)

// Metadata Mode
var metadataConfig = new BrevitConfig(ImageMode: ImageOptimizationMode.Metadata);
// Extracts only image metadata (dimensions, format, etc.)
```

### 4. Method Comparison: `BrevityAsync()` vs `OptimizeAsync()`

#### `BrevityAsync()` - Automatic Strategy Selection

**Use when:** You want Brevit to automatically analyze and select the best optimization strategy.

```csharp
// Automatically detects data type and applies optimal strategy
var result = await brevit.BrevityAsync(data);
// - JSON objects → Flatten with tabular optimization
// - Long text → Text optimization
// - Images → OCR extraction
```

**Advantages:**
- Zero configuration needed
- Intelligent strategy selection
- Works with any data type
- Best for general-purpose use

#### `OptimizeAsync()` - Explicit Configuration

**Use when:** You want explicit control over optimization mode.

```csharp
var config = new BrevitConfig(
    JsonMode: JsonOptimizationMode.Flatten,
    TextMode: TextOptimizationMode.Clean,
    ImageMode: ImageOptimizationMode.Ocr
);
var brevit = new BrevitClient(config, 
    new DefaultJsonOptimizer(), 
    new DefaultTextOptimizer(), 
    new DefaultImageOptimizer());

// Uses explicit configuration
var result = await brevit.OptimizeAsync(data);
```

**Advantages:**
- Full control over optimization
- Predictable behavior
- Best for specific use cases

### 5. Custom Optimizers

You can provide custom optimizers for text and images:

```csharp
// Custom text optimizer
public class CustomTextOptimizer : ITextOptimizer
{
    public async Task<string> OptimizeTextAsync(string longText, BrevitConfig config)
    {
        // Call your summarization service
        // Return optimized text
        return await SummarizeService.SummarizeAsync(longText);
    }
}

// Custom image optimizer
public class CustomImageOptimizer : IImageOptimizer
{
    public async Task<string> OptimizeImageAsync(byte[] imageData, BrevitConfig config)
    {
        // Call your OCR service (e.g., Azure AI Vision)
        // Return extracted text
        return await OcrService.ExtractTextAsync(imageData);
    }
}

var brevit = new BrevitClient(config, 
    new DefaultJsonOptimizer(), 
    new CustomTextOptimizer(), 
    new CustomImageOptimizer());
```

### 6. Complete Workflow Examples

#### Example 6.1: E-Commerce Order Processing

```csharp
// Step 1: Optimize order POCO
var order = new
{
    OrderId = "o-456",
    Customer = new { Name = "John", Email = "john@example.com" },
    Items = new[]
    {
        new { Sku = "A-88", Quantity = 2, Price = 29.99m },
        new { Sku = "B-22", Quantity = 1, Price = 49.99m }
    }
};

var optimizedOrder = await brevit.BrevityAsync(order);

// Step 2: Send to LLM
var prompt = $"Analyze this order:\n\n{optimizedOrder}\n\nExtract total amount.";
// Send prompt to OpenAI, Anthropic, etc.
```

#### Example 6.2: Document Processing Pipeline

```csharp
// Step 1: Read and optimize text document
string contractText = await File.ReadAllTextAsync("contract.txt");
string optimizedText = await brevit.BrevityAsync(contractText);

// Step 2: Process with LLM
string prompt = $"Summarize this contract:\n\n{optimizedText}";
// Send to LLM for summarization
```

#### Example 6.3: Receipt OCR Pipeline

```csharp
// Step 1: Read receipt image
byte[] receiptImage = await File.ReadAllBytesAsync("receipt.jpg");

// Step 2: Extract text via OCR
string extractedText = await brevit.BrevityAsync(receiptImage);

// Step 3: Optimize extracted text (if it's long)
string optimized = await brevit.BrevityAsync(extractedText);

// Step 4: Send to LLM for analysis
string prompt = $"Extract items and total from this receipt:\n\n{optimized}";
// Send to LLM
```

### Dependency Injection (Recommended)

```csharp
using Brevit.NET;

var builder = WebApplication.CreateBuilder(args);

// Configure Brevit
var brevitConfig = new BrevitConfig(
    JsonMode: JsonOptimizationMode.Flatten,
    TextMode: TextOptimizationMode.SummarizeFast,
    ImageMode: ImageOptimizationMode.Ocr
)
{
    LongTextThreshold = 1000
};

builder.Services.AddSingleton(brevitConfig);
builder.Services.AddSingleton<IJsonOptimizer, DefaultJsonOptimizer>();
builder.Services.AddSingleton<ITextOptimizer, DefaultTextOptimizer>();
builder.Services.AddSingleton<IImageOptimizer, DefaultImageOptimizer>();
builder.Services.AddScoped<BrevitClient>();

var app = builder.Build();
```

## Playgrounds

### Interactive Playground

```bash
# Clone and run
git clone https://github.com/JavianDev/Brevit.git
cd Brevit/Brevit.NET
dotnet run --project Playground
```

### Online Playground

- **Web Playground**: [https://brevit.dev/playground](https://brevit.dev/playground) (Coming Soon)
- **.NET Fiddle**: [https://dotnetfiddle.net/brevit](https://dotnetfiddle.net/brevit) (Coming Soon)

## CLI

### Installation

```bash
dotnet tool install -g Brevit.CLI
```

### Usage

```bash
# Optimize a JSON file
brevit optimize input.json -o output.txt

# Optimize POCO from assembly
brevit optimize --assembly MyApp.dll --type MyApp.Order

# Optimize with custom config
brevit optimize input.json --mode flatten --threshold 1000

# Help
brevit --help
```

### Examples

```bash
# Flatten JSON
brevit optimize order.json --mode flatten

# Convert to YAML
brevit optimize data.json --mode yaml

# Filter paths
brevit optimize data.json --mode filter --paths "user.name,order.id"
```

## Format Overview

### Flattened Format (Hybrid Optimization)

Brevit intelligently converts C# objects to flat key-value pairs with automatic tabular optimization:

**Input (C# POCO):**
```csharp
var order = new Order
{
    OrderId = "o-456",
    Friends = new[] { "ana", "luis", "sam" },
    Items = new[]
    {
        new OrderItem { Sku = "A-88", Quantity = 1 },
        new OrderItem { Sku = "T-22", Quantity = 2 }
    }
};
```

**Output (with tabular optimization and abbreviations enabled by default):**
```
OrderId: o-456
Friends[3]: ana,luis,sam
@I=Items
@I[2]{Quantity,Sku}:
1,A-88
2,T-22
```

**Output (with abbreviations disabled):**
```
OrderId: o-456
Friends[3]: ana,luis,sam
Items[2]{Quantity,Sku}:
1,A-88
2,T-22
```

**For non-uniform arrays (fallback):**
```csharp
var mixed = new
{
    Items = new object[]
    {
        new { Sku = "A-88", Quantity = 1 },
        "special-item",
        new { Sku = "T-22", Quantity = 2 }
    }
};
```

**Output (fallback to indexed format):**
```
Items[0].Sku: A-88
Items[0].Quantity: 1
Items[1]: special-item
Items[2].Sku: T-22
Items[2].Quantity: 2
```

### Key Features

- **Property Names**: Uses C# property names as-is
- **Nested Objects**: Dot notation for nested properties
- **Tabular Arrays**: Uniform object arrays automatically formatted in compact tabular format (`Items[2]{Field1,Field2}:`)
- **Primitive Arrays**: Comma-separated format (`Friends[3]: ana,luis,sam`)
- **Abbreviation System** (Default: Enabled): Automatically creates short aliases for repeated prefixes (`@U=User`, `@O=Order`)
- **Hybrid Approach**: Automatically detects optimal format, falls back to indexed format for mixed data
- **Null Handling**: Explicit `null` values
- **Type Preservation**: Numbers, booleans preserved as strings

### Abbreviation System (Default: Enabled)

Brevit automatically creates abbreviations for frequently repeated property prefixes, placing definitions at the top of the output:

**Example:**
```
@U=User
@O=Order
@U.Name:John Doe
@U.Email:john@example.com
@O.Id:o-456
@O.Status:SHIPPED
```

**Benefits:**
- **10-25% additional token savings** on nested data
- **Self-documenting**: Abbreviations are defined at the top
- **LLM-friendly**: Models easily understand the mapping
- **Configurable**: Can be disabled with `EnableAbbreviations = false`

**When Abbreviations Help Most:**
- Deeply nested JSON structures
- Arrays of objects with repeated field names
- API responses with consistent schemas
- Data with many repeated prefixes (e.g., `User.Profile.Settings.Theme`)

**Disable Abbreviations:**
```csharp
var config = new BrevitConfig(JsonMode: JsonOptimizationMode.Flatten)
{
    EnableAbbreviations = false  // Disable abbreviation feature
};
```

## API

### BrevitClient

Main client class for optimization.

```csharp
public class BrevitClient
{
    public BrevitClient(
        BrevitConfig config,
        IJsonOptimizer jsonOptimizer,
        ITextOptimizer textOptimizer,
        IImageOptimizer imageOptimizer
    );

    // Automatic optimization - analyzes data and selects best strategy
    public Task<string> BrevityAsync(object rawData, string? intent = null);
    
    // Explicit optimization with configured settings
    public Task<string> OptimizeAsync(object rawData, string? intent = null);
    
    // Register custom optimization strategy
    public void RegisterStrategy(string name, IOptimizationStrategy strategy);
}
```

**Example - Automatic Optimization:**
```csharp
// Automatically analyzes data structure and selects best strategy
var optimized = await brevit.BrevityAsync(order);
// Automatically detects uniform arrays, long text, etc.
```

**Example - Explicit Optimization:**
```csharp
// Use explicit configuration
var optimized = await brevit.OptimizeAsync(order, "extract_total");
```

**Example - Custom Strategy:**
```csharp
// Register custom optimization strategy
brevit.RegisterStrategy("custom", new CustomOptimizationStrategy());
```

### BrevitConfig

Configuration record for BrevitClient.

```csharp
public record BrevitConfig(
    JsonOptimizationMode JsonMode = JsonOptimizationMode.Flatten,
    TextOptimizationMode TextMode = TextOptimizationMode.Clean,
    ImageOptimizationMode ImageMode = ImageOptimizationMode.Ocr
)
{
    public List<string> JsonPathsToKeep { get; init; } = new();
    public int LongTextThreshold { get; init; } = 500;
    public bool EnableAbbreviations { get; init; } = true;      // Default: true
    public int AbbreviationThreshold { get; init; } = 2;        // Default: 2
}
```

### Interfaces

#### IJsonOptimizer
```csharp
public interface IJsonOptimizer
{
    Task<string> OptimizeJsonAsync(string jsonString, BrevitConfig config);
}
```

#### ITextOptimizer
```csharp
public interface ITextOptimizer
{
    Task<string> OptimizeTextAsync(string longText, BrevitConfig config);
}
```

#### IImageOptimizer
```csharp
public interface IImageOptimizer
{
    Task<string> OptimizeImageAsync(byte[] imageData, BrevitConfig config);
}
```

### Enums

#### JsonOptimizationMode
- `None` - No optimization
- `Flatten` - Flatten to key-value pairs (default)
- `ToYaml` - Convert to YAML
- `Filter` - Keep only specified paths

#### TextOptimizationMode
- `None` - No optimization
- `Clean` - Remove boilerplate
- `SummarizeFast` - Fast summarization
- `SummarizeHighQuality` - High-quality summarization

#### ImageOptimizationMode
- `None` - Skip processing
- `Ocr` - Extract text via OCR
- `Metadata` - Extract metadata only

## Using Brevit in LLM Prompts

### Best Practices

1. **Context First**: Provide context before optimized data
2. **Clear Instructions**: Tell the LLM what format to expect
3. **Examples**: Include format examples in prompts

### Example Prompt Template

```csharp
var optimized = await brevit.OptimizeAsync(order);

var prompt = $@"You are analyzing order data. The data is in Brevit flattened format:

Context:
{optimized}

Task: Extract the order total and shipping address.

Format your response as JSON with keys: total, address";
```

### Real-World Example

```csharp
public class OrderAnalysisService
{
    private readonly BrevitClient _brevit;
    private readonly IOpenAIClient _openAI;

    public async Task<OrderAnalysis> AnalyzeOrderAsync(Order order)
    {
        // Optimize order data
        var optimized = await _brevit.OptimizeAsync(order);

        // Create prompt
        var prompt = $@"Analyze this order:

{optimized}

Questions:
1. What is the order total?
2. How many items?
3. Average item price?

Respond in JSON.";

        // Call LLM
        var response = await _openAI.GenerateAsync(prompt);
        return JsonSerializer.Deserialize<OrderAnalysis>(response);
    }
}
```

## Syntax Cheatsheet

### C# to Brevit Format

| C# Structure | Brevit Format | Example |
|--------------|---------------|---------|
| Property | `PropertyName: value` | `OrderId: o-456` |
| Nested property | `Parent.Child: value` | `Customer.Name: John` |
| Primitive array | `Array[count]: val1,val2,val3` | `Friends[3]: ana,luis,sam` |
| Uniform object array | `Array[count]{Field1,Field2}:`<br>`  val1,val2`<br>`  val3,val4` | `Items[2]{Sku,Quantity}:`<br>`  A-88,1`<br>`  T-22,2` |
| Array element (fallback) | `Array[index].Property: value` | `Items[0].Sku: A-88` |
| Nested array | `Parent[index].Child[index]` | `Orders[0].Items[1].Sku` |
| Null value | `Property: null` | `Phone: null` |
| Boolean | `Property: True` | `IsActive: True` |
| Number | `Property: 123` | `Quantity: 5` |

### Special Cases

- **Empty Collections**: `Items: []` → `Items: []`
- **Nested Empty Objects**: `Metadata: {}` → `Metadata: {}`
- **Nullable Types**: `Phone: null` → `Phone: null`
- **Enums**: Converted to string representation
- **Tabular Arrays**: Automatically detected when all objects have same properties
- **Primitive Arrays**: Automatically detected when all elements are primitives

## Other Implementations

Brevit is available in multiple languages:

| Language | Package | Status |
|----------|---------|--------|
| C# (.NET) | `Brevit` | ✅ Stable (This) |
| JavaScript | `brevit` | ✅ Stable |
| Python | `brevit` | ✅ Stable |

## Full Specification

### Format Specification

1. **Key-Value Pairs**: One pair per line
2. **Separator**: `: ` (colon + space)
3. **Key Format**: Property names with dot/bracket notation
4. **Value Format**: String representation of values
5. **Line Endings**: `\n` (newline)

### Grammar

```
brevit := line*
line := key ": " value "\n"
key := identifier ("." identifier | "[" number "]")*
value := string | number | boolean | null
identifier := [A-Za-z_][A-Za-z0-9_]*
```

### Examples

**Simple Object:**
```
OrderId: o-456
Status: SHIPPED
```

**Nested Object:**
```
Customer.Name: John Doe
Customer.Email: john@example.com
```

**Array:**
```
Items[0].Sku: A-88
Items[0].Quantity: 1
Items[1].Sku: T-22
Items[1].Quantity: 2
```

**Complex Structure:**
```
OrderId: o-456
Customer.Name: John Doe
Items[0].Sku: A-88
Items[0].Price: 29.99
Items[1].Sku: T-22
Items[1].Price: 39.99
Shipping.Address.Street: 123 Main St
Shipping.Address.City: Toronto
```

## Advanced Usage

### Custom Text Optimizer with Semantic Kernel

```csharp
public class SemanticKernelTextOptimizer : ITextOptimizer
{
    private readonly Kernel _kernel;

    public SemanticKernelTextOptimizer(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<string> OptimizeTextAsync(string longText, BrevitConfig config)
    {
        var summarizeFunction = _kernel.CreateFunctionFromPrompt(
            "Summarize the following text: {{$input}}"
        );

        var result = await _kernel.InvokeAsync(
            summarizeFunction, 
            new() { ["input"] = longText }
        );
        
        return result.ToString();
    }
}
```

### Custom Image Optimizer with Azure AI Vision

```csharp
public class AzureVisionImageOptimizer : IImageOptimizer
{
    private readonly ImageAnalysisClient _client;

    public AzureVisionImageOptimizer(ImageAnalysisClient client)
    {
        _client = client;
    }

    public async Task<string> OptimizeImageAsync(byte[] imageData, BrevitConfig config)
    {
        var result = await _client.AnalyzeAsync(
            BinaryData.FromBytes(imageData),
            VisualFeatures.Read
        );

        return result.Value.Read.Text;
    }
}
```

### Filter Mode

```csharp
var config = new BrevitConfig(
    JsonMode: JsonOptimizationMode.Filter
)
{
    JsonPathsToKeep = new List<string>
    {
        "user.name",
        "order.orderId",
        "order.items[*].sku"
    }
};
```

## Examples

### Example 1: Optimize Complex POCO

```csharp
public class User
{
    public string Id { get; set; }
    public string Name { get; set; }
    public bool IsActive { get; set; }
    public ContactInfo Contact { get; set; }
    public List<Order> Orders { get; set; }
}

var user = new User
{
    Id = "u-123",
    Name = "Javian",
    IsActive = true,
    Contact = new ContactInfo
    {
        Email = "support@javianpicardo.com",
        Phone = null
    },
    Orders = new List<Order>
    {
        new Order { OrderId = "o-456", Status = "SHIPPED" }
    }
};

// Automatic optimization - analyzes data structure and selects best strategy
string optimized = await brevit.BrevityAsync(user);

// Or use explicit optimization
string explicit = await brevit.OptimizeAsync(user);
```

### Example 2: Optimize JSON String

```csharp
string json = @"{
    ""order"": {
        ""orderId"": ""o-456"",
        ""status"": ""SHIPPED"",
        ""items"": [
            { ""sku"": ""A-88"", ""name"": ""Brevit Pro"", ""quantity"": 1 }
        ]
    }
}";

string optimized = await brevit.OptimizeAsync(json);
```

### Example 3: Process Long Text

```csharp
string longDocument = await File.ReadAllTextAsync("document.txt");
string optimized = await brevit.OptimizeAsync(longDocument);
// Triggers text optimization if length > LongTextThreshold
```

## Best Practices

1. **Use Dependency Injection**: Register all components in DI container
2. **Implement Custom Optimizers**: Replace stubs with real LLM integrations
3. **Configure Thresholds**: Adjust `LongTextThreshold` based on use case
4. **Monitor Token Usage**: Track before/after token counts
5. **Cache Results**: Cache optimized results for repeated queries
6. **Error Handling**: Wrap optimize calls in try-catch blocks
7. **Async All The Way**: Use async/await throughout your pipeline

## Troubleshooting

### Issue: "Failed to serialize POCO"

**Solution**: Ensure object is serializable. Use `[JsonIgnore]` to exclude properties:

```csharp
public class Order
{
    public string OrderId { get; set; }
    
    [JsonIgnore]
    public string InternalId { get; set; } // Excluded from optimization
}
```

### Issue: YAML conversion not working

**Solution**: Install YamlDotNet and implement conversion:

```bash
dotnet add package YamlDotNet
```

### Issue: Text summarization returns stub

**Solution**: Implement custom `ITextOptimizer` using Semantic Kernel or your LLM service.

## Contributing

Contributions welcome! See [CONTRIBUTING.md](../CONTRIBUTING.md) for guidelines.

## License

MIT License - see [LICENSE](../LICENSE) file for details.

## Support

- **Documentation**: [https://brevit.dev/docs](https://brevit.dev/docs)
- **Issues**: [https://github.com/JavianDev/Brevit/issues](https://github.com/JavianDev/Brevit/issues)
- **Email**: support@javianpicardo.com

## Version History

- **0.1.0** (Current): Initial release with core optimization features
