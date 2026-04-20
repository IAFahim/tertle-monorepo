// <copyright file="AudioVolumeDataTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Tests.Data
{
    using BovineLabs.Bridge.Data.Audio;
    using NUnit.Framework;

    public class AudioVolumeDataTests
    {
        [Test]
        public void SharedStatics_CanBeUpdatedAndRead()
        {
            var previousMute = AudioVolumeData.MuteWhenUnfocused.Data;
            var previousMusic = AudioVolumeData.MusicVolume.Data;
            var previousAmbiance = AudioVolumeData.AmbianceVolume.Data;
            var previousEffect = AudioVolumeData.EffectVolume.Data;

            try
            {
                AudioVolumeData.MuteWhenUnfocused.Data = true;
                AudioVolumeData.MusicVolume.Data = 0.25f;
                AudioVolumeData.AmbianceVolume.Data = 0.5f;
                AudioVolumeData.EffectVolume.Data = 0.75f;

                Assert.IsTrue(AudioVolumeData.MuteWhenUnfocused.Data);
                Assert.That(AudioVolumeData.MusicVolume.Data, Is.EqualTo(0.25f).Within(0.0001f));
                Assert.That(AudioVolumeData.AmbianceVolume.Data, Is.EqualTo(0.5f).Within(0.0001f));
                Assert.That(AudioVolumeData.EffectVolume.Data, Is.EqualTo(0.75f).Within(0.0001f));
            }
            finally
            {
                AudioVolumeData.MuteWhenUnfocused.Data = previousMute;
                AudioVolumeData.MusicVolume.Data = previousMusic;
                AudioVolumeData.AmbianceVolume.Data = previousAmbiance;
                AudioVolumeData.EffectVolume.Data = previousEffect;
            }
        }
    }
}
#endif
