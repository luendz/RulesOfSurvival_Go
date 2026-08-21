using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ROS.Game.Audio
{
    public static class RandomAudioPlayer
    {
        private static readonly Dictionary<int, int> _lastIndex =
            new Dictionary<int, int>();

        public static void Play(AudioSource source, AudioClip[] clips)
        {
            if (source == null || clips == null || clips.Length == 0)
                return;

            AudioClip clip = Pick(clips);
            if (clip != null)
                source.PlayOneShot(clip);
        }

        public static AudioClip Pick(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
                return null;

            if (clips.Length == 1)
                return clips[0];

            int id = RuntimeHelpers.GetHashCode(clips);
            _lastIndex.TryGetValue(id, out int last);

            int index;
            do
            {
                index = Random.Range(0, clips.Length);
            }
            while (index == last);

            _lastIndex[id] = index;
            return clips[index];
        }
    }
}
