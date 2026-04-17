using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class SettingsUILogic : MonoBehaviour
{
    public AudioMixer audioMixer;
    private Toggle hdrTog, vSyncTog, motionBlurTog, dofTog, muteTog;
    private DropdownField resDropdown;

    private Slider masterVolSlide, musicVolSlide, sfxVolSlide, sensSlide;

    private Button backBtn, applyBtn, cancelBtn;

    private readonly List<(int w, int h, string label)> resolutions = new()
    {
        (1280, 720,  "720p (1280x720)"),
        (1366, 768,  "1366x768"),
        (1920, 1080, "1080p (1920x1080)"),
        (2560, 1440, "1440p (2560x1440)"),
        (3840, 2160, "4K (3840x2160)"),
        (1280, 800,  "720p 16:10 (1280x800)"),
        (1440, 900,  "1440x900 16:10"),
        (1920, 1200, "1080p 16:10 (1920x1200)"),
        (2560, 1600, "1440p 16:10 (2560x1600)"),
        (3840, 2400, "4K 16:10 (3840x2400)")
    };

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        hdrTog = root.Q<Toggle>("HDRToggle");
        vSyncTog = root.Q<Toggle>("VsyncToggle");
        motionBlurTog = root.Q<Toggle>("MotionBlurToggle");
        dofTog = root.Q<Toggle>("DOFToggle");
        resDropdown = root.Q<DropdownField>("Resolution");

        muteTog = root.Q<Toggle>("MuteToggle");
        masterVolSlide = root.Q<Slider>("MasterSlider");
        musicVolSlide = root.Q<Slider>("MusicSlider");
        sfxVolSlide = root.Q<Slider>("SFXSlider");

        sensSlide = root.Q<Slider>("SensSlider");

        backBtn = root.Q<Button>("BackBtn");
        applyBtn = root.Q<Button>("ApplyBtn");
        cancelBtn = root.Q<Button>("CancelBtn");

        PopulateRes();
        LoadSettings();
        WireEvents();

        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;


    }

    private void WireEvents()
    {
        applyBtn.clicked += ApplySettings;
        cancelBtn.clicked += LoadSettings;
        backBtn.clicked += OnBack;

        masterVolSlide.RegisterValueChangedCallback(evt => SetMixerVolume("MasterVol", evt.newValue));
        musicVolSlide.RegisterValueChangedCallback(evt => SetMixerVolume("MusicVol", evt.newValue));
        sfxVolSlide.RegisterValueChangedCallback(evt => SetMixerVolume("SFXVol", evt.newValue));
        muteTog.RegisterValueChangedCallback(evt => AudioListener.pause = evt.newValue);
    }

    private void ApplySettings()
    {
        int idx = resDropdown.index;
        if (idx >= 0 && idx < resolutions.Count)
        {
            var r = resolutions[idx];
            Screen.SetResolution(r.w, r.h, Screen.fullScreenMode);
            PlayerPrefs.SetInt("ResolutionIndex", idx);
        }

        QualitySettings.vSyncCount = vSyncTog.value ? 1 : 0;
        PlayerPrefs.SetInt("Vsync", vSyncTog.value ? 1 : 0);

        PlayerPrefs.SetInt("HDR", hdrTog.value ? 1 : 0);
        PlayerPrefs.SetInt("MotionBlur", motionBlurTog.value ? 1 : 0);
        PlayerPrefs.SetInt("DOF", dofTog.value ? 1 : 0);

        AudioListener.pause = muteTog.value;

        SetMixerVolume("MasterVol", masterVolSlide.value);
        SetMixerVolume("MusicVol", musicVolSlide.value);
        SetMixerVolume("SFXVol", sfxVolSlide.value);

        PlayerPrefs.SetFloat("MasterVolume", masterVolSlide.value);
        PlayerPrefs.SetFloat("MusicVolume", musicVolSlide.value);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolSlide.value);
        PlayerPrefs.SetInt("Mute", muteTog.value ? 1 : 0);

        PlayerPrefs.SetFloat("MouseSensitivity", sensSlide.value);

        PlayerPrefs.Save();
        Debug.Log("Saved Player Settings");
    }

    private void SetMixerVolume(string paramName, float sliderVal)
    {
        float db = sliderVal <= 0.01f ? -80f : Mathf.Log10(sliderVal / 100f) * 20f;
        audioMixer.SetFloat(paramName, db);
    }

    private void PopulateRes()
    {
        var choices = new List<string>();
        foreach (var r in resolutions) choices.Add(r.label);
        resDropdown.choices = choices;

    }

    void LoadSettings()
    {
        hdrTog.value = PlayerPrefs.GetInt("HDR", 0) == 1;
        vSyncTog.value = PlayerPrefs.GetInt("Vsync", 1) == 1;
        motionBlurTog.value = PlayerPrefs.GetInt("MotionBlur", 0) == 1;
        dofTog.value = PlayerPrefs.GetInt("DOF", 0) == 1;
        muteTog.value = PlayerPrefs.GetInt("Mute", 0) == 1;

        masterVolSlide.value = PlayerPrefs.GetFloat("MasterVolume", 80f);
        musicVolSlide.value = PlayerPrefs.GetFloat("MusicVolume", 80f);
        sfxVolSlide.value = PlayerPrefs.GetFloat("SFXVolume", 80f);
        sensSlide.value = PlayerPrefs.GetFloat("MouseSensitivity", 42f);

        SetMixerVolume("MasterVol", masterVolSlide.value);
        SetMixerVolume("MusicVol", musicVolSlide.value);
        SetMixerVolume("SFXVol", sfxVolSlide.value);

        int resIdx = PlayerPrefs.GetInt("ResolutionIndex", 2);
        if (resIdx >= 0 && resIdx < resolutions.Count) resDropdown.index = resIdx;
    }

    void OnBack()
    {
        SceneManager.LoadScene("MainMenu");
    }
}

