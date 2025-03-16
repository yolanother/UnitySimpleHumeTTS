/*
 * Copyright (c) Doubling Technologies
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace DoubTech.ThirdParty.AI.Hume
{
    /// <summary>
    /// Represents a custom voice from the Hume API
    /// </summary>
    [Serializable]
    public class CustomVoice
    {
        [JsonProperty("id")]
        public string Id;
        
        [JsonProperty("version")]
        public int Version;
        
        [JsonProperty("name")]
        public string Name;
        
        [JsonProperty("created_on")]
        public long CreatedOn;
        
        [JsonProperty("modified_on")]
        public long ModifiedOn;
        
        [JsonProperty("base_voice")]
        public string BaseVoice;
        
        [JsonProperty("parameter_model")]
        public string ParameterModel;
        
        [JsonProperty("parameters")]
        public Dictionary<string, object> Parameters;
    }

    /// <summary>
    /// Response from the Hume API for custom voices
    /// </summary>
    [Serializable]
    public class CustomVoicesResponse
    {
        [JsonProperty("page_number")]
        public int PageNumber;
        
        [JsonProperty("page_size")]
        public int PageSize;
        
        [JsonProperty("total_pages")]
        public int TotalPages;
        
        [JsonProperty("custom_voices_page")]
        public List<CustomVoice> CustomVoicesPage;
    }

    /// <summary>
    /// Request to create a custom voice
    /// </summary>
    [Serializable]
    public class CreateVoiceRequest
    {
        [JsonProperty("generation_id")]
        public string GenerationId;
        
        [JsonProperty("name")]
        public string Name;
    }

    /// <summary>
    /// Response from the Hume API for creating a custom voice
    /// </summary>
    [Serializable]
    public class CreateVoiceResponse
    {
        [JsonProperty("name")]
        public string Name;
        
        [JsonProperty("id")]
        public string Id;
    }
    
    public struct AudioTask
    {
        public AudioClip Clip;
        public TaskCompletionSource<bool> TaskCompletionSource;
        public Utterance Utterance;
        public string GenerationId;
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum Provider
    {
        [EnumMember(Value = "HUME_AI")]
        Hume,
        [EnumMember(Value = "CUSTOM_VOICE")]
        CustomVoice
    }

    public class Voice 
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id;
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name;
        
        [JsonProperty("provider", NullValueHandling = NullValueHandling.Ignore)]
        public Provider? Provider
        {
            get;
            set;
        }
    }

    public struct Utterance
    {
        [JsonProperty("text")]
        public string Text;
        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description;
        [JsonProperty("voice", NullValueHandling = NullValueHandling.Ignore)]
        public Voice Voice;
    }

    internal class RequestBody
    {
        [JsonProperty("utterances")]
        public Utterance[] Utterances { get; set; }
        [JsonProperty("context", NullValueHandling = NullValueHandling.Ignore)]
        public Context Context { get; set; }
        [JsonProperty("format")]
        public Format Format { get; set; }
        [JsonProperty("num_generations")]
        public int NumGenerations { get; set; } = 1;
    }

    internal class Context
    {
        [JsonProperty("utterances", NullValueHandling = NullValueHandling.Ignore)]
        public Utterance[] Utterances { get; set; }
        [JsonProperty("generation_id", NullValueHandling = NullValueHandling.Ignore)]
        public string GenerationId { get; set; }
    }

    internal class Format
    {
        [JsonProperty("type")]
        public string Type { get; set; }
    }

    internal class ResponseBody
    {
        [JsonProperty("generations")]
        public Generation[] Generations { get; set; }
    }

    internal class Generation
    {
        [JsonProperty("audio")]
        public string Audio { get; set; }
        [JsonProperty("generation_id", NullValueHandling = NullValueHandling.Ignore)]
        public string GenerationId { get; set; }
    }
}