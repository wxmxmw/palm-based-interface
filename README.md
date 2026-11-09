# Palm-Based Interface

A palm-based Mixed Reality (MR) experimental interface developed for Meta Quest 3. The project leverages hand tracking and pinch gestures to enable users to show, hide, and interact with a spatial UI positioned in front of their torso, without requiring physical controllers.

## Features

The application uses Meta Quest 3's Passthrough capability to display the real-world environment while presenting a spatial task panel in front of the user's torso. The entire interaction workflow is supported by hand tracking, eliminating the need for controllers.

- **Palm-Activated Menu Toggle**: Users can toggle the interface's visibility by opening their right hand, facing their palm upward, and holding the gesture for approximately `0.7` seconds. A cooldown mechanism and gesture-release detection prevent accidental repeated activation.
- **Torso-Following Interface**: The spatial panel is positioned in front of the user based on chest and spine tracking data, with smoothing and stabilization applied to its position and orientation.
- **Natural Gesture Interaction**: Users can hover over UI elements using hand rays and perform button clicks or number selections through thumb-index finger pinch gestures.
- **Participant Identification**: Before starting the experiment, users select a participant ID ranging from `P001` to `P099` to distinguish data collected from different participants.
- **Three-Stage Task Workflow**: Each trial consists of three sequential stages: navigating to a designated station, reading a number printed on paper, and selecting and submitting the corresponding number. A new trial can begin after submission.
- **Answer and Trial Management**: The system supports configurable correct answers for each trial, records participants' selections and their correctness, and automatically increments the trial number when a new trial begins.
- **Experimental Data Logging**: The application independently measures the actual completion time of each task stage. After each trial, data is immediately appended to a CSV file, including the participant ID, experimental version, trial number, selected answer, correct answer, and UTC timestamp.
- **Timed Feedback**: Brief feedback messages can be displayed after interactions to confirm successful actions.

## Development Environment

- Unity `6000.3.21f1`
- Meta XR All-in-One SDK `205.0.0`
- Universal Render Pipeline `17.3.0`
- OpenXR `1.16.1`
- Target Device: Meta Quest 3 (Android)

## Project Structure

```text
Assets/Scenes/          Main scene
Assets/Prefabs/         Palm menu prefabs
Assets/Scripts/         Gesture recognition, panel tracking, task workflow, and data logging
documentation/          Requirements and development documentation
Packages/               Unity package dependencies
ProjectSettings/        Unity project settings
```

## Notes

This project is currently intended for research and prototype validation. No public license has been provided.
