/*
 * Copyright (c) Doubling Technologies
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace DoubTech.ThirdParty.AI.Hume
{
    /// <summary>
    /// HumeClient is responsible for interacting with the Hume API to generate and play back audio.
    /// </summary>
    public class HumeClient : MonoBehaviour
    {
        #region API Configuration

        /// <summary>
        /// The API key for authenticating with the Hume API.
        /// </summary>
        [Header("API Configuration")]
        [SerializeField, HideInInspector, Tooltip("The API key for authenticating with the Hume API.")]
        public string apiKey;

        #endregion

        #region Voice Settings

        /// <summary>
        /// The voice ID to be used for generating audio.
        /// </summary>
        [Header("Voice Settings")]
        [SerializeField, Tooltip("The voice ID to be used for generating audio.")]
        private string voiceId;

        /// <summary>
        /// The available voices that can be selected.
        /// </summary>
        [SerializeField, HideInInspector, Tooltip("The available voices that can be selected.")]
        private List<CustomVoice> availableVoices = new List<CustomVoice>();

        #endregion

        #region Audio Settings

        /// <summary>
        /// The AudioSource component for playing back audio.
        /// </summary>
        [Header("Audio Settings")]
        [SerializeField, Tooltip("The AudioSource component for playing back audio.")]
        private AudioSource audioSource;

        #endregion

        #region Context Settings

        /// <summary>
        /// The number of previous utterances to include as context.
        /// </summary>
        [Header("Context Settings")]
        [SerializeField, Tooltip("The number of previous utterances to include as context.")]
        private int contextSize = 2;

        #endregion

        #region Private Fields

        private readonly HttpClient _httpClient = new HttpClient();
        private readonly Queue<AudioTask> _audioQueue = new Queue<AudioTask>();
        private readonly List<Utterance> _utteranceHistory = new List<Utterance>();
        private Coroutine _playbackCoroutine;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the history of utterances.
        /// </summary>
        public List<Utterance> UtteranceHistory => _utteranceHistory;

        /// <summary>
        /// Gets the context of the current utterance.
        /// </summary>
        public Utterance[] UtteranceContext
        {
            get
            {
                if (_utteranceHistory.Count == 0 || contextSize == 0)
                {
                    return null;
                }

                return _utteranceHistory.TakeLast(contextSize).ToArray();
            }
        }

        /// <summary>
        /// Gets or sets the voice ID to be used for generating audio.
        /// </summary>
        /// <value>The voice ID to be used for generating audio.</value>
        /// <remarks>
        /// The voice ID is used to specify the voice to be used for generating audio.
        /// </remarks>
        public string VoiceId
        {
            get => string.IsNullOrEmpty(voiceId) ? null : voiceId;
            set => voiceId = value;
        }

        /// <summary>
        /// Gets the list of available voices that can be selected.
        /// </summary>
        public List<CustomVoice> AvailableVoices => availableVoices;

        /// <summary>
        /// Gets a value indicating whether the client has context.
        /// </summary>
        public bool HasContext => null != UtteranceContext;

        /// <summary>
        /// Gets the current audio clip being played.
        /// </summary>
        public AudioClip CurrentAudioClip => audioSource.clip;

        /// <summary>
        /// Occurs when the audio task changes.
        /// </summary>
        public event Action<AudioClip> OnAudioTaskChanged;

        /// <summary>
        /// Occurs when an utterance starts playing.
        /// </summary>
        public event Action<Utterance> OnUtteranceStarted;

        /// <summary>
        /// Occurs when an utterance stops playing.
        /// </summary>
        public event Action<Utterance> OnUtteranceStopped;

        /// <summary>
        /// Occurs when an error occurs.
        /// </summary>
        public event Action<string> OnError;

        /// <summary>
        /// Occurs when the request status changes.
        /// </summary>
        public event Action<string, AudioTask?> OnRequestStatusChanged;

        #endregion

        #region Public Methods

        /// <summary>
        /// Generates and plays back audio using the Hume API.
        /// </summary>
        /// <param name="text">The text to be converted to speech.</param>
        /// <param name="description">The description for the speech generation.</param>
        public async Task<bool> Speak(string text, string description = "")
        {
            OnRequestStatusChanged?.Invoke("Requesting...", null);
            var utterance = new Utterance
            {
                Text = text,
                Description = string.IsNullOrEmpty(description) ? null : description,
                Voice = string.IsNullOrEmpty(VoiceId) ? null : new Voice { Id = VoiceId }
            };
            var requestBody = new RequestBody
            {
                Utterances = new[] { utterance },
                Context = HasContext ? new Context
                {
                    Utterances = UtteranceContext
                } : null,
                Format = new Format
                {
                    Type = "pcm"
                },
                NumGenerations = 1
            };

            var json = JsonConvert.SerializeObject(requestBody, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.hume.ai/v0/tts")
            {
                Headers =
                {
                    { "X-Hume-Api-Key", apiKey }
                },
                Content = content
            };

            string responseBody = string.Empty;
            try
            {
                var response = await _httpClient.SendAsync(request);
                
                responseBody = await response.Content.ReadAsStringAsync();
                response.EnsureSuccessStatusCode();

                var responseJson = JsonConvert.DeserializeObject<ResponseBody>(responseBody);

                string audioBase64 = responseJson.Generations[0].Audio;
                string generationId = responseJson.Generations[0].GenerationId;
                byte[] audioBytes = Convert.FromBase64String(audioBase64);

                // Create an audio clip from the byte array
                var audioClip = ToAudioClip(audioBytes, 24000);

                // Create a task completion source for the audio clip
                var taskCompletionSource = new TaskCompletionSource<bool>();

                // Queue the audio clip for playback
                var audioTask = new AudioTask { Clip = audioClip, TaskCompletionSource = taskCompletionSource, Utterance = utterance, GenerationId = generationId };
                _audioQueue.Enqueue(audioTask);

                // Add the current utterance to the history
                _utteranceHistory.Add(audioTask.Utterance);

                // Start the playback coroutine if not already running
                if (_playbackCoroutine == null)
                {
                    _playbackCoroutine = StartCoroutine(PlayAudioQueue());
                }

                OnRequestStatusChanged?.Invoke($"Request completed. Generation ID: {generationId}", audioTask);
                // Wait for the task to complete
                return await taskCompletionSource.Task;
            }
            catch (HttpRequestException ex)
            {
                string formattedResponseBody = FormatJson(responseBody);
                string requestString = JsonConvert.SerializeObject(requestBody, Formatting.Indented);
                OnError?.Invoke($"Request failed: {ex.Message}\n\nRequest: {requestString}\n\nResponse: {formattedResponseBody}");
                OnRequestStatusChanged?.Invoke("Request failed.", null);
                return false;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Request failed: {ex.Message}");
                OnRequestStatusChanged?.Invoke("Request failed.", null);
                return false;
            }
        }

        /// <summary>
        /// Generates and plays back audio using the Hume API. Callable from the inspector.
        /// </summary>
        /// <param name="text">The text to be converted to speech.</param>
        public void Speak(string text)
        {
            Speak(text, string.Empty);
        }

        /// <summary>
        /// Updates the list of available voices from the Hume API.
        /// </summary>
        /// <returns>A task representing the asynchronous operation. Returns true if successful, false otherwise.</returns>
        public async Task<bool> UpdateVoices()
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                OnError?.Invoke("API key is not set.");
                return false;
            }

            OnRequestStatusChanged?.Invoke("Fetching available voices...", null);

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://api.hume.ai/v0/evi/custom_voices")
                {
                    Headers =
                    {
                        { "X-Hume-Api-Key", apiKey }
                    }
                };

                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                Debug.Log(responseBody);
                response.EnsureSuccessStatusCode();

                var voicesResponse = JsonConvert.DeserializeObject<CustomVoicesResponse>(responseBody);
                
                // Update the available voices list
                availableVoices.Clear();
                if (voicesResponse?.CustomVoicesPage != null)
                {
                    availableVoices.AddRange(voicesResponse.CustomVoicesPage);
                }

                OnRequestStatusChanged?.Invoke($"Successfully fetched {availableVoices.Count} voices.", null);
                return true;
            }
            catch (HttpRequestException ex)
            {
                string formattedResponseBody = FormatJson(ex.Message);
                OnError?.Invoke($"Failed to fetch voices: {formattedResponseBody}");
                OnRequestStatusChanged?.Invoke("Failed to fetch voices.", null);
                return false;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Failed to fetch voices: {ex.Message}");
                OnRequestStatusChanged?.Invoke("Failed to fetch voices.", null);
                return false;
            }
        }

        /// <summary>
        /// Pauses the audio playback.
        /// </summary>
        public void Pause()
        {
            if (audioSource.isPlaying)
            {
                audioSource.Pause();
            }
        }

        /// <summary>
        /// Resumes the audio playback.
        /// </summary>
        public void Play()
        {
            if (!audioSource.isPlaying && audioSource.clip != null)
            {
                audioSource.Play();
            }
        }

        /// <summary>
        /// Stops the audio playback and flushes the queue.
        /// </summary>
        public void Stop()
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            while (_audioQueue.Count > 0)
            {
                var audioTask = _audioQueue.Dequeue();
                audioTask.TaskCompletionSource.SetResult(false);
            }
            if (_playbackCoroutine != null)
            {
                StopCoroutine(_playbackCoroutine);
                _playbackCoroutine = null;
            }
            audioSource.clip = null;
            OnAudioTaskChanged?.Invoke(null);
        }

        /// <summary>
        /// Flushes the audio queue but continues playing the current clip.
        /// </summary>
        public void FlushQueue()
        {
            while (_audioQueue.Count > 0)
            {
                var audioTask = _audioQueue.Dequeue();
                audioTask.TaskCompletionSource.SetResult(false);
            }
        }

        #endregion

        #region Private Methods

        private string FormatJson(string json)
        {
            try
            {
                var parsedJson = JsonConvert.DeserializeObject(json);
                return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
            }
            catch
            {
                return json;
            }
        }

        /// <summary>
        /// Coroutine to play audio clips from the queue sequentially.
        /// </summary>
        private IEnumerator<WaitForSeconds> PlayAudioQueue()
        {
            while (_audioQueue.Count > 0)
            {
                var audioTask = _audioQueue.Dequeue();
                audioSource.clip = audioTask.Clip;
                OnAudioTaskChanged?.Invoke(audioSource.clip);
                OnUtteranceStarted?.Invoke(audioTask.Utterance);
                audioSource.Play();

                while (audioSource.isPlaying || (audioSource.time > 0 && audioSource.time < audioSource.clip.length)) 
                {
                    yield return null;
                }

                audioTask.TaskCompletionSource.SetResult(true);
                OnUtteranceStopped?.Invoke(audioTask.Utterance);
            }

            // Stop the coroutine when the queue is empty
            _playbackCoroutine = null;
        }

        /// <summary>
        /// Converts a PCM byte array to an AudioClip.
        /// </summary>
        /// <param name="pcmData">The PCM byte array.</param>
        /// <param name="sampleRate">The sample rate of the audio.</param>
        /// <returns>The generated AudioClip.</returns>
        private AudioClip ToAudioClip(byte[] pcmData, int sampleRate)
        {
            int sampleCount = pcmData.Length / 2;
            float[] audioData = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                short sample = BitConverter.ToInt16(pcmData, i * 2);
                audioData[i] = sample / 32768.0f;
            }

            AudioClip audioClip = AudioClip.Create("GeneratedAudio", sampleCount, 1, sampleRate, false);
            audioClip.SetData(audioData, 0);

            return audioClip;
        }

        #endregion
    }
}