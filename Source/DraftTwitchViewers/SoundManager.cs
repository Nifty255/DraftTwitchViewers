using System.Collections.Generic;
using UnityEngine;

namespace DraftTwitchViewers
{
    class SoundManager
    {
        /// <summary>
        /// The static instance of this object.
        /// </summary>
        static SoundManager instance;

        /// <summary>
        /// A GameObject which follows the main camera.
        /// </summary>
        static GameObject cameraFollower;

        /// <summary>
        /// A collection of AudioClips
        /// </summary>
        Dictionary<string, AudioClip> Sounds;

        /// <summary>
        /// Is this object initialized?
        /// </summary>
        public static bool IsInitialized
        {
            get
            {
                return (instance != null);
            }
        }

        /// <summary>
        /// Creates a new SoundManager.
        /// </summary>
        SoundManager()
        {
            Sounds = new Dictionary<string, AudioClip>();
        }

        /// <summary>
        /// Initializes the static SoundManager.
        /// </summary>
        public static void Initialize()
        {
            if (instance == null)
            {
                instance = new SoundManager();
                cameraFollower = new GameObject();
                cameraFollower.AddComponent<DTVCamFollower>();
                Logger.DebugLog("Sound Manager STARTED.");
            }
        }

        /// <summary>
        /// Loads a sound into the collection.
        /// </summary>
        /// <param name="filePath">The path to the sound.</param>
        /// <param name="soundName">The name of the sound entry in the collection.</param>
        public static void LoadSound(string filePath, string soundName)
        {
            if (instance != null)
            {
                foreach (KeyValuePair<string, AudioClip> pair in instance.Sounds)
                {
                    if (pair.Key == soundName)
                    {
                        return;
                    }
                }

                if (GameDatabase.Instance.ExistsAudioClip(filePath))
                {
                    instance.Sounds.Add(soundName, GameDatabase.Instance.GetAudioClip(filePath));
                    Logger.DebugLog("Loaded: " + soundName);
                }
                else
                {
                    Logger.DebugError("ERROR: Sound \"" + soundName + "\" not found in the database!");
                }
            }
            else
            {
                Initialize();
                LoadSound(filePath, soundName);
            }
        }

        /// <summary>
        /// Gets the specified sound from the collection.
        /// </summary>
        /// <param name="soundName">The name of the sound entry in the collection.</param>
        /// <returns>An AudioClip object representing the sound.</returns>
        public static AudioClip GetSound(string soundName)
        {
            try
            {
                return instance.Sounds[soundName];
            }
            catch
            {
                Logger.DebugError("ERROR: AudioClip \"" + soundName + "\" not found! Ensure it is being properly loaded.");
                return null;
            }
        }

        /// <summary>
        /// Creates a sound and assigns it to the camera following GameObject.
        /// </summary>
        /// <param name="defaultSound">The name of the sound entry in the collection.</param>
        /// <param name="loop">Should this sound loop until stopped?</param>
        /// <returns>The AudioSource object representing the sound.</returns>
        public static AudioSource CreateSound(string defaultSound, bool loop)
        {
            AudioSource audio = cameraFollower.AddComponent<AudioSource>();
            audio.volume = GameSettings.SHIP_VOLUME;
            audio.rolloffMode = AudioRolloffMode.Linear;
            audio.dopplerLevel = 0f;
            audio.panLevel = 1f;
            audio.maxDistance = 30f;
            audio.loop = loop;
            audio.playOnAwake = false;
            audio.clip = GetSound(defaultSound);

            return audio;
        }
    }
}