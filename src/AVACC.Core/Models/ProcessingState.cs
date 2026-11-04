namespace AVACC.Core.Models;

public enum ProcessingState
{
    Idle,
    ButtonPressed,
    Recording,
    ButtonReleased,
    ProcessingAudio,
    Transcribing,
    InterpretingCommand,
    Completed,
    Error
}

