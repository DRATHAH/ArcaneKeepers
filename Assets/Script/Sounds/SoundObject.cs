using UnityEngine;
using System.Collections;

public class SoundObject : MonoBehaviour
{
    public float pitchVar = 0.1f;
    public AudioSource sound;

    public void Initialize(Sound soundToPlay)
    {
        sound.clip = soundToPlay.clip;

        float pitch = Random.Range(-pitchVar, pitchVar);
        sound.pitch = soundToPlay.pitch;
        sound.pitch += pitch;
        sound.volume = soundToPlay.volume;

        sound.Play();
        StartCoroutine(DeleteAfterTime());
    }
    public void InitializePitchless(Sound soundToPlay)
    {
        sound.clip = soundToPlay.clip;

        sound.volume = soundToPlay.volume;

        sound.Play();
        StartCoroutine(DeleteAfterTime());
    }

    IEnumerator DeleteAfterTime()
    {
        float waitTime = 1;

        if (sound.clip)
        {
            waitTime = sound.clip.length + 1f;
        }
        yield return new WaitForSeconds(waitTime);
        Destroy(gameObject);
    }
}
