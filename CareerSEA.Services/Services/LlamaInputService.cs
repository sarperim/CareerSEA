using CareerSEA.Contracts.Requests;
using CareerSEA.Services.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CareerSEA.Services.Services
{
    public class LlamaInputService : ILlamaInputService
    {
        private readonly IChatClient _chatClient;
        private readonly ILogger<LlamaInputService> _logger;

        public LlamaInputService(IChatClient chatClient, ILogger<LlamaInputService> logger)
        {
            _chatClient = chatClient;
            _logger = logger;
        }
        public async Task<AIRequest?> ExtractCareerDataAsync(string cvText, CancellationToken cancellationToken = default)
        {
            var systemPrompt = @"You are a professional HR data extraction engine.
TASK: Extract distinct professional experiences from the CV text.

STRICT EXTRACTION RULES:
1. ENTITY GROUPING: A single 'job' entry must represent one continuous period. 
2. NO TECH-STACK SPLITTING: Do not create separate entries for different technical phases.
3. DESCRIPTION MAPPING: Summarize all related tasks into a 2-3 sentence 'description'.
4. TITLE FORMAT: The 'title' field must contain both the Role and the Organization name.
5. SKILLS: Extract technical tools and return them as a SINGLE comma-separated string, NOT an array.

JSON SCHEMA:
{
  ""jobs"": [
    {
      ""title"": ""string"",
      ""description"": ""string"",
      ""skills"": ""string (must be comma separated, do not use arrays)""
    }
  ]
}
Return ONLY valid JSON matching this exact schema.";
            var options = new ChatOptions
            {
                // 1. Shrink context to save VRAM. 4096 is enough for a CV and stops 
                // the model from "paging" to your slow system RAM.
                AdditionalProperties = new AdditionalPropertiesDictionary
                {
                    ["num_ctx"] = 4096
                },

                // 2. Increase the output limit. The default is often too low (128 or 256).
                // This is why your JSON was cutting off!
                MaxOutputTokens = 2048,

                // 3. Lower temperature for strict JSON logic.
                Temperature = 0.1f
            };
            try
            {
                var chatResponse = await _chatClient.GetResponseAsync(new[]
                {
                new ChatMessage(ChatRole.System, systemPrompt),
                new ChatMessage(ChatRole.User, cvText)
            }, cancellationToken: cancellationToken);

                var jsonString = chatResponse.Text;

                // Clean up any accidental markdown blocks Llama might output
                if (!string.IsNullOrWhiteSpace(jsonString))
                {
                    jsonString = jsonString.Replace("```json", "").Replace("```", "").Trim();
                }

                if (string.IsNullOrWhiteSpace(jsonString)) return null;

                // 1. SMART CLEAN: Find the first '{' and last '}'
                // This ignores "Here is the data:" or any other text Llama adds.
                var startIndex = jsonString.IndexOf('{');
                var endIndex = jsonString.LastIndexOf('}');

                if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
                {
                    jsonString = jsonString.Substring(startIndex, (endIndex - startIndex) + 1);
                }
                // Deserialize directly into your AIRequest class!
                var extractedData = JsonSerializer.Deserialize<AIRequest>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true, // Helps if Llama capitalizes "Jobs" instead of "jobs"
                    AllowTrailingCommas = true, // Llama often leaves a comma at the end of lists
                    ReadCommentHandling = JsonCommentHandling.Skip // In case it adds explanations
                });

                return extractedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract structured CV data using Llama 3.2.");
                return null;
            }
        }
    }
}
