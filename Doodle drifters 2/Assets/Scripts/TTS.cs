using System;
using System.Collections.Generic;
using Lingotion.Thespeon.Core;
using Lingotion.Thespeon.Engine;
using Lingotion.Thespeon.Inputs;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using Unity.Burst;
#endif

/// <summary>
/// An advanced character controller that uses the Thespeon engine for real-time voice synthesis.
/// This example demonstrates how to use advanced features such as custom speed, loudness, and language switching.
/// </summary>
[RequireComponent(typeof(ThespeonComponent))]
[RequireComponent(typeof(AudioSource))]
public class TTS : MonoBehaviour
{
    [Header("Debug Sample (Legacy)")]
    [SerializeField] private bool enableLegacySampleHotkey = false;

    private ThespeonComponent engine;
    private AudioSource audioSource;
    private List<float> audioData;
    private AudioClip audioClip;
    public ThespeonCharacterAsset characterAsset;
    private string inputLineStart;
    private string inputLineStory;
    private string inputLinePrompt;
    private Emotion inputEmotionStart;
    private Emotion inputEmotionStory;
    private Emotion inputEmotionPrompt;
    private AnimationCurve speed;
    private AnimationCurve loudness;

    void Start()
    {
#if UNITY_EDITOR
        if (BurstCompiler.Options.EnableBurstDebug)
        {
            Debug.LogWarning("[Warning] Burst Native Debug Mode Compilation is ON; performance will be slower in Editor when running Thespeon on CPU.");
        }
#endif
        engine = GetComponent<ThespeonComponent>();
        // Register audio receive callback and final package callback
        engine.OnAudioReceived += OnAudioPacketReceive;
        engine.OnSynthesisComplete += OnFinalPacketReceived;

        audioData = new List<float>();
        audioClip = AudioClip.Create("ThespeonClip", 1024, 1, 44100, true, OnAudioRead);
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = audioClip;
        audioSource.loop = true;
        audioSource.Play();


        speed = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.5f, 1.5f),
            new Keyframe(0.9f, 0.7f),
            new Keyframe(1f, 0.5f)
        );

        loudness = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.5f, 0.7f),
            new Keyframe(1f, 1f)
        );
        LingotionLogger.CurrentLevel = VerbosityLevel.Warning;
        if (characterAsset == null)
        {
            LingotionLogger.Warning("Assign a character asset in the Example Character inspector window if you want to use a specific character. \nYou will find the assets under Assets > Lingotion Thespeon > CharacterAssets.");
            characterAsset = ScriptableObject.CreateInstance<ThespeonCharacterAsset>();
            return;
        }

        engine.TryPreloadCharacter(characterAsset.characterName, characterAsset.moduleType, runWarmup: true);
        LingotionLogger.CurrentLevel = new InferenceConfig().Verbosity;

        SetPrompts();
    }

    void Update()
    {
        if (!enableLegacySampleHotkey)
        {
            return;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
        {
            SpeakStructuredSample();
        }
    }

    /// <summary>
    /// Public helper to synthesize a single line of text from other gameplay scripts.
    /// </summary>
    public void SpeakText(string text, string sessionID = "OllamaSession", Emotion emotion = Emotion.Serenity)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            LingotionLogger.Warning("SpeakText called with empty text.");
            return;
        }

        if (engine == null || characterAsset == null)
        {
            LingotionLogger.Warning("TTS is not ready. Ensure character asset and components are initialized.");
            return;
        }

        engine.TryPreloadCharacter(characterAsset.characterName, characterAsset.moduleType, runWarmup: true);

        List<ThespeonInputSegment> segments = new()
        {
            new(text, emotion: emotion)
        };

        ThespeonInput input = new(
            segments,
            characterAsset.characterName,
            characterAsset.moduleType,
            defaultEmotion: emotion,
            defaultLanguage: "",
            defaultDialect: "",
            speed: speed,
            loudness: loudness);

        engine.Synthesize(input, sessionID: sessionID);
    }

    private void SpeakStructuredSample()
    {
        char pauseChar = (char)ControlCharacters.Pause;
        List<ThespeonInputSegment> segments = new() {
            new(inputLineStart + $"{pauseChar}", emotion: inputEmotionStart),
            new(inputLineStory + $"{pauseChar}", emotion: inputEmotionStory),
            new($"{pauseChar}" + inputLinePrompt, emotion: inputEmotionPrompt)
        };
        ThespeonInput input = new(segments, characterAsset.characterName, characterAsset.moduleType, defaultEmotion: Emotion.Joy, defaultLanguage: "", defaultDialect: "", speed: speed, loudness: loudness);
        engine.Synthesize(input, sessionID: "SampleSynthesisSession");
    }

    void SetPrompts()
    {
        inputLineStart = "You enter a huge treasure room, with tons of gold and chests!";
        inputLineStory = "...and then you suddenly see three big angry ogres charging at you!!!";
        inputLinePrompt = "Quick...! Draw your weapons!";
        inputEmotionStart = Emotion.Admiration;
        inputEmotionStory = Emotion.Remorse;
        inputEmotionPrompt = Emotion.Anticipation;
    }

    // Simply add the received data to the audio buffer. 
    void OnAudioPacketReceive(string sessionID, float[] data)
    {
        lock (audioData)
        {
            audioData.AddRange(data);
        }
    }

    // Whenever the Unity audio thread needs data, it calls this function for us to fill the float[] data. 
    void OnAudioRead(float[] data)
    {
        lock (audioData)
        {
            int currentCopyLength = Mathf.Min(data.Length, audioData.Count);
            // take slice of buffer
            audioData.CopyTo(0, data, 0, currentCopyLength);
            audioData.RemoveRange(0, currentCopyLength);
            if (currentCopyLength < data.Length)
            {
                Array.Fill(data, 0f, currentCopyLength, data.Length - currentCopyLength);
            }
        }
    }

    private void OnFinalPacketReceived(string sessionID)
    {
        LingotionLogger.Info($"Synthesis complete for session: {sessionID}");
        engine.TryUnloadCharacter(characterAsset.characterName, characterAsset.moduleType);
    }

    void OnDestroy()
    {
        engine.OnAudioReceived -= OnAudioPacketReceive;
        engine.OnSynthesisComplete -= OnFinalPacketReceived;
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = null; // Clear the clip to release resources
        }
    }

}