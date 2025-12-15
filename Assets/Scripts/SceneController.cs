using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR;

public class SceneController : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup faderCanvasGroup; 
    public AudioSource backgroundMusic;

    [Header("Flow Settings")]
    public string nextSceneName = "IntroScene"; // Normal flow
    public string restartSceneName = "IntroScene"; // RESET flow
    
    [Header("Scene Configuration")]
    public bool isIntroScene = false;       
    public float waitTime = 15f; 

    // Private State
    private bool isFadingOut = false;
    private bool isExperienceEnded = false; 
    private List<InputDevice> devices = new List<InputDevice>();

    IEnumerator Start()
    {
        if (backgroundMusic == null) backgroundMusic = GetComponent<AudioSource>();

        // 1. SETUP VISUALS
        if (isIntroScene)
        {
            faderCanvasGroup.alpha = 0; // Scene 1: Clear
        }
        else
        {
            faderCanvasGroup.alpha = 1; // Scene 2: Black
            yield return StartCoroutine(FadeInRoutine());
        }

        // 2. WAIT LOOP
        float timer = 0;
        while (timer < waitTime)
        {
            // CHECK 1: Universal Reset (B/Y Button)
            if (CheckForResetButton()) 
            {
                SceneManager.LoadScene(restartSceneName);
                yield break;
            }

            // CHECK 2: Skip Forward (Trigger/A Button)
            if (CheckForSkipButton() || Input.GetKeyDown(KeyCode.Space))
            {
                break; // Skip the wait
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // 3. EXIT
        StartCoroutine(FadeOutAndSwitch());
    }

    void Update()
    {
        // ALWAYS LISTEN FOR RESET (B/Y Button)
        // Works during fading, working during scenes, works at the end.
        if (CheckForResetButton()) 
        {
            SceneManager.LoadScene(restartSceneName);
        }
    }

    // --- CONTROLLER MAPPINGS ---

    // Use this for "NEXT" or "SKIP"
    bool CheckForSkipButton()
    {
        InputDevices.GetDevices(devices);
        foreach (var device in devices)
        {
            // Primary Trigger OR Primary Button (A / X)
            if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool isTrigger) && isTrigger) return true;
            if (device.TryGetFeatureValue(CommonUsages.primaryButton, out bool isButton) && isButton) return true;
        }
        return false;
    }

    // Use this for "RESET" or "RESTART"
    bool CheckForResetButton()
    {
        InputDevices.GetDevices(devices);
        foreach (var device in devices)
        {
            // Secondary Button (B / Y)
            if (device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool isButton) && isButton) return true;
        }
        return false;
    }

    // --- FADING LOGIC ---

    IEnumerator FadeOutAndSwitch()
    {
        if (isFadingOut) yield break;
        isFadingOut = true;
        
        float timer = 0;
        float startVolume = (backgroundMusic != null) ? backgroundMusic.volume : 1f;
        
        while (timer < 2.0f)
        {
            timer += Time.deltaTime;
            float progress = timer / 2.0f;

            faderCanvasGroup.alpha = progress;
            if (backgroundMusic != null) backgroundMusic.volume = Mathf.Lerp(startVolume, 0, progress);

            yield return null;
        }

        faderCanvasGroup.alpha = 1;

        // If next scene is empty, we wait in the dark
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.Log("Experience Ended. Waiting for Reset Button.");
            isExperienceEnded = true; 
        }
    }

    IEnumerator FadeInRoutine()
    {
        float timer = 0;
        while (timer < 2.0f)
        {
            timer += Time.deltaTime;
            faderCanvasGroup.alpha = 1 - (timer / 2.0f);
            yield return null;
        }
        faderCanvasGroup.alpha = 0;
    }
}