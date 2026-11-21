using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEngine;

// ApproachVideoRig の動画を Unity Recorder（パッケージ com.unity.recorder）で MP4 に書き出す
// エディタで再生したときだけ働く（ビルドには入らない）
[InitializeOnLoad]
public static class ApproachVideoRecorder
{
    static RecorderController controller;

    static ApproachVideoRecorder()
    {
        ApproachVideoRig.RecordingStarted = Start;
        ApproachVideoRig.RecordingStopped = Stop;
        ApproachVideoRig.AllFinished = Finished;
    }

    static void Start(ApproachVideoRig rig, ApproachVideoRig.Clip clip, string fileWithoutExtension)
    {
        Stop(rig);

        var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movie.name = clip.id;
        movie.Enabled = true;
        movie.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
        movie.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = rig.OutputWidth,
            OutputHeight = rig.OutputHeight,
        };
        movie.AudioInputSettings.PreserveAudio = false;
        movie.OutputFile = fileWithoutExtension.Replace('\\', '/');

        settings.AddRecorderSettings(movie);
        settings.SetRecordModeToManual();
        settings.FrameRate = rig.frameRate;
        settings.FrameRatePlayback = FrameRatePlayback.Constant;
        settings.CapFrameRate = true;

        controller = new RecorderController(settings);
        controller.PrepareRecording();
        controller.StartRecording();
        Debug.Log("[ApproachVideo] 録画開始：" + clip.id + " " + clip.label + " → " + fileWithoutExtension + ".mp4");
    }

    static void Stop(ApproachVideoRig rig)
    {
        if (controller == null) return;
        if (controller.IsRecording()) controller.StopRecording();
        controller = null;
    }

    static void Finished(ApproachVideoRig rig)
    {
        Stop(rig);
        if (rig.stopPlayingWhenDone) EditorApplication.isPlaying = false;
    }
}
