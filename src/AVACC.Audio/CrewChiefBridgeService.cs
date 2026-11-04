using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using AVACC.Core.Interfaces;
using AVACC.Core.Models;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AVACC.Audio;

public class CrewChiefBridgeService : ICrewChiefBridgeService
{
    private readonly ISettingsService _settingsService;
    private readonly ICommandRegistryService _registryService;
    private readonly IEventLogService _eventLogService;
    private readonly MicrophoneManager _microphoneManager;

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private static readonly Dictionary<string, string> CanonicalPhrases = new(StringComparer.OrdinalIgnoreCase)
    {
        // Tyres
        ["HOWS_MY_TYRE_WEAR"] = "how's my tire wear",
        ["WHAT_ARE_MY_TYRE_PRESSURES"] = "what are my tire pressures",
        ["HOW_ARE_MY_TYRE_PRESSURES"] = "how are my pressures",
        ["HOW_ARE_MY_TYRE_TEMPS"] = "how are my tire temps",
        ["WHAT_ARE_MY_TYRE_TEMPS"] = "what are my tire temps",
        ["WHAT_TYRES_AM_I_ON"] = "what tires am I on",
        ["HOW_LONG_WILL_THESE_TYRES_LAST"] = "how long will the tires last",
        ["WHAT_ARE_THE_RELATIVE_TYRE_PERFORMANCES"] = "compare tire compounds",
        ["HOW_MANY_LAPS_ON_TYRE_SET"] = "how many laps on tire set",
        ["HOW_ARE_MY_FRONT_TYRE_PRESSURES"] = "how are my front pressures",
        ["HOW_ARE_MY_REAR_TYRE_PRESSURES"] = "how are my rear pressures",
        ["HOWS_MY_FRONT_CAMBER"] = "how's my front camber",
        ["HOWS_MY_REAR_CAMBER"] = "how's my rear camber",

        // Brakes & Powertrain
        ["HOWS_MY_BRAKES"] = "how are my brakes",
        ["HOW_ARE_MY_BRAKE_TEMPS"] = "how are my brake temps",
        ["WHAT_ARE_MY_BRAKE_TEMPS"] = "what are my brake temps",
        ["HOWS_MY_ENGINE"] = "how's my engine",
        ["HOW_ARE_MY_ENGINE_TEMPS"] = "how are my engine temps",
        ["WHAT_ARE_MY_ENGINE_TEMPS"] = "what are my engine temps",
        ["WHAT_IS_MY_WATER_TEMP"] = "what's my water temp",
        ["WHAT_IS_MY_OIL_TEMP"] = "what's my oil temp",
        ["HOWS_MY_TRANSMISSION"] = "how's my transmission",
        ["HOWS_MY_BATTERY"] = "how's my battery",
        ["HOWS_MY_SUSPENSION"] = "how's my suspension",

        // Damage & Status
        ["DAMAGE_REPORT"] = "damage report",
        ["HOWS_MY_AERO"] = "how's my aero",
        ["CAR_STATUS"] = "car status",
        ["SESSION_STATUS"] = "session status",
        ["STATUS"] = "full status",

        // Fuel
        ["WHATS_MY_FUEL_LEVEL"] = "what's my fuel level",
        ["HOWS_MY_FUEL"] = "how's my fuel",
        ["WHATS_MY_FUEL_USAGE"] = "what's my fuel usage",
        ["HOW_MUCH_FUEL_TO_END_OF_RACE"] = "how much fuel to the end",
        ["CALCULATE_FUEL_FOR"] = "how much fuel for next stint",

        // Gaps & Timing
        ["WHATS_MY_GAP_IN_FRONT"] = "what's the gap ahead",
        ["WHATS_MY_GAP_BEHIND"] = "what's the gap behind",
        ["WHAT_WAS_MY_LAST_LAP_TIME"] = "what's my last lap time",
        ["WHATS_MY_BEST_LAP_TIME"] = "what's my best lap time",
        ["WHATS_THE_FASTEST_LAP_TIME"] = "what's the fastest lap",
        ["WHAT_ARE_MY_SECTOR_TIMES"] = "what are my sector times",
        ["WHATS_MY_LAST_SECTOR_TIME"] = "what's my last sector time",
        ["HOWS_MY_PACE"] = "how's my pace",
        ["HOWS_MY_SELF_PACE"] = "how's my personal pace",
        ["WHERE_AM_I_FASTER"] = "where am I faster",
        ["WHERE_AM_I_SLOWER"] = "where am I slower",

        // Session & Standings
        ["WHATS_MY_POSITION"] = "what's my position",
        ["WHATS_MY_EXPECTED_FINISH_POSITION"] = "what's my expected position",
        ["HOW_LONGS_LEFT"] = "how long's left",
        ["WHOS_LEADING"] = "who's leading",
        ["WHOS_IN_FRONT_IN_THE_RACE"] = "who's in front in the race",
        ["WHOS_BEHIND_IN_THE_RACE"] = "who's behind in the race",
        ["WHOS_IN_FRONT_ON_TRACK"] = "who's in front on track",
        ["WHOS_BEHIND_ON_TRACK"] = "who's behind on track",
        ["IS_CAR_AHEAD_MY_CLASS"] = "is the car ahead in my class",
        ["IS_CAR_BEHIND_MY_CLASS"] = "is the car behind in my class",
        ["WHAT_CLASS_IS_CAR_AHEAD"] = "what class is the car ahead",
        ["WHAT_CLASS_IS_CAR_BEHIND"] = "what class is the car behind",
        ["WHATS_THE_AIR_TEMP"] = "what's the air temp",
        ["WHATS_THE_TRACK_TEMP"] = "what's the track temp",
        ["WHATS_THE_TIME"] = "what's the time",
        ["WHATS_MY_IRATING"] = "what's my irating",
        ["WHATS_MY_LICENSE_CLASS"] = "what's my license class",
        ["WHATS_MY_RANK"] = "what's my rank",
        ["WHATS_MY_RATING"] = "what's my rating",
        ["WHATS_MY_REPUTATION"] = "what's my reputation",
        ["HOW_MANY_INCIDENT_POINTS"] = "how many incident points do I have",
        ["WHATS_THE_INCIDENT_LIMIT"] = "what's the incident limit",
        ["WHATS_THE_SOF"] = "what's the sof",
        ["STOP_WATCHING_ALL"] = "stop monitoring all",

        // Pitstop Actions
        ["DO_I_HAVE_A_MANDATORY_PIT_STOP"] = "do I have a mandatory pit stop",
        ["IS_MY_PIT_BOX_OCCUPIED"] = "is my pit box occupied",
        ["WHATS_PITLANE_SPEED_LIMIT"] = "what's the pitlane speed limit",
        ["PLAY_POST_PIT_POSITION_ESTIMATE"] = "where will I be after a stop",
        ["PRACTICE_PIT_STOP"] = "time this stop",
        ["WHAT_ARE_THE_PIT_ACTIONS"] = "what are the pit actions",
        ["PIT_STOP_ADD"] = "pitstop add",
        ["PIT_STOP_FUEL_TO_THE_END"] = "pitstop fuel to the end",
        ["PIT_STOP_REFUEL"] = "pitstop refuel",
        ["PIT_STOP_DONT_REFUEL"] = "pitstop don't refuel",
        ["PIT_STOP_CLEAR_FUEL"] = "pitstop clear fuel",
        ["PIT_STOP_CLEAR_ALL"] = "pitstop clear all",
        ["PIT_STOP_TEAROFF"] = "pitstop tearoff",
        ["PIT_STOP_CLEAR_WIND_SCREEN"] = "pitstop clear tearoff",
        ["PIT_STOP_FAST_REPAIR"] = "pitstop fast repair",
        ["PIT_STOP_CLEAR_FAST_REPAIR"] = "pitstop clear fast repair",
        ["PIT_STOP_CHANGE_ALL_TYRES"] = "pitstop change all tires",
        ["PIT_STOP_CHANGE_TYRES"] = "pitstop change tires",
        ["PIT_STOP_CLEAR_TYRES"] = "pitstop don't change tires",
        ["PIT_STOP_CHANGE_FRONT_LEFT_TYRE"] = "pitstop change left front tire",
        ["PIT_STOP_CHANGE_FRONT_RIGHT_TYRE"] = "pitstop change right front tire",
        ["PIT_STOP_CHANGE_REAR_LEFT_TYRE"] = "pitstop change left rear tire",
        ["PIT_STOP_CHANGE_REAR_RIGHT_TYRE"] = "pitstop change right rear tire",
        ["PIT_STOP_CHANGE_LEFT_SIDE_TYRES"] = "pitstop change left side tires",
        ["PIT_STOP_CHANGE_RIGHT_SIDE_TYRES"] = "pitstop change right side tires",
        ["PIT_STOP_CHANGE_FRONT_TYRES"] = "pitstop change front tires only",
        ["PIT_STOP_CHANGE_REAR_TYRES"] = "pitstop change rear tires only",
        ["PIT_STOP_CHANGE_TYRE_PRESSURE"] = "pitstop change tire pressures",
        ["PIT_STOP_CHANGE_FRONT_PRESSURES"] = "pitstop change front pressures",
        ["PIT_STOP_CHANGE_REAR_PRESSURES"] = "pitstop change rear pressures",
        ["PIT_STOP_SELECT_TYRE_SET"] = "pitstop tire set",
        ["PIT_STOP_SELECT_LEAST_USED_TYRE_SET"] = "pitstop select best tires",
        ["PIT_STOP_HARD_TYRES"] = "pitstop hard tires",
        ["PIT_STOP_MEDIUM_TYRES"] = "pitstop medium tires",
        ["PIT_STOP_SOFT_TYRES"] = "pitstop soft tires",
        ["PIT_STOP_SUPERSOFT_TYRES"] = "pitstop super soft tires",
        ["PIT_STOP_ULTRASOFT_TYRES"] = "pitstop ultra soft tires",
        ["PIT_STOP_HYPERSOFT_TYRES"] = "pitstop hyper soft tires",
        ["PIT_STOP_INTERMEDIATE_TYRES"] = "pitstop intermediate tires",
        ["PIT_STOP_WET_TYRES"] = "pitstop wet tires",
        ["PIT_STOP_DRY_TYRES"] = "pitstop dry tires",
        ["PIT_STOP_FIX_ALL"] = "pitstop fix all",
        ["PIT_STOP_FIX_BODY"] = "pitstop fix body",
        ["PIT_STOP_FIX_NONE"] = "pitstop fix none",
        ["PIT_STOP_FIX_ALL_AERO"] = "pitstop fix all aero",
        ["PIT_STOP_FIX_FRONT_AERO"] = "pitstop fix front aero only",
        ["PIT_STOP_FIX_REAR_AERO"] = "pitstop fix rear aero only",
        ["PIT_STOP_FIX_NO_AERO"] = "pitstop don't fix aero",
        ["PIT_STOP_FIX_SUSPENSION"] = "pitstop fix suspension",
        ["PIT_STOP_DONT_FIX_SUSPENSION"] = "pitstop don't fix suspension",
        ["PIT_STOP_SERVE_PENALTY"] = "pitstop serve penalty",
        ["PIT_STOP_DONT_SERVE_PENALTY"] = "pitstop don't serve penalty",

        // Penalties & Rules
        ["DO_I_HAVE_A_PENALTY"] = "do I have a penalty",
        ["DO_I_STILL_HAVE_A_PENALTY"] = "do I still have a penalty",
        ["HAVE_I_SERVED_MY_PENALTY"] = "have I served my penalty",
        ["ENABLE_CUT_TRACK_WARNINGS"] = "enable cut track warnings",
        ["DISABLE_CUT_TRACK_WARNINGS"] = "no cut track warnings",
        ["ENABLE_YELLOW_FLAG_MESSAGES"] = "give me yellows",
        ["DISABLE_YELLOW_FLAG_MESSAGES"] = "no more yellows",
        ["ENABLE_MANUAL_FORMATION_LAP"] = "this is the formation lap",
        ["DISABLE_MANUAL_FORMATION_LAP"] = "standing start",

        // Radio & Comms
        ["RADIO_CHECK"] = "radio check",
        ["REPEAT_LAST_MESSAGE"] = "repeat last message",
        ["MORE_INFO"] = "tell me more",
        ["I_AM_OK"] = "I'm OK",
        ["KEEP_QUIET"] = "keep quiet",
        ["KEEP_ME_INFORMED"] = "keep me informed",
        ["TELL_ME_THE_GAPS"] = "tell me the gaps",
        ["DONT_TELL_ME_THE_GAPS"] = "don't tell me the gaps",
        ["DONT_TALK_IN_THE_CORNERS"] = "don't talk in the corners",
        ["TALK_TO_ME_ANYWHERE"] = "talk to me anywhere",
        ["SPOT"] = "start spotting",
        ["DONT_SPOT"] = "don't spot",
        ["STOP_COMPLAINING"] = "stop complaining",
        ["PLAY_CORNER_NAMES"] = "read corner names",

        // Display
        ["DISPLAY_SECTORS"] = "display sectors",
        ["DISPLAY_PIT_MENU"] = "display pit menu",
        ["DISPLAY_TYRES"] = "display tires",
        ["DISPLAY_TEMPS"] = "display temps",
        ["DISPLAY_RACE_INFO"] = "display race info",
        ["DISPLAY_STANDINGS"] = "display standings",
        ["DISPLAY_PENALTIES"] = "display penalties",
        ["DISPLAY_NEXT"] = "display next",
        ["SHOW_OVERLAY"] = "show overlay",
        ["HIDE_OVERLAY"] = "hide overlay",
        ["SHOW_All_OVERLAYS"] = "show all overlays",
        ["SHOW_CONSOLE"] = "show console",
        ["HIDE_CONSOLE"] = "hide console",
        ["SHOW_CHART"] = "show chart",
        ["HIDE_CHART"] = "hide chart",
        ["CLEAR_CHART"] = "clear chart",
        ["SHOW_SUBTITLES"] = "show subtitles",
        ["HIDE_SUBTITLES"] = "hide subtitles",
        ["TOGGLE_VR_OVERLAYS"] = "toggle VR overlays"
    };

    public CrewChiefBridgeService(
        ISettingsService settingsService,
        ICommandRegistryService registryService,
        IEventLogService eventLogService,
        MicrophoneManager microphoneManager)
    {
        _settingsService = settingsService;
        _registryService = registryService;
        _eventLogService = eventLogService;
        _microphoneManager = microphoneManager;
    }

    public string GetCanonicalPhrase(string commandId)
    {
        if (string.IsNullOrWhiteSpace(commandId)) return string.Empty;

        var cleanId = commandId.Trim().ToUpperInvariant();
        if (CanonicalPhrases.TryGetValue(cleanId, out var phrase))
        {
            return phrase;
        }

        // Try to find the first example in registered command definitions
        var def = _registryService.Commands.FirstOrDefault(c => string.Equals(c.CrewChiefCommandId, cleanId, StringComparison.OrdinalIgnoreCase));
        if (def != null && def.Examples.Count > 0 && !string.IsNullOrWhiteSpace(def.Examples[0]))
        {
            return def.Examples[0];
        }

        // Fallback: convert SCREAMING_SNAKE_CASE to readable lower case words
        return cleanId.Replace('_', ' ').ToLowerInvariant();
    }

    public IReadOnlyList<AudioDeviceInfo> GetPlaybackDevices()
    {
        return _microphoneManager.GetPlaybackDevices();
    }

    public async Task<bool> SendCommandAsync(string commandId, CancellationToken cancellationToken = default)
    {
        var phrase = GetCanonicalPhrase(commandId);
        if (string.IsNullOrWhiteSpace(phrase))
        {
            _eventLogService.LogWarning($"Cannot bridge command '{commandId}': no verbal trigger phrase available.");
            return false;
        }

        _eventLogService.LogInfo($"Bridging command to Crew Chief: \"{phrase}\" [{commandId}]");
        return await SpeakPhraseAsync(phrase, cancellationToken);
    }

    public async Task<bool> SpeakPhraseAsync(string phrase, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phrase)) return false;

        return await Task.Run(async () =>
        {
            byte? pttVk = null;
            var settings = _settingsService.CurrentSettings;

            if (!string.IsNullOrWhiteSpace(settings.CrewChiefPttKey) && 
                !settings.CrewChiefPttKey.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                pttVk = ParseVk(settings.CrewChiefPttKey);
            }

            try
            {
                using var synth = new SpeechSynthesizer();
                synth.Rate = Math.Clamp(settings.TtsSpeechRate, -10, 10);

                using var ms = new MemoryStream();
                synth.SetOutputToWaveStream(ms);
                synth.Speak(phrase);
                ms.Position = 0;

                using var enumerator = new MMDeviceEnumerator();
                MMDevice? targetDevice = null;
                var targetDeviceId = settings.CrewChiefAudioOutputDeviceId;

                if (!string.IsNullOrEmpty(targetDeviceId) && targetDeviceId != "default")
                {
                    try
                    {
                        targetDevice = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                            .FirstOrDefault(d => string.Equals(d.ID, targetDeviceId, StringComparison.OrdinalIgnoreCase));
                    }
                    catch { }
                }

                if (targetDevice == null)
                {
                    targetDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                }

                // Press Crew Chief PTT key if configured
                if (pttVk.HasValue)
                {
                    keybd_event(pttVk.Value, 0, 0, 0);
                    await Task.Delay(100, cancellationToken);
                }

                using var waveReader = new WaveFileReader(ms);
                using var wasapiOut = new WasapiOut(targetDevice, AudioClientShareMode.Shared, false, 50);
                wasapiOut.Init(waveReader);
                wasapiOut.Play();

                while (wasapiOut.PlaybackState == PlaybackState.Playing && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(30, cancellationToken);
                }

                // Release Crew Chief PTT key
                if (pttVk.HasValue)
                {
                    await Task.Delay(100, cancellationToken);
                    keybd_event(pttVk.Value, 0, KEYEVENTF_KEYUP, 0);
                }

                return true;
            }
            catch (Exception ex)
            {
                if (pttVk.HasValue)
                {
                    keybd_event(pttVk.Value, 0, KEYEVENTF_KEYUP, 0);
                }
                _eventLogService.LogError($"Crew Chief audio bridge error: {ex.Message}");
                return false;
            }
        }, cancellationToken);
    }

    private static byte? ParseVk(string keyName)
    {
        return keyName.Trim().ToUpperInvariant() switch
        {
            "INSERT" => 0x2D,
            "DELETE" => 0x2E,
            "HOME" => 0x24,
            "END" => 0x23,
            "PAGEUP" => 0x21,
            "PAGEDOWN" => 0x22,
            "F1" => 0x70,
            "F2" => 0x71,
            "F3" => 0x72,
            "F4" => 0x73,
            "F5" => 0x74,
            "F6" => 0x75,
            "F7" => 0x76,
            "F8" => 0x77,
            "F9" => 0x78,
            "F10" => 0x79,
            "F11" => 0x7A,
            "F12" => 0x7B,
            "F13" => 0x7C,
            "F14" => 0x7D,
            "F15" => 0x7E,
            "F16" => 0x7F,
            "SCROLLLOCK" => 0x91,
            "PAUSE" => 0x13,
            _ => null
        };
    }
}
