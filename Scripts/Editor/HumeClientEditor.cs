/*
 * Copyright (c) Doubling Technologies
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DoubTech.ThirdParty.AI.Hume
{
    [CustomEditor(typeof(HumeClient))]
    public class HumeClientEditor : Editor
    {
        private string text = "";
        private string description = "";
        private Texture2D waveformTexture;
        private AudioClip lastAudioClip;
        private string errorMessage = "";
        private string requestStatus = "";
        private string generationId = "";

        private void OnEnable()
        {
            HumeClient humeClient = (HumeClient)target;
            humeClient.OnAudioTaskChanged += OnAudioTaskChanged;
            humeClient.OnUtteranceStarted += OnUtteranceStarted;
            humeClient.OnUtteranceStopped += OnUtteranceStopped;
            humeClient.OnError += OnError;
            humeClient.OnRequestStatusChanged += OnRequestStatusChanged;
        }

        private void OnDisable()
        {
            HumeClient humeClient = (HumeClient)target;
            humeClient.OnAudioTaskChanged -= OnAudioTaskChanged;
            humeClient.OnUtteranceStarted -= OnUtteranceStarted;
            humeClient.OnUtteranceStopped -= OnUtteranceStopped;
            humeClient.OnError -= OnError;
            humeClient.OnRequestStatusChanged -= OnRequestStatusChanged;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            HumeClient humeClient = (HumeClient)target;

            var key = EditorGUILayout.PasswordField("API Key", humeClient.apiKey);
            // Apply changes to the object
            if (GUI.changed)
            {
                humeClient.apiKey = key;
                // Mark the object as dirty to ensure changes are saved
                EditorUtility.SetDirty(target);
            }

            GUILayout.Space(10);

            GUILayout.BeginVertical("Voice Selection", GUI.skin.box);
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.BeginVertical();

            // Voice selection dropdown
            GUILayout.Space(5);
            GUILayout.Label("Select Voice", EditorStyles.boldLabel);
            
            // Create a list of display options with "New Voice" as the first option
            List<string> voiceOptions = new List<string> { "New Voice" };
            
            // Add available voices to the options
            foreach (var voice in humeClient.AvailableVoices)
            {
                string displayName = string.IsNullOrEmpty(voice.Name) ? voice.Id : $"{voice.Name} ({voice.Id})";
                voiceOptions.Add(displayName);
            }

            // Find the current index
            int currentIndex = 0;
            if (!string.IsNullOrEmpty(humeClient.VoiceId))
            {
                for (int i = 0; i < humeClient.AvailableVoices.Count; i++)
                {
                    if (humeClient.AvailableVoices[i].Id == humeClient.VoiceId)
                    {
                        currentIndex = i + 1; // +1 because "New Voice" is at index 0
                        break;
                    }
                }
            }

            GUILayout.BeginHorizontal();
            // Display the dropdown
            int selectedIndex = EditorGUILayout.Popup("Voice", currentIndex, voiceOptions.ToArray());

            // Button to update voices
            if (GUILayout.Button("Refresh", GUILayout.Width(75)))
            {
                UpdateVoicesAsync(humeClient);
            }
            GUILayout.EndHorizontal();
            
            // Handle selection change
            if (selectedIndex != currentIndex)
            {
                if (selectedIndex == 0)
                {
                    // "New Voice" selected - clear the voice ID
                    humeClient.VoiceId = null;
                }
                else
                {
                    // A specific voice was selected
                    humeClient.VoiceId = humeClient.AvailableVoices[selectedIndex - 1].Id;
                }
                
                // Mark the object as dirty to ensure changes are saved
                EditorUtility.SetDirty(target);
            }

            GUILayout.EndVertical();
            GUILayout.Space(10);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.EndVertical();

            GUILayout.Space(24);

            GUILayout.BeginVertical("Input", GUI.skin.box);
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.BeginVertical();

            GUILayout.Label("Text", EditorStyles.miniLabel);
            text = EditorGUILayout.TextArea(text, GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 3));

            GUILayout.Label("Description", EditorStyles.miniLabel);
            description = EditorGUILayout.TextArea(description, GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 3));

            GUILayout.Space(10);
            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("Speak"))
            {
                humeClient.Speak(text, description);
            }
            GUI.enabled = true;

            GUILayout.EndVertical();
            GUILayout.Space(10);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.EndVertical();

            GUILayout.Space(24);

            GUILayout.BeginVertical("Status", GUI.skin.box);
            GUILayout.Space(10);

            GUILayout.Label("Request Status", EditorStyles.boldLabel);
            GUILayout.Label(requestStatus);

            if (!string.IsNullOrEmpty(generationId))
            {
                if (GUILayout.Button($"Generation ID: {generationId}"))
                {
                    EditorGUIUtility.systemCopyBuffer = generationId;
                }
            }

            GUILayout.Space(10);
            GUILayout.EndVertical();

            GUILayout.Space(24);

            GUILayout.BeginVertical("Debugging", GUI.skin.box);
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.BeginVertical();

            var context = humeClient.UtteranceContext;
            if (null != context && context.Length > 0)
            {
                GUILayout.Label("Utterance Context", EditorStyles.boldLabel);
                foreach (var utterance in context)
                {
                    GUILayout.Label($"Text: {utterance.Text}");
                    GUILayout.Label($"Description: {utterance.Description}");
                    GUILayout.Space(5);
                }
            }

            GUILayout.Space(10);
            GUILayout.Label("Current Audio Clip", EditorStyles.boldLabel);
            if (humeClient.CurrentAudioClip != null)
            {
                GUILayout.Label($"Name: {humeClient.CurrentAudioClip.name}");
                GUILayout.Label($"Length: {humeClient.CurrentAudioClip.length} seconds");

                // Draw waveform preview
                if (waveformTexture != null)
                {
                    float aspectRatio = (float)waveformTexture.width / waveformTexture.height;
                    float viewWidth = EditorGUIUtility.currentViewWidth - 40; // Adjust for padding
                    float viewHeight = viewWidth / aspectRatio;
                    GUILayout.Label(waveformTexture, GUILayout.Width(viewWidth), GUILayout.Height(viewHeight));
                }
            }
            else
            {
                GUILayout.Label("No audio clip is currently playing.");
            }

            GUILayout.Space(10);
            GUILayout.Label("Playback Controls", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Play"))
            {
                humeClient.Play();
            }
            if (GUILayout.Button("Pause"))
            {
                humeClient.Pause();
            }
            if (GUILayout.Button("Stop"))
            {
                humeClient.Stop();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.EndVertical();
            GUILayout.Space(10);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.EndVertical();

            GUILayout.Space(24);

            GUILayout.BeginVertical("Errors", GUI.skin.box);
            GUILayout.Space(10);

            GUILayout.Label("Errors", EditorStyles.boldLabel);
            GUILayout.Label(errorMessage, EditorStyles.wordWrappedLabel);

            GUILayout.Space(10);
            GUILayout.EndVertical();
        }

        private void OnAudioTaskChanged(AudioClip audioClip)
        {
            if (audioClip != lastAudioClip)
            {
                waveformTexture = GenerateWaveformTexture(audioClip);
                lastAudioClip = audioClip;
            }
            Repaint();
        }

        private void OnUtteranceStarted(Utterance utterance)
        {
            Repaint();
        }

        private void OnUtteranceStopped(Utterance utterance)
        {
            Repaint();
        }

        private void OnError(string message)
        {
            errorMessage = message;
            Repaint();
        }

        private void OnRequestStatusChanged(string status, AudioTask? audioTask)
        {
            requestStatus = status;
            generationId = audioTask?.GenerationId;
            Repaint();
        }

        /// <summary>
        /// Calls the UpdateVoices method on the HumeClient asynchronously
        /// </summary>
        private async void UpdateVoicesAsync(HumeClient humeClient)
        {
            bool success = await humeClient.UpdateVoices();
            if (success)
            {
                Debug.Log("Successfully updated available voices.");
            }
            else
            {
                Debug.LogError("Failed to update available voices.");
            }
            Repaint();
        }

        private Texture2D GenerateWaveformTexture(AudioClip audioClip)
        {
            if (audioClip == null) return null;
        
            float[] samples = new float[audioClip.samples * audioClip.channels];
            audioClip.GetData(samples, 0);
        
            int width = 500; // Adjust this to fit the view's width dynamically if needed
            int height = 100;
            Texture2D texture = new Texture2D(width, height);
        
            // Clear to transparent
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        
            // Normalize the waveform
            float maxSample = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                if (Mathf.Abs(samples[i]) > maxSample)
                {
                    maxSample = Mathf.Abs(samples[i]);
                }
            }
            if (maxSample > 0f)
            {
                for (int i = 0; i < samples.Length; i++)
                {
                    samples[i] /= maxSample;
                }
            }
        
            // Draw the waveform
            for (int x = 0; x < width; x++)
            {
                int sampleIndex = (int)((float)x / width * samples.Length);
                float sampleValue = samples[sampleIndex] * 0.5f + 0.5f;
                int y = (int)(sampleValue * height);
        
                // Connect samples as lines
                if (x > 0)
                {
                    int prevSampleIndex = (int)((float)(x - 1) / width * samples.Length);
                    float prevSampleValue = samples[prevSampleIndex] * 0.5f + 0.5f;
                    int prevY = (int)(prevSampleValue * height);
        
                    int minY = Mathf.Min(y, prevY);
                    int maxY = Mathf.Max(y, prevY);
                    for (int lineY = minY; lineY <= maxY; lineY++)
                    {
                        texture.SetPixel(x, lineY, Color.blue);
                    }
                }
                else
                {
                    texture.SetPixel(x, y, Color.blue);
                }
            }
        
            texture.Apply();
            return texture;
        }
    }
}