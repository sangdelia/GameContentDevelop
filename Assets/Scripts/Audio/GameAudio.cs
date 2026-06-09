using System.Collections.Generic;
using UnityEngine;

public static class GameAudio
{
    private const float DefaultVolume = 0.75f;
    private static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();
    private static readonly Dictionary<string, float> LastPlayedTimes = new Dictionary<string, float>();

    public static void PlayPlayerShoot(Vector3 position)
    {
        Play("sfx_player_gunshot", position, 0.85f, 0.035f, 0.96f, 1.04f);
        Play("sfx_player_shoot_body", position, 0.45f, 0.035f, 0.9f, 1.0f);
        Play("sfx_player_shoot_click", position, 0.32f, 0.035f, 1.05f, 1.18f);
    }

    public static void PlayHit(Vector3 position)
    {
        Play("sfx_hit", position, 0.5f, 0.06f);
    }

    public static void PlayEnemyDie(Vector3 position)
    {
        Play("sfx_enemy_die", position, 0.65f, 0.08f);
    }

    public static void PlayPickup(Vector3 position)
    {
        Play("sfx_pickup", position, 0.45f, 0.05f);
    }

    public static void PlayLevelUp(Vector3 position)
    {
        Play("sfx_level_up", position, 0.85f, 0.1f);
    }

    public static void PlayPortalOpen(Vector3 position)
    {
        Play("sfx_portal_open", position, 0.85f, 0.2f);
    }

    public static void PlayBossWarning(Vector3 position)
    {
        Play("sfx_warning", position, 0.7f, 0.4f);
    }

    public static void PlayBossLaser(Vector3 position)
    {
        Play("sfx_boss_laser", position, 0.85f, 0.12f);
    }

    private static void Play(
        string clipName,
        Vector3 position,
        float volume = DefaultVolume,
        float cooldown = 0f,
        float minPitch = 1f,
        float maxPitch = 1f)
    {
        if (cooldown > 0f && LastPlayedTimes.TryGetValue(clipName, out float lastPlayed) && Time.time - lastPlayed < cooldown)
            return;

        AudioClip clip = GetClip(clipName);

        if (clip == null)
            return;

        LastPlayedTimes[clipName] = Time.time;
        PlayClipAtPoint(clip, position, volume, Random.Range(minPitch, maxPitch));
    }

    private static void PlayClipAtPoint(AudioClip clip, Vector3 position, float volume, float pitch)
    {
        GameObject audioObject = new GameObject("OneShotAudio_" + clip.name);
        audioObject.transform.position = position;

        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.spatialBlend = 0.15f;
        source.Play();

        Object.Destroy(audioObject, clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch)) + 0.05f);
    }

    private static AudioClip GetClip(string clipName)
    {
        if (Clips.TryGetValue(clipName, out AudioClip clip))
            return clip;

        clip = Resources.Load<AudioClip>("Audio/" + clipName);
        Clips[clipName] = clip;
        return clip;
    }
}
