using MagicLeap.Android;
using MagicLeap.Examples;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

public class VoiceIntentsExample : MonoBehaviour
{
    [SerializeField, Tooltip("The text used to display status information for the example.")]
    private Text statusText = null;

    [SerializeField, Tooltip("The text used to display input controls for the example.")]
    private Text controlsText = null;

    [SerializeField, Tooltip("The configuration file that holds the list of intents used for this application.")]
    private MLVoiceIntentsConfiguration voiceConfiguration01;

    // The configuration that can be switched to at runtime.
    private MLVoiceIntentsConfiguration voiceConfiguration02;
    
    [SerializeField]
    private MeshRenderer cube;

    [SerializeField]
    private Material clearColor;
    [SerializeField]
    private Material redColor;
    [SerializeField]
    private Material greenColor;
    [SerializeField]
    private Material blueColor;
    [SerializeField]
    private Material purpleColor;

    [SerializeField]
    private string slotCategoryName;

    private string startupStatus = "Requesting Permission...";
    private string lastResults = "";
    private bool isProcessing = false;

    private bool useFirstConfiguration = true;


    private void Start()
    {
        // Example of adding a command programtically before setup
        MLVoiceIntentsConfiguration.CustomVoiceIntents newIntent;
        newIntent.Value = "Clear The Cube";
        newIntent.Id = 998;

        voiceConfiguration01.VoiceCommandsToAdd.Add(newIntent);

        // Create a duplicate configuration at runtime to modify.
        voiceConfiguration02 = Instantiate(voiceConfiguration01);

        MLVoiceIntentsConfiguration.CustomVoiceIntents config2Only;
        config2Only.Value = "The Cube is Purple";
        config2Only.Id = 999;

        voiceConfiguration02.VoiceCommandsToAdd.Add(config2Only);

        Permissions.RequestPermission(Permissions.VoiceInput, OnPermissionGranted, OnPermissionDenied);
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            Permissions.RequestPermission(Permissions.VoiceInput, OnPermissionGranted, OnPermissionDenied);
        }
        else
        {
            if (isProcessing)
            {
                MLResult result = MLVoice.Stop();
                if (result.IsOk)
                {
                    isProcessing = false;
                }
                else
                {
                    Debug.LogError("Failed to Stop Processing Voice Intents on Pause with result: " + result);
                }
            }
        }
    }

    private void Initialize()
    {
        if (!Permissions.CheckPermission(Permissions.VoiceInput))
        {
            return;
        }
        bool isEnabled = MLVoice.VoiceEnabled;
        startupStatus = "System Supports Voice Intents: " + isEnabled.ToString();

        if (isEnabled)
        {
            MLResult result = MLVoice.SetupVoiceIntents(useFirstConfiguration ? voiceConfiguration01 : voiceConfiguration02);

            if (result.IsOk)
            {
                startupStatus += "\nUsing Config " + (useFirstConfiguration ? "01" : "02");
                // Confirm the events only register once
                MagicLeapController.Instance.BumperPressed -= HandleOnBumper;
                MagicLeapController.Instance.MenuPressed -= HandleOnMenu;

                MagicLeapController.Instance.BumperPressed += HandleOnBumper;
                MagicLeapController.Instance.MenuPressed += HandleOnMenu;
                isProcessing = true;

                MLVoice.OnVoiceEvent += VoiceEvent;

                SetControlsText();
            }
            else
            {
                startupStatus += "\nSetup failed with result: " + result.ToString();
                Debug.LogError("Failed to Setup Voice Intents with result: " + result);
            }
        }
    }

    private void OnDestroy()
    {
        MLVoice.OnVoiceEvent -= VoiceEvent;

        MagicLeapController.Instance.BumperPressed -= HandleOnBumper;
        MagicLeapController.Instance.MenuPressed -= HandleOnMenu;
    }


    void Update()
    {
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        statusText.text = $"<color=#B7B7B8><b>Voice Intents Data</b></color>\n{startupStatus}";
        statusText.text += "\n\nIs Processing: " + isProcessing;
        statusText.text += "\n\nInstructions and List of commands in Controls Tab";
        statusText.text += lastResults;
    }

    private void SetControlsText()
    {
        MLVoiceIntentsConfiguration activeConfig = useFirstConfiguration ? voiceConfiguration01 : voiceConfiguration02;
        StringBuilder controlsScrollview = new StringBuilder();
        controlsScrollview.Append($"<color=#B7B7B8><b>Menu Button</b></color> to switch Configurations at runtime\n\n");

        controlsScrollview.Append($"<color=#B7B7B8><b>Speak the App Specific command out loud</b></color>\n");
        controlsScrollview.Append($"Use one of these listed commands: \n");

        controlsScrollview.AppendJoin('\n', activeConfig.GetValues());

        controlsScrollview.Append($"\n\n<color=#B7B7B8><b>To Use a System Intent speak \"Hey Magic Leap\"</b></color>\nDots to indicate the device is listening should appear. Then speak one of the enabled system commands: \n");

        foreach (MLVoiceIntentsConfiguration.SystemIntentFlags flag in System.Enum.GetValues(typeof(MLVoiceIntentsConfiguration.SystemIntentFlags)))
        {
            if (activeConfig.AutoAllowAllSystemIntents || activeConfig.SystemCommands.HasFlag(flag))
            {
                controlsScrollview.Append($"{flag.ToString()}\n");

            }
        }

        controlsScrollview.Append($"\n\n<color=#B7B7B8><b>Slots</b></color>\nA Slot is a placeholder for a list of possible values. The name of the slot is placed between brackets within the App Specific commands value and when uttering the phrase one of the slots values is used in its place.\n");
        controlsScrollview.Append($"Slots Values Used:");

        foreach (MLVoiceIntentsConfiguration.SlotData slot in activeConfig.SlotsForVoiceCommands)
        {
            controlsScrollview.Append($"\n{slot.name} : {string.Join(" - ", slot.values)}");
        }

        controlsScrollview.Append($"\n\n<color=#B7B7B8><b>Controller Bumper</b></color>\nBy Default this example scene starts processing Voice Intents. Tap the bumper to stop processing, then tap it again to begin processing again.");

        controlsText.text = controlsScrollview.ToString();
    }

    void VoiceEvent(in bool wasSuccessful, in MLVoice.IntentEvent voiceEvent)
    {
        StringBuilder strBuilder = new StringBuilder();
        strBuilder.Append($"\n\n<color=#B7B7B8><b>Last Voice Event:</b></color>\n");
        strBuilder.Append($"Was Successful: <i>{wasSuccessful}</i>\n");
        strBuilder.Append($"State: <i>{voiceEvent.State}</i>\n");
        strBuilder.Append($"No Intent Reason\n(Expected NoReason): \n<i>{voiceEvent.NoIntentReason}</i>\n");
        strBuilder.Append($"Event Unique Name:\n<i>{voiceEvent.EventName}</i>\n");
        strBuilder.Append($"Event Unique Id: <i>{voiceEvent.EventID}</i>\n");

        strBuilder.Append($"Slots Used:\n");
        strBuilder.AppendJoin("\n", voiceEvent.EventSlotsUsed.Select(v => $"Name: {v.SlotName} - Value: {v.SlotValue}"));

        lastResults = strBuilder.ToString();

        switch (voiceEvent.EventID)
        {
            case 998:
            {
                cube.material = clearColor;
                break;
            }
            case 999:
            {
                cube.material = purpleColor;
                break;
            }
            case 1:
            {
                cube.material = redColor;
                break;
            }
            case 2:
            {
                MLVoice.EventSlot SlotData = voiceEvent.EventSlotsUsed.FirstOrDefault(s => s.SlotName == slotCategoryName);
                if (SlotData.SlotName == slotCategoryName)
                    {
                        switch (SlotData.SlotValue)
                        {
                            case "Green":
                            {
                                cube.material = greenColor;
                                break;
                            }
                            case "Blue":
                            {
                                cube.material = blueColor;
                                break;
                            }
                        }
                    }
                break;
            }
        }
    }

    private void HandleOnBumper(InputAction.CallbackContext _)
    {
        MLResult result;
        if (isProcessing)
        {
            result = MLVoice.Stop();
            if (result.IsOk)
            {
                isProcessing = false;
            }
            else
            {
                Debug.LogError("Failed to Stop Processing Voice Intents with result: " + result);
            }
        }
        else
        {
            result = MLVoice.SetupVoiceIntents(useFirstConfiguration ? voiceConfiguration01 : voiceConfiguration02);
            if (result.IsOk)
            {
                isProcessing = true;
            }
            else
            {
                Debug.LogError("Failed to Re-Setup Voice Intents with result: " + result);
            }
        }
    }

    private void HandleOnMenu(InputAction.CallbackContext _)
    {
        useFirstConfiguration = !useFirstConfiguration;
        Initialize();
    }

    private void OnPermissionDenied(string permission)
    {
        startupStatus = "<color=#ff0000><b>Permission Denied!</b></color>";
    }

    private void OnPermissionGranted(string permission)
    {
        startupStatus = "<color=#00ff00><b>Permission Granted!</b></color>";
        Initialize();
    }
}
